// 源生成器行为测试共享宿主：封装编译对象创建与生成器驱动逻辑。
// 消除各行为测试中重复的反射加载与基础引用组装代码。

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示源生成器行为测试共享宿主。
/// </summary>
internal static class GeneratorTestHost
{
    /// <summary>
    /// 创建测试用编译对象。
    /// </summary>
    /// <param name="source">测试源代码。</param>
    /// <param name="extraReferences">额外元数据引用。</param>
    /// <returns>C# 编译对象。</returns>
    public static CSharpCompilation CreateCompilation(string source, params MetadataReference[] extraReferences)
    {
        CSharpParseOptions parseOptions = new(LanguageVersion.Preview);
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);

        List<MetadataReference> references = new(GetBaseReferences());
        references.AddRange(extraReferences);

        CSharpCompilationOptions options = new(
            OutputKind.DynamicallyLinkedLibrary,
            nullableContextOptions: NullableContextOptions.Enable);

        return CSharpCompilation.Create(
            "MviGeneratorTestAssembly",
            new[] { syntaxTree },
            references,
            options);
    }

    /// <summary>
    /// 驱动生成器并返回运行结果。
    /// </summary>
    /// <typeparam name="TGenerator">生成器类型。</typeparam>
    /// <param name="source">测试源代码。</param>
    /// <param name="extraReferences">额外元数据引用。</param>
    /// <returns>生成器运行结果。</returns>
    public static GeneratorDriverRunResult RunGenerator<TGenerator>(
        string source, params MetadataReference[] extraReferences)
        where TGenerator : IIncrementalGenerator, new()
    {
        TGenerator generator = new();
        CSharpCompilation compilation = CreateCompilation(source, extraReferences);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        return driver.RunGenerators(compilation).GetRunResult();
    }

    /// <summary>
    /// 获取基础元数据引用列表。
    /// </summary>
    private static IEnumerable<MetadataReference> GetBaseReferences()
    {
        yield return MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        yield return MetadataReference.CreateFromFile(typeof(Console).Assembly.Location);
        yield return MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location);
        yield return MetadataReference.CreateFromFile(typeof(EventArgs).Assembly.Location);
        yield return MetadataReference.CreateFromFile(typeof(System.ComponentModel.INotifyPropertyChanged).Assembly.Location);

        string? coreDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
        if (coreDir is not null)
        {
            string[] runtimeAssemblies = new[]
            {
                "System.Runtime.dll",
                "System.ComponentModel.Primitives.dll",
                "System.Collections.dll",
                "System.Linq.dll",
            };

            foreach (string dllName in runtimeAssemblies)
            {
                string path = Path.Combine(coreDir, dllName);
                if (File.Exists(path))
                {
                    yield return MetadataReference.CreateFromFile(path);
                }
            }
        }
    }
}
