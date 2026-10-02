using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.V2.Tests;

/// <summary>通过标准创建工厂验证构造失败回收与可观察的释放故障。</summary>
public sealed class FeatureFactoryTests
{
    private static readonly TimeSpan Watchdog = TimeSpan.FromSeconds(15);

    /// <summary>失败嵌套构造接管尚未退出的外层构造时，清理不能同步等待或提前释放。</summary>
    /// <returns>嵌套所有权构造依赖回收验证任务。</returns>
    [Test]
    public async Task NestedFailedConstructionOwningOuterConstructionDefersCleanupUntilOuterExit()
    {
        NestedOwningOptions options = new();
        ServiceCollection services = new();
        services.AddSingleton(options);
        services.AddScoped(_ =>
        {
            LifetimeResource resource = new();
            options.Resources.Add(resource);
            return resource;
        });
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await Assert.That(async () => await FeatureFactory.CreateAsync<NestedOwnedOuter>(provider)).Throws<InvalidOperationException>();
        await Assert.That(options.Failure).IsNotNull();
        await Assert.That(options.ResourcesWereAlive).IsTrue();
        ReleaseResult released = await options.Failure!.Cleanup.Released.WaitAsync(Watchdog);
        await Assert.That(released.Succeeded).IsTrue();
        await Assert.That(options.Resources.Count).IsEqualTo(2);
        await Assert.That(options.Resources.All(resource => resource.DisposeCount == 1)).IsTrue();
    }

    /// <summary>同步构造不能等待自身或尚在构造的祖先释放，后台继承上下文不误判为构造线程。</summary>
    /// <param name="nested">是否从嵌套构造访问祖先票据。</param>
    /// <returns>准确构造自等待保护验证任务。</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task ConstructorReleaseSelfWaitIsRejectedWithoutMisclassifyingBackgroundWork(bool nested)
    {
        ConstructorWaitOptions options = new(nested);
        LifetimeResource? resource = null;
        ServiceCollection services = new();
        services.AddSingleton(options);
        services.AddScoped(_ => resource = new LifetimeResource());
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await Assert.That(async () => await FeatureFactory.CreateAsync<ConstructorWaitFeature>(provider)).Throws<InvalidOperationException>();
        await Assert.That(options.Rejected).IsTrue();
        await Assert.That(options.ScopeWasAlive).IsTrue();
        await Assert.That(options.BackgroundWasAllowed).IsTrue();
        await Assert.That(resource!.DisposeCount).IsEqualTo(1);
    }

    /// <summary>构造失败目标接管正在等待工厂的调用者时，不得提前释放仍被目标 IO 使用的范围。</summary>
    /// <param name="releaseFault">延后的真实范围释放是否发生异常。</param>
    /// <returns>构造失败所有权等待依赖验证任务。</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task FailedCreationThatOwnsItsCallerKeepsScopeUntilAllRealUsersExit(bool releaseFault)
    {
        OwningFactoryOptions options = new();
        LifetimeResource? resource = null;
        ServiceCollection services = new();
        services.AddSingleton(options);
        InvalidOperationException releaseFailure = new("dependent cleanup disposal failure");
        services.AddScoped(_ => resource = new LifetimeResource(releaseFault ? () => ValueTask.FromException(releaseFailure) : null));
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        RequestDetailsFeature caller = new(async (_, _) =>
        {
            try { await FeatureFactory.CreateAsync<OwningFactoryFeature>(provider); }
            catch (Exception failure) { options.Observed = failure; }
            options.Failed.SetResult();
            return "caller exited";
        });
        options.Caller = caller;
        Task<OperationResult<string>> callerExecution = caller.LoadAsync(new(1));
        try
        {
            await options.Failed.Task.WaitAsync(Watchdog);
            await Assert.That(resource!.DisposeCount).IsEqualTo(0);
            await Assert.That(options.Target!.IsClosed && caller.IsClosed).IsTrue();
            await Assert.That(options.Target.Close().Ticket.Released.IsCompleted).IsFalse();
            await Assert.That(options.Observed).IsTypeOf<FeatureCreationException>();
            FeatureCreationException creationFailure = (FeatureCreationException)options.Observed!;
            await Assert.That(creationFailure.InnerException!.Message).IsEqualTo("owned caller construction failure");
            await Assert.That(ReferenceEquals(creationFailure.Cleanup, options.Target.Close().Ticket)).IsTrue();
            options.Release.SetResult();
            await callerExecution.WaitAsync(Watchdog);
            ReleaseResult cleanup = await creationFailure.Cleanup.Released.WaitAsync(Watchdog);
            await Assert.That(cleanup.Succeeded).IsEqualTo(!releaseFault);
            if (releaseFault) await Assert.That(cleanup.Exception).IsEqualTo(releaseFailure);
            await Assert.That(resource.DisposeCount).IsEqualTo(1);
            await Assert.That(options.Observed).IsNotNull();
        }
        finally
        {
            options.Release.TrySetResult();
            await callerExecution.WaitAsync(Watchdog);
            if (options.Target is not null) await options.Target.Close().Ticket.Released.WaitAsync(Watchdog);
        }
    }

