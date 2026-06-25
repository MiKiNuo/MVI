using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 <c>MviReducerDispatchGenerator</c> 源生成器的行为测试。
/// 使用 <c>CSharpGeneratorDriver</c> 驱动生成器并验证生成产物。
/// </summary>
public sealed class MviReducerDispatchGeneratorBehaviorTests
{
    /// <summary>
    /// 验证含 [MviReduce] 方法的规约器触发生成器产出 Reduce override。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceReduceOverrideAsync()
    {
        IIncrementalGenerator generator = CreateReducerDispatchGenerator();
        CSharpCompilation compilation = CreateTestCompilation(ReducerSource);
        GeneratorDriverRunResult result = RunGenerator(generator, compilation);

        await Assert.That(result.GeneratedTrees.Length).IsGreaterThan(0);

        string generatedCode = result.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("public override MviReduceResult<");
        await Assert.That(generatedCode).Contains("Reduce(");
    }

    /// <summary>
    /// 验证生成的代码包含 switch 分发逻辑。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceSwitchDispatchAsync()
    {
        IIncrementalGenerator generator = CreateReducerDispatchGenerator();
        CSharpCompilation compilation = CreateTestCompilation(ReducerSource);
        GeneratorDriverRunResult result = RunGenerator(generator, compilation);

        string generatedCode = result.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("return intent switch");
        await Assert.That(generatedCode).Contains("HandleChangeUserName");
    }

    /// <summary>
    /// 验证生成的代码包含默认分支返回原状态。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceDefaultBranchAsync()
    {
        IIncrementalGenerator generator = CreateReducerDispatchGenerator();
        CSharpCompilation compilation = CreateTestCompilation(ReducerSource);
        GeneratorDriverRunResult result = RunGenerator(generator, compilation);

        string generatedCode = result.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("_ => MviReduceResult.State<");
        await Assert.That(generatedCode).Contains("(state)");
    }

    /// <summary>
    /// 验证生成的代码包含 null 检查。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceNullChecksAsync()
    {
        IIncrementalGenerator generator = CreateReducerDispatchGenerator();
        CSharpCompilation compilation = CreateTestCompilation(ReducerSource);
        GeneratorDriverRunResult result = RunGenerator(generator, compilation);

        string generatedCode = result.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("ArgumentNullException.ThrowIfNull(state)");
        await Assert.That(generatedCode).Contains("ArgumentNullException.ThrowIfNull(intent)");
    }

    /// <summary>
    /// 验证 Guard 谓词生成 when 子句。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceGuardWhenClauseAsync()
    {
        IIncrementalGenerator generator = CreateReducerDispatchGenerator();
        CSharpCompilation compilation = CreateTestCompilation(ReducerWithGuardSource);
        GeneratorDriverRunResult result = RunGenerator(generator, compilation);

        string generatedCode = result.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("when CanSubmit(state)");
    }

