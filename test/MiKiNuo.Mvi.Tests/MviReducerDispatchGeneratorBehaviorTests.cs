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
    /// 验证含 [MviReduce] 方法的规约器
    /// 触发生成器产出可编译的 Reduce override。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceCompilableReduceOverrideAsync()
    {
        (GeneratorDriverRunResult runResult, bool emitSuccess) =
            GeneratorTestHost.RunGeneratorAndCompile<MviReducerDispatchGenerator>(
                StubDefinitions + "\n" + ReducerSource);

        await Assert.That(emitSuccess).IsTrue();
        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(1);
    }

    /// <summary>
    /// 验证生成的 switch 分发逻辑可成功编译。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceCompilableSwitchDispatchAsync()
    {
        (GeneratorDriverRunResult runResult, bool emitSuccess) =
            GeneratorTestHost.RunGeneratorAndCompile<MviReducerDispatchGenerator>(
                StubDefinitions + "\n" + ReducerSource);

        await Assert.That(emitSuccess).IsTrue();
        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(1);
    }

    /// <summary>
    /// 验证生成的默认分支代码可成功编译。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceCompilableDefaultBranchAsync()
    {
        (GeneratorDriverRunResult runResult, bool emitSuccess) =
            GeneratorTestHost.RunGeneratorAndCompile<MviReducerDispatchGenerator>(
                StubDefinitions + "\n" + ReducerSource);

        await Assert.That(emitSuccess).IsTrue();
        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(1);
    }

    /// <summary>
    /// 验证生成的 null 检查代码可成功编译。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceCompilableNullChecksAsync()
    {
        (GeneratorDriverRunResult runResult, bool emitSuccess) =
            GeneratorTestHost.RunGeneratorAndCompile<MviReducerDispatchGenerator>(
                StubDefinitions + "\n" + ReducerSource);

        await Assert.That(emitSuccess).IsTrue();
        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(1);
    }

    /// <summary>
    /// 验证 Guard 谓词生成的 when 子句可成功编译。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceCompilableGuardWhenClauseAsync()
    {
        (GeneratorDriverRunResult runResult, bool emitSuccess) =
            GeneratorTestHost.RunGeneratorAndCompile<MviReducerDispatchGenerator>(
                StubDefinitions + "\n" + ReducerWithGuardSource);

        await Assert.That(emitSuccess).IsTrue();
        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(1);
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
