﻿﻿﻿using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示目录架构回归测试。
/// <para>
/// 仅承载 Roslyn 分析器无法表达的"文件系统级"约束：
/// 顶层目录布局、示例项目专属源文件存在性等。
/// 凡是能在 Compilation 上下文里表达的项目引用 / 包引用 / 命名空间反向渗透，
/// 都已迁移到 <c>MiKiNuoArchitectureAnalyzer</c> 与
/// <c>MiKiNuoInfrastructureSampleIsolationAnalyzer</c>（ARCH0007-0009 规则），
/// 不再依赖运行期文件扫描。
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

    /// <summary>
    /// 验证 Dashboard 子组件副作用分发器通过共享交互分发器请求中介者。
    /// 该约束属于示例项目业务代码模式，留在示例侧或保留运行期测试，
    /// 框架分析器不感知示例项目类型。
    /// </summary>
    [Test]
    public async Task DashboardEffectDispatchers_Should_UseSharedInteractionDispatcherAsync()
    {
        string root = FindRepositoryRoot();
        string dashboardDirectory = Path.Combine(
            root,
            "sample",
            "MiKiNuo.Mvi.Samples.Avalonia",
            "Features",
            "Dashboard");
        string interactionDispatcher = Path.Combine(
            dashboardDirectory,
            "Mediator",
            "DashboardComponentInteractionDispatcher.cs");

        await Assert.That(File.Exists(interactionDispatcher)).IsTrue();

        List<string> directMediatorFiles = [];
        foreach (string dispatcherPath in Directory.EnumerateFiles(
                     dashboardDirectory,
                     "*EffectDispatcher.cs",
                     SearchOption.AllDirectories))
        {
            if (dispatcherPath.Contains($"{Path.DirectorySeparatorChar}Mediator{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            string dispatcher = await File.ReadAllTextAsync(dispatcherPath);
            if (dispatcher.Contains("SendAsync<DashboardComponentInteractionRequest", StringComparison.Ordinal)
                || dispatcher.Contains("new DashboardComponentInteractionRequest", StringComparison.Ordinal))
            {
                directMediatorFiles.Add(Path.GetRelativePath(root, dispatcherPath));
            }
        }

        await Assert.That(directMediatorFiles).IsEmpty();
    }

    /// <summary>
    /// 验证示例专属的组合根生成器已被拆分到 sample/MiKiNuo.Mvi.Samples.Avalonia.BuildTime，
    /// 不再寄居在框架 Infrastructure 层。
    /// 这是一项"文件迁移"事实检查，分析器无法表达文件存在性，保留为运行期回归。
    /// </summary>
    [Test]
    public async Task AvaloniaSampleBuildTimeProject_Should_HostSampleCompositionGeneratorAsync()
    {
        string root = FindRepositoryRoot();
        string generatorPath = Path.Combine(
            root,
            "sample",
            "MiKiNuo.Mvi.Samples.Avalonia.BuildTime",
            "SourceGeneration",
            "DiContainerGenerator.cs");
        string oldGeneratorPath = Path.Combine(
            root,
            "src",
            "MiKiNuo.Mvi.Infrastructure",
            "BuildTime",
            "SourceGeneration",
            "DiContainerGenerator.cs");

        await Assert.That(File.Exists(generatorPath)).IsTrue();
        await Assert.That(File.Exists(oldGeneratorPath)).IsFalse();

        string generator = await File.ReadAllTextAsync(generatorPath);
        await Assert.That(generator).Contains("AvaloniaSampleDiContainerGenerator");
        await Assert.That(generator).Contains("MiKiNuo.Mvi.Samples.Avalonia.BuildTime.SourceGeneration");
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