    /// <summary>
    /// 驱动生成器并返回运行结果。
    /// </summary>
    /// <param name="generator">增量源生成器实例。</param>
    /// <param name="compilation">测试用编译对象。</param>
    /// <returns>生成器运行结果。</returns>
    private static GeneratorDriverRunResult RunGenerator(IIncrementalGenerator generator, CSharpCompilation compilation)
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        return driver.RunGenerators(compilation).GetRunResult();
    }

    /// <summary>
    /// 加载 Infrastructure 程序集并创建规约器分发生成器实例。
    /// </summary>
    private static IIncrementalGenerator CreateReducerDispatchGenerator()
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
            "MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration.MviReducerDispatchGenerator")
            ?? throw new InvalidOperationException("未找到规约器分发生成器类型。");

        object? instance = Activator.CreateInstance(generatorType);
        return (IIncrementalGenerator)(instance ?? throw new InvalidOperationException("无法创建生成器实例。"));
    }

    /// <summary>
    /// 创建包含桩类型与测试规约器的编译对象。
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

        System.Reflection.Assembly coreAssembly = typeof(object).Assembly;
        string? coreDir = Path.GetDirectoryName(coreAssembly.Location);
        if (coreDir is not null)
        {
            string[] runtimeAssemblies = new[]
            {
                "System.Runtime.dll",
                "System.Collections.dll",
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
            "MviReducerDispatchTestAssembly",
            new[] { syntaxTree },
            references,
            options);
    }

    /// <summary>
    /// 桩类型定义：模拟 MVI 框架关键类型。
    /// </summary>
    private const string StubDefinitions = """
        namespace MiKiNuo.Mvi.Domain.MVI.State
        {
            public interface IMviState { }
        }

        namespace MiKiNuo.Mvi.Domain.MVI.Intent
        {
            public interface IMviIntent { }
        }

        namespace MiKiNuo.Mvi.Domain.MVI.Effect
        {
            public interface IMviEffect { }
        }

        namespace MiKiNuo.Mvi.Domain.MVI.Reducer
        {
            public sealed record MviReduceResult<TState, TEffect>(
                TState State,
                System.Collections.Generic.IReadOnlyList<TEffect> Effects)
                where TState : MiKiNuo.Mvi.Domain.MVI.State.IMviState
                where TEffect : MiKiNuo.Mvi.Domain.MVI.Effect.IMviEffect;

            public static class MviReduceResult
            {
                public static MviReduceResult<TState, TEffect> State<TState, TEffect>(TState state)
                    where TState : MiKiNuo.Mvi.Domain.MVI.State.IMviState
                    where TEffect : MiKiNuo.Mvi.Domain.MVI.Effect.IMviEffect
                    => new MviReduceResult<TState, TEffect>(state, System.Array.Empty<TEffect>());
            }

            [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
            public sealed class MviReduceAttribute : System.Attribute
            {
                public MviReduceAttribute(System.Type intentType) { IntentType = intentType; }
                public System.Type IntentType { get; }
                public string? Guard { get; set; }
            }
        }

        namespace MiKiNuo.Mvi.Application.MVI.Reducer
        {
            public abstract class MviReducerBase<TState, TIntent, TEffect>
                where TState : MiKiNuo.Mvi.Domain.MVI.State.IMviState
                where TIntent : MiKiNuo.Mvi.Domain.MVI.Intent.IMviIntent
                where TEffect : MiKiNuo.Mvi.Domain.MVI.Effect.IMviEffect
            {
                public abstract MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult<TState, TEffect> Reduce(
                    TState state, TIntent intent);
            }
        }
        """;

    /// <summary>
    /// 测试源代码：含 [MviReduce] 方法的规约器。
    /// </summary>
    private const string ReducerSource = """
        namespace TestApp
        {
            public sealed record LoginState(string UserName) : MiKiNuo.Mvi.Domain.MVI.State.IMviState;
            public sealed record LoginEffect : MiKiNuo.Mvi.Domain.MVI.Effect.IMviEffect;

            public abstract partial record LoginIntent : MiKiNuo.Mvi.Domain.MVI.Intent.IMviIntent
            {
                public sealed partial record ChangeUserName(string UserName) : LoginIntent;
            }

            public sealed partial class LoginReducer
                : MiKiNuo.Mvi.Application.MVI.Reducer.MviReducerBase<LoginState, LoginIntent, LoginEffect>
            {
                [MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduce(typeof(LoginIntent.ChangeUserName))]
                private static MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult<LoginState, LoginEffect> HandleChangeUserName(
                    LoginState state, LoginIntent.ChangeUserName intent)
                {
                    return MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult.State<LoginState, LoginEffect>(
                        state with { UserName = intent.UserName });
                }
            }
        }
        """;

    /// <summary>
    /// 测试源代码：含 Guard 谓词的规约器。
    /// </summary>
    private const string ReducerWithGuardSource = """
        namespace TestApp
        {
            public sealed record LoginState(string UserName, bool CanSubmit) : MiKiNuo.Mvi.Domain.MVI.State.IMviState;
            public sealed record LoginEffect : MiKiNuo.Mvi.Domain.MVI.Effect.IMviEffect;

            public abstract partial record LoginIntent : MiKiNuo.Mvi.Domain.MVI.Intent.IMviIntent
            {
                public sealed partial record Submit : LoginIntent;
            }

            public sealed partial class LoginReducer
                : MiKiNuo.Mvi.Application.MVI.Reducer.MviReducerBase<LoginState, LoginIntent, LoginEffect>
            {
                [MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduce(typeof(LoginIntent.Submit), Guard = nameof(CanSubmit))]
                private static MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult<LoginState, LoginEffect> HandleSubmit(
                    LoginState state, LoginIntent.Submit intent)
                {
                    return MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult.State<LoginState, LoginEffect>(state);
                }

                private static bool CanSubmit(LoginState state) => state.CanSubmit;
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
