using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 <c>MviEventSourceGenerator</c> 源生成器的契约测试。
/// <para>
/// 源生成器全量扫描编译中的 Avalonia / Godot 控件类型，为每个控件的每个 public 事件生成
/// <c>ToEventSource().EventName</c> 链式扩展方法，返回 <c>IEventSource&lt;TEvent&gt;</c>。
/// 对标 ReactiveUI 的 <c>ReactiveMarbles.ObservableEvents.SourceGenerator</c>，无白名单。
/// </para>
/// </summary>
public sealed class MviEventSourceGeneratorTests
{
    /// <summary>
    /// 验证源生成器文件存在且包含基本结构。
    /// </summary>
    [Test]
    public async Task GeneratorFile_Should_ExistWithBasicStructureAsync()
    {
        string root = FindRepositoryRoot();
        string generatorPath = Path.Combine(
            root,
            "src",
            "MiKiNuo.Mvi.Infrastructure",
            "BuildTime",
            "SourceGeneration",
            "MviEventSourceGenerator.cs");

        string content = await File.ReadAllTextAsync(generatorPath);

        await Assert.That(File.Exists(generatorPath)).IsTrue();
        await Assert.That(content).Contains("class MviEventSourceGenerator");
        await Assert.That(content).Contains("IIncrementalGenerator");
    }

    /// <summary>
    /// 验证生成器使用全量扫描而非硬编码白名单。
    /// </summary>
    [Test]
    public async Task Generator_Should_UseFullScanInsteadOfWhitelistAsync()
    {
        string root = FindRepositoryRoot();
        string generatorPath = Path.Combine(
            root,
            "src",
            "MiKiNuo.Mvi.Infrastructure",
            "BuildTime",
            "SourceGeneration",
            "MviEventSourceGenerator.cs");

        string content = await File.ReadAllTextAsync(generatorPath);

        // 应包含全量扫描逻辑
        await Assert.That(content).Contains("GlobalNamespace");
        await Assert.That(content).Contains("IEventSymbol");
        await Assert.That(content).Contains("GetMembers");

        // 不应包含硬编码白名单字段
        await Assert.That(content).DoesNotContain("AvaloniaWhitelist");
        await Assert.That(content).DoesNotContain("GodotWhitelist");
    }

    /// <summary>
    /// 验证生成器包含通用委托类型解析逻辑。
    /// </summary>
    [Test]
    public async Task Generator_Should_ContainDelegateInvokeMethodResolutionAsync()
    {
        string root = FindRepositoryRoot();
        string generatorPath = Path.Combine(
            root,
            "src",
            "MiKiNuo.Mvi.Infrastructure",
            "BuildTime",
            "SourceGeneration",
            "MviEventSourceGenerator.cs");

        string content = await File.ReadAllTextAsync(generatorPath);

        // 应通过 DelegateInvokeMethod 通用解析委托参数
        await Assert.That(content).Contains("DelegateInvokeMethod");
    }

    /// <summary>
    /// 验证生成器检测 Avalonia 和 Godot 平台类型。
    /// </summary>
    [Test]
    public async Task Generator_Should_DetectPlatformTypesAsync()
    {
        string root = FindRepositoryRoot();
        string generatorPath = Path.Combine(
            root,
            "src",
            "MiKiNuo.Mvi.Infrastructure",
            "BuildTime",
            "SourceGeneration",
            "MviEventSourceGenerator.cs");

        string content = await File.ReadAllTextAsync(generatorPath);

        await Assert.That(content).Contains("Avalonia.Controls.Control");
        await Assert.That(content).Contains("Godot.Control");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MiKiNuo.Mvi.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException("未找到解决方案根目录。");
        }

        return directory.FullName;
    }
}
