using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 ViewModel 绑定测试。
/// </summary>
public sealed class LoginViewModelBindingTests
{
    /// <summary>
    /// 验证双向属性 setter 会派发 Intent 并通过 R3 状态流回写。
    /// </summary>
    [Test]
    public async Task TwoWayBindingSetter_Should_DispatchIntentAsync()
    {
        using MviStore<LoginState, LoginIntent, LoginEffect> store = new(
            LoginState.Initial,
            new LoginIntentHandler(new FakeAuthService()),
            new LoginReducer(),
            new EmptyLoginEffectDispatcher());
        using LoginViewModel viewModel = new(store);

        viewModel.UserName = "admin";
        await Task.Delay(50);

        await Assert.That(store.CurrentState.UserName).IsEqualTo("admin");
    }

    /// <summary>
    /// 验证命令可执行状态由 R3 CanExecute 流驱动。
    /// </summary>
    [Test]
    public async Task CommandCanExecute_Should_BeDrivenByStateStreamAsync()
    {
        using MviStore<LoginState, LoginIntent, LoginEffect> store = new(
            LoginState.Initial,
            new LoginIntentHandler(new FakeAuthService()),
            new LoginReducer(),
            new EmptyLoginEffectDispatcher());
        using LoginViewModel viewModel = new(store);

        viewModel.UserName = "admin";
        viewModel.Password = "123456";
        await Task.Delay(50);

        await Assert.That(viewModel.SubmitCommand.CanExecute(null)).IsTrue();
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
