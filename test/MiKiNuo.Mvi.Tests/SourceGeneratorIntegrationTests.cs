﻿﻿﻿﻿using MiKiNuo.Mvi.Samples.Avalonia.Composition;
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
    /// 验证示例项目不再提交手写的源生成产物。
    /// </summary>
    [Test]
    public async Task SampleSource_Should_NotContainHandWrittenGeneratedFilesAsync()
    {
        string root = FindRepositoryRoot();
        string sampleDirectory = Path.Combine(root, "sample", "MiKiNuo.Mvi.Samples.Avalonia");
        string[] generatedFiles = Directory.GetFiles(sampleDirectory, "*.Generated.cs", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(sampleDirectory, "SampleGenerated*.cs", SearchOption.AllDirectories))
            .Where(static path => !IsBuildArtifact(path))
            .ToArray();
        string[] templateFiles = Directory.GetFiles(sampleDirectory, "*.mvi.g.cs.template", SearchOption.AllDirectories)
            .Select(Path.GetFileName)
            .Order(StringComparer.Ordinal)
            .ToArray()!;

        await Assert.That(generatedFiles).IsEmpty();
        await Assert.That(templateFiles).IsEmpty();
    }

    /// <summary>
    /// 排除 <c>obj/</c> 与 <c>bin/</c> 目录下的源生成产物。
    /// 这些文件由源生成器在编译时写入，不属于手写源码。
    /// </summary>
    private static bool IsBuildArtifact(string path)
    {
        string normalized = path.Replace('\\', '/');
        return normalized.Contains("/obj/", StringComparison.Ordinal)
            || normalized.Contains("/bin/", StringComparison.Ordinal);
    }

    /// <summary>
    /// 验证示例组合生成器不依赖模板文件作为生成输入。
    /// </summary>
    [Test]
    public async Task SampleCompositionGenerator_Should_NotUseTemplateAdditionalFilesAsync()
    {
        string root = FindRepositoryRoot();
        string generator = await File.ReadAllTextAsync(Path.Combine(
            root,
            "sample",
            "MiKiNuo.Mvi.Samples.Avalonia.BuildTime",
            "SourceGeneration",
            "DiContainerGenerator.cs"));

        await Assert.That(generator).DoesNotContain("AdditionalTextsProvider");
        await Assert.That(generator).DoesNotContain(".mvi.g.cs.template");
    }

    /// <summary>
    /// 验证 Reducer 与源生成组合根类型在编译期可用。
    /// </summary>
    [Test]
    public async Task GeneratedTypes_Should_BeAvailableAtCompileTimeAsync()
    {
        LoginReducer reducer = new();
        SampleGeneratedContainer container = new();
        LoginViewModel loginViewModel = container.Resolve<LoginViewModel>();

        await Assert.That(reducer).IsNotNull();
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
