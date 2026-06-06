using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示源生成器文件布局回归测试。
/// <para>
/// 历史上 MVI 框架存在一个超薄入口类（<c>MviDiContainerGenerator</c> / <c>MviViewModelGenerator</c>）
/// 单独成文件的形态，入口类只调用 <c>Analysis.Discover</c> 与 <c>Emission.GenerateContainerSource</c>，
/// 整个文件 30 行左右，与其分析、发射同源实现割裂在不同文件，增加跳转成本。
/// </para>
/// <para>
/// 合并后入口类与分析同处一个文件，发射/模型另成文件；保持 Roslyn
/// <c>IIncrementalGenerator</c> 入口公共 API 不变。
/// </para>
/// </summary>
public sealed class SourceGeneratorFileLayoutTests
{
    /// <summary>
    /// 验证 DI 容器入口生成器与分析阶段已合并到同一源文件，源生成器入口不再单独成文件。
    /// 分析逻辑以嵌套 <c>Analysis</c> 类的形式承载，源文件只暴露入口 + 嵌套职责。
    /// </summary>
    [Test]
    public async Task DiContainerGenerator_AndAnalysis_Should_ShareSingleFileAsync()
    {
        string root = FindRepositoryRoot();
        string[] sourceFiles = Directory.GetFiles(
            Path.Combine(root, "src", "MiKiNuo.Mvi.Infrastructure", "BuildTime", "SourceGeneration"),
            "*.cs",
            SearchOption.TopDirectoryOnly);

        string? generatorFile = FindFileDeclaringTopLevelType(sourceFiles, "MviDiContainerGenerator");
        string? separateAnalysisFile = FindFileDeclaringTopLevelType(sourceFiles, "MviDiContainerAnalysis");

        await Assert.That(generatorFile).IsNotNull();
        await Assert.That(separateAnalysisFile).IsNull();
        await Assert.That(File.ReadAllText(generatorFile!)).Contains("class Analysis");
    }

    /// <summary>
    /// 验证 ViewModel 入口生成器与分析阶段已合并到同一源文件，源生成器入口不再单独成文件。
    /// 分析逻辑以嵌套 <c>Analysis</c> 类的形式承载，源文件只暴露入口 + 嵌套职责。
    /// </summary>
    [Test]
    public async Task ViewModelGenerator_AndAnalysis_Should_ShareSingleFileAsync()
    {
        string root = FindRepositoryRoot();
        string[] sourceFiles = Directory.GetFiles(
            Path.Combine(root, "src", "MiKiNuo.Mvi.Infrastructure", "BuildTime", "SourceGeneration"),
            "*.cs",
            SearchOption.TopDirectoryOnly);

        string? generatorFile = FindFileDeclaringTopLevelType(sourceFiles, "MviViewModelGenerator");
        string? separateAnalysisFile = FindFileDeclaringTopLevelType(sourceFiles, "MviViewModelAnalysis");

        await Assert.That(generatorFile).IsNotNull();
        await Assert.That(separateAnalysisFile).IsNull();
        await Assert.That(File.ReadAllText(generatorFile!)).Contains("class Analysis");
    }

    /// <summary>
    /// 查找声明指定顶级类型的源文件（即类型不在另一个类型的嵌套作用域内）。
    /// </summary>
    private static string? FindFileDeclaringTopLevelType(string[] sourceFiles, string typeShortName)
    {
        System.Text.RegularExpressions.Regex typeRegex = new(
            $@"\b(class|static class|sealed class|partial class)\s+{System.Text.RegularExpressions.Regex.Escape(typeShortName)}\b",
            System.Text.RegularExpressions.RegexOptions.Compiled);

        foreach (string file in sourceFiles)
        {
            string content = File.ReadAllText(file);
            if (typeRegex.IsMatch(content))
            {
                return file;
            }
        }

        return null;
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
