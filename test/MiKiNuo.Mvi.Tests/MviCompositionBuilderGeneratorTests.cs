using Microsoft.CodeAnalysis;
using MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 [MviComposition] 组合构建器生成的行为测试（引用真实框架程序集）。
/// </summary>
public sealed class MviCompositionBuilderGeneratorTests
{
    internal const string CompositionSource = """
        namespace CompositionTest
        {
            using System.Threading;
            using System.Threading.Tasks;
            using MiKiNuo.Mvi.Application.MVI.Effect;
            using MiKiNuo.Mvi.Application.MVI.Mediator;
            using MiKiNuo.Mvi.Application.MVI.Reducer;
            using MiKiNuo.Mvi.Application.MVI.Store;
            using MiKiNuo.Mvi.Application.MVI.Threading;
            using MiKiNuo.Mvi.Application.MVI.ViewModel;
            using MiKiNuo.Mvi.Domain.DI;
            using MiKiNuo.Mvi.Domain.MVI.Effect;
            using MiKiNuo.Mvi.Domain.MVI.Intent;
            using MiKiNuo.Mvi.Domain.MVI.Mediator;
            using MiKiNuo.Mvi.Domain.MVI.Reducer;
            using MiKiNuo.Mvi.Domain.MVI.State;

            public sealed record PingRequest(string Text) : IMviRequest<string>;

            public sealed record PongNotification(string Text) : IMviNotification;

            public sealed record ServerState(int Count) : IMviState
            {
                public static ServerState Initial { get; } = new(0);
            }

            public abstract partial record ServerIntent : IMviIntent
            {
                public sealed partial record Noop : ServerIntent;
            }

            public abstract partial record ServerEffect : IMviEffect
            {
                public sealed partial record None : ServerEffect;
            }

            [MviFeature]
            public sealed partial class ServerReducer : MviReducerBase<ServerState, ServerIntent, ServerEffect>
            {
                public override MviReduceResult<ServerState, ServerEffect> Reduce(ServerState state, ServerIntent intent)
                {
                    return MviReduceResult.State<ServerState, ServerEffect>(state);
                }
            }

            public sealed partial class ServerEffectDispatcher : MviEffectDispatcherBase<ServerIntent, ServerEffect>
            {
                public ServerEffectDispatcher(IMviMediator mediator)
                {
                }

                [MviRouteHandler(typeof(PingRequest))]
                internal ValueTask<string> HandlePing(PingRequest request, CancellationToken cancellationToken)
                {
                    return ValueTask.FromResult(request.Text);
                }

                [MviNotificationAcceptor(typeof(PongNotification))]
                internal void AcceptPong(PongNotification notification)
                {
                }

                protected override ValueTask DispatchCoreAsync(ServerEffect effect, CancellationToken cancellationToken)
                {
                    return ValueTask.CompletedTask;
                }
            }

            public sealed partial class ServerViewModel : MviViewModelBase<ServerState, ServerIntent, ServerEffect>
            {
                public ServerViewModel(IMviStore<ServerState, ServerIntent, ServerEffect> store, IMviUiDispatcher? uiDispatcher = null)
                    : base(store, uiDispatcher)
                {
                }

                protected override void ApplyStateCore(ServerState state)
                {
                }
            }

            public sealed record ClientState(int Count) : IMviState
            {
                public static ClientState Initial { get; } = new(0);
            }

            public abstract partial record ClientIntent : IMviIntent
            {
                public sealed partial record Noop : ClientIntent;
            }

            public abstract partial record ClientEffect : IMviEffect
            {
                public sealed partial record Ask : ClientEffect;
            }

            [MviFeature]
            public sealed partial class ClientReducer : MviReducerBase<ClientState, ClientIntent, ClientEffect>
            {
                public override MviReduceResult<ClientState, ClientEffect> Reduce(ClientState state, ClientIntent intent)
                {
                    return MviReduceResult.State<ClientState, ClientEffect>(state);
                }
            }

            public sealed partial class ClientEffectDispatcher : MviEffectDispatcherBase<ClientIntent, ClientEffect>
            {
                private readonly IMviMediator _mediator;

                public ClientEffectDispatcher(IMviMediator mediator)
                {
                    _mediator = mediator;
                }

                public async ValueTask AskAsync()
                {
                    _ = await _mediator.SendAsync(new PingRequest("x"));
                }

                protected override ValueTask DispatchCoreAsync(ClientEffect effect, CancellationToken cancellationToken)
                {
                    return ValueTask.CompletedTask;
                }
            }

            public sealed partial class ClientViewModel : MviViewModelBase<ClientState, ClientIntent, ClientEffect>
            {
                public ClientViewModel(IMviStore<ClientState, ClientIntent, ClientEffect> store, IMviUiDispatcher? uiDispatcher = null)
                    : base(store, uiDispatcher)
                {
                }

                protected override void ApplyStateCore(ClientState state)
                {
                }
            }

            [MviComposition(typeof(ServerReducer), typeof(ClientReducer))]
            public sealed partial class TestComposition
            {
            }
        }
        """;

