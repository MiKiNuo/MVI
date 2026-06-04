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

    /// <summary>
    /// 验证 Presentation 抽象层不直接引用具体 UI 平台包。
    /// </summary>
    [Test]
    public async Task PresentationProject_Should_NotReferenceConcretePlatformPackagesAsync()
    {
        string root = FindRepositoryRoot();
        string projectPath = Path.Combine(root, "src", "MiKiNuo.Mvi.Presentation", "MiKiNuo.Mvi.Presentation.csproj");
        string project = await File.ReadAllTextAsync(projectPath);

        await Assert.That(project.Contains("PackageReference Include=\"Avalonia\"", StringComparison.Ordinal)).IsFalse();
        await Assert.That(project.Contains("PackageReference Include=\"GodotSharp\"", StringComparison.Ordinal)).IsFalse();
    }

    /// <summary>
    /// 验证 Dashboard 子组件副作用分发器通过共享交互分发器请求中介者。
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
    /// 验证框架源生成器不包含示例项目专属命名空间或示例生成类型。
    /// </summary>
    [Test]
    public async Task InfrastructureSourceGenerators_Should_NotReferenceSampleSpecificCodeAsync()
    {
        string root = FindRepositoryRoot();
        string sourceGenerationDirectory = Path.Combine(
            root,
            "src",
            "MiKiNuo.Mvi.Infrastructure",
            "BuildTime",
            "SourceGeneration");

        List<string> sampleSpecificFiles = [];
        foreach (string sourcePath in Directory.EnumerateFiles(
                     sourceGenerationDirectory,
                     "*.cs",
                     SearchOption.AllDirectories))
        {
            string source = await File.ReadAllTextAsync(sourcePath);
            if (source.Contains("MiKiNuo.Mvi.Samples.", StringComparison.Ordinal)
                || source.Contains("SampleGeneratedContainer", StringComparison.Ordinal)
                || source.Contains("SampleGeneratedViewRegistry", StringComparison.Ordinal)
                || source.Contains("MviAvaloniaView", StringComparison.Ordinal)
                || source.Contains("\"Sample\"", StringComparison.Ordinal)
                || source.Contains("SampleCompositionPrefix", StringComparison.Ordinal)
                || source.Contains("GetCompositionPrefix", StringComparison.Ordinal))
            {
                sampleSpecificFiles.Add(Path.GetRelativePath(root, sourcePath));
            }
        }

        await Assert.That(sampleSpecificFiles).IsEmpty();
    }

    /// <summary>
    /// 验证示例专属的组合根生成器已被拆分到 sample/MiKiNuo.Mvi.Samples.Avalonia.BuildTime，
    /// 不再寄居在框架 Infrastructure 层。
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
