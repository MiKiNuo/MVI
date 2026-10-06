using MiKiNuo.Mvi.Abstractions.MVI.Mediator;
using MiKiNuo.Mvi.Runtime.MVI.Mediator;
using MiKiNuo.Mvi.Runtime.MVI.Store;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Register;
using MiKiNuo.Mvi.Samples.Avalonia.Features.ResetPassword;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;
using TUnit.Assertions;
using TUnit.Core;
namespace MiKiNuo.Mvi.Tests;
/// <summary>通过真实登录 ViewModel、Handler 与 Reducer 验证完整业务边界。</summary>
public sealed class SampleLoginFlowTests
{
    /// <summary>命令先捕获输入再清空密码，服务看到正确的快照。</summary>
    [Test] public async Task GeneratedCommandCapturesBeforeClearingPasswordAsync()
    {
        ProbeAuthApi api = new(); ProbeMediator mediator = new();
        await using var store = new MviStore<LoginState, LoginIntent>(LoginState.Initial, new LoginHandler(api, mediator), new LoginReducer());
        using LoginViewModel model = new(store) { UserName = "alice", Password = "secret" };
        await model.SubmitCommand.ExecuteAsync(null);
        await Assert.That(api.LastCredentials).IsEqualTo("alice:secret");
        await Assert.That(model.Password).IsEqualTo("");
        await Assert.That(model.State.IsBusy).IsFalse();
        await Assert.That(mediator.Requests.Single().Page).IsEqualTo(ShellPage.Home);
    }
    /// <summary>失败显示业务错误并释放 Busy，重试仍然可用。</summary>
    [Test] public async Task FailureResetsBusyAndPreservesMessageAsync()
    {
        ProbeAuthApi api = new() { Result = AuthResult.Failure("invalid") };
        await using var store = new MviStore<LoginState, LoginIntent>(LoginState.Initial, new LoginHandler(api, new ProbeMediator()), new LoginReducer());
        using LoginViewModel model = new(store) { UserName = "alice", Password = "bad" };
        await model.SubmitCommand.ExecuteAsync(null);
        await Assert.That(model.State.ErrorMessage).IsEqualTo("invalid");
        await Assert.That(model.State.IsBusy).IsFalse();
    }
    /// <summary>直接派发也不能绕过 Busy 守卫；等待导航时仍不可重提。</summary>
    [Test] public async Task BusyGuardProtectsEveryEntryPointAsync()
    {
        TaskCompletionSource entered = TestCheck.Signal(), release = TestCheck.Signal();
        ProbeAuthApi api = new() { Login = async token => { entered.TrySetResult(); await release.Task.WaitAsync(token); return AuthResult.Success("alice"); } };
        await using var store = new MviStore<LoginState, LoginIntent>(LoginState.Initial, new LoginHandler(api, new ProbeMediator()), new LoginReducer());
        using LoginViewModel model = new(store) { UserName = "alice", Password = "first" };
        Task first = model.SubmitCommand.ExecuteAsync(null).AsTask();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        try
        {
            model.UserName = "edited"; model.Password = "second";
            await store.DispatchAsync(new LoginIntent.Submit("edited", "second"));
            await model.GoRegisterCommand.ExecuteAsync(null);
            await Assert.That(api.LoginCalls).IsEqualTo(1);
            await Assert.That(api.LastCredentials).IsEqualTo("alice:first");
        }
        finally { release.TrySetResult(); await first; }
    }
    /// <summary>即使服务忽略取消并返回成功，也不会继续导航。</summary>
    [Test] public async Task CanceledLateSuccessCannotNavigateAsync()
    {
        TaskCompletionSource entered = TestCheck.Signal(), release = TestCheck.Signal();
        ProbeAuthApi api = new() { Login = async _ => { entered.TrySetResult(); await release.Task; return AuthResult.Success("alice"); } };
        ProbeMediator mediator = new();
        await using var store = new MviStore<LoginState, LoginIntent>(LoginState.Initial, new LoginHandler(api, mediator), new LoginReducer());
        using CancellationTokenSource cancellation = new();
        Task operation = store.DispatchAsync(new LoginIntent.Submit("alice", "secret"), cancellation.Token).AsTask();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5)); cancellation.Cancel(); release.TrySetResult();
        await TestCheck.ThrowsAsync<OperationCanceledException>(() => operation);
        await Assert.That(mediator.Requests.Count).IsEqualTo(0);
        await Assert.That(store.CurrentState.IsBusy).IsFalse();
    }
    /// <summary>日志式 ToString 不输出敏感凭据，状态也不包含密码属性。</summary>
    [Test] public async Task PasswordIsNotPartOfObservableStateAsync()
    {
        await Assert.That(typeof(LoginState).GetProperty("Password")).IsNull();
        await Assert.That(new LoginIntent.Submit("alice", "secret").ToString().Contains("secret", StringComparison.Ordinal)).IsFalse();
    }
}
/// <summary>不联网的可控制认证服务，用于实际 Handler 测试。</summary>
internal sealed class ProbeAuthApi : IAuthService
{
    /// <summary>获取登录调用数。</summary>
    public int LoginCalls { get; private set; }
    /// <summary>获取表单调用数。</summary>
    public int FormCalls { get; private set; }
    /// <summary>获取测试观察的输入快照。</summary>
    public string? LastCredentials { get; private set; }
    /// <summary>设置普通返回结果。</summary>
    public AuthResult Result { get; set; } = AuthResult.Success("alice");
    /// <summary>设置受控的登录操作。</summary>
    public Func<CancellationToken, Task<AuthResult>>? Login { get; set; }
    /// <summary>记录快照并执行受控任务。</summary>
    public Task<AuthResult> LoginAsync(string userName, string password, CancellationToken cancellationToken)
    { LoginCalls++; LastCredentials = userName + ":" + password; return Login is null ? Task.FromResult(Result) : Login(cancellationToken); }
    /// <summary>返回注册结果。</summary>
    public Task<AuthResult> RegisterAsync(string userName, string email, string password, CancellationToken cancellationToken)
    { FormCalls++; return Task.FromResult(Result); }
    /// <summary>返回密码重置结果。</summary>
    public Task<AuthResult> ResetPasswordAsync(string userName, string newPassword, CancellationToken cancellationToken)
    { FormCalls++; return Task.FromResult(Result); }
}
/// <summary>只记录当前样例的显式导航请求。</summary>
internal sealed class ProbeMediator : IMviMediator
{
    /// <summary>获取已完成的导航请求。</summary>
    public List<NavigateToPageRequest> Requests { get; } = [];
    /// <summary>接收类型化请求。</summary>
    public ValueTask<TResponse> SendAsync<TResponse>(IMviRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request is not NavigateToPageRequest navigation) throw new InvalidOperationException("意外请求。");
        Requests.Add(navigation); return ValueTask.FromResult((TResponse)(object)true);
    }
}
