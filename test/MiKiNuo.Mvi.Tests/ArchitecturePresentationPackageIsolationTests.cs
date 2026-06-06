using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 ARCH0009（Presentation 平台包隔离）分析器回归测试。
/// 验证 <c>MiKiNuoArchitectureAnalyzer</c> 在 Presentation 项目的编译上下文里，
/// 会针对 <c>Avalonia.*</c>、<c>Godot*</c> 等具体平台包触发
/// <c>ArchPresentationPackageIsolation</c> 规则（位于 test 外部的 Infrastructure 分析器内）。
/// </summary>
public sealed class ArchitecturePresentationPackageIsolationTests
{
    /// <summary>
    /// 验证 <c>DiagnosticIdCatalog</c> 已经登记 <c>ArchPresentationPackageIsolation</c>，且 ID 串为 <c>ARCH0009</c>。
    /// </summary>
    [Test]
    public async Task PresentationPackageIsolation_Should_BeRegisteredInCatalogAsync()
    {
        string root = FindRepositoryRoot();
        string catalogPath = Path.Combine(
            root,
            "src",
            "MiKiNuo.Mvi.Infrastructure",
            "BuildTime",
            "Diagnostics",
            "DiagnosticIdCatalog.cs");

        string catalog = await File.ReadAllTextAsync(catalogPath);

        await Assert.That(catalog).Contains("ArchPresentationPackageIsolation");
        await Assert.That(catalog).Contains("\"ARCH0009\"");
        await Assert.That(catalog).Contains("ArchPresentationPackageIsolation,");
    }

    /// <summary>
    /// 验证 <c>MiKiNuoArchitectureAnalyzer</c> 的 <c>SupportedDiagnostics</c> 已经把
    /// <c>ArchPresentationPackageIsolation</c> 规则挂在 <c>PresentationReferencePlatformRule</c> 之外。
    /// </summary>
    [Test]
    public async Task ArchitectureAnalyzer_Should_ExposePresentationPackageIsolationRuleAsync()
    {
        string root = FindRepositoryRoot();
        string analyzerPath = Path.Combine(
            root,
            "src",
            "MiKiNuo.Mvi.Infrastructure",
            "BuildTime",
            "Analyzers",
            "MiKiNuoArchitectureAnalyzer.cs");

        string analyzer = await File.ReadAllTextAsync(analyzerPath);

        // 规则 ID 来自 DiagnosticIdCatalog，而非裸字符串
        await Assert.That(analyzer).Contains("DiagnosticIdCatalog.ArchPresentationPackageIsolation");
        // 平台包判定逻辑（同时覆盖 Avalonia 与 Godot 两个家族）
        await Assert.That(analyzer).Contains("IsConcretePlatformPackage");
        await Assert.That(analyzer).Contains("\"Avalonia\"");
        await Assert.That(analyzer).Contains("\"Godot\"");
    }

    /// <summary>
    /// 验证 <c>AnalyzerReleases.Unshipped.md</c> 已经记录 <c>ARCH0009</c> 规则，
    /// 防止分析器实现后忘记同步发布追踪。
    /// </summary>
    [Test]
    public async Task PresentationPackageIsolationRule_Should_BeBackedByReleaseFileAsync()
    {
        string root = FindRepositoryRoot();
        string unshipped = await File.ReadAllTextAsync(Path.Combine(
            root,
            "src",
            "MiKiNuo.Mvi.Infrastructure",
            "AnalyzerReleases.Unshipped.md"));

        await Assert.That(unshipped).Contains("ARCH0009");
        await Assert.That(unshipped).Contains("Presentation");
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
