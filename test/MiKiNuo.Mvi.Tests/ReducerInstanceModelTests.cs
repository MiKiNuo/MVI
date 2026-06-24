using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示变更规约器实例化模型测试。
/// </summary>
public sealed class ReducerInstanceModelTests
{
    /// <summary>
    /// 验证登录变更规约器实现统一的 MVI 变更规约器抽象。
    /// </summary>
    [Test]
    public async Task LoginMutationReducer_Should_ImplementMviMutationReducerAbstractionAsync()
    {
        IMviMutationReducer<LoginState, LoginMutation, LoginEffect> reducer = new LoginMutationReducer();

        await Assert.That(reducer).IsNotNull();
    }

    /// <summary>
    /// 验证源生成的变更规约入口可以根据具体变更完成状态转换。
    /// </summary>
    [Test]
    public async Task GeneratedReduceOverride_Should_DispatchConcreteMutationAsync()
    {
        IMviMutationReducer<LoginState, LoginMutation, LoginEffect> reducer = new LoginMutationReducer();

        MviReduceResult<LoginState, LoginEffect> result = reducer.Reduce(
            LoginState.Initial,
            new LoginMutation.SetUserName("admin"));

        await Assert.That(result.State.UserName).IsEqualTo("admin");
    }
}
