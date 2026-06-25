using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 MVI Store 测试。
/// </summary>
public sealed class MviStoreTests
{
    /// <summary>
    /// 验证 Store 可以通过 Intent 更新状态。
    /// </summary>
    [Test]
    public async Task DispatchAsync_Should_UpdateCurrentStateAsync()
    {
        using MviStore<LoginState, LoginIntent, LoginEffect> store = new(
            LoginState.Initial,
            new LoginIntentHandler(new FakeAuthService()),
            new LoginReducer(),
            new EmptyLoginEffectDispatcher());

        await store.DispatchAsync(new LoginIntent.ChangeUserName("admin"));

        await Assert.That(store.CurrentState.UserName).IsEqualTo("admin");
    }

    private sealed class EmptyLoginEffectDispatcher
        : MiKiNuo.Mvi.Application.MVI.Effect.IMviEffectDispatcher<LoginEffect>
    {
        /// <summary>
        /// 分发副作用。
        /// </summary>
        /// <param name="effect">副作用。</param>
        /// <param name="cancellationToken">取消标记。</param>
        /// <returns>表示异步分发过程的任务。</returns>
        public ValueTask DispatchAsync(LoginEffect effect, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