    /// <summary>构造选择、可选值与 keyed 服务保持标准 ActivatorUtilities 的合法调用行为。</summary>
    /// <param name="keyed">是否提供 keyed 依赖以选择较长构造函数。</param>
    /// <returns>表示公开构造规则兼容验证完成的任务。</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task ConstructorDefaultsAndKeyedSelectionMatchStandardActivation(bool keyed)
    {
        ServiceCollection services = new();
        if (keyed) services.AddKeyedScoped<LifetimeResource>("selected", static (_, _) => new());
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using AsyncServiceScope referenceScope = provider.CreateAsyncScope();
        ConstructorSelectionFeature reference = ActivatorUtilities.CreateInstance<ConstructorSelectionFeature>(referenceScope.ServiceProvider);
        ConstructorSelectionFeature actual = await FeatureFactory.CreateAsync<ConstructorSelectionFeature>(provider);
        try
        {
            await Assert.That(actual.Choice).IsEqualTo(reference.Choice);
            await Assert.That(actual.Count).IsEqualTo(reference.Count);
            await Assert.That(actual.Mode).IsEqualTo(reference.Mode);
            await Assert.That(actual.Initialized).IsEqualTo("field initialized");
            await Assert.That(actual.Choice).IsEqualTo(keyed ? "keyed" : "empty");
            await Assert.That(actual.Count).IsEqualTo(keyed ? 42 : 0);
            await Assert.That((await actual.Close().Ticket.Released.WaitAsync(Watchdog)).Succeeded).IsTrue();
            if (keyed) await Assert.That(actual.Resource!.DisposeCount).IsEqualTo(1);
        }
        finally
        {
            await actual.Close().Ticket.Released.WaitAsync(Watchdog);
            await reference.Close().Ticket.Released.WaitAsync(Watchdog);
        }
    }

    /// <summary>标记的构造函数优先于较长候选，this 构造链仍在同一真实目标上完整运行。</summary>
    /// <returns>表示标记构造与构造链验证完成的任务。</returns>
    [Test]
    public async Task PreferredConstructorAndThisChainMatchStandardActivation()
    {
        await using ServiceProvider provider = LifetimeServices.Create(new());
        await using AsyncServiceScope referenceScope = provider.CreateAsyncScope();
        PreferredConstructorFeature reference = ActivatorUtilities.CreateInstance<PreferredConstructorFeature>(referenceScope.ServiceProvider);
        PreferredConstructorFeature actual = await FeatureFactory.CreateAsync<PreferredConstructorFeature>(provider);
        try
        {
            await Assert.That(actual.Choice).IsEqualTo(reference.Choice);
            await Assert.That(actual.Choice).IsEqualTo("preferred chain");
            await Assert.That((await actual.Close().Ticket.Released.WaitAsync(Watchdog)).Succeeded).IsTrue();
            await Assert.That(actual.Resource.DisposeCount).IsEqualTo(1);
        }
        finally
        {
            await actual.Close().Ticket.Released.WaitAsync(Watchdog);
            await reference.Close().Ticket.Released.WaitAsync(Watchdog);
        }
    }

