using Microsoft.CodeAnalysis;
using MiKiNuo.Mvi.Generators.SourceGeneration;
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
                CustomControlSource, AvaloniaReferences);

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
                CustomControlSource, AvaloniaReferences);

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
                CustomControlSource, AvaloniaReferences);

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
                CustomControlSource, AvaloniaReferences);

        await Assert.That(emitSuccess).IsTrue();
        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(1);
    }

    /// <summary>无平台引用时不生成事件适配器，且原始源码仍可编译。</summary>
    [Test]
    public async Task Generate_Should_NotProduceCode_ForCompilationWithoutControlAsync()
    {
        (GeneratorDriverRunResult result, bool emitSuccess) =
            GeneratorTestHost.RunGeneratorAndCompile<MviEventSourceGenerator>(PlainSource);

        await Assert.That(result.GeneratedTrees.Length).IsEqualTo(0);
        await Assert.That(emitSuccess).IsTrue();
    }

    /// <summary>默认编译不可见测试进程携带的框架和平台类型。</summary>
    [Test]
    public async Task CreateCompilation_Should_ExcludeImplicitFrameworkAndPlatformReferencesAsync()
    {
        Compilation compilation = GeneratorTestHost.CreateCompilation(PlainSource);

        await Assert.That(compilation.GetSpecialType(SpecialType.System_Object).TypeKind).IsEqualTo(TypeKind.Class);
        await Assert.That(compilation.GetTypeByMetadataName("Avalonia.Controls.Control")).IsNull();
        await Assert.That(compilation.GetTypeByMetadataName("Godot.Control")).IsNull();
        await Assert.That(compilation.GetTypeByMetadataName("MiKiNuo.Mvi.Abstractions.MVI.State.IMviState")).IsNull();
        await Assert.That(compilation.GetTypeByMetadataName("MiKiNuo.Mvi.Binding.EventBinding.IEventSource`1")).IsNull();
        await Assert.That(compilation.GetTypeByMetadataName("R3.Observable")).IsNull();
    }

    /// <summary>显式框架引用本身不应隐式引入平台控件。</summary>
    [Test]
    public async Task Generate_Should_NotProduceCode_ForFrameworkOnlyCompilationAsync()
    {
        (GeneratorDriverRunResult result, bool emitSuccess) =
            GeneratorTestHost.RunGeneratorAndCompile<MviEventSourceGenerator>(PlainSource, GeneratorTestHost.FrameworkReferences);

        await Assert.That(result.GeneratedTrees.Length).IsEqualTo(0);
        await Assert.That(emitSuccess).IsTrue();
    }

    /// <summary>无源码控件时仍扫描显式平台引用，生成的真实事件源调用可编译。</summary>
    [Test]
    public async Task Generate_Should_CompileEventSource_FromExplicitPlatformReferenceAsync()
    {
        const string source = """
            using MiKiNuo.Mvi.Platforms.Avalonia.Events;

            namespace TestApp
            {
                public static class EventConsumer
                {
                    public static MiKiNuo.Mvi.Binding.EventBinding.IEventSource<Avalonia.Interactivity.RoutedEventArgs>
                        Clicks(Avalonia.Controls.Button button) => button.ToEventSource().Click;
                }
            }
            """;
        (GeneratorDriverRunResult result, bool emitSuccess) =
            GeneratorTestHost.RunGeneratorAndCompile<MviEventSourceGenerator>(source, AvaloniaReferences);

        await Assert.That(result.GeneratedTrees.Length).IsEqualTo(1);
        await Assert.That(result.Results.Single().GeneratedSources.Single().HintName).IsEqualTo("MviAvaloniaEventSourceExtensions.g.cs");
        await Assert.That(emitSuccess).IsTrue();
    }

    /// <summary>友元程序集使用本程序集生成的事件适配器。</summary>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task FriendAssembliesUseTheirOwnEventAdaptersAsync(bool godot)
    {
        string suffix = Guid.NewGuid().ToString("N");
        string nameA = "Events-A" + suffix, nameB = "Events_A" + suffix;
        string platform = godot ? "Godot.Control" : "Avalonia.Controls.Control";
        string imports = godot ? "MiKiNuo.Mvi.Platforms.Godot.Binding" : "MiKiNuo.Mvi.Platforms.Avalonia.Events";
        MetadataReference[] references = godot ? GeneratorTestHost.FrameworkReferences : AvaloniaReferences;
        string sourceA = "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"" + nameB + "\")]\n"
            + (godot ? "namespace Godot { public class Control { } }" : "")
            + "namespace Shared { public sealed class FriendButton : " + platform + " { public event System.Action? Click; public void Raise() => Click?.Invoke(); } internal static class Probe { internal static bool Value => true; } }";
        var inputA = GeneratorTestHost.CreateCompilation(sourceA, references).WithAssemblyName(nameA);
        var driverA = Microsoft.CodeAnalysis.CSharp.CSharpGeneratorDriver.Create(
            [new MviEventSourceGenerator().AsSourceGenerator()], parseOptions: (Microsoft.CodeAnalysis.CSharp.CSharpParseOptions)inputA.SyntaxTrees.First().Options);
        driverA.RunGeneratorsAndUpdateCompilation(inputA, out Compilation outputA, out _);
        using MemoryStream imageA = new();
        var emittedA = outputA.Emit(imageA);
        await Assert.That(string.Join("\n", emittedA.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))).IsEqualTo("");
        string sourceB = "using " + imports + ";\n" + """
            public static class InstanceProbe
            {
                public static bool Run()
                {
                    var button = new Shared.FriendButton();
                    var adapter = button.ToEventSource();
                    _ = adapter.Click;
                    return Shared.Probe.Value && adapter.GetType().Assembly == typeof(InstanceProbe).Assembly;
                }
            }
            """;
        var inputB = GeneratorTestHost.CreateCompilation(sourceB, references)
            .AddReferences(MetadataReference.CreateFromImage(imageA.ToArray())).WithAssemblyName(nameB);
        var driverB = Microsoft.CodeAnalysis.CSharp.CSharpGeneratorDriver.Create(
            [new MviEventSourceGenerator().AsSourceGenerator()], parseOptions: (Microsoft.CodeAnalysis.CSharp.CSharpParseOptions)inputB.SyntaxTrees.First().Options);
        driverB.RunGeneratorsAndUpdateCompilation(inputB, out Compilation outputB, out _);
        using MemoryStream imageB = new();
        var emittedB = outputB.Emit(imageB);
        await Assert.That(string.Join("\n", emittedB.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error || d.Id == "CS0436" || d.Id == "CS0121"))).IsEqualTo("");
        var context = new System.Runtime.Loader.AssemblyLoadContext(nameB, isCollectible: true);
        try
        {
            imageA.Position = 0; imageB.Position = 0;
            context.LoadFromStream(imageA);
            var assembly = context.LoadFromStream(imageB);
            await Assert.That((bool)assembly.GetType("InstanceProbe")!.GetMethod("Run")!.Invoke(null, null)!).IsTrue();
        }
        finally { context.Unload(); }
    }

    /// <summary>仅显式提供真实 MVI 框架和 Avalonia 控件依赖。</summary>
    private static MetadataReference[] AvaloniaReferences =>
    [
        .. GeneratorTestHost.FrameworkReferences,
        MetadataReference.CreateFromFile(typeof(Avalonia.AvaloniaObject).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Avalonia.Controls.Control).Assembly.Location),
    ];

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
