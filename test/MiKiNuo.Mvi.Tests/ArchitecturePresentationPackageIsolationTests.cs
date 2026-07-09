using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using MiKiNuo.Mvi.Infrastructure.BuildTime.Analyzers;
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
    /// 验证 Presentation 项目引用 Avalonia 平台包时，
    /// 分析器产出 ARCH0009 诊断。
    /// </summary>
    [Test]
    public async Task ArchitectureAnalyzer_ReportsArch0009_WhenPresentationReferencesAvaloniaAsync()
    {
        ImmutableArray<Diagnostic> diagnostics = await RunArchitectureAnalyzerAsync(
            "MiKiNuo.Mvi.Presentation",
            MetadataReference.CreateFromFile(typeof(global::Avalonia.AvaloniaObject).Assembly.Location));

        bool hasArch0009 = diagnostics.Any(d => d.Id == "ARCH0009");
        await Assert.That(hasArch0009).IsTrue();
    }

    /// <summary>
    /// 验证 Presentation 项目不引用平台包时，
    /// 分析器不产出 ARCH0009 诊断。
    /// </summary>
    [Test]
    public async Task ArchitectureAnalyzer_DoesNotReportArch0009_WhenPresentationHasNoPlatformPackageAsync()
    {
        ImmutableArray<Diagnostic> diagnostics = await RunArchitectureAnalyzerAsync(
            "MiKiNuo.Mvi.Presentation");

        bool hasArch0009 = diagnostics.Any(d => d.Id == "ARCH0009");
        await Assert.That(hasArch0009).IsFalse();
    }

    /// <summary>
    /// 构造指定程序集名的编译并运行架构分析器，
    /// 返回分析器产出的诊断集合。
    /// </summary>
    /// <param name="assemblyName">被分析项目的程序集名。</param>
    /// <param name="extraReferences">额外的元数据引用。</param>
    /// <returns>分析器产出的诊断集合。</returns>
    private static async Task<ImmutableArray<Diagnostic>> RunArchitectureAnalyzerAsync(
        string assemblyName,
        params MetadataReference[] extraReferences)
    {
        CSharpParseOptions parseOptions = new(LanguageVersion.Preview);
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText("// empty", parseOptions);

        List<MetadataReference> references = new()
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        };
        references.AddRange(extraReferences);

        CSharpCompilationOptions options = new(
            OutputKind.DynamicallyLinkedLibrary,
            nullableContextOptions: NullableContextOptions.Enable);

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName,
            new[] { syntaxTree },
            references,
            options);

        CompilationWithAnalyzers analysis = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new MiKiNuoArchitectureAnalyzer()));

        return await analysis.GetAnalyzerDiagnosticsAsync();
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