    /// <summary>依赖可查询时选择最长候选，包含 provider 注入和可选参数。</summary>
    /// <returns>表示最长构造选择验证完成的任务。</returns>
    [Test]
    public async Task LongestResolvableConstructorMatchesStandardActivation()
    {
        await using ServiceProvider provider = LifetimeServices.Create(new());
        await using AsyncServiceScope referenceScope = provider.CreateAsyncScope();
        GreedyConstructorFeature reference = ActivatorUtilities.CreateInstance<GreedyConstructorFeature>(referenceScope.ServiceProvider);
        GreedyConstructorFeature actual = await FeatureFactory.CreateAsync<GreedyConstructorFeature>(provider);
        try
        {
            await Assert.That(actual.Choice).IsEqualTo(reference.Choice);
            await Assert.That(actual.Choice).IsEqualTo("longest 31");
            await Assert.That((await actual.Close().Ticket.Released.WaitAsync(Watchdog)).Succeeded).IsTrue();
        }
        finally
        {
            await actual.Close().Ticket.Released.WaitAsync(Watchdog);
            await reference.Close().Ticket.Released.WaitAsync(Watchdog);
        }
    }

    /// <summary>歧义与缺失依赖保持明确拒绝，未进入 Feature 基类的构造失败也安全回收范围。</summary>
    /// <returns>表示构造拒绝和基类初始化前失败验证完成的任务。</returns>
    [Test]
    public async Task InvalidSelectionAndFailureBeforeBaseInitializationAreCleanedUp()
    {
        ConcurrentQueue<LifetimeResource> resources = new();
        await using ServiceProvider provider = LifetimeServices.Create(new(), _ =>
        {
            LifetimeResource resource = new();
            resources.Enqueue(resource);
            return resource;
        });
        await Assert.That(async () => await AmbiguousConstructorFeature.CreateAsync(provider)).Throws<InvalidOperationException>();
        await Assert.That(async () => await MissingConstructorFeature.CreateAsync(provider)).Throws<InvalidOperationException>();
        await Assert.That(async () => await FeatureFactory.CreateAsync<MultiplePreferredFeature>(provider)).Throws<InvalidOperationException>();
        await Assert.That(resources.Count).IsEqualTo(0);
        await Assert.That(async () => await BeforeBaseFailureFeature.CreateAsync(provider)).Throws<InvalidOperationException>();
        await Assert.That(resources.Count).IsEqualTo(1);
        await Assert.That(resources.Single().DisposeCount).IsEqualTo(1);
    }

    /// <summary>工厂只能捕获实际激活的对象，依赖解析、基类实参和构造体中的同型对象均保持独立。</summary>
    /// <param name="location">独立同型对象的直接构造位置。</param>
    /// <returns>表示实际激活目标归属验证完成的任务。</returns>
    [Test]
    [Arguments("dependency")]
    [Arguments("initializer")]
    [Arguments("body")]
    public async Task FailedActivationClosesOnlyItsRealTargetAndWaitsForItsWork(string location)
    {
        CaptureProbeOptions options = new(location);
        LifetimeResource? mainResource = null;
        ServiceCollection services = new();
        services.AddSingleton(options);
        services.AddScoped(_ =>
        {
            if (location == "dependency") options.CreateExternal();
            return mainResource = new LifetimeResource();
        });
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        Task<CaptureProbeFeature> creation = FeatureFactory.CreateAsync<CaptureProbeFeature>(provider).AsTask();
        try
        {
            await options.Entered.Task.WaitAsync(Watchdog);
            await Assert.That(options.External!.IsClosed).IsFalse();
            await Assert.That(creation.IsCompleted).IsFalse();
            await Assert.That(mainResource!.DisposeCount).IsEqualTo(0);
            options.Release.SetResult();
            await Assert.That(async () => await creation.WaitAsync(Watchdog)).Throws<InvalidOperationException>();
            await Assert.That(options.Main!.IsClosed).IsTrue();
            await Assert.That(mainResource.DisposeCount).IsEqualTo(1);
            await Assert.That(options.External.IsClosed).IsFalse();
            await options.External.RejectAsync().WaitAsync(Watchdog);
            await Assert.That(options.External.Resource.DisposeCount).IsEqualTo(0);
        }
        finally
        {
            options.Release.TrySetResult();
            try { await creation.WaitAsync(Watchdog); }
            catch (Exception) { }
            if (options.Main is not null) await options.Main.Close().Ticket.Released.WaitAsync(Watchdog);
            if (options.External is not null) await options.External.Close().Ticket.Released.WaitAsync(Watchdog);
            await options.ExternalResource.DisposeAsync();
        }
    }

