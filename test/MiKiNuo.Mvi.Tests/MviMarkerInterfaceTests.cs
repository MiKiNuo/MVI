using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.State;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Home;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Register;
using MiKiNuo.Mvi.Samples.Avalonia.Features.ResetPassword;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 MVI 标记接口回归测试：示例各 Feature 的 State/Intent/Effect 必须实现对应标记接口。
/// </summary>
public sealed class MviMarkerInterfaceTests
{
    /// <summary>
    /// 验证示例各 Feature 的状态实现 IMviState。
    /// </summary>
    [Test]
    public async Task SampleStates_Should_ImplementStateMarkerAsync()
    {
        await Assert.That(typeof(IMviState).IsAssignableFrom(typeof(LoginState))).IsTrue();
        await Assert.That(typeof(IMviState).IsAssignableFrom(typeof(RegisterState))).IsTrue();
        await Assert.That(typeof(IMviState).IsAssignableFrom(typeof(ResetPasswordState))).IsTrue();
        await Assert.That(typeof(IMviState).IsAssignableFrom(typeof(HomeState))).IsTrue();
        await Assert.That(typeof(IMviState).IsAssignableFrom(typeof(AppShellState))).IsTrue();
    }

    /// <summary>
    /// 验证示例各 Feature 的意图实现 IMviIntent。
    /// </summary>
    [Test]
    public async Task SampleIntents_Should_ImplementIntentMarkerAsync()
    {
        await Assert.That(typeof(IMviIntent).IsAssignableFrom(typeof(LoginIntent))).IsTrue();
        await Assert.That(typeof(IMviIntent).IsAssignableFrom(typeof(RegisterIntent))).IsTrue();
        await Assert.That(typeof(IMviIntent).IsAssignableFrom(typeof(ResetPasswordIntent))).IsTrue();
        await Assert.That(typeof(IMviIntent).IsAssignableFrom(typeof(HomeIntent))).IsTrue();
        await Assert.That(typeof(IMviIntent).IsAssignableFrom(typeof(AppShellIntent))).IsTrue();
    }

    /// <summary>
    /// 验证示例各 Feature 的副作用实现 IMviEffect，应用壳使用 UnitEffect。
    /// </summary>
    [Test]
    public async Task SampleEffects_Should_ImplementEffectMarkerAsync()
    {
        await Assert.That(typeof(IMviEffect).IsAssignableFrom(typeof(LoginEffect))).IsTrue();
        await Assert.That(typeof(IMviEffect).IsAssignableFrom(typeof(RegisterEffect))).IsTrue();
        await Assert.That(typeof(IMviEffect).IsAssignableFrom(typeof(ResetPasswordEffect))).IsTrue();
        await Assert.That(typeof(IMviEffect).IsAssignableFrom(typeof(HomeEffect))).IsTrue();
        await Assert.That(typeof(IMviEffect).IsAssignableFrom(typeof(UnitEffect))).IsTrue();
    }
}