    /// <summary>
    /// 组合清理行为测试共享：可释放探针与录制器。各探针捕获各自实例端点用于观察范围关闭，
    /// 并经由可控 <see cref="System.Threading.Tasks.TaskCompletionSource{TResult}"/> 验证逆序逐 await 释放。
    /// </summary>
    private const string DisposalProbeSource = @"
        namespace CompositionTest
        {
            using System.Threading.Tasks;

            internal static class DisposalRecorder
            {
                public static System.Collections.Generic.List<string>? Order;
                public static MiKiNuo.Mvi.Application.MVI.Mediator.IMviMediator? CapturedEndpoint;
                public static string? ThrowMember;
                public static TaskCompletionSource<bool>? FirstRecorded;
                public static TaskCompletionSource<bool>? FirstGate;
                public static void Reset()
                {
                    Order = new System.Collections.Generic.List<string>();
                    CapturedEndpoint = null;
                    ThrowMember = null;
                    FirstRecorded = null;
                    FirstGate = null;
                }
            }

            [MiKiNuo.Mvi.Domain.DI.DiService(MiKiNuo.Mvi.Domain.DI.ServiceLifetime.Scoped)]
            public sealed class DisposalProbe : System.IAsyncDisposable
            {
                private readonly MiKiNuo.Mvi.Application.MVI.Mediator.IMviMediator _endpoint;
                public string? MemberName;
                public DisposalProbe(MiKiNuo.Mvi.Application.MVI.Mediator.IMviMediator mediator) { _endpoint = mediator; }
                public async ValueTask DisposeAsync()
                {
                    DisposalRecorder.CapturedEndpoint = _endpoint;
                    DisposalRecorder.Order!.Add(MemberName!);
                    if (DisposalRecorder.FirstRecorded is not null) { DisposalRecorder.FirstRecorded.TrySetResult(true); }
                    if (DisposalRecorder.FirstGate is not null) { await DisposalRecorder.FirstGate.Task.ConfigureAwait(false); }
                    if (DisposalRecorder.ThrowMember is not null && DisposalRecorder.ThrowMember == MemberName) { throw new System.InvalidOperationException(""Disposal probe cleanup failed.""); }
                }
            }
        }
        ";

    /// <summary>
    /// 在组合源中接入销毁探针：将两个 EffectDispatcher 的构造函数追加对应探针依赖，
    /// 使探针成为成员实例资源，并在源末追加 <see cref="DisposalProbeSource"/>。
    /// </summary>
    /// <param name="source">组合测试源。</param>
    /// <returns>接入探针后的源。</returns>
    private static string WithDisposalProbes(string source)
    {
        source = source
            .Replace("public ServerEffectDispatcher(IMviMediator mediator)",
                "public ServerEffectDispatcher(IMviMediator mediator, DisposalProbe serverProbe)")
            .Replace("public ClientEffectDispatcher(IMviMediator mediator)",
                "public ClientEffectDispatcher(IMviMediator mediator, DisposalProbe clientProbe)");

        // 注入成员名赋值：原始字符串去缩进与 CRLF 行尾使固定 \n 锚定失效，故用正则容忍任意空白。
        source = System.Text.RegularExpressions.Regex.Replace(
            source,
            @"DisposalProbe serverProbe\)\s*\{\s*\}",
            "DisposalProbe serverProbe)\r\n        {\r\n            serverProbe.MemberName = \"Server\";\r\n        }");
        source = System.Text.RegularExpressions.Regex.Replace(
            source,
            @"_mediator = mediator;\s*",
            "_mediator = mediator;\r\n            clientProbe.MemberName = \"Client\";\r\n");

        return source + DisposalProbeSource;
    }

