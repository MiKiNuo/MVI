using MiKiNuo.Mvi.Samples.Avalonia.Composition;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示源生成器集成测试。
/// </summary>
public sealed class SourceGeneratorIntegrationTests
{
    /// <summary>
    /// 验证示例项目不再提交手写的 Generated.cs 文件。
    /// </summary>
    [Test]
    public async Task SampleSource_Should_NotContainHandWrittenGeneratedFilesAsync()
    {
        string root = FindRepositoryRoot();
        string sampleDirectory = Path.Combine(root, "sample", "MiKiNuo.Mvi.Samples.Avalonia");
        string[] generatedFiles = Directory.GetFiles(sampleDirectory, "*.Generated.cs", SearchOption.AllDirectories);

        await Assert.That(generatedFiles).IsEmpty();
    }

    /// <summary>
    /// 验证 Reducer Dispatcher 与 DI 容器确实来自编译期源生成结果。
    /// </summary>
    [Test]
    public async Task GeneratedTypes_Should_BeAvailableAtCompileTimeAsync()
    {
        LoginReducer dispatcher = new();
        SampleGeneratedContainer container = new();
        LoginViewModel loginViewModel = container.Resolve<LoginViewModel>();

        await Assert.That(dispatcher).IsNotNull();
        await Assert.That(loginViewModel).IsNotNull();
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
