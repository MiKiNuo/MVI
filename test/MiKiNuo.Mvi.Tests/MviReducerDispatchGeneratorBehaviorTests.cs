using Microsoft.CodeAnalysis;
using MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;
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
        GeneratorDriverRunResult result = GeneratorTestHost.RunGenerator<MviReducerDispatchGenerator>(
            StubDefinitions + "\n" + ReducerSource);

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
        GeneratorDriverRunResult result = GeneratorTestHost.RunGenerator<MviReducerDispatchGenerator>(
            StubDefinitions + "\n" + ReducerSource);

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
        GeneratorDriverRunResult result = GeneratorTestHost.RunGenerator<MviReducerDispatchGenerator>(
            StubDefinitions + "\n" + ReducerSource);

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
        GeneratorDriverRunResult result = GeneratorTestHost.RunGenerator<MviReducerDispatchGenerator>(
            StubDefinitions + "\n" + ReducerSource);

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
        GeneratorDriverRunResult result = GeneratorTestHost.RunGenerator<MviReducerDispatchGenerator>(
            StubDefinitions + "\n" + ReducerWithGuardSource);

        string generatedCode = result.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("when CanSubmit(state)");
    }

    /// <summary>
    /// 验证生成的 Reduce override 可成功编译并发射。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceCompilableCodeAsync()
    {
        (GeneratorDriverRunResult runResult, bool emitSuccess) =
            GeneratorTestHost.RunGeneratorAndCompile<MviReducerDispatchGenerator>(
                StubDefinitions + "\n" + ReducerSource);

        await Assert.That(runResult.GeneratedTrees.Length).IsGreaterThan(0);
        await Assert.That(emitSuccess).IsTrue();
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

        namespace MiKiNuo.Mvi.Domain.MVI.Business
        {
            public interface IMviBusinessResult { }
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
                    TState state, TIntent intent, MiKiNuo.Mvi.Domain.MVI.Business.IMviBusinessResult? result);
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
                private MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult<LoginState, LoginEffect> HandleChangeUserName(
                    LoginState state, LoginIntent.ChangeUserName intent, MiKiNuo.Mvi.Domain.MVI.Business.IMviBusinessResult? result)
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
                private MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult<LoginState, LoginEffect> HandleSubmit(
                    LoginState state, LoginIntent.Submit intent, MiKiNuo.Mvi.Domain.MVI.Business.IMviBusinessResult? result)
                {
                    return MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult.State<LoginState, LoginEffect>(state);
                }

                private bool CanSubmit(LoginState state) => state.CanSubmit;
            }
        }
        """;
}
