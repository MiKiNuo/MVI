using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示实例化 Reducer 模型测试。
/// </summary>
public sealed class ReducerInstanceModelTests
{
    /// <summary>
    /// 验证登录规约器实现统一的 MVI 规约器抽象。
    /// </summary>
    [Test]
    public async Task LoginReducer_Should_ImplementMviReducerAbstractionAsync()
    {
        IMviReducer<LoginState, LoginIntent, LoginEffect> reducer = new LoginReducer();

        await Assert.That(reducer).IsNotNull();
    }

    /// <summary>
    /// 验证源生成的实例规约入口可以根据具体意图完成状态转换。
    /// </summary>
    [Test]
    public async Task GeneratedReduceOverride_Should_DispatchConcreteIntentAsync()
    {
        IMviReducer<LoginState, LoginIntent, LoginEffect> reducer = new LoginReducer();

        MviReduceResult<LoginState, LoginEffect> result = reducer.Reduce(
            LoginState.Initial,
            new LoginIntent.ChangeUserName("admin"));

        await Assert.That(result.State.UserName).IsEqualTo("admin");
    }
}