    /// <summary>
    /// 验证生成器为 [MviComposition] 声明 emit 组合构建器与组合句柄。
    /// </summary>
    [Test]
    public async Task Generator_Should_EmitCompositionBuilderAsync()
    {
        (GeneratorDriverRunResult runResult, bool emitSuccess) =
            GeneratorTestHost.RunGeneratorAndCompile<MviDiContainerGenerator>(
                CompositionSource,
                GeneratorTestHost.FrameworkReferences);

        string generated = string.Join("\n", runResult.GeneratedTrees.Select(tree => tree.GetText().ToString()));

        await Assert.That(generated).Contains("CreateTestCompositionAsync");
        await Assert.That(generated).Contains("partial class TestComposition");
        await Assert.That(generated).Contains("CreateEndpoint(");
        await Assert.That(generated).Contains("CreateServerInstanceCoreAsync");
        await Assert.That(generated).Contains("CreateClientInstanceCoreAsync");
        await Assert.That(emitSuccess).IsTrue();
    }

    /// <summary>
    /// 验证组合构建器体内调用的实例核心方法名与容器内声明的内部方法一一对应：
    /// 实例工厂发射与组合发射两侧必须共享同一方法名事实，任何单侧改名都会导致此测试变红。
    /// </summary>
    [Test]
    public async Task Generator_Should_CallOnlyDeclaredInstanceCoreMethodsAsync()
    {
        (GeneratorDriverRunResult runResult, bool emitSuccess) =
            GeneratorTestHost.RunGeneratorAndCompile<MviDiContainerGenerator>(
                CompositionSource,
                GeneratorTestHost.FrameworkReferences);

        string generated = string.Join("\n", runResult.GeneratedTrees.Select(tree => tree.GetText().ToString()));

        string[] declared = System.Text.RegularExpressions.Regex.Matches(
                generated,
                @"internal async global::System\.Threading\.Tasks\.ValueTask<\([^)]+\)> (Create\w+InstanceCoreAsync)\(")
            .Select(match => match.Groups[1].Value)
            .Distinct()
            .ToArray();
        string[] invoked = System.Text.RegularExpressions.Regex.Matches(
                generated,
                @"await (Create\w+InstanceCoreAsync)\(")
            .Select(match => match.Groups[1].Value)
            .Distinct()
            .ToArray();

        await Assert.That(declared.Length).IsEqualTo(2);
        await Assert.That(invoked.Length).IsEqualTo(2);
        foreach (string method in invoked)
        {
            await Assert.That(declared.Contains(method)).IsTrue();
        }

        await Assert.That(emitSuccess).IsTrue();
    }

    /// <summary>
    /// 验证唯一提供方的请求契约自动完成注册与消费方绑定。
    /// </summary>
    [Test]
    public async Task Generator_Should_WireUniqueRouteProviderAsync()
    {
        (GeneratorDriverRunResult runResult, bool emitSuccess) =
            GeneratorTestHost.RunGeneratorAndCompile<MviDiContainerGenerator>(
                CompositionSource,
                GeneratorTestHost.FrameworkReferences);

        string generated = string.Join("\n", runResult.GeneratedTrees.Select(tree => tree.GetText().ToString()));

        await Assert.That(generated).Contains("Register<global::CompositionTest.PingRequest, string>");
        await Assert.That(generated).Contains("Bind<global::CompositionTest.PingRequest>");
        await Assert.That(generated).Contains("HandlePing");
        await Assert.That(emitSuccess).IsTrue();
    }

    /// <summary>
    /// 验证声明了接纳器的成员自动建立通知订阅。
    /// </summary>
    [Test]
    public async Task Generator_Should_WireNotificationSubscriptionAsync()
    {
        (GeneratorDriverRunResult runResult, bool emitSuccess) =
            GeneratorTestHost.RunGeneratorAndCompile<MviDiContainerGenerator>(
                CompositionSource,
                GeneratorTestHost.FrameworkReferences);

        string generated = string.Join("\n", runResult.GeneratedTrees.Select(tree => tree.GetText().ToString()));

        await Assert.That(generated).Contains("Subscribe<global::CompositionTest.PongNotification>");
        await Assert.That(generated).Contains("AcceptPong");
        await Assert.That(emitSuccess).IsTrue();
    }