    /// <summary>构造异常或已关闭对象的绑定失败均须等待已启动执行退出后回收范围。</summary>
    /// <param name="start">构造期间是否已有真实执行。</param>
    /// <param name="closeBeforeBind">构造返回前是否关闭以触发归属绑定失败。</param>
    /// <param name="releaseFault">回收服务是否也发生异常。</param>
    /// <returns>表示创建失败安全回收验证完成的任务。</returns>
    [Test]
    [Arguments(false, false, false)]
    [Arguments(true, false, false)]
    [Arguments(true, true, false)]
    [Arguments(false, false, true)]
    public async Task CreationFailureWaitsForStartedWorkAndPreservesBothFailures(bool start, bool closeBeforeBind, bool releaseFault)
    {
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        InvalidOperationException constructionFailure = new("constructor failed");
        InvalidOperationException disposalFailure = new("scope disposal failed");
        FactoryProbeOptions options = new(start, closeBeforeBind, closeBeforeBind ? null : constructionFailure);
        LifetimeResource? resource = null;
        ServiceCollection services = new();
        services.AddScoped(_ => resource = new(releaseFault ? () => ValueTask.FromException(disposalFailure) : null));
        services.AddSingleton(options);
        services.AddSingleton(new LifetimeWork(async (_, value, dependency) =>
        {
            options.Started.SetResult();
            await release.Task;
            dependency.Use();
            return value;
        }));
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        Task<FactoryProbeFeature> creation = FactoryProbeFeature.CreateAsync(provider).AsTask();
        try
        {
            if (start)
            {
                await options.Started.Task.WaitAsync(Watchdog);
                await Assert.That(creation.IsCompleted).IsFalse();
                await Assert.That(resource!.DisposeCount).IsEqualTo(0);
            }

            release.SetResult();
            Exception? observed = null;
            try
            {
                await creation.WaitAsync(Watchdog);
            }
            catch (Exception failure)
            {
                observed = failure;
            }

            await Assert.That(observed).IsNotNull();
            if (releaseFault)
            {
                await Assert.That(observed).IsTypeOf<AggregateException>();
                Exception[] failures = ((AggregateException)observed!).Flatten().InnerExceptions.ToArray();
                await Assert.That(failures.Any(failure => ReferenceEquals(failure.GetBaseException(), constructionFailure))).IsTrue();
                await Assert.That(failures.Any(failure => ReferenceEquals(failure, disposalFailure))).IsTrue();
            }
            else if (!closeBeforeBind)
            {
                await Assert.That(ReferenceEquals(observed!.GetBaseException(), constructionFailure)).IsTrue();
            }

            await Assert.That(resource!.DisposeCount).IsEqualTo(1);
        }
        finally
        {
            release.TrySetResult();
            try { await creation.WaitAsync(Watchdog); }
            catch (Exception) { }
        }
    }

    /// <summary>释放任务只在真实释放结束后完成，释放故障通过相同票据保留并且不重试释放。</summary>
    /// <returns>表示释放异常验证完成的任务。</returns>
    [Test]
    public async Task ScopeReleaseFailureIsObservableThroughTheUniqueTicket()
    {
        TaskCompletionSource disposing = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseDisposal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        InvalidOperationException failure = new("real release failure");
        LifetimeResource? resource = null;
        await using ServiceProvider provider = LifetimeServices.Create(new(), _ => resource = new(async () =>
        {
            disposing.SetResult();
            await releaseDisposal.Task;
            throw failure;
        }));
        LifecycleFeature feature = await FeatureFactory.CreateAsync<LifecycleFeature>(provider);
        CloseResult close = feature.Close();
        try
        {
            await disposing.Task.WaitAsync(Watchdog);
            await Assert.That(feature.IsClosed).IsTrue();
            await Assert.That(close.Ticket.Released.IsCompleted).IsFalse();
            releaseDisposal.SetResult();
            ReleaseResult result = await close.Ticket.Released.WaitAsync(Watchdog);
            await Assert.That(result.Succeeded).IsFalse();
            await Assert.That(ReferenceEquals(result.Exception, failure)).IsTrue();
            await Assert.That(ReferenceEquals(feature.Close().Ticket, close.Ticket)).IsTrue();
            await Assert.That(ReferenceEquals(await feature.Close().Ticket.Released, result)).IsTrue();
            await Assert.That(resource!.DisposeCount).IsEqualTo(1);
        }
        finally
        {
            releaseDisposal.TrySetResult();
            await close.Ticket.Released.WaitAsync(Watchdog);
        }
    }

