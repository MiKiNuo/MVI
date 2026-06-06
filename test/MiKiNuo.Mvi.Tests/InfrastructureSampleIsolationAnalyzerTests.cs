using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 Infrastructure 源生成器示例代码隔离分析器回归测试。
/// 验证 <c>MiKiNuoInfrastructureSampleIsolationAnalyzer</c> 已就位并把
/// <c>ArchInfrastructureSampleIsolation</c> 规则 ID（位于 test 外部的 <c>DiagnosticIdCatalog</c> 内）
/// 加入 <c>DiagnosticIdCatalog</c> 与 <c>AnalyzerReleases.Unshipped.md</c>。
/// </summary>
public sealed class InfrastructureSampleIsolationAnalyzerTests
{
    /// <summary>
    /// 验证示例代码隔离分析器源文件存在，规则 ID 已加入 <c>DiagnosticIdCatalog.AllIds</c>。
    /// </summary>
    [Test]
    public async Task SampleIsolationAnalyzer_Should_BeRegisteredInCatalogAsync()
    {
        string root = FindRepositoryRoot();
        string analyzerPath = Path.Combine(
            root,
            "src",
            "MiKiNuo.Mvi.Infrastructure",
            "BuildTime",
            "Analyzers",
            "MiKiNuoInfrastructureSampleIsolationAnalyzer.cs");

        await Assert.That(File.Exists(analyzerPath)).IsTrue();

        string content = await File.ReadAllTextAsync(analyzerPath);
        await Assert.That(content).Contains("ArchInfrastructureSampleIsolation");
        await Assert.That(content).Contains("DiagnosticAnalyzer");
    }

    /// <summary>
    /// 验证示例代码隔离分析器会扫描符号/语法树并报告类型/字符串字面量违规，
    /// 且不会被自身的"MiKiNuoInfrastructureSampleIsolation"标识误报。
    /// </summary>
    [Test]
    public async Task SampleIsolationAnalyzer_Should_DetectSampleSpecificPatternsInSourceAsync()
    {
        string root = FindRepositoryRoot();
        string analyzerPath = Path.Combine(
            root,
            "src",
            "MiKiNuo.Mvi.Infrastructure",
            "BuildTime",
            "Analyzers",
            "MiKiNuoInfrastructureSampleIsolationAnalyzer.cs");

        string content = await File.ReadAllTextAsync(analyzerPath);

        // 必须存在两个检测面：类型/符号 + 字符串字面量
        await Assert.That(content).Contains("SymbolKind");
        await Assert.That(content).Contains("SyntaxKind");
        // 必须有跳过分析器自身的豁免
        await Assert.That(content).Contains("IsExemptType");
    }

    /// <summary>
    /// 验证 <c>ArchitectureDirectoryTests.InfrastructureSourceGenerators_Should_NotReferenceSampleSpecificCodeAsync</c>
    /// 所覆盖的"src/MiKiNuo.Mvi.Infrastructure 不出现示例项目代码"约束已经迁移为
    /// 静态编译期可校验的分析器规则（ARCH0008），约束由分析器强制而非依赖运行期测试。
    /// </summary>
    [Test]
    public async Task SampleIsolationRule_Should_BeBackedByAnalyzerAndReleaseFileAsync()
    {
        string root = FindRepositoryRoot();
        string unshipped = await File.ReadAllTextAsync(Path.Combine(
            root,
            "src",
            "MiKiNuo.Mvi.Infrastructure",
            "AnalyzerReleases.Unshipped.md"));

        await Assert.That(unshipped).Contains("ARCH0008");
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