    /// <summary>
    /// 验证组合内同一请求契约存在多个提供方时报告 MVI0022。
    /// </summary>
    [Test]
    public async Task Generator_Should_ReportMvi0022WhenMultipleProvidersAsync()
    {
        string ambiguousSource = CompositionSource.Replace(
            "        protected override ValueTask DispatchCoreAsync(ClientEffect effect, CancellationToken cancellationToken)",
            "        [MviRouteHandler(typeof(PingRequest))]\n        internal ValueTask<string> HandlePingToo(PingRequest request, CancellationToken cancellationToken) => ValueTask.FromResult(request.Text);\n\n        protected override ValueTask DispatchCoreAsync(ClientEffect effect, CancellationToken cancellationToken)");

        GeneratorDriverRunResult runResult = GeneratorTestHost.RunGenerator<MviDiContainerGenerator>(
            ambiguousSource,
            GeneratorTestHost.FrameworkReferences);

        await Assert.That(runResult.Diagnostics.Any(d => d.Id == "MVI0022")).IsTrue();
    }

    /// <summary>
    /// 验证请求契约存在消费方但无提供方时报告 MVI0023。
    /// </summary>
    [Test]
    public async Task Generator_Should_ReportMvi0023WhenConsumerHasNoProviderAsync()
    {
        string orphanSource = CompositionSource.Replace(
            "        [MviRouteHandler(typeof(PingRequest))]",
            "        // 提供方声明已移除");

        GeneratorDriverRunResult runResult = GeneratorTestHost.RunGenerator<MviDiContainerGenerator>(
            orphanSource,
            GeneratorTestHost.FrameworkReferences);

        await Assert.That(runResult.Diagnostics.Any(d => d.Id == "MVI0023")).IsTrue();
    }

    /// <summary>
    /// 验证路由处理器签名或可见性非法时报告 MVI0024。
    /// </summary>
    [Test]
    public async Task Generator_Should_ReportMvi0024WhenHandlerSignatureInvalidAsync()
    {
        string invalidSource = CompositionSource.Replace(
            "        internal ValueTask<string> HandlePing(PingRequest request, CancellationToken cancellationToken)",
            "        private ValueTask<string> HandlePing(PingRequest request, CancellationToken cancellationToken)");

        GeneratorDriverRunResult runResult = GeneratorTestHost.RunGenerator<MviDiContainerGenerator>(
            invalidSource,
            GeneratorTestHost.FrameworkReferences);

        await Assert.That(runResult.Diagnostics.Any(d => d.Id == "MVI0024")).IsTrue();
    }

    /// <summary>
    /// 验证组合声明类未标记 partial 时报告 MVI0025 且不 emit 句柄。
    /// </summary>
    [Test]
    public async Task Generator_Should_ReportMvi0025WhenDeclarationNotPartialAsync()
    {
        string nonPartialSource = CompositionSource.Replace(
            "public sealed partial class TestComposition",
            "public sealed class TestComposition");

        GeneratorDriverRunResult runResult = GeneratorTestHost.RunGenerator<MviDiContainerGenerator>(
            nonPartialSource,
            GeneratorTestHost.FrameworkReferences);

        await Assert.That(runResult.Diagnostics.Any(d => d.Id == "MVI0025")).IsTrue();
        string generated = string.Join("\n", runResult.GeneratedTrees.Select(tree => tree.GetText().ToString()));
        await Assert.That(generated).DoesNotContain("CreateTestCompositionAsync");
    }

    /// <summary>
    /// 验证同一 Feature 类型在组合中多次出现时成员变量与属性名去重，生成代码可编译。
    /// </summary>
    [Test]
    public async Task Generator_Should_DeduplicateMemberNamesWhenFeatureRepeatsAsync()
    {
        string repeatedSource = CompositionSource.Replace(
            "[MviComposition(typeof(ServerReducer), typeof(ClientReducer))]",
            "[MviComposition(typeof(ServerReducer), typeof(ServerReducer))]");

        (GeneratorDriverRunResult runResult, bool emitSuccess) =
            GeneratorTestHost.RunGeneratorAndCompile<MviDiContainerGenerator>(
                repeatedSource,
                GeneratorTestHost.FrameworkReferences);

        string generated = string.Join("\n", runResult.GeneratedTrees.Select(tree => tree.GetText().ToString()));
        await Assert.That(generated).Contains("server2");
        await Assert.That(emitSuccess).IsTrue();
    }