    /// <summary>嵌套工厂恢复父构造边界，父失败只回收父范围，不归属外部或独立子实例。</summary>
    /// <returns>表示嵌套工厂边界验证完成的任务。</returns>
    [Test]
    public async Task NestedFactoryFailureDoesNotTakeOwnershipOfIndependentOrExternalFeatures()
    {
        ConcurrentQueue<LifetimeResource> resources = new();
        NestedFactoryOptions options = new();
        ServiceCollection services = new();
        services.AddScoped(_ =>
        {
            LifetimeResource resource = new();
            resources.Enqueue(resource);
            return resource;
        });
        services.AddSingleton<LifetimeSingleton>();
        services.AddSingleton(new LifetimeWork());
        services.AddSingleton(options);
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await Assert.That(async () => await FeatureFactory.CreateAsync<NestedFactoryFeature>(provider)).Throws<InvalidOperationException>();
        LifecycleFeature child = options.Child!;
        LifecycleFeature external = options.External!;
        try
        {
            LifetimeResource[] scopes = resources.ToArray();
            await Assert.That(scopes.Length).IsEqualTo(2);
            await Assert.That(scopes[0].DisposeCount).IsEqualTo(1);
            await Assert.That(child.Resource.DisposeCount).IsEqualTo(0);
            await Assert.That(child.IsClosed).IsFalse();
            await Assert.That(external.Resource.DisposeCount).IsEqualTo(0);
            await Assert.That(external.IsClosed).IsFalse();
            await Assert.That((await child.RejectAsync().WaitAsync(Watchdog)).Kind).IsEqualTo(OperationResultKind.Completed);
            await child.Close().Ticket.Released.WaitAsync(Watchdog);
            await external.Close().Ticket.Released.WaitAsync(Watchdog);
            await Assert.That(child.Resource.DisposeCount).IsEqualTo(1);
            await Assert.That(external.Resource.DisposeCount).IsEqualTo(0);
        }
        finally
        {
            await child.Close().Ticket.Released.WaitAsync(Watchdog);
            await external.Close().Ticket.Released.WaitAsync(Watchdog);
            await external.Resource.DisposeAsync();
        }
    }
}

internal sealed class FactoryProbeOptions(bool start, bool closeBeforeBind, Exception? failure)
{
    internal bool Start { get; } = start;
    internal bool CloseBeforeBind { get; } = closeBeforeBind;
    internal Exception? Failure { get; } = failure;
    internal TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

internal sealed partial class FactoryProbeFeature : Feature<LifetimeState>
{
    public FactoryProbeFeature(LifetimeResource resource, LifetimeWork work, FactoryProbeOptions options) : base(new())
    {
        if (options.Start)
        {
            _ = DispatchOperation("Construct", null, operation => work.Run(operation, 1, resource), CancellationToken.None);
            options.Started.Task.WaitAsync(TimeSpan.FromSeconds(15)).GetAwaiter().GetResult();
        }

        if (options.CloseBeforeBind) Close();
        if (options.Failure is not null) throw options.Failure;
    }
}

internal sealed class NestedFactoryOptions
{
    internal LifecycleFeature? Child { get; set; }
    internal LifecycleFeature? External { get; set; }
}

internal sealed partial class NestedFactoryFeature : Feature<LifetimeState>
{
    public NestedFactoryFeature(LifetimeResource resource, IServiceProvider services, NestedFactoryOptions options) : base(new())
    {
        resource.Use();
        options.Child = FeatureFactory.CreateAsync<LifecycleFeature>(services).AsTask().GetAwaiter().GetResult();
        options.External = new LifecycleFeature(new(), new(), new());
        throw new InvalidOperationException("outer construction failed");
    }
}

internal sealed class CaptureProbeOptions(string location)
{
    private LifetimeResource? externalResource;
    internal string Location { get; } = location;
    internal LifetimeResource ExternalResource => externalResource ??= new();
    internal CaptureProbeFeature? External { get; set; }
    internal CaptureProbeFeature? Main { get; set; }
    internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal void CreateExternal() => External = new CaptureProbeFeature(ExternalResource, new("external"));
}

internal sealed partial class CaptureProbeFeature : Feature<LifetimeState>
{
    private readonly LifetimeResource resource;

