using Microsoft.CodeAnalysis;
using MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 <c>MviEventSourceGenerator</c> 源生成器的行为测试。
/// 使用 <c>CSharpGeneratorDriver</c> 驱动生成器并验证生成产物。
/// </summary>
public sealed class MviEventSourceGeneratorBehaviorTests
{
    /// <summary>
    /// 验证含继承 Avalonia.Control 的类型触发生成器产出扩展方法。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceEventSourceExtensionsAsync()
    {
        GeneratorDriverRunResult result = GeneratorTestHost.RunGenerator<MviEventSourceGenerator>(
            AvaloniaControlStub + "\n" + CustomControlSource);

        await Assert.That(result.GeneratedTrees.Length).IsGreaterThan(0);

        string generatedCode = result.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("ToEventSource");
    }

    /// <summary>
    /// 验证生成的代码包含事件源适配器类。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceEventSourceAdapterClassAsync()
    {
        GeneratorDriverRunResult result = GeneratorTestHost.RunGenerator<MviEventSourceGenerator>(
            AvaloniaControlStub + "\n" + CustomControlSource);

        string generatedCode = result.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("CustomButtonEventSourceAdapter");
    }

    /// <summary>
    /// 验证生成的代码包含事件属性。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceEventPropertyAsync()
    {
        GeneratorDriverRunResult result = GeneratorTestHost.RunGenerator<MviEventSourceGenerator>(
            AvaloniaControlStub + "\n" + CustomControlSource);

        string generatedCode = result.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("Click");
        await Assert.That(generatedCode).Contains("Pressed");
    }

    /// <summary>
    /// 验证生成的代码包含 Avalonia 扩展方法类。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceAvaloniaExtensionsClassAsync()
    {
        GeneratorDriverRunResult result = GeneratorTestHost.RunGenerator<MviEventSourceGenerator>(
            AvaloniaControlStub + "\n" + CustomControlSource);

        string generatedCode = result.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("MviAvaloniaEventSourceExtensions");
    }

    /// <summary>
    /// 验证无 Avalonia.Control 类型的编译不触发生成器。
    /// </summary>
    [Test]
    public async Task Generate_Should_NotProduceCode_ForCompilationWithoutControlAsync()
    {
        GeneratorDriverRunResult result = GeneratorTestHost.RunGenerator<MviEventSourceGenerator>(
            PlainSource);

        await Assert.That(result.GeneratedTrees.Length).IsEqualTo(0);
    }

    /// <summary>
    /// 桩类型定义：模拟 Avalonia.Control 控件基类。
    /// </summary>
    private const string AvaloniaControlStub = """
        namespace Avalonia.Controls
        {
            public class Control
            {
                public event System.EventHandler? Click;
            }
        }
        """;

    /// <summary>
    /// 测试源代码：含继承 Avalonia.Control 的自定义控件。
    /// </summary>
    private const string CustomControlSource = """
        namespace TestApp
        {
            public sealed class CustomButton : Avalonia.Controls.Control
            {
                public event System.EventHandler<System.EventArgs>? Pressed;
            }
        }
        """;

    /// <summary>
    /// 测试源代码：无控件类型的普通类。
    /// </summary>
    private const string PlainSource = """
        namespace TestApp
        {
            public sealed class PlainClass
            {
            }
        }
        """;
}