    /// <summary>
    /// 验证组合创建中途失败时已创建的成员实例被回收（资源不泄漏）。
    /// </summary>
    [Test]
    public async Task Generator_Should_DisposeCreatedMembersWhenBuilderFailsAsync()
    {
        string failingSource = CompositionSource
            .Replace(
                "        public ServerEffectDispatcher(IMviMediator mediator)",
                "        public ServerEffectDispatcher(IMviMediator mediator, DisposeProbe probe)")
            .Replace(
                "            _mediator = mediator;",
                "            throw new System.InvalidOperationException(\"模拟构造失败。\");")
            + """

            namespace CompositionTest
            {
                [MiKiNuo.Mvi.Domain.DI.DiService(MiKiNuo.Mvi.Domain.DI.ServiceLifetime.Scoped)]
                public sealed class DisposeProbe : System.IDisposable
                {
                    public static int DisposedCount;
                    public void Dispose() { DisposedCount++; }
                }
            }

            public static class InstanceProbe
            {
                public static async System.Threading.Tasks.Task<bool> Run()
                {
                    MviGeneratorTestAssembly.Composition.GeneratedMviContainer container = new();
                    CompositionTest.DisposeProbe.DisposedCount = 0;
                    try
                    {
                        await container.CreateTestCompositionAsync();
                        return false;
                    }
                    catch (System.InvalidOperationException)
                    {
                    }

                    return CompositionTest.DisposeProbe.DisposedCount == 1;
                }
            }
            """;

        await Assert.That(
            GeneratorTestHost.RunGeneratorProbeAsync<MviDiContainerGenerator>(
                failingSource,
                GeneratorTestHost.FrameworkReferences)).IsTrue();
    }

    /// <summary>
    /// 验证组合正常释放时按成员逆序逐个 await，所有成员与组合范围都被释放，
    /// 且范围关闭后端点发送会抛出 ObjectDisposedException（无需反射私有状态）。
    /// 以可控 TaskCompletionSource 驱动释放流程，避免依赖计时与并行。
    /// </summary>
    [Test]
    public async Task Generator_Should_DisposeMembersInReverseOrderAndCloseScopeAsync()
    {
        string source = WithDisposalProbes(CompositionSource) + @"
            public static class InstanceProbe
            {
                public static async System.Threading.Tasks.Task<bool> Run()
                {
                    CompositionTest.DisposalRecorder.Reset();
                    CompositionTest.DisposalRecorder.FirstRecorded = new System.Threading.Tasks.TaskCompletionSource<bool>(System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);
                    CompositionTest.DisposalRecorder.FirstGate = new System.Threading.Tasks.TaskCompletionSource<bool>(System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);
                    MviGeneratorTestAssembly.Composition.GeneratedMviContainer container = new();
                    CompositionTest.TestComposition composition = await container.CreateTestCompositionAsync();
                    System.Threading.Tasks.ValueTask disposeTask = composition.DisposeAsync();
                    await CompositionTest.DisposalRecorder.FirstRecorded.Task.ConfigureAwait(false);
                    if (CompositionTest.DisposalRecorder.Order!.Contains(""Server""))
                    {
                        throw new System.InvalidOperationException(""Server released before Client finished (not reverse awaited)."");
                    }
                    CompositionTest.DisposalRecorder.FirstGate.SetResult(true);
                    await disposeTask.ConfigureAwait(false);
                    if (CompositionTest.DisposalRecorder.Order.Count != 2 || CompositionTest.DisposalRecorder.Order[0] != ""Client"" || CompositionTest.DisposalRecorder.Order[1] != ""Server"")
                    {
                        throw new System.InvalidOperationException(""Release order is not reverse: "" + string.Join("","", CompositionTest.DisposalRecorder.Order));
                    }
                    if (CompositionTest.DisposalRecorder.CapturedEndpoint is null)
                    {
                        throw new System.InvalidOperationException(""Endpoint not captured."");
                    }
                    try
                    {
                        await CompositionTest.DisposalRecorder.CapturedEndpoint.SendAsync(new CompositionTest.PingRequest(""x"")).ConfigureAwait(false);
                        throw new System.InvalidOperationException(""Scope not closed: endpoint still sends."");
                    }
                    catch (System.ObjectDisposedException) { }
                    return true;
                }
            }
            ";

        await Assert.That(
            GeneratorTestHost.RunGeneratorProbeAsync<MviDiContainerGenerator>(
                source,
                GeneratorTestHost.FrameworkReferences)).IsTrue();
    }

