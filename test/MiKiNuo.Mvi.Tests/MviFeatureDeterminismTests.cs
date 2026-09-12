using Microsoft.CodeAnalysis;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Domain.MVI.State;
using MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>验证 Feature 自动装配拒绝歧义并遵循显式顺序。</summary>
public sealed class MviFeatureDeterminismTests
{
    /// <summary>可编译的最小 Feature 声明。</summary>
    private const string Source = """
        using MiKiNuo.Mvi.Domain.DI;
        using MiKiNuo.Mvi.Domain.MVI.State;
        using MiKiNuo.Mvi.Domain.MVI.Intent;
        using MiKiNuo.Mvi.Domain.MVI.Effect;
        using MiKiNuo.Mvi.Domain.MVI.Reducer;
        using MiKiNuo.Mvi.Application.MVI.Reducer;
        using MiKiNuo.Mvi.Application.MVI.Effect;
        using MiKiNuo.Mvi.Application.MVI.ViewModel;
        using MiKiNuo.Mvi.Application.MVI.Store;
        using MiKiNuo.Mvi.Application.MVI.Middleware;
        public sealed record State : IMviState { public static State Initial { get; } = new(); }
        public sealed record Intent : IMviIntent;
        public sealed record Effect : IMviEffect;
        [MviFeature]
        public sealed class DemoReducer : MviReducerBase<State, Intent, Effect>
        {
            public override MviReduceResult<State, Effect> Reduce(State state, Intent intent)
                => MviReduceResult.State<State, Effect>(state);
        }
        """;

    /// <summary>多个分发器匹配必须报告错误且不生成该 Feature。</summary>
    [Test]
    public async Task AmbiguousDispatchersAreRejectedAsync()
    {
        string component = """
            public sealed class DISPATCHER : MviEffectDispatcherBase<Intent, Effect>
            {
                protected override System.Threading.Tasks.ValueTask DispatchCoreAsync(Effect effect,
                    System.Threading.CancellationToken cancellationToken) => System.Threading.Tasks.ValueTask.CompletedTask;
            }
            """;
        GeneratorDriverRunResult result = Run(Source + component.Replace("DISPATCHER", "Alpha") + component.Replace("DISPATCHER", "Zulu"));
        await Assert.That(result.Diagnostics.Any(d => d.Id == "MVI0018" && d.Severity == DiagnosticSeverity.Error)).IsTrue();
        await Assert.That(string.Join("\n", result.GeneratedTrees)).DoesNotContain("CreateDemoStore");
    }

    /// <summary>多个视图模型匹配必须报告错误且不生成该 Feature。</summary>
    [Test]
    public async Task AmbiguousViewModelsAreRejectedAsync()
    {
        string component = """
            public sealed class VIEWMODEL : MviViewModelBase<State, Intent, Effect>
            {
                public VIEWMODEL(IMviStore<State, Intent, Effect> store) : base(store) { }
                protected override void ApplyStateCore(State state) { }
            }
            """;
        GeneratorDriverRunResult result = Run(Source + component.Replace("VIEWMODEL", "Alpha") + component.Replace("VIEWMODEL", "Zulu"));
        await Assert.That(result.Diagnostics.Any(d => d.Id == "MVI0019" && d.Severity == DiagnosticSeverity.Error)).IsTrue();
        await Assert.That(string.Join("\n", result.GeneratedTrees)).DoesNotContain("CreateDemoStore");
    }

    /// <summary>多个中间件必须全部声明顺序。</summary>
    [Test]
    public async Task MultipleMiddlewaresRequireExplicitOrderAsync()
    {
        GeneratorDriverRunResult result = Run(Source + Middleware("Alpha") + Middleware("Zulu"));
        await Assert.That(result.Diagnostics.Any(d => d.Id == "MVI0020" && d.Severity == DiagnosticSeverity.Error)).IsTrue();
        await Assert.That(string.Join("\n", result.GeneratedTrees)).DoesNotContain("CreateDemoStore");
    }

    /// <summary>构造实现真实中间件契约的测试输入。</summary>
    /// <param name="name">中间件名称。</param>
    /// <returns>中间件声明源码。</returns>
    private static string Middleware(string name) => $$"""
        public sealed class {{name}} : IMviMiddleware<State, Intent, Effect>
        {
            public System.Threading.Tasks.ValueTask<MviReduceResult<State, Effect>> InvokeAsync(
                MviMiddlewareContext<State, Intent, Effect> context,
                MviMiddlewareStep<State, Intent, Effect> nextMiddleware,
                System.Threading.CancellationToken cancellationToken) => nextMiddleware(context, cancellationToken);
        }
        """;

    /// <summary>通过真实框架引用运行生成器。</summary>
    /// <param name="source">待生成的源代码。</param>
    /// <returns>生成器运行结果。</returns>
    private static GeneratorDriverRunResult Run(string source)
        => GeneratorTestHost.RunGenerator<MviDiContainerGenerator>(source, References());

    /// <summary>提供生成代码所依赖的真实程序集。</summary>
    /// <returns>框架元数据引用。</returns>
    private static MetadataReference[] References() =>
    [
        MetadataReference.CreateFromFile(typeof(IMviState).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(IMviStore<,,>).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(R3.Observable).Assembly.Location),
    ];
}
