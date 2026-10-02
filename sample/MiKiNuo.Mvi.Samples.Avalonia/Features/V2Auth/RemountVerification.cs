using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.V2Auth;

internal static class RemountVerification
{
    private static readonly TimeSpan Watchdog = TimeSpan.FromSeconds(10);

    internal static async Task<string> RunAsync(RemountWindow window)
    {
        Require(window.TryGetPlatformHandle()?.Handle != IntPtr.Zero && window.TryGetPlatformHandle() is not null, "真实 HWND");
        Require(Dispatcher.UIThread.CheckAccess(), "原生 UI 线程");
        window.Host.Unmount();
        VerificationAuthService service = new();
        LoginFeature first = new(service);
        LoginFeature second = new(service);
        int factories = 0;
        LoginForm Make(LoginFeature feature) { factories++; return new LoginForm(feature); }
        window.Host.Mount(first, Make);
        LoginForm original = (LoginForm)window.Host.Content!;
        original.UserNameInput.Text = "first";
        original.PasswordInput.Text = "password";
        window.Host.Mount(first, Make);
        Require(factories == 1 && ReferenceEquals(window.Host.Content, original), "稳定同实例挂载只创建一次 View");
        await FlushAsync();
        Require(TopLevel.GetTopLevel(original) == window && original.Bounds.Width > 0 && original.Bounds.Height > 0,
            "ContentControl 原生主题呈现真实表单");

        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<AuthResult> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        service.Handler = async (_, _, _, _) => { entered.SetResult(); return await response.Task; };
        Task<OperationResult<AuthResult>> execution = first.SubmitAsync();
        try
        {
            await entered.Task.WaitAsync(Watchdog);
            window.Host.Unmount();
            original.UserNameInput.Text = "detached old input";
            Require(first.Snapshot.State.UserName == "first" && !first.IsClosed, "卸载断开旧 TextBox 且不关闭实例");
            first.SetUserName("edited while absent");
            window.Host.Mount(first, Make);
            LoginForm running = (LoginForm)window.Host.Content!;
            await FlushAsync();
            Require(running.UserNameInput.Text == "edited while absent" && running.Feedback.Loading.IsVisible,
                "在途重挂载恢复最新输入与运行状态");
            window.Host.Unmount();
            response.SetResult(AuthResult.Success("retained result"));
            Require((await execution.WaitAsync(Watchdog)).Kind == OperationResultKind.Completed, "View 卸载期间 IO 继续完成");
            window.Host.Mount(first, Make);
            LoginForm completed = (LoginForm)window.Host.Content!;
            await FlushAsync();
            Require(completed.Projection.Snapshot.State.Result?.DisplayName == "retained result" && !completed.Feedback.Loading.IsVisible,
                "完成后重挂载显示最新业务结果");

            first.SetUserName("pending old callback");
            second.SetUserName("second instance");
            window.Host.Mount(second, Make);
            LoginForm replacement = (LoginForm)window.Host.Content!;
            completed.UserNameInput.Text = "invalid old write";
            await FlushAsync();
            Require(replacement.UserNameInput.Text == "second instance" && first.Snapshot.State.UserName == "pending old callback",
                "替换实例隔离状态与旧回调");
            bool failed = false;
            try { window.Host.Mount(first, static _ => ThrowView()); }
            catch (InvalidOperationException) { failed = true; }
            Require(failed && ReferenceEquals(window.Host.Content, replacement) && !first.IsClosed && !second.IsClosed,
                "新工厂失败保留旧合法挂载");

            window.Surface.Children.Remove(window.Host);
            replacement.UserNameInput.Text = "old visual detach";
            second.SetUserName("changed off tree");
            window.Surface.Children.Add(window.Host);
            LoginForm reattached = (LoginForm)window.Host.Content!;
            await FlushAsync();
            Require(!ReferenceEquals(reattached, replacement) && reattached.UserNameInput.Text == "changed off tree" && !second.IsClosed,
                "视觉 Detach/Attach 释放旧连接并创建最新投影");

            window.Surface.Children.Remove(window.Host);
            int factoriesBeforeClosedAttach = factories;
            CloseResult secondClosed = second.Close();
            window.Surface.Children.Add(window.Host);
            await FlushAsync();
            Require(factories == factoriesBeforeClosedAttach && window.Host.Content is null,
                "Detached 实例逻辑关闭后 Attach 不调用工厂或挂载已闭 View");
            Require((await secondClosed.Ticket.Released.WaitAsync(Watchdog)).Succeeded, "原实例按正常生命周期释放");

            // 以下内容保留验收使用仍由窗口逻辑所有者管理的第一实例。
            window.Host.Mount(first, Make);

            TextBlock unrelated = new() { Text = "caller content" };
            window.Host.Content = unrelated;
            window.Host.Unmount();
            Require(ReferenceEquals(window.Host.Content, unrelated), "Unmount 不清调用者无关 Content");
            window.Host.Content = null;
            window.Host.Mount(first, Make);
            LoginForm beforeExternalContent = (LoginForm)window.Host.Content!;
            window.Host.Content = unrelated;
            window.Host.Mount(first, Make);
            Require(window.Host.Content is LoginForm afterExternalContent
                && !ReferenceEquals(afterExternalContent, beforeExternalContent), "同实例失去自有 Content 后释放旧投影再重建");
            window.Host.Unmount();
            using (global::MiKiNuo.Mvi.Platforms.Avalonia.AvaloniaFeatureHost disposable = new())
            {
                disposable.Mount(first, Make);
                disposable.Content = unrelated;
                disposable.Dispose();
                Require(ReferenceEquals(disposable.Content, unrelated) && !first.IsClosed, "Dispose 不清外部 Content 或关闭实例");
            }

            RegisterFeature register = new(service);
            ResetPasswordFeature reset = new(service);
            window.Host.Mount(register, static instance => new RegisterForm(instance));
            Require(ReferenceEquals(((RegisterForm)window.Host.Content!).Feature, register), "注册表单复用既有实例");
            window.Host.Mount(reset, static instance => new ResetPasswordForm(instance));
            Require(ReferenceEquals(((ResetPasswordForm)window.Host.Content!).Feature, reset), "重置表单复用既有实例");
            window.Host.Unmount();
            register.Close();
            reset.Close();
            await VerifyClosingScopeAsync(window);
            return "PASS v2-remount: real Windows HWND and UI thread; native ContentControl template and visible form bounds; stable same-instance mount creates one View; old TextBox disconnected; IO continues while unmounted; remount restores latest state/result/loading; instance replacement and old queued callbacks isolated; factory failure retains previous mount; visual detach/attach recreates projection; detached Feature close prevents auto-attach factory and closed View; unrelated Content preserved; all auth forms accept existing Features; logical close unmount does not await scoped uncooperative IO; scope released only after real exit.";
        }
        finally
        {
            response.TrySetResult(AuthResult.Success("cleanup"));
            window.Host.Unmount();
            await execution.WaitAsync(Watchdog);
            await first.Close().Ticket.Released.WaitAsync(Watchdog);
            await second.Close().Ticket.Released.WaitAsync(Watchdog);
        }
    }

