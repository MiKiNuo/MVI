using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示目录架构测试。
/// </summary>
public sealed class ArchitectureDirectoryTests
{
    /// <summary>
    /// 验证顶层目录严格为 src、test、sample。
    /// </summary>
    [Test]
    public async Task Repository_Should_UseSrcTestSampleFoldersAsync()
    {
        string testProjectDirectory = AppContext.BaseDirectory;
        DirectoryInfo? directory = new DirectoryInfo(testProjectDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MiKiNuo.Mvi.slnx")))
        {
            directory = directory.Parent;
        }

        await Assert.That(directory).IsNotNull();
        string root = directory!.FullName;

        await Assert.That(Directory.Exists(Path.Combine(root, "src"))).IsTrue();
        await Assert.That(Directory.Exists(Path.Combine(root, "test"))).IsTrue();
        await Assert.That(Directory.Exists(Path.Combine(root, "sample"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(root, "MiKiNuo.Mvi.slnx"))).IsTrue();
    }
}