    /// <summary>
    /// 验证某个成员清理（Dispose）抛异常时，其余成员与组合范围仍被释放，
    /// 且 DisposeAsync 抛出聚合异常。覆盖正常释放路径的异常不中断语义。
    /// </summary>
    [Test]
    public async Task Generator_Should_ReleaseRemainingMembersAndScopeWhenCleanupFailsAsync()
    {
        string source = WithDisposalProbes(CompositionSource) + @"
            public static class InstanceProbe
            {
                public static async System.Threading.Tasks.Task<bool> Run()
                {
                    CompositionTest.DisposalRecorder.Reset();
                    CompositionTest.DisposalRecorder.ThrowMember = ""Client"";
                    MviGeneratorTestAssembly.Composition.GeneratedMviContainer container = new();
                    CompositionTest.TestComposition composition = await container.CreateTestCompositionAsync();
                    System.Exception? observed = null;
                    try { await composition.DisposeAsync().ConfigureAwait(false); }
                    catch (System.Exception ex) { observed = ex; }
                    if (observed is null) { throw new System.InvalidOperationException(""DisposeAsync did not throw cleanup exception.""); }
                    if (observed is not System.AggregateException) { throw new System.InvalidOperationException(""DisposeAsync threw non-aggregate: "" + observed); }
                    if (!CompositionTest.DisposalRecorder.Order!.Contains(""Server"")) { throw new System.InvalidOperationException(""Server not released when Client cleanup failed.""); }
                    if (CompositionTest.DisposalRecorder.CapturedEndpoint is null) { throw new System.InvalidOperationException(""Endpoint not captured.""); }
                    try
                    {
                        await CompositionTest.DisposalRecorder.CapturedEndpoint.SendAsync(new CompositionTest.PingRequest(""x"")).ConfigureAwait(false);
                        throw new System.InvalidOperationException(""Scope not closed: endpoint still sends."");
                    }
                    catch (System.ObjectDisposedException) { }
                    return true;
                }
            }
            ";

        await Assert.That(
            GeneratorTestHost.RunGeneratorProbeAsync<MviDiContainerGenerator>(
                source,
                GeneratorTestHost.FrameworkReferences)).IsTrue();
    }

    /// <summary>
    /// 验证组合构建中途失败时：已创建成员被回滚释放、范围关闭，
    /// 且当清理无异常时原构建异常类型被原样保持（非包装为聚合异常）。
    /// </summary>
    [Test]
    public async Task Generator_Should_PreserveOriginalExceptionWhenRollbackHasNoCleanupFailureAsync()
    {
        string source = WithDisposalProbes(CompositionSource)
            .Replace("_mediator = mediator;",
                "_mediator = mediator; throw new System.InvalidOperationException(\"builder failed\");")
            + @"
            public static class InstanceProbe
            {
                public static async System.Threading.Tasks.Task<bool> Run()
                {
                    CompositionTest.DisposalRecorder.Reset();
                    MviGeneratorTestAssembly.Composition.GeneratedMviContainer container = new();
                    System.Exception? observed = null;
                    try { await container.CreateTestCompositionAsync().ConfigureAwait(false); }
                    catch (System.Exception ex) { observed = ex; }
                    if (observed is null) { throw new System.InvalidOperationException(""Builder did not throw.""); }
                    if (observed is System.AggregateException) { throw new System.InvalidOperationException(""Rollback wrapped original into aggregate, type not preserved: "" + observed); }
                    if (observed is not System.InvalidOperationException) { throw new System.InvalidOperationException(""Rollback did not preserve original type: "" + observed); }
                    if (!CompositionTest.DisposalRecorder.Order!.Contains(""Server"")) { throw new System.InvalidOperationException(""Rollback did not release created Server.""); }
                    if (CompositionTest.DisposalRecorder.CapturedEndpoint is null) { throw new System.InvalidOperationException(""Endpoint not captured.""); }
                    try
                    {
                        await CompositionTest.DisposalRecorder.CapturedEndpoint.SendAsync(new CompositionTest.PingRequest(""x"")).ConfigureAwait(false);
                        throw new System.InvalidOperationException(""Scope not closed: endpoint still sends."");
                    }
                    catch (System.ObjectDisposedException) { }
                    return true;
                }
            }
            ";

        await Assert.That(
            GeneratorTestHost.RunGeneratorProbeAsync<MviDiContainerGenerator>(
                source,
                GeneratorTestHost.FrameworkReferences)).IsTrue();
    }

