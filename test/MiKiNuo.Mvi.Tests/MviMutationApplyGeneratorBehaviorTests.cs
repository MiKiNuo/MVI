using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 <c>MviMutationApplyGenerator</c> 源生成器的行为测试。
/// 验证带 <c>[MviMutation]</c> 特性的变更记录能正确生成 <c>State.Apply(Mutation)</c> 扩展方法。
/// </summary>
public sealed class MviMutationApplyGeneratorBehaviorTests
{
    /// <summary>
    /// 验证带 <c>[MviMutation(Path)]</c> 的 Set 操作生成正确的 Apply 扩展方法。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceApplyExtensionForSetOpAsync()
    {
        IIncrementalGenerator generator = CreateMutationApplyGenerator();
        CSharpCompilation compilation = CreateTestCompilation(SetOpSource);
        GeneratorDriverRunResult result = RunGenerator(generator, compilation);

        await Assert.That(result.GeneratedTrees.Length).IsGreaterThan(0);

        string generatedCode = result.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("Apply");
        await Assert.That(generatedCode).Contains("CounterState");
        await Assert.That(generatedCode).Contains("SetValue");
        await Assert.That(generatedCode).Contains("state with { Value = mutation.Value }");
    }

    /// <summary>
    /// 验证 <c>Op = MutationOp.Add</c> 生成累加表达式。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceAddExpressionForAddOpAsync()
    {
        IIncrementalGenerator generator = CreateMutationApplyGenerator();
        CSharpCompilation compilation = CreateTestCompilation(AddOpSource);
        GeneratorDriverRunResult result = RunGenerator(generator, compilation);

        await Assert.That(result.GeneratedTrees.Length).IsGreaterThan(0);

        string generatedCode = result.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("state with { Value = state.Value + mutation.Amount }");
    }

    /// <summary>
    /// 验证 <c>Source</c> 参数指定变更值来源字段名。
    /// </summary>
    [Test]
    public async Task Generate_Should_UseSourceFieldNameWhenSpecifiedAsync()
    {
        IIncrementalGenerator generator = CreateMutationApplyGenerator();
        CSharpCompilation compilation = CreateTestCompilation(CustomSourceField);
        GeneratorDriverRunResult result = RunGenerator(generator, compilation);

        await Assert.That(result.GeneratedTrees.Length).IsGreaterThan(0);

        string generatedCode = result.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("mutation.NewValue");
    }

    /// <summary>
    /// 验证嵌套路径 <c>Path = "Player.Stamina"</c> 生成嵌套 with 表达式。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceNestedWithExpressionForPathAsync()
    {
        IIncrementalGenerator generator = CreateMutationApplyGenerator();
        CSharpCompilation compilation = CreateTestCompilation(NestedPathSource);
        GeneratorDriverRunResult result = RunGenerator(generator, compilation);

        await Assert.That(result.GeneratedTrees.Length).IsGreaterThan(0);

        string generatedCode = result.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("state with { Player = state.Player with { Stamina = mutation.Value } }");
    }

