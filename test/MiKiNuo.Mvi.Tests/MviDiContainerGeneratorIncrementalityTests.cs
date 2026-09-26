using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 验证 MviDiContainerGenerator 的增量缓存行为：
/// 无关源码变化不得使已解析的服务模型失效（值相等是增量缓存的前提）。
/// </summary>
public sealed class MviDiContainerGeneratorIncrementalityTests
{
    private const string ServiceSource = """
        using MiKiNuo.Mvi.Domain.DI;

        namespace IncTest
        {
            [DiService(ServiceLifetime.Singleton)]
            public sealed class TrackedService
            {
            }
        }
        """;

    /// <summary>
    /// 验证无关类型变化后 DiService 解析步骤输出保持 Unchanged。
    /// </summary>
    [Test]
    public async Task UnrelatedChange_Should_KeepServiceModelUnchangedAsync()
    {
        CSharpCompilation first = CreateCompilation(ServiceSource + "\nnamespace IncTest { public sealed class UnrelatedOne { } }");
        CSharpCompilation second = CreateCompilation(ServiceSource + "\nnamespace IncTest { public sealed class UnrelatedOne { public int Value; } }");

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new MviDiContainerGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

        driver = driver.RunGenerators(first);
        driver = driver.RunGenerators(second);
        GeneratorDriverRunResult result = driver.GetRunResult();

        await Assert.That(result.Results.Length).IsEqualTo(1);
        await Assert.That(result.Results[0].TrackedSteps.ContainsKey("MviDiServices")).IsTrue();
        IncrementalStepRunReason[] reasons = result.Results[0].TrackedSteps["MviDiServices"]
            .SelectMany(step => step.Outputs)
            .Select(output => output.Reason)
            .ToArray();

        await Assert.That(reasons.Length).IsGreaterThan(0);
        foreach (IncrementalStepRunReason reason in reasons)
        {
            bool cached = reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged;
            await Assert.That(cached).IsTrue();
        }
    }

    /// <summary>
    /// 创建含基础运行时引用的测试编译对象。
    /// </summary>
    /// <param name="source">测试源代码。</param>
    /// <returns>C# 编译对象。</returns>
    private static CSharpCompilation CreateCompilation(string source)
    {
        List<MetadataReference> references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.ComponentModel.INotifyPropertyChanged).Assembly.Location),
            .. GeneratorTestHost.FrameworkReferences,
        ];

        string coreDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        foreach (string name in new[] { "System.Runtime.dll", "System.Collections.dll", "System.Linq.dll" })
        {
            string path = Path.Combine(coreDirectory, name);
            if (File.Exists(path))
            {
                references.Add(MetadataReference.CreateFromFile(path));
            }
        }

        return CSharpCompilation.Create(
            "MviIncrementalityAssembly",
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
