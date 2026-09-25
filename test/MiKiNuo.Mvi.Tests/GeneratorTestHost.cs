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
    /// 获取真实框架程序集引用（Domain / Application / R3 / INPC），
    /// 使生成器测试直接对真实接口编译，消除手写桩与真实接口漂移的风险。
    /// </summary>
    public static MetadataReference[] FrameworkReferences =>
    [
        MetadataReference.CreateFromFile(typeof(Domain.MVI.State.IMviState).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Application.MVI.Store.IMviStore<,,>).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(R3.Observable).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(System.ComponentModel.INotifyPropertyChanged).Assembly.Location),
    ];

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
    /// 驱动生成器并将生成代码合并回编译,
    /// 验证生成产物可成功编译。
    /// </summary>
    /// <typeparam name="TGenerator">生成器类型。</typeparam>
    /// <param name="source">测试源代码。</param>
    /// <param name="extraReferences">额外元数据引用。</param>
    /// <returns>生成器运行结果与发射是否成功。</returns>
    public static (GeneratorDriverRunResult RunResult, bool EmitSuccess)
        RunGeneratorAndCompile<TGenerator>(
            string source, params MetadataReference[] extraReferences)
        where TGenerator : IIncrementalGenerator, new()
    {
        TGenerator generator = new();
        CSharpCompilation compilation = CreateCompilation(source, extraReferences);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        GeneratorDriverRunResult runResult = driver.RunGenerators(compilation).GetRunResult();

        // 生成器产出的语法树语言版本可能与编译对象不一致,
        // 需用相同解析选项重新解析后再合并。
        CSharpParseOptions parseOptions = new(LanguageVersion.Preview);
        List<SyntaxTree> recompiledTrees = new();
        foreach (SyntaxTree tree in runResult.GeneratedTrees)
        {
            recompiledTrees.Add(CSharpSyntaxTree.ParseText(tree.GetText(), parseOptions));
        }

        CSharpCompilation finalCompilation = compilation.AddSyntaxTrees(recompiledTrees);
        using MemoryStream stream = new();
        bool emitSuccess = finalCompilation.Emit(stream).Success;

        return (runResult, emitSuccess);
    }

    /// <summary>
    /// 驱动生成器、将生成代码合并编译为程序集并执行其中的
    /// <c>InstanceProbe.Run()</c> 公开验收入口。
    /// </summary>
    /// <typeparam name="TGenerator">生成器类型。</typeparam>
    /// <param name="source">测试源代码（须含 InstanceProbe.Run）。</param>
    /// <param name="extraReferences">额外元数据引用。</param>
    /// <returns>探针返回的验收结果。</returns>
    public static async Task<bool> RunGeneratorProbeAsync<TGenerator>(
        string source, params MetadataReference[] extraReferences)
        where TGenerator : IIncrementalGenerator, new()
    {
        TGenerator generator = new();
        CSharpCompilation compilation = CreateCompilation(source, extraReferences);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        GeneratorDriverRunResult runResult = driver.RunGenerators(compilation).GetRunResult();

        CSharpParseOptions parseOptions = new(LanguageVersion.Preview);
        foreach (SyntaxTree tree in runResult.GeneratedTrees)
        {
            compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(tree.GetText(), parseOptions));
        }

        using MemoryStream stream = new();
        Microsoft.CodeAnalysis.Emit.EmitResult emitted = compilation.Emit(stream);
        if (!emitted.Success)
        {
            throw new InvalidOperationException("生成代码编译失败：" + string.Join("\n", emitted.Diagnostics));
        }

        System.Reflection.Assembly assembly = System.Reflection.Assembly.Load(stream.ToArray());
        System.Reflection.MethodInfo method = assembly.GetType("InstanceProbe")!.GetMethod("Run")!;
        return await (Task<bool>)method.Invoke(null, null)!;
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