    public CaptureProbeFeature(LifetimeResource resource, CaptureProbeOptions options) : base(Initial(options))
    {
        this.resource = resource;
        if (options.Location == "external") return;
        options.Main = this;
        if (options.Location == "body") options.CreateExternal();
        _ = DispatchOperation("Construct", null, async _ =>
        {
            options.Entered.SetResult();
            await options.Release.Task;
            resource.Use();
            return 1;
        }, CancellationToken.None);
        options.Entered.Task.WaitAsync(TimeSpan.FromSeconds(15)).GetAwaiter().GetResult();
        throw new InvalidOperationException("real target construction failed");
    }

    internal LifetimeResource Resource => resource;

    internal Task<OperationResult<int>> RejectAsync()
        => DispatchOperation("Use", null, _ => { resource.Use(); return ValueTask.FromResult(1); }, CancellationToken.None);

    private static LifetimeState Initial(CaptureProbeOptions options)
    {
        if (options.Location == "initializer") options.CreateExternal();
        return new();
    }
}

internal enum ConstructorMode
{
    Selected = 4,
}

internal sealed partial class ConstructorSelectionFeature : Feature<LifetimeState>
{
    private readonly string initialized = "field initialized";

    public ConstructorSelectionFeature() : base(new()) => Choice = "empty";

    public ConstructorSelectionFeature([FromKeyedServices("selected")] LifetimeResource resource, int count = 42,
        ConstructorMode? mode = ConstructorMode.Selected) : base(new())
    {
        Resource = resource;
        Choice = "keyed";
        Count = count;
        Mode = mode;
    }

    internal string Choice { get; }
    internal int Count { get; }
    internal ConstructorMode? Mode { get; }
    internal string Initialized => initialized;
    internal LifetimeResource? Resource { get; }
}

internal sealed partial class PreferredConstructorFeature : Feature<LifetimeState>
{
    [ActivatorUtilitiesConstructor]
    public PreferredConstructorFeature(LifetimeResource resource) : this(resource, "preferred chain")
    {
    }

    public PreferredConstructorFeature(LifetimeResource resource, LifetimeSingleton shared, int value = 12)
        : this(resource, "longer")
    {
        _ = shared;
        _ = value;
    }

    private PreferredConstructorFeature(LifetimeResource resource, string choice) : base(new())
    {
        Resource = resource;
        Choice = choice;
    }

    internal LifetimeResource Resource { get; }
    internal string Choice { get; }
}

internal sealed partial class GreedyConstructorFeature : Feature<LifetimeState>
{
    public GreedyConstructorFeature() : base(new()) => Choice = "empty";

    public GreedyConstructorFeature(LifetimeResource resource, IServiceProvider services, int value = 31) : base(new())
    {
        resource.Use();
        _ = services;
        Choice = $"longest {value}";
    }

    internal string Choice { get; }
}

internal sealed partial class AmbiguousConstructorFeature : Feature<LifetimeState>
{
    public AmbiguousConstructorFeature(LifetimeResource resource) : base(new()) => resource.Use();
    public AmbiguousConstructorFeature(LifetimeSingleton shared) : base(new()) => _ = shared;
}

internal sealed class MissingDependency;

internal sealed partial class MissingConstructorFeature : Feature<LifetimeState>
{
    public MissingConstructorFeature(MissingDependency dependency) : base(new()) => _ = dependency;
}

#pragma warning disable MVI2016 // 运行时工厂拒绝路径的既有负例；生成声明诊断另由编译消费者验证。
internal sealed partial class MultiplePreferredFeature : Feature<LifetimeState>
{
    [ActivatorUtilitiesConstructor]
    public MultiplePreferredFeature() : base(new())
    {
    }

