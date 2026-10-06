using MiKiNuo.Mvi.Generators.SourceGeneration;
using TUnit.Assertions;
using TUnit.Core;
namespace MiKiNuo.Mvi.Tests;
/// <summary>多生成器协作必须得到可编译且行为正确的单一对象图。</summary>
public sealed class GeneratedFeatureTests
{
    internal const string FeatureSource = """
        using System;
        using System.Threading;
        using System.Threading.Tasks;
        using MiKiNuo.Mvi.Abstractions.DI;
        using MiKiNuo.Mvi.Abstractions.MVI.Binding;
        using MiKiNuo.Mvi.Abstractions.MVI.Intent;
        using MiKiNuo.Mvi.Abstractions.MVI.Mutation;
        using MiKiNuo.Mvi.Abstractions.MVI.Reducer;
        using MiKiNuo.Mvi.Abstractions.MVI.State;
        using MiKiNuo.Mvi.Binding.Command;
        using MiKiNuo.Mvi.Binding.ViewModel;
        using MiKiNuo.Mvi.Runtime.MVI.Intent;
        using MiKiNuo.Mvi.Runtime.MVI.Reducer;
        namespace FeatureTest
        {
            public sealed record TestState(string Text = "", int Count = 0) : IMviState
            { public static TestState Initial { get; } = new(); }
            public sealed record TestIntent(string Text, string Password) : IMviIntent;
            public sealed record TestMutation(string Text) : IMviMutation<TestState>;
            public sealed partial class TestReducer : MviReducerBase<TestState>
            {
                [MviReduce(typeof(TestMutation))]
                private static TestState Apply(TestState state, TestMutation mutation)
                    => state with { Text = mutation.Text, Count = state.Count + 1 };
            }
            [MviFeature]
            public sealed partial class TestHandler : MviIntentHandler<TestState, TestIntent>
            {
                [MviHandle(typeof(TestIntent))]
                private static ValueTask SubmitAsync(TestIntent intent, IIntentContext<TestState> context, CancellationToken token)
                { context.Reduce(new TestMutation(intent.Text + ":" + intent.Password)); return ValueTask.CompletedTask; }
            }
            public sealed partial class TestViewModel : MviViewModelBase<TestState, TestIntent>
            {
                [MviBind] public partial string Text { get; set; }
                [MviBind(Sensitive = true)] public partial string Password { get; set; }
                [MviCommand(typeof(TestIntent), nameof(Text), nameof(Password))]
                public partial IMviAsyncCommand SubmitCommand { get; }
            }
            [MviComposition(typeof(TestHandler))]
            public sealed partial class TestComposition { }
        }
        """;
    /// <summary>所有新契约和构造函数在同一编译轮次成立。</summary>
    [Test] public async Task FrameworkGeneratorsCompileTogetherAsync()
    {
        var (_, errors) = GeneratorTestHost.CompileFramework(FeatureSource);
        await Assert.That(string.Join("\n", errors)).IsEqualTo("");
    }
    /// <summary>真实生成的两个实例身份与状态相互隔离，快照在清理之前完成。</summary>
    [Test] public async Task GeneratedCompositionRunsWithoutForwardingMethodsAsync()
    {
        string source = FeatureSource + """
            public static class InstanceProbe
            {
                public static async System.Threading.Tasks.Task<bool> Run()
                {
                    var container = new MviGeneratorTestAssembly.Composition.GeneratedMviContainer();
                    await using var first = await container.CreateTestCompositionAsync();
                    await using var second = await container.CreateTestCompositionAsync();
                    first.Test.ViewModel.Text = "alpha";
                    first.Test.ViewModel.Password = "captured";
                    await first.Test.ViewModel.SubmitCommand.ExecuteAsync(null);
                    return first.Test.ViewModel.State.Text == "alpha:captured"
                        && first.Test.ViewModel.Password == ""
                        && second.Test.ViewModel.State.Count == 0
                        && first.Test.Id != second.Test.Id;
                }
            }
            """;
        await Assert.That(await GeneratorTestHost.ProbeFrameworkAsync(source)).IsTrue();
    }
    /// <summary>不同命名空间下同名 ViewModel 不发生 hintName 冲突。</summary>
    [Test] public async Task SameSimpleTypeNamesHaveDistinctGeneratedFilesAsync()
    {
        string source = FeatureSource + """
            namespace Other
            {
                public sealed partial class TestViewModel : MiKiNuo.Mvi.Binding.ViewModel.MviViewModelBase<FeatureTest.TestState, FeatureTest.TestIntent>
                { [MiKiNuo.Mvi.Abstractions.MVI.Binding.MviBind] public partial string Draft { get; set; } }
            }
            """;
        // 单独验证 ViewModel：两个默认 ViewModel 对同一 Feature 本就应由 DI 报歧义。
        var (result, success) = GeneratorTestHost.RunGeneratorAndCompile<MviViewModelGenerator>(source
            .Replace("[MviFeature]", "")
            .Replace("public sealed partial class TestHandler : MviIntentHandler<TestState, TestIntent>", "public abstract partial class TestHandler : MviIntentHandler<TestState, TestIntent>")
            .Replace("public sealed partial class TestReducer : MviReducerBase<TestState>", "public abstract partial class TestReducer : MviReducerBase<TestState>"), GeneratorTestHost.FrameworkReferences);
        await Assert.That(result.GeneratedTrees.Length).IsEqualTo(2);
        await Assert.That(success).IsTrue();
    }
    /// <summary>纯 Reducer 不能用实例方法捕获服务依赖。</summary>
    [Test] public async Task InstanceReducerMethodProducesDiagnosticAsync()
    {
        var (_, errors) = GeneratorTestHost.CompileFramework(FeatureSource.Replace("private static TestState Apply", "private TestState Apply"));
        await Assert.That(errors.Any(error => error.Id == "MVI0201")).IsTrue();
    }
    /// <summary>不存在的输入参数在声明处报错。</summary>
    [Test] public async Task MissingInputPropertyProducesDiagnosticAsync()
    {
        var (_, errors) = GeneratorTestHost.CompileFramework(FeatureSource.Replace("nameof(Text), nameof(Password)", "\"Missing\", nameof(Password)"));
        await Assert.That(errors.Any(error => error.Id == "MVI0200")).IsTrue();
    }
    /// <summary>敏感状态投影被拒绝，不能悄悄把密码放入可观察状态。</summary>
    [Test] public async Task SensitiveStateProjectionProducesDiagnosticAsync()
    {
        var (_, errors) = GeneratorTestHost.CompileFramework(FeatureSource.Replace("[MviBind] public partial string Text { get; set; }", "[MviBind(Sensitive = true)] public partial string Text { get; }"));
        await Assert.That(errors.Any(error => error.Id == "MVI0200")).IsTrue();
    }
    /// <summary>声明状态来源的可写属性必须具有输入 Intent。</summary>
    [Test] public async Task WritableStateWithoutIntentIsRejectedAsync()
    {
        var (_, errors) = GeneratorTestHost.CompileFramework(FeatureSource.Replace("[MviBind] public partial string Text", "[MviBind(nameof(TestState.Text))] public partial string Text"));
        await Assert.That(errors.Any(error => error.Id == "MVI0200")).IsTrue();
    }
    /// <summary>同一 Feature 有两个 Reducer 时必须拒绝而不是按扫描顺序选择。</summary>
    [Test] public async Task AmbiguousReducerIsRejectedAsync()
    {
        string source = FeatureSource + """
            namespace FeatureTest
            {
                public sealed class OtherReducer : MiKiNuo.Mvi.Runtime.MVI.Reducer.MviReducerBase<TestState>
                { public override TestState Reduce(TestState state, MiKiNuo.Mvi.Abstractions.MVI.Mutation.IMviMutation<TestState> mutation) => state; }
            }
            """;
        var (_, errors) = GeneratorTestHost.CompileFramework(source);
        await Assert.That(errors.Any(error => error.Id == "MVI0202")).IsTrue();
    }
}
