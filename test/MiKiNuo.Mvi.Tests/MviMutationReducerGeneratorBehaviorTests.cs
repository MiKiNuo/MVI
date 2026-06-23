using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 <c>MviMutationReducerGenerator</c> 源生成器的行为测试。
/// 验证带 <c>[MviReduceMutation]</c> 特性的方法能正确生成 <c>Reduce</c> 分发逻辑。
/// </summary>
public sealed class MviMutationReducerGeneratorBehaviorTests
{
    /// <summary>
    /// 验证带 <c>[MviReduceMutation]</c> 的方法生成 Reduce 分发 override。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceReduceOverrideWithDispatchAsync()
    {
        IIncrementalGenerator generator = CreateMutationReducerGenerator();
        CSharpCompilation compilation = CreateTestCompilation(ReducerSource);
        GeneratorDriverRunResult result = RunGenerator(generator, compilation);

        await Assert.That(result.GeneratedTrees.Length).IsGreaterThan(0);

        string generatedCode = result.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("Reduce");
        await Assert.That(generatedCode).Contains("switch");
        await Assert.That(generatedCode).Contains("HandleAddValue");
        await Assert.That(generatedCode).Contains("AddValue");
    }

    /// <summary>
    /// 验证生成的 Reduce override 包含默认分支返回原状态。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceDefaultBranchReturningOriginalStateAsync()
    {
        IIncrementalGenerator generator = CreateMutationReducerGenerator();
        CSharpCompilation compilation = CreateTestCompilation(ReducerSource);
        GeneratorDriverRunResult result = RunGenerator(generator, compilation);

        string generatedCode = result.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("MviReduceResult.State");
    }

    /// <summary>
    /// 验证多个 <c>[MviReduceMutation]</c> 方法生成多个分发分支。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceMultipleDispatchBranchesAsync()
    {
        IIncrementalGenerator generator = CreateMutationReducerGenerator();
        CSharpCompilation compilation = CreateTestCompilation(MultiMethodReducerSource);
        GeneratorDriverRunResult result = RunGenerator(generator, compilation);

        string generatedCode = result.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("HandleAddValue");
        await Assert.That(generatedCode).Contains("HandleSetValue");
        await Assert.That(generatedCode).Contains("AddValue");
        await Assert.That(generatedCode).Contains("SetValue");
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
    /// 加载 Infrastructure 程序集并创建变更规约器生成器实例。
    /// </summary>
    private static IIncrementalGenerator CreateMutationReducerGenerator()
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
            "MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration.MviMutationReducerGenerator")
            ?? throw new InvalidOperationException("未找到变更规约器生成器类型。");

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
            "MviMutationReducerBehaviorTestAssembly",
            new[] { syntaxTree },
            references,
            options);
    }

    /// <summary>
    /// 桩类型定义：模拟 MVI 框架的变更规约相关类型。
    /// </summary>
    private const string StubDefinitions = """
        namespace MiKiNuo.Mvi.Domain.MVI.State
        {
            public interface IMviState { }
        }

        namespace MiKiNuo.Mvi.Domain.MVI.Effect
        {
            public interface IMviEffect { }
        }

        namespace MiKiNuo.Mvi.Domain.MVI.Mutation
        {
            public interface IMviMutation { }

            public interface IMviMutation<TState> : IMviMutation
                where TState : MiKiNuo.Mvi.Domain.MVI.State.IMviState
            {
            }
        }

        namespace MiKiNuo.Mvi.Domain.MVI.Reducer
        {
            public sealed record MviReduceResult<TState, TEffect>
            {
                public TState State { get; init; }
                public System.Collections.Generic.IReadOnlyList<TEffect> Effects { get; init; }

                public static MviReduceResult<TState, TEffect> State<TS, TE>(TS state)
                    where TS : TState
                    where TE : TEffect
                    => new MviReduceResult<TState, TEffect> { State = state, Effects = new System.Collections.Generic.List<TE>() };
            }
        }

        namespace MiKiNuo.Mvi.Application.MVI.Reducer
        {
            public abstract class MviMutationReducerBase<TState, TMutation, TEffect>
                where TState : MiKiNuo.Mvi.Domain.MVI.State.IMviState
                where TMutation : MiKiNuo.Mvi.Domain.MVI.Mutation.IMviMutation
                where TEffect : MiKiNuo.Mvi.Domain.MVI.Effect.IMviEffect
            {
                public abstract MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult<TState, TEffect> Reduce(TState state, TMutation mutation);
            }
        }

        namespace MiKiNuo.Mvi.Domain.MVI.Reducer
        {
            [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
            public sealed class MviReduceMutationAttribute : System.Attribute
            {
            }
        }
        """;

    /// <summary>
    /// 测试源码：单个规约方法的变更规约器。
    /// </summary>
    private const string ReducerSource = """
        namespace TestApp
        {
            public sealed record CounterState(int Value) : MiKiNuo.Mvi.Domain.MVI.State.IMviState;

            public abstract record CounterMutation : MiKiNuo.Mvi.Domain.MVI.Mutation.IMviMutation<CounterState>;

            public sealed record AddValue(int Amount) : CounterMutation;

            public sealed record CounterEffect : MiKiNuo.Mvi.Domain.MVI.Effect.IMviEffect;

            public sealed partial class CounterMutationReducer
                : MiKiNuo.Mvi.Application.MVI.Reducer.MviMutationReducerBase<CounterState, CounterMutation, CounterEffect>
            {
                [MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceMutation]
                public MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult<CounterState, CounterEffect> HandleAddValue(
                    CounterState state,
                    AddValue mutation)
                    => MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult.State<CounterState, CounterEffect>(state);
            }
        }
        """;

    /// <summary>
    /// 测试源码：多个规约方法的变更规约器。
    /// </summary>
    private const string MultiMethodReducerSource = """
        namespace TestApp
        {
            public sealed record CounterState(int Value) : MiKiNuo.Mvi.Domain.MVI.State.IMviState;

            public abstract record CounterMutation : MiKiNuo.Mvi.Domain.MVI.Mutation.IMviMutation<CounterState>;

            public sealed record AddValue(int Amount) : CounterMutation;
            public sealed record SetValue(int NewValue) : CounterMutation;

            public sealed record CounterEffect : MiKiNuo.Mvi.Domain.MVI.Effect.IMviEffect;

            public sealed partial class CounterMutationReducer
                : MiKiNuo.Mvi.Application.MVI.Reducer.MviMutationReducerBase<CounterState, CounterMutation, CounterEffect>
            {
                [MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceMutation]
                public MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult<CounterState, CounterEffect> HandleAddValue(
                    CounterState state,
                    AddValue mutation)
                    => MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult.State<CounterState, CounterEffect>(state);

                [MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceMutation]
                public MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult<CounterState, CounterEffect> HandleSetValue(
                    CounterState state,
                    SetValue mutation)
                    => MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult.State<CounterState, CounterEffect>(state);
            }
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
