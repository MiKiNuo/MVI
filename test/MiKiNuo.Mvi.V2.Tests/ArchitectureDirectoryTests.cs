using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示目录架构回归测试。
/// <para>
/// 仅承载 Roslyn 分析器无法表达的"文件系统级"约束：
/// 顶层目录布局、示例项目数量与唯一 v2 项目边界等。
/// </para>
/// </summary>
public sealed class ArchitectureDirectoryTests
{
    /// <summary>
    /// 验证顶层目录严格为 src、test、sample。
    /// Roslyn 分析器只看到 Compilation，无法表达文件系统布局，保留为运行期检查。
    /// </summary>
    [Test]
    public async Task Repository_Should_UseSrcTestSampleFoldersAsync()
    {
        string root = FindRepositoryRoot();

        await Assert.That(Directory.Exists(Path.Combine(root, "src"))).IsTrue();
        await Assert.That(Directory.Exists(Path.Combine(root, "test"))).IsTrue();
        await Assert.That(Directory.Exists(Path.Combine(root, "sample"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(root, "MiKiNuo.Mvi.slnx"))).IsTrue();
    }

    /// <summary>
    /// 验证示例目录只保留 Avalonia 联网示例与 Godot HUD 示例，
    /// 且不存在示例专属的构建期生成器项目（业务入口由 Feature 子类型生成器提供）。
    /// </summary>
    [Test]
    public async Task Sample_Should_OnlyContainSupportedPlatformSamplesAsync()
    {
        string root = FindRepositoryRoot();
        string sampleRoot = Path.Combine(root, "sample");

        List<string> projectDirectories = Directory
            .EnumerateDirectories(sampleRoot)
            .Select(Path.GetFileName)
            .Where(static name => name is not null)
            .Cast<string>()
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToList();

        await Assert.That(projectDirectories)
            .IsEquivalentTo(new[] { "MiKiNuo.Mvi.Samples.Avalonia", "MiKiNuo.Mvi.Samples.Godot" });
        await Assert.That(Directory.Exists(Path.Combine(sampleRoot, "MiKiNuo.Mvi.Samples.Avalonia.BuildTime")))
            .IsFalse();
    }

    /// <summary>验证默认源码树只保留 v2 核心、生成器与两个平台项目。</summary>
    /// <returns>项目边界验证任务。</returns>
    [Test]
    public async Task Source_Should_OnlyContainV2ProjectsAsync()
    {
        string root = FindRepositoryRoot();
        string[] projects = Directory.EnumerateFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension).Order(StringComparer.Ordinal).ToArray()!;
        await Assert.That(projects).IsEquivalentTo(new[]
        {
            "MiKiNuo.Mvi", "MiKiNuo.Mvi.Avalonia", "MiKiNuo.Mvi.Generators", "MiKiNuo.Mvi.Godot"
        });
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
