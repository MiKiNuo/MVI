using Microsoft.CodeAnalysis;
using MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 [MviEffect] 副作用分派源生成器的行为测试。
/// </summary>
public sealed class MviEffectDispatchGeneratorBehaviorTests
{
    private const string RuntimeStubs = """
        namespace MiKiNuo.Mvi.Domain.MVI.Intent
        {
            public interface IMviIntent { }
        }

        namespace MiKiNuo.Mvi.Domain.MVI.Effect
        {
            public interface IMviEffect { }

            [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
            public sealed class MviEffectAttribute : System.Attribute
            {
                public MviEffectAttribute(System.Type effectType) { EffectType = effectType; }
                public System.Type EffectType { get; }
            }
        }

        namespace MiKiNuo.Mvi.Application.MVI.Effect
        {
            public interface IMviEffectDispatcher<in TEffect> where TEffect : MiKiNuo.Mvi.Domain.MVI.Effect.IMviEffect
            {
                System.Threading.Tasks.ValueTask DispatchAsync(
                    TEffect effect,
                    System.Threading.CancellationToken cancellationToken = default);
            }

            public interface IMviIntentSink<in TIntent> where TIntent : MiKiNuo.Mvi.Domain.MVI.Intent.IMviIntent
            {
                System.Threading.Tasks.ValueTask DispatchAsync(
                    TIntent intent,
                    System.Threading.CancellationToken cancellationToken = default);
            }

            public abstract class MviEffectDispatcherBase<TIntent, TEffect>
                : IMviEffectDispatcher<TEffect>
                where TIntent : MiKiNuo.Mvi.Domain.MVI.Intent.IMviIntent
                where TEffect : MiKiNuo.Mvi.Domain.MVI.Effect.IMviEffect
            {
                public System.Threading.Tasks.ValueTask DispatchAsync(
                    TEffect effect,
                    System.Threading.CancellationToken cancellationToken = default)
                {
                    return DispatchCoreAsync(effect, cancellationToken);
                }

                protected System.Threading.Tasks.ValueTask DispatchIntentAsync(
                    TIntent intent,
                    System.Threading.CancellationToken cancellationToken = default)
                {
                    return System.Threading.Tasks.ValueTask.CompletedTask;
                }

                protected abstract System.Threading.Tasks.ValueTask DispatchCoreAsync(
                    TEffect effect,
                    System.Threading.CancellationToken cancellationToken);
            }
        }

        namespace TestFeature
        {
            using MiKiNuo.Mvi.Application.MVI.Effect;
            using MiKiNuo.Mvi.Domain.MVI.Effect;
            using MiKiNuo.Mvi.Domain.MVI.Intent;

            public abstract partial record LoginIntent : IMviIntent
            {
                public sealed partial record Succeeded(string DisplayName) : LoginIntent;
            }

            public abstract partial record LoginEffect : IMviEffect
            {
                public sealed partial record NavigateToHome(string DisplayName) : LoginEffect;

                public sealed partial record ShowToast(string Message) : LoginEffect;
            }
        }
        """;

    private const string ValidDispatcher = """
        namespace TestFeature
        {
            using MiKiNuo.Mvi.Domain.MVI.Effect;
            using MiKiNuo.Mvi.Application.MVI.Effect;

            public sealed partial class LoginEffectDispatcher
                : MviEffectDispatcherBase<LoginIntent, LoginEffect>
            {
                [MviEffect(typeof(LoginEffect.NavigateToHome))]
                private System.Threading.Tasks.ValueTask HandleNavigateToHome(
                    LoginEffect.NavigateToHome effect,
                    System.Threading.CancellationToken cancellationToken)
                {
                    return System.Threading.Tasks.ValueTask.CompletedTask;
                }

                [MviEffect(typeof(LoginEffect.ShowToast))]
                private System.Threading.Tasks.ValueTask HandleShowToast(
                    LoginEffect.ShowToast effect,
                    System.Threading.CancellationToken cancellationToken)
                {
                    return System.Threading.Tasks.ValueTask.CompletedTask;
                }
            }
        }
        """;

    /// <summary>
    /// 验证生成器为分派器 emit DispatchCoreAsync 的 switch 分派，且产物可编译。
    /// </summary>
    [Test]
    public async Task Generator_Should_EmitDispatchCoreAsyncSwitchAsync()
    {
        (GeneratorDriverRunResult runResult, bool emitSuccess) =
            GeneratorTestHost.RunGeneratorAndCompile<MviEffectDispatchGenerator>(
                RuntimeStubs + ValidDispatcher);

        await Assert.That(emitSuccess).IsTrue();
        string generated = string.Join("\n", runResult.GeneratedTrees.Select(tree => tree.GetText().ToString()));
        await Assert.That(generated).Contains("DispatchCoreAsync");
        await Assert.That(generated).Contains("HandleNavigateToHome");
        await Assert.That(generated).Contains("HandleShowToast");
    }

