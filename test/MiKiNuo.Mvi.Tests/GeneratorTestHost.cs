using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MiKiNuo.Mvi.Generators.SourceGeneration;
namespace MiKiNuo.Mvi.Tests;
/// <summary>真实框架引用下合并编译生成产物的测试宿主。</summary>
internal static class GeneratorTestHost
{
    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Preview);
    /// <summary>真实 Abstractions、Runtime、Binding 和 R3 程序集，不手写替代桩。</summary>
    public static MetadataReference[] FrameworkReferences => new[]
    {
        typeof(MiKiNuo.Mvi.Abstractions.MVI.State.IMviState).Assembly.Location,
        typeof(MiKiNuo.Mvi.Runtime.MVI.Store.IMviStore<,>).Assembly.Location,
        typeof(MiKiNuo.Mvi.Binding.ViewModel.MviViewModelBase<,>).Assembly.Location,
        typeof(R3.Observable).Assembly.Location,
        typeof(System.ComponentModel.INotifyPropertyChanged).Assembly.Location,
    }.Distinct().Select(path => MetadataReference.CreateFromFile(path)).ToArray();
    /// <summary>使用当前测试进程的平台引用创建编译。</summary>
    public static CSharpCompilation CreateCompilation(string source, params MetadataReference[] extraReferences)
    {
        string[] platform = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? typeof(object).Assembly.Location).Split(Path.PathSeparator);
        IEnumerable<MetadataReference> refs = platform.Where(File.Exists).Select(path => MetadataReference.CreateFromFile(path)).Concat(extraReferences)
            .GroupBy(reference => reference.Display, StringComparer.OrdinalIgnoreCase).Select(group => group.First());
        return CSharpCompilation.Create("MviGeneratorTestAssembly", [CSharpSyntaxTree.ParseText(source, ParseOptions)], refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }
    /// <summary>仅取得诊断和生成树；负例使用此入口。</summary>
    public static GeneratorDriverRunResult RunGenerator<TGenerator>(string source, params MetadataReference[] references)
        where TGenerator : IIncrementalGenerator, new()
        => Driver(new TGenerator()).RunGenerators(CreateCompilation(source, references)).GetRunResult();
    /// <summary>合并生成结果后真正发射 IL；不能仅断言生成文本。</summary>
    public static (GeneratorDriverRunResult RunResult, bool EmitSuccess) RunGeneratorAndCompile<TGenerator>(string source, params MetadataReference[] references)
        where TGenerator : IIncrementalGenerator, new()
    {
        GeneratorDriver driver = Driver(new TGenerator()).RunGeneratorsAndUpdateCompilation(CreateCompilation(source, references), out Compilation compilation, out _);
        using MemoryStream stream = new();
        return (driver.GetRunResult(), compilation.Emit(stream).Success);
    }
    /// <summary>保留原有单生成器探针入口。</summary>
    public static Task<bool> RunGeneratorProbeAsync<TGenerator>(string source, params MetadataReference[] references)
        where TGenerator : IIncrementalGenerator, new() => ProbeAsync(source, references, new TGenerator());
    /// <summary>让绑定、Handler、Reducer、DI、StatePath 在同一轮真实协同。</summary>
    public static (GeneratorDriverRunResult Result, ImmutableArray<Diagnostic> Errors) CompileFramework(string source)
    {
        GeneratorDriver driver = Driver(FrameworkGenerators()).RunGeneratorsAndUpdateCompilation(CreateCompilation(source, FrameworkReferences), out Compilation compilation, out ImmutableArray<Diagnostic> generated);
        using MemoryStream stream = new();
        var emitted = compilation.Emit(stream);
        return (driver.GetRunResult(), generated.Concat(emitted.Diagnostics).Where(d => d.Severity == DiagnosticSeverity.Error).ToImmutableArray());
    }
    /// <summary>实际执行生成后的组件图；反射只存在于测试宿主。</summary>
    public static Task<bool> ProbeFrameworkAsync(string source) => ProbeAsync(source, FrameworkReferences, FrameworkGenerators());
    private static IIncrementalGenerator[] FrameworkGenerators() =>
    [new MviIntentDispatchGenerator(), new MviReducerDispatchGenerator(), new MviViewModelGenerator(), new MviDiContainerGenerator(), new MviStatePathsGenerator()];
    private static GeneratorDriver Driver(params IIncrementalGenerator[] generators)
        => CSharpGeneratorDriver.Create(generators.Select(g => g.AsSourceGenerator()), parseOptions: ParseOptions);
    private static async Task<bool> ProbeAsync(string source, MetadataReference[] references, params IIncrementalGenerator[] generators)
    {
        GeneratorDriver driver = Driver(generators).RunGeneratorsAndUpdateCompilation(CreateCompilation(source, references), out Compilation compilation, out ImmutableArray<Diagnostic> diagnostics);
        using MemoryStream stream = new(); var emitted = compilation.Emit(stream);
        if (!emitted.Success || diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            throw new InvalidOperationException("生成代码编译失败：\n" + string.Join("\n", diagnostics.Concat(emitted.Diagnostics)));
        System.Reflection.Assembly assembly = System.Reflection.Assembly.Load(stream.ToArray());
        System.Reflection.MethodInfo method = assembly.GetType("InstanceProbe")!.GetMethod("Run")!;
        return await (Task<bool>)method.Invoke(null, null)!;
    }
}
