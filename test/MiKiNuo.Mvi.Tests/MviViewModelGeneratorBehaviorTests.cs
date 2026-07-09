using Microsoft.CodeAnalysis;
using MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 <c>MviViewModelGenerator</c> 源生成器的行为测试。
/// 使用 <c>CSharpGeneratorDriver</c> 驱动生成器并验证生成产物。
/// </summary>
public sealed class MviViewModelGeneratorBehaviorTests
{
    /// <summary>
    /// 验证含 [MviBind] 属性的 ViewModel
    /// 触发生成器产出绑定代码。
    /// </summary>
    /// <remarks>
    /// 使用生成树检查而非编译验证:ViewModel 生成代码
    /// 依赖完整的 MviViewModelBase/R3/ReactiveProperty 等运行时接口,
    /// 桩定义无法完整模拟,编译验证不可行。
    /// </remarks>
    [Test]
    public async Task Generate_Should_ProduceBindPropertyCodeAsync()
    {
        GeneratorDriverRunResult result =
            GeneratorTestHost.RunGenerator<MviViewModelGenerator>(
                StubDefinitions + "\n" + ViewModelWithBindSource);

        await Assert.That(result.GeneratedTrees.Length).IsEqualTo(1);
        string generatedCode = result.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("ApplyStateCore");
        await Assert.That(generatedCode).Contains("UserName");
    }

    /// <summary>
    /// 验证含 [MviCommand] 属性的 ViewModel
    /// 触发生成器产出命令代码。
    /// </summary>
    /// <remarks>
    /// 同上,桩定义无法完整模拟运行时接口。
    /// </remarks>
    [Test]
    public async Task Generate_Should_ProduceCommandPropertyCodeAsync()
    {
        GeneratorDriverRunResult result =
            GeneratorTestHost.RunGenerator<MviViewModelGenerator>(
                StubDefinitions + "\n" + ViewModelWithCommandSource);

        await Assert.That(result.GeneratedTrees.Length).IsEqualTo(1);
        string generatedCode = result.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("SubmitCommand");
        await Assert.That(generatedCode).Contains("OnConstructed");
    }

    /// <summary>
    /// 验证无 [MviBind] / [MviCommand] 的 ViewModel 不触发生成器。
    /// </summary>
    [Test]
    public async Task Generate_Should_NotProduceCode_ForViewModelWithoutAttributesAsync()
    {
        GeneratorDriverRunResult result = GeneratorTestHost.RunGenerator<MviViewModelGenerator>(
            StubDefinitions + "\n" + ViewModelWithoutAttributesSource);

        await Assert.That(result.GeneratedTrees.Length).IsEqualTo(0);
    }

    /// <summary>
    /// 桩类型定义：模拟 MVI 框架关键类型。
    /// </summary>
    private const string StubDefinitions = """
        namespace MiKiNuo.Mvi.Domain.MVI.Binding
        {
            [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
            public sealed class MviBindAttribute : System.Attribute
            {
                public MviBindAttribute(string stateProperty) { StateProperty = stateProperty; }
                public string StateProperty { get; }
                public MviBindingMode BindingMode { get; set; } = MviBindingMode.OneWay;
                public System.Type? IntentType { get; set; }
            }

            [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
            public sealed class MviCommandAttribute : System.Attribute
            {
                public MviCommandAttribute(System.Type intentType) { IntentType = intentType; }
                public System.Type IntentType { get; }
                public string? CanExecuteProperty { get; set; }
                public bool IsAsync { get; set; }
                public System.Type? PayloadType { get; set; }
            }

            public enum MviBindingMode { OneWay = 0, TwoWay = 1 }
        }

        namespace MiKiNuo.Mvi.Application.MVI.ViewModel
        {
            public abstract class MviViewModelBase<TState, TIntent, TEffect> { }
        }
        """;

    /// <summary>
    /// 测试源代码：含 [MviBind] 属性的 ViewModel。
    /// </summary>
    private const string ViewModelWithBindSource = """
        namespace TestApp
        {
            public sealed record TestState(string UserName);

            public sealed partial class TestViewModel
                : MiKiNuo.Mvi.Application.MVI.ViewModel.MviViewModelBase<TestState, object, object>
            {
                [MiKiNuo.Mvi.Domain.MVI.Binding.MviBind("UserName")]
                public string UserName { get; private set; } = string.Empty;
            }
        }
        """;

    /// <summary>
    /// 测试源代码：含 [MviCommand] 属性的 ViewModel。
    /// </summary>
    private const string ViewModelWithCommandSource = """
        namespace TestApp
        {
            public sealed record TestState;
            public sealed record TestIntent;

            public sealed partial class TestViewModel
                : MiKiNuo.Mvi.Application.MVI.ViewModel.MviViewModelBase<TestState, TestIntent, object>
            {
                [MiKiNuo.Mvi.Domain.MVI.Binding.MviCommand(typeof(TestIntent))]
                public object SubmitCommand { get; private set; } = new();
            }
        }
        """;

    /// <summary>
    /// 测试源代码：无 MVI 绑定特性的 ViewModel。
    /// </summary>
    private const string ViewModelWithoutAttributesSource = """
        namespace TestApp
        {
            public sealed record TestState;

            public sealed partial class TestViewModel
                : MiKiNuo.Mvi.Application.MVI.ViewModel.MviViewModelBase<TestState, object, object>
            {
                public string PlainProperty { get; } = string.Empty;
            }
        }
        """;
}