    /// <summary>
    /// 验证组合构建中途失败且回滚清理也失败时：聚合异常同时保留原始构建异常与清理异常。
    /// </summary>
    [Test]
    public async Task Generator_Should_AggregateOriginalAndCleanupExceptionsWhenRollbackCleanupFailsAsync()
    {
        string source = WithDisposalProbes(CompositionSource)
            .Replace("_mediator = mediator;",
                "_mediator = mediator; throw new System.InvalidOperationException(\"builder failed\");")
            + @"
            public static class InstanceProbe
            {
                public static async System.Threading.Tasks.Task<bool> Run()
                {
                    CompositionTest.DisposalRecorder.Reset();
                    CompositionTest.DisposalRecorder.ThrowMember = ""Server"";
                    MviGeneratorTestAssembly.Composition.GeneratedMviContainer container = new();
                    System.Exception? observed = null;
                    try { await container.CreateTestCompositionAsync().ConfigureAwait(false); }
                    catch (System.Exception ex) { observed = ex; }
                    if (observed is null) { throw new System.InvalidOperationException(""Builder did not throw.""); }
                    if (observed is not System.AggregateException agg) { throw new System.InvalidOperationException(""Rollback did not aggregate original and cleanup: "" + observed); }
                    System.Collections.Generic.IEnumerable<System.Exception> flattened = agg.Flatten().InnerExceptions;
                    bool hasBuild = false;
                    bool hasCleanup = false;
                    foreach (System.Exception e in flattened)
                    {
                        if (e is System.InvalidOperationException && e.Message.Contains(""builder failed"")) hasBuild = true;
                        if (e.Message.Contains(""Disposal probe cleanup failed."")) hasCleanup = true;
                    }
                    if (!hasBuild || !hasCleanup) { throw new System.InvalidOperationException(""Aggregate did not preserve both original and cleanup.""); }
                    if (!CompositionTest.DisposalRecorder.Order!.Contains(""Server"")) { throw new System.InvalidOperationException(""Rollback did not release created Server.""); }
                    try
                    {
                        await CompositionTest.DisposalRecorder.CapturedEndpoint!.SendAsync(new CompositionTest.PingRequest(""x"")).ConfigureAwait(false);
                        throw new System.InvalidOperationException(""Scope not closed: endpoint still sends."");
                    }
                    catch (System.ObjectDisposedException) { }
                    return true;
                }
            }
            ";

        await Assert.That(
            GeneratorTestHost.RunGeneratorProbeAsync<MviDiContainerGenerator>(
                source,
                GeneratorTestHost.FrameworkReferences)).IsTrue();
    }

    /// <summary>
    /// 验证组合成员不是已发现的 [MviFeature] Reducer 时报告 MVI0026。
    /// </summary>
    [Test]
    public async Task Generator_Should_ReportMvi0026WhenMemberUnknownAsync()
    {
        string unknownMemberSource = CompositionSource.Replace(
            "[MviComposition(typeof(ServerReducer), typeof(ClientReducer))]",
            "[MviComposition(typeof(ServerReducer), typeof(PingRequest))]");

        GeneratorDriverRunResult runResult = GeneratorTestHost.RunGenerator<MviDiContainerGenerator>(
            unknownMemberSource,
            GeneratorTestHost.FrameworkReferences);

        await Assert.That(runResult.Diagnostics.Any(d => d.Id == "MVI0026")).IsTrue();
    }
}