    /// <summary>
    /// 驱动生成器并返回运行结果。
    /// </summary>
    private static GeneratorDriverRunResult RunGenerator(IIncrementalGenerator generator, CSharpCompilation compilation)
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        return driver.RunGenerators(compilation).GetRunResult();
    }

    /// <summary>
    /// 加载 Infrastructure 程序集并创建变更应用生成器实例。
    /// </summary>
    private static IIncrementalGenerator CreateMutationApplyGenerator()
    {
        string root = FindRepositoryRoot();
        string infrastructurePath = Path.Combine(
            root,
            "src",
            "MiKiNuo.Mvi.Infrastructure",
            "bin",
            "Release",
            "netstandard2.0",
            "MiKiNuo.Mvi.Infrastructure.dll");

        if (!File.Exists(infrastructurePath))
        {
            infrastructurePath = Path.Combine(
                root,
                "src",
                "MiKiNuo.Mvi.Infrastructure",
                "bin",
                "Debug",
                "netstandard2.0",
                "MiKiNuo.Mvi.Infrastructure.dll");
        }

        System.Reflection.Assembly assembly = System.Reflection.Assembly.LoadFrom(infrastructurePath);
        Type generatorType = assembly.GetType(
            "MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration.MviMutationApplyGenerator")
            ?? throw new InvalidOperationException("未找到变更应用生成器类型。");

        object? instance = Activator.CreateInstance(generatorType);
        return (IIncrementalGenerator)(instance ?? throw new InvalidOperationException("无法创建生成器实例。"));
    }

    /// <summary>
    /// 创建包含桩类型与测试源码的编译对象。
    /// </summary>
    private static CSharpCompilation CreateTestCompilation(string testSource)
    {
        string fullSource = StubDefinitions + "\n" + testSource;

        CSharpParseOptions parseOptions = new(LanguageVersion.Preview);
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(fullSource, parseOptions);

        List<MetadataReference> references = new()
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(EventArgs).Assembly.Location),
        };

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
                    references.Add(MetadataReference.CreateFromFile(path));
                }
            }
        }

        CSharpCompilationOptions options = new(
            OutputKind.DynamicallyLinkedLibrary,
            nullableContextOptions: NullableContextOptions.Enable);

        return CSharpCompilation.Create(
            "MviMutationApplyBehaviorTestAssembly",
            new[] { syntaxTree },
            references,
            options);
    }

    /// <summary>
    /// 桩类型定义：模拟 MVI 框架的变更相关类型。
    /// </summary>
    private const string StubDefinitions = """
        namespace MiKiNuo.Mvi.Domain.MVI.State
        {
            public interface IMviState { }
        }

        namespace MiKiNuo.Mvi.Domain.MVI.Mutation
        {
            public interface IMviMutation { }

            public interface IMviMutation<TState> : IMviMutation
                where TState : MiKiNuo.Mvi.Domain.MVI.State.IMviState
            {
            }

            public enum MutationOp
            {
                Set = 0,
                Add = 1,
                Append = 2,
            }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
            public sealed class MviMutationAttribute : System.Attribute
            {
                public string Path { get; set; } = string.Empty;
                public MutationOp Op { get; set; } = MutationOp.Set;
                public string Source { get; set; } = "Value";
            }
        }
        """;

    /// <summary>
    /// 测试源码：简单的 Set 操作。
    /// </summary>
    private const string SetOpSource = """
        namespace TestApp
        {
            public sealed record CounterState(int Value) : MiKiNuo.Mvi.Domain.MVI.State.IMviState;

            public abstract record CounterMutation : MiKiNuo.Mvi.Domain.MVI.Mutation.IMviMutation<CounterState>;

            [MiKiNuo.Mvi.Domain.MVI.Mutation.MviMutation(Path = "Value")]
            public sealed record SetValue(int Value) : CounterMutation;
        }
        """;

    /// <summary>
    /// 测试源码：Add 操作。
    /// </summary>
    private const string AddOpSource = """
        namespace TestApp
        {
            public sealed record CounterState(int Value) : MiKiNuo.Mvi.Domain.MVI.State.IMviState;

            public abstract record CounterMutation : MiKiNuo.Mvi.Domain.MVI.Mutation.IMviMutation<CounterState>;

            [MiKiNuo.Mvi.Domain.MVI.Mutation.MviMutation(Path = "Value", Op = MiKiNuo.Mvi.Domain.MVI.Mutation.MutationOp.Add, Source = "Amount")]
            public sealed record AddAmount(int Amount) : CounterMutation;
        }
        """;

    /// <summary>
    /// 测试源码：自定义 Source 字段名。
    /// </summary>
    private const string CustomSourceField = """
        namespace TestApp
        {
            public sealed record CounterState(int Value) : MiKiNuo.Mvi.Domain.MVI.State.IMviState;

            public abstract record CounterMutation : MiKiNuo.Mvi.Domain.MVI.Mutation.IMviMutation<CounterState>;

            [MiKiNuo.Mvi.Domain.MVI.Mutation.MviMutation(Path = "Value", Source = "NewValue")]
            public sealed record ReplaceValue(int NewValue) : CounterMutation;
        }
        """;

    /// <summary>
    /// 测试源码：嵌套路径。
    /// </summary>
    private const string NestedPathSource = """
        namespace TestApp
        {
            public sealed record PlayerState(int Stamina) : MiKiNuo.Mvi.Domain.MVI.State.IMviState;

            public sealed record LobbyState(PlayerState Player) : MiKiNuo.Mvi.Domain.MVI.State.IMviState;

            public abstract record LobbyMutation : MiKiNuo.Mvi.Domain.MVI.Mutation.IMviMutation<LobbyState>;

            [MiKiNuo.Mvi.Domain.MVI.Mutation.MviMutation(Path = "Player.Stamina")]
            public sealed record SetStamina(int Value) : LobbyMutation;
        }
        """;

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
