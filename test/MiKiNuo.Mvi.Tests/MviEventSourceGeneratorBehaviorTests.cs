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
    /// 验证含继承 Avalonia.Control 的类型
    /// 触发生成器产出可编译的扩展方法。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceCompilableEventSourceExtensionsAsync()
    {
        (GeneratorDriverRunResult runResult, bool emitSuccess) =
            GeneratorTestHost.RunGeneratorAndCompile<MviEventSourceGenerator>(
                AvaloniaControlStub + "\n" + CustomControlSource);

        await Assert.That(emitSuccess).IsTrue();
        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(1);
    }

    /// <summary>
    /// 验证生成的事件源适配器类可成功编译。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceCompilableEventSourceAdapterClassAsync()
    {
        (GeneratorDriverRunResult runResult, bool emitSuccess) =
            GeneratorTestHost.RunGeneratorAndCompile<MviEventSourceGenerator>(
                AvaloniaControlStub + "\n" + CustomControlSource);

        await Assert.That(emitSuccess).IsTrue();
        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(1);
    }

    /// <summary>
    /// 验证生成的事件属性代码可成功编译。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceCompilableEventPropertyAsync()
    {
        (GeneratorDriverRunResult runResult, bool emitSuccess) =
            GeneratorTestHost.RunGeneratorAndCompile<MviEventSourceGenerator>(
                AvaloniaControlStub + "\n" + CustomControlSource);

        await Assert.That(emitSuccess).IsTrue();
        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(1);
    }

    /// <summary>
    /// 验证生成的 Avalonia 扩展方法类可成功编译。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceCompilableAvaloniaExtensionsClassAsync()
    {
        (GeneratorDriverRunResult runResult, bool emitSuccess) =
            GeneratorTestHost.RunGeneratorAndCompile<MviEventSourceGenerator>(
                AvaloniaControlStub + "\n" + CustomControlSource);

        await Assert.That(emitSuccess).IsTrue();
        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(1);
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
    /// 桩类型定义：模拟 Avalonia.Control 控件基类与应用层事件绑定接口。
    /// </summary>
    private const string AvaloniaControlStub = """
        namespace Avalonia.Controls
        {
            public class Control
            {
                public event System.EventHandler? Click;
            }
        }

        namespace MiKiNuo.Mvi.Application.MVI.EventBinding
        {
            public interface IEventSource<out TEvent>
            {
                System.IDisposable Subscribe(System.Action<TEvent> handler);
            }

            public sealed class DelegateEventSource<TEvent> : IEventSource<TEvent>
            {
                private readonly System.Func<System.Action<TEvent>, System.IDisposable> _subscribeFunc;

                public DelegateEventSource(System.Func<System.Action<TEvent>, System.IDisposable> subscribeFunc)
                {
                    _subscribeFunc = subscribeFunc;
                }

                public System.IDisposable Subscribe(System.Action<TEvent> handler)
                {
                    return _subscribeFunc(handler);
                }
            }

            public sealed class ActionDisposable : System.IDisposable
            {
                private System.Action? _disposeAction;

                public ActionDisposable(System.Action disposeAction)
                {
                    _disposeAction = disposeAction;
                }

                public void Dispose()
                {
                    _disposeAction?.Invoke();
                    _disposeAction = null;
                }
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