    [ActivatorUtilitiesConstructor]
    public MultiplePreferredFeature(LifetimeResource resource) : base(new()) => resource.Use();
}
#pragma warning restore MVI2016

internal sealed partial class BeforeBaseFailureFeature : Feature<LifetimeState>
{
    public BeforeBaseFailureFeature(LifetimeResource resource) : base(Fail(resource))
    {
    }

    private static LifetimeState Fail(LifetimeResource resource)
    {
        resource.Use();
        throw new InvalidOperationException("failure before store initialization");
    }
}

internal sealed class OwningFactoryOptions
{
    internal RequestDetailsFeature? Caller { get; set; }
    internal OwningFactoryFeature? Target { get; set; }
    internal Exception? Observed { get; set; }
    internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource Failed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

internal sealed partial class OwningFactoryFeature : Feature<LifetimeState>
{
    public OwningFactoryFeature(LifetimeResource resource, OwningFactoryOptions options) : base(new())
    {
        options.Target = this;
        Children.Add(options.Caller!);
        _ = DispatchOperation("ConstructIO", null, async _ =>
        {
            options.Entered.SetResult();
            await options.Release.Task;
            resource.Use();
            return 1;
        }, CancellationToken.None);
        options.Entered.Task.WaitAsync(TimeSpan.FromSeconds(15)).GetAwaiter().GetResult();
        throw new InvalidOperationException("owned caller construction failure");
    }
}

internal sealed class ConstructorWaitOptions(bool nested)
{
    internal bool Nested { get; } = nested;
    internal bool Rejected { get; set; }
    internal bool ScopeWasAlive { get; set; }
    internal bool BackgroundWasAllowed { get; set; }
    internal CloseTicket? Ticket { get; set; }
}

internal sealed partial class ConstructorWaitFeature : Feature<LifetimeState>
{
    public ConstructorWaitFeature(LifetimeResource resource, IServiceProvider services, ConstructorWaitOptions options) : base(new())
    {
        options.Ticket = Close().Ticket;
        Task.Factory.StartNew(() => { _ = options.Ticket.Released; options.BackgroundWasAllowed = true; },
            CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).GetAwaiter().GetResult();
        try
        {
        if (options.Nested) FeatureFactory.CreateAsync<NestedConstructorWaitFeature>(services).AsTask().GetAwaiter().GetResult();
            else options.Ticket.Released.GetAwaiter().GetResult();
        }
        catch (FeatureCreationException failure) when (failure.InnerException is InvalidOperationException)
        {
            options.Rejected = true;
        }
        catch (InvalidOperationException)
        {
            options.Rejected = true;
        }

        options.ScopeWasAlive = resource.DisposeCount == 0;
    }
}

internal sealed partial class NestedConstructorWaitFeature : Feature<LifetimeState>
{
    public NestedConstructorWaitFeature(ConstructorWaitOptions options) : base(new())
        => options.Ticket!.Released.GetAwaiter().GetResult();
}

internal sealed class NestedOwningOptions
{
    internal NestedOwnedOuter? Outer { get; set; }
    internal FeatureCreationException? Failure { get; set; }
    internal List<LifetimeResource> Resources { get; } = [];
    internal bool ResourcesWereAlive { get; set; }
}

internal sealed partial class NestedOwnedOuter : Feature<LifetimeState>
{
    public NestedOwnedOuter(LifetimeResource resource, IServiceProvider services, NestedOwningOptions options) : base(new())
    {
        options.Outer = this;
        try { FeatureFactory.CreateAsync<NestedOwningInner>(services).AsTask().GetAwaiter().GetResult(); }
        catch (FeatureCreationException failure) { options.Failure = failure; }
        options.ResourcesWereAlive = options.Resources.All(dependency => dependency.DisposeCount == 0);
        resource.Use();
    }
}

internal sealed partial class NestedOwningInner : Feature<LifetimeState>
{
    public NestedOwningInner(LifetimeResource resource, NestedOwningOptions options) : base(new())
    {
        Children.Add(options.Outer!);
        resource.Use();
        throw new InvalidOperationException("nested constructor owns outer failure");
    }
}