    /// <summary>
    /// 验证缺少处理方法的副作用子类型报告 MVI0013 警告。
    /// </summary>
    [Test]
    public async Task Generator_Should_WarnWhenEffectSubtypeMissingHandlerAsync()
    {
        const string source = RuntimeStubs + """

            namespace TestFeature
            {
                using MiKiNuo.Mvi.Domain.MVI.Effect;
                using MiKiNuo.Mvi.Application.MVI.Effect;

                public sealed partial class LoginEffectDispatcher
                    : MviEffectDispatcherBase<LoginIntent, LoginEffect>
                {
                    [MviEffect(typeof(LoginEffect.NavigateToHome))]
                    private System.Threading.Tasks.ValueTask HandleNavigateToHome(
                        LoginEffect.NavigateToHome effect,
                        System.Threading.CancellationToken cancellationToken)
                    {
                        return System.Threading.Tasks.ValueTask.CompletedTask;
                    }
                }
            }
            """;

        GeneratorDriverRunResult runResult = GeneratorTestHost.RunGenerator<MviEffectDispatchGenerator>(source);

        await Assert.That(runResult.Diagnostics.Any(d => d.Id == "MVI0013")).IsTrue();
    }

    /// <summary>
    /// 验证同一副作用子类型被多个方法标记时报告 MVI0014 错误。
    /// </summary>
    [Test]
    public async Task Generator_Should_ErrorWhenEffectSubtypeDuplicatedAsync()
    {
        const string source = RuntimeStubs + """

            namespace TestFeature
            {
                using MiKiNuo.Mvi.Domain.MVI.Effect;
                using MiKiNuo.Mvi.Application.MVI.Effect;

                public sealed partial class LoginEffectDispatcher
                    : MviEffectDispatcherBase<LoginIntent, LoginEffect>
                {
                    [MviEffect(typeof(LoginEffect.NavigateToHome))]
                    private System.Threading.Tasks.ValueTask HandleA(
                        LoginEffect.NavigateToHome effect,
                        System.Threading.CancellationToken cancellationToken)
                    {
                        return System.Threading.Tasks.ValueTask.CompletedTask;
                    }

                    [MviEffect(typeof(LoginEffect.NavigateToHome))]
                    private System.Threading.Tasks.ValueTask HandleB(
                        LoginEffect.NavigateToHome effect,
                        System.Threading.CancellationToken cancellationToken)
                    {
                        return System.Threading.Tasks.ValueTask.CompletedTask;
                    }
                }
            }
            """;

        GeneratorDriverRunResult runResult = GeneratorTestHost.RunGenerator<MviEffectDispatchGenerator>(source);

        await Assert.That(runResult.Diagnostics.Any(d => d.Id == "MVI0014")).IsTrue();
    }

    /// <summary>
    /// 验证签名不符的处理方法报告 MVI0015 错误。
    /// </summary>
    [Test]
    public async Task Generator_Should_ErrorWhenHandlerSignatureInvalidAsync()
    {
        const string source = RuntimeStubs + """

            namespace TestFeature
            {
                using MiKiNuo.Mvi.Domain.MVI.Effect;
                using MiKiNuo.Mvi.Application.MVI.Effect;

                public sealed partial class LoginEffectDispatcher
                    : MviEffectDispatcherBase<LoginIntent, LoginEffect>
                {
                    [MviEffect(typeof(LoginEffect.NavigateToHome))]
                    private System.Threading.Tasks.ValueTask HandleWrong(
                        LoginEffect.NavigateToHome effect)
                    {
                        return System.Threading.Tasks.ValueTask.CompletedTask;
                    }
                }
            }
            """;

        GeneratorDriverRunResult runResult = GeneratorTestHost.RunGenerator<MviEffectDispatchGenerator>(source);

        await Assert.That(runResult.Diagnostics.Any(d => d.Id == "MVI0015")).IsTrue();
    }

    /// <summary>
    /// 验证非 partial 分派器报告 MVI0012 错误。
    /// </summary>
    [Test]
    public async Task Generator_Should_ErrorWhenDispatcherNotPartialAsync()
    {
        const string source = RuntimeStubs + """

            namespace TestFeature
            {
                using MiKiNuo.Mvi.Domain.MVI.Effect;
                using MiKiNuo.Mvi.Application.MVI.Effect;

                public sealed class LoginEffectDispatcher
                    : MviEffectDispatcherBase<LoginIntent, LoginEffect>
                {
                    [MviEffect(typeof(LoginEffect.NavigateToHome))]
                    private System.Threading.Tasks.ValueTask HandleNavigateToHome(
                        LoginEffect.NavigateToHome effect,
                        System.Threading.CancellationToken cancellationToken)
                    {
                        return System.Threading.Tasks.ValueTask.CompletedTask;
                    }
                }
            }
            """;

        GeneratorDriverRunResult runResult = GeneratorTestHost.RunGenerator<MviEffectDispatchGenerator>(source);

        await Assert.That(runResult.Diagnostics.Any(d => d.Id == "MVI0012")).IsTrue();
    }
}
