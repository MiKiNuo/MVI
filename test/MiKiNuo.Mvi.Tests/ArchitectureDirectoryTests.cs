using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示目录架构回归测试。
/// <para>
/// 仅承载 Roslyn 分析器无法表达的"文件系统级"约束：
/// 顶层目录布局、示例项目数量与专属构建期工具的缺失等。
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
    /// 验证示例目录只保留唯一的联网登录注册示例，
    /// 且不存在示例专属的构建期生成器项目（装配能力已收敛到框架 [MviFeature] 生成器）。
    /// </summary>
    [Test]
    public async Task Sample_Should_OnlyContainUnifiedAvaloniaSampleAsync()
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
            .IsEquivalentTo(new[] { "MiKiNuo.Mvi.Samples.Avalonia" });
        await Assert.That(Directory.Exists(Path.Combine(sampleRoot, "MiKiNuo.Mvi.Samples.Avalonia.BuildTime")))
            .IsFalse();
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
