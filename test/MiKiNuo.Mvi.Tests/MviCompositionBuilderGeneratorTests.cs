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
    /// 验证生成器为 [MviComposition] 声明 emit 组合构建器与组合句柄。
    /// </summary>
    [Test]
    public async Task Generator_Should_EmitCompositionBuilderAsync()
    {
        (GeneratorDriverRunResult runResult, bool emitSuccess) =
            GeneratorTestHost.RunGeneratorAndCompile<MviDiContainerGenerator>(
                CompositionSource,
                MviFeatureContainerGeneratorTests.GetFrameworkReferences());

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
                MviFeatureContainerGeneratorTests.GetFrameworkReferences());

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
                MviFeatureContainerGeneratorTests.GetFrameworkReferences());

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
                MviFeatureContainerGeneratorTests.GetFrameworkReferences());

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
            MviFeatureContainerGeneratorTests.GetFrameworkReferences());

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
            MviFeatureContainerGeneratorTests.GetFrameworkReferences());

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
            MviFeatureContainerGeneratorTests.GetFrameworkReferences());

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
            MviFeatureContainerGeneratorTests.GetFrameworkReferences());

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
                MviFeatureContainerGeneratorTests.GetFrameworkReferences());

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
                MviFeatureContainerGeneratorTests.GetFrameworkReferences())).IsTrue();
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
            MviFeatureContainerGeneratorTests.GetFrameworkReferences());

        await Assert.That(runResult.Diagnostics.Any(d => d.Id == "MVI0026")).IsTrue();
    }
}
