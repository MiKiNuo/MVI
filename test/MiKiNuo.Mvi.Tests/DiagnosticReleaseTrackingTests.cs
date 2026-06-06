using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 MVI 框架诊断 ID 发布追踪的回归测试。
/// 避免新增/修改诊断规则时漏更新 AnalyzerReleases.Shipped.md / Unshipped.md。
/// </summary>
public sealed class DiagnosticReleaseTrackingTests
{
    /// <summary>
    /// 验证 <c>DiagnosticIdCatalog</c> 列出的所有 ID 都被
    /// <c>AnalyzerReleases.Shipped.md</c> 与 <c>AnalyzerReleases.Unshipped.md</c> 覆盖。
    /// 任何一边的 ID 列表缺漏都会使本测试失败，强制把 ID 注册进发布追踪。
    /// </summary>
    [Test]
    public async Task DiagnosticIdCatalog_Should_BeCoveredByAnalyzerReleaseFilesAsync()
    {
        string root = FindRepositoryRoot();
        string shipped = await File.ReadAllTextAsync(Path.Combine(
            root,
            "src",
            "MiKiNuo.Mvi.Infrastructure",
            "AnalyzerReleases.Shipped.md"));
        string unshipped = await File.ReadAllTextAsync(Path.Combine(
            root,
            "src",
            "MiKiNuo.Mvi.Infrastructure",
            "AnalyzerReleases.Unshipped.md"));
        string catalog = await File.ReadAllTextAsync(Path.Combine(
            root,
            "src",
            "MiKiNuo.Mvi.Infrastructure",
            "BuildTime",
            "Diagnostics",
            "DiagnosticIdCatalog.cs"));

        HashSet<string> catalogIds = ExtractCatalogIds(catalog);
        HashSet<string> releasedIds = ExtractTableIds(shipped + Environment.NewLine + unshipped);

        List<string> missing = [];
        foreach (string id in catalogIds)
        {
            if (!releasedIds.Contains(id))
            {
                missing.Add(id);
            }
        }

        missing.Sort(StringComparer.Ordinal);
        await Assert.That(missing).IsEmpty();
    }

    /// <summary>
    /// 验证 <c>DiagnosticIdCatalog</c> 中不允许出现重复 ID。
    /// 一旦出现重复会让诊断规则出现歧义。
    /// </summary>
    [Test]
    public async Task DiagnosticIdCatalog_Should_NotContainDuplicatesAsync()
    {
        string root = FindRepositoryRoot();
        string catalog = await File.ReadAllTextAsync(Path.Combine(
            root,
            "src",
            "MiKiNuo.Mvi.Infrastructure",
            "BuildTime",
            "Diagnostics",
            "DiagnosticIdCatalog.cs"));

        List<string> ids = ExtractCatalogIds(catalog).ToList();
        HashSet<string> uniqueIds = new(ids, StringComparer.Ordinal);

        await Assert.That(ids.Count).IsEqualTo(uniqueIds.Count);
    }

    /// <summary>
    /// 验证 <c>AnalyzerReleases.Shipped.md</c> 与 <c>AnalyzerReleases.Unshipped.md</c> 合并后没有重复 ID。
    /// 同一规则不能同时在两份发布文件里登记，否则发布追踪会失去准确性。
    /// </summary>
    [Test]
    public async Task AnalyzerReleaseFiles_Should_NotListTheSameIdTwiceAsync()
    {
        string root = FindRepositoryRoot();
        string shipped = await File.ReadAllTextAsync(Path.Combine(
            root,
            "src",
            "MiKiNuo.Mvi.Infrastructure",
            "AnalyzerReleases.Shipped.md"));
        string unshipped = await File.ReadAllTextAsync(Path.Combine(
            root,
            "src",
            "MiKiNuo.Mvi.Infrastructure",
            "AnalyzerReleases.Unshipped.md"));

        List<string> released = ExtractTableIds(shipped + Environment.NewLine + unshipped).ToList();
        HashSet<string> unique = new(released, StringComparer.Ordinal);

        await Assert.That(released.Count).IsEqualTo(unique.Count);
    }

    /// <summary>
    /// 验证 4 个分析器/生成器文件全部通过 <c>DiagnosticIdCatalog</c> 读取 ID，
    /// 没有任何文件留下裸的诊断 ID 字符串。
    /// </summary>
    [Test]
    public async Task AnalyzersAndGenerators_Should_ReadIdsFromCatalogAsync()
    {
        string root = FindRepositoryRoot();
        string infrastructure = Path.Combine(root, "src", "MiKiNuo.Mvi.Infrastructure");
        string[] sourceFiles =
        [
            Path.Combine(infrastructure, "BuildTime", "Analyzers", "MiKiNuoArchitectureAnalyzer.cs"),
            Path.Combine(infrastructure, "BuildTime", "Analyzers", "MiKiNuoChineseDocumentationAnalyzer.cs"),
            Path.Combine(infrastructure, "BuildTime", "Analyzers", "MiKiNuoMicrosoftCodingAnalyzer.cs"),
            Path.Combine(infrastructure, "BuildTime", "SourceGeneration", "MviViewModelGenerator.cs"),
        ];

        List<string> violations = [];
        foreach (string path in sourceFiles)
        {
            string content = await File.ReadAllTextAsync(path);
            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(
                content,
                "id:\\s*\"(?<id>[A-Z]+\\d+)\""))
            {
                violations.Add($"{Path.GetFileName(path)}: {match.Groups["id"].Value}");
            }
        }

        await Assert.That(violations).IsEmpty();
    }

    private static HashSet<string> ExtractCatalogIds(string source)
    {
        HashSet<string> result = new(StringComparer.Ordinal);
        foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(
            source,
            "public const string (?<name>\\w+)\\s*=\\s*\"(?<id>[A-Z]+\\d+)\""))
        {
            result.Add(match.Groups["id"].Value);
        }

        return result;
    }

    private static HashSet<string> ExtractTableIds(string markdown)
    {
        HashSet<string> result = new(StringComparer.Ordinal);
        foreach (string line in markdown.Split('\n'))
        {
            string trimmed = line.Trim();
            int pipeIndex = trimmed.IndexOf('|');
            if (pipeIndex <= 0)
            {
                continue;
            }

            string firstCell = trimmed.Substring(0, pipeIndex).Trim();
            if (System.Text.RegularExpressions.Regex.IsMatch(firstCell, "^[A-Z]+\\d+$"))
            {
                result.Add(firstCell);
            }
        }

        return result;
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
