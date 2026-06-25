using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示规约器源生成器单/复数统一回归测试。
/// 框架只保留基于 <see cref="MiKiNuo.Mvi.Application.MVI.Reducer.MviReducerBase{TState, TIntent, TEffect}"/> 的
/// 经典 MVI 规约器模式；旧的静态 <c>*Reducers</c> 工具类 + <c>IMviReducerDispatcher</c> 分发器
/// 路径已废弃，相关类型和生成器必须全部清出框架与示例代码。
/// </summary>
public sealed class ReducerSourceGeneratorUnificationTests
{
    /// <summary>
    /// 验证静态 Reducers 分发器源生成器已被删除，避免开发误用已废弃的"以类名后缀复数识别"模式。
    /// </summary>
    [Test]
    public async Task PluralReducerDispatcherGenerator_Should_BeDeletedAsync()
    {
        string root = FindRepositoryRoot();
        string generatorPath = Path.Combine(
            root,
            "src",
            "MiKiNuo.Mvi.Infrastructure",
            "BuildTime",
            "SourceGeneration",
            "MviReducerDispatcherGenerator.cs");

        await Assert.That(File.Exists(generatorPath)).IsFalse();
    }

    /// <summary>
    /// 验证 <c>IMviReducerDispatcher</c> 接口已被删除。该接口的唯一生产者是
    /// <c>MviReducerDispatcherGenerator</c>，被删除后接口也变成零消费者死代码。
    /// </summary>
    [Test]
    public async Task ReducerDispatcherInterface_Should_BeDeletedAsync()
    {
        string root = FindRepositoryRoot();
        string interfacePath = Path.Combine(
            root,
            "src",
            "MiKiNuo.Mvi.Application",
            "MVI",
            "Reducer",
            "IMviReducerDispatcher.cs");

        await Assert.That(File.Exists(interfacePath)).IsFalse();
    }

    /// <summary>
    /// 验证示例与测试中没有任何 "Reducers" 复数后缀的规约类。
    /// 命名约定统一为单数 <c>*Reducer</c>，避免生成器误识别。
    /// </summary>
    [Test]
    public async Task SampleAndTestReducers_Should_UseSingularNamingAsync()
    {
        string root = FindRepositoryRoot();
        List<string> violations = [];

        foreach (string directory in new[]
                 {
                     Path.Combine(root, "sample"),
                     Path.Combine(root, "test"),
                 })
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (string sourcePath in Directory.EnumerateFiles(
                         directory,
                         "*.cs",
                         SearchOption.AllDirectories))
            {
                string source = await File.ReadAllTextAsync(sourcePath);
                foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(
                    source,
                    "(?:public|internal|private|sealed|partial|static|abstract|class|record)\\s+\\w*Reducers\\b"))
                {
                    violations.Add($"{Path.GetRelativePath(root, sourcePath)}: {match.Value}");
                }
            }
        }

        await Assert.That(violations).IsEmpty();
    }

    /// <summary>
    /// 验证 src 框架代码也不再依赖已废弃的 <c>IMviReducerDispatcher</c> 类型。
    /// </summary>
    [Test]
    public async Task FrameworkSource_Should_NotReferenceDispatcherInterfaceAsync()
    {
        string root = FindRepositoryRoot();
        List<string> violations = [];

        foreach (string directory in new[]
                 {
                     Path.Combine(root, "src"),
                 })
        {
            foreach (string sourcePath in Directory.EnumerateFiles(
                         directory,
                         "*.cs",
                         SearchOption.AllDirectories))
            {
                string source = await File.ReadAllTextAsync(sourcePath);
                if (source.Contains("IMviReducerDispatcher", StringComparison.Ordinal))
                {
                    violations.Add(Path.GetRelativePath(root, sourcePath));
                }
            }
        }

        await Assert.That(violations).IsEmpty();
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