    private static async Task VerifyClosingScopeAsync(RemountWindow window)
    {
        RemountCall call = new();
        ServiceCollection services = new();
        services.AddSingleton(call);
        services.AddScoped<IAuthService, RemountScopedService>();
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        LoginFeature feature = await FeatureFactory.CreateAsync<LoginFeature>(provider);
        window.Host.Mount(feature, static instance => new LoginForm(instance));
        LoginForm form = (LoginForm)window.Host.Content!;
        form.UserNameInput.Text = "scoped";
        form.PasswordInput.Text = "password";
        Task<OperationResult<AuthResult>> execution = feature.SubmitAsync();
        try
        {
            await call.Entered.Task.WaitAsync(Watchdog);
            CloseResult closed = feature.Close();
            window.Host.Unmount();
            Require(!closed.Ticket.Released.IsCompleted && call.Disposed == 0, "关闭可立即卸载且范围仍由 IO 持有");
            await FlushAsync();
            call.Response.SetResult(AuthResult.Success("late"));
            Require((await execution.WaitAsync(Watchdog)).Kind == OperationResultKind.Canceled, "关闭后不合作 IO 真实退出而迟到结果不写回");
            Require((await closed.Ticket.Released.WaitAsync(Watchdog)).Succeeded && call.Disposed == 1 && call.UsedAfterClose,
                "IO 尾部仍可使用 scoped 服务，之后只释放一次");
        }
        finally
        {
            call.Response.TrySetResult(AuthResult.Success("cleanup"));
            await execution.WaitAsync(Watchdog);
            window.Host.Unmount();
            await feature.Close().Ticket.Released.WaitAsync(Watchdog);
        }
    }

    private static LoginForm ThrowView() => throw new InvalidOperationException("controlled view factory failure");
    private static async Task FlushAsync() => await Dispatcher.UIThread.InvokeAsync(static () => { }, DispatcherPriority.Background);
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

internal sealed class RemountCall
{
    internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource<AuthResult> Response { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal int Disposed { get; set; }
    internal bool UsedAfterClose { get; set; }
}

/// <summary>刻意忽略取消以验证真实退出资源归属的认证服务。</summary>
/// <param name="call">由验收控制的调用与释放记录。</param>
internal sealed class RemountScopedService(RemountCall call) : IAuthService, IAsyncDisposable
{
    /// <summary>保持范围资源直到受控外部调用真实返回。</summary>
    /// <param name="userName">本次登录名称。</param>
    /// <param name="password">本次登录密码。</param>
    /// <param name="cancellationToken">刻意不合作的执行取消令牌。</param>
    /// <returns>受控认证响应。</returns>
    public async Task<AuthResult> LoginAsync(string userName, string password, CancellationToken cancellationToken)
    {
        call.Entered.SetResult();
        AuthResult result = await call.Response.Task.ConfigureAwait(false);
        ObjectDisposedException.ThrowIf(call.Disposed != 0, this);
        call.UsedAfterClose = true;
        return result;
    }

    /// <summary>提供本验收未使用的注册契约。</summary>
    /// <param name="userName">注册名称。</param>
    /// <param name="email">注册邮箱。</param>
    /// <param name="password">注册密码。</param>
    /// <param name="cancellationToken">执行取消令牌。</param>
    /// <returns>注册完成结果。</returns>
    public Task<AuthResult> RegisterAsync(string userName, string email, string password, CancellationToken cancellationToken)
        => Task.FromResult(AuthResult.Success(userName));
    /// <summary>提供本验收未使用的重置契约。</summary>
    /// <param name="userName">重置名称。</param>
    /// <param name="newPassword">重置密码。</param>
    /// <param name="cancellationToken">执行取消令牌。</param>
    /// <returns>重置完成结果。</returns>
    public Task<AuthResult> ResetPasswordAsync(string userName, string newPassword, CancellationToken cancellationToken)
        => Task.FromResult(AuthResult.Success(userName));
    /// <summary>记录范围真实释放的唯一调用。</summary>
    /// <returns>释放完成的任务。</returns>
    public ValueTask DisposeAsync() { call.Disposed++; return ValueTask.CompletedTask; }
}
