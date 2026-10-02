using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.V2Auth;

internal static class AuthFormsVerification
{
    internal static async Task<string> RunAsync(AuthFormsWindow window, VerificationAuthService service, string imagePath)
    {
        Require(window.TryGetPlatformHandle()?.Handle != IntPtr.Zero && window.TryGetPlatformHandle() is not null, "真实平台窗口句柄");
        Require(Dispatcher.UIThread.CheckAccess(), "验收运行于 UI 线程");
        await FlushAsync();
        await VerifyFormAsync(window.Login.Feature, window.Login.Projection, window.Login.Feedback,
            [window.Login.UserNameInput, window.Login.PasswordInput], ["neo", "abc123"],
            window.Login.Feature.SubmitAsync, static state => state.UserName, static state => state.Result, service);
        window.Tabs.SelectedIndex = 1;
        await FlushAsync();
        await VerifyFormAsync(window.Register.Feature, window.Register.Projection, window.Register.Feedback,
            [window.Register.UserNameInput, window.Register.EmailInput, window.Register.PasswordInput, window.Register.ConfirmPasswordInput],
            ["neo", "neo@example.com", "abc123", "abc123"], window.Register.Feature.SubmitAsync,
            static state => state.UserName, static state => state.Result, service);
        window.Tabs.SelectedIndex = 2;
        await FlushAsync();
        await VerifyFormAsync(window.ResetPassword.Feature, window.ResetPassword.Projection, window.ResetPassword.Feedback,
            [window.ResetPassword.UserNameInput, window.ResetPassword.NewPasswordInput, window.ResetPassword.ConfirmPasswordInput],
            ["neo", "abc123", "abc123"], window.ResetPassword.Feature.SubmitAsync,
            static state => state.UserName, static state => state.Result, service);

        window.Tabs.SelectedIndex = 0;
        service.Handler = static (_, _, _, _) => Task.FromResult(AuthResult.Success("Neo"));
        await FlushAsync();
        Invoke(window.Login.Feedback.SubmitButton);
        await FlushAsync();
        await window.Login.Projection.SubmitAsyncCommand.Execution!.WaitAsync(TimeSpan.FromSeconds(10));
        await FlushAsync();
        using RenderTargetBitmap bitmap = new(new PixelSize((int)window.ClientSize.Width, (int)window.ClientSize.Height), new Vector(96, 96));
        bitmap.Render(window);
        bitmap.Save(imagePath);
        long before = window.Login.Feature.Snapshot.Version;
        window.Login.Dispose();
        window.Login.UserNameInput.SetCurrentValue(TextBox.TextProperty, "disconnected");
        Require(window.Login.Feature.Snapshot.Version == before, "释放 View 后旧控件不再回写");
        Require(!window.Login.Projection.SubmitAsyncCommand.CanExecute(null), "释放 View 后旧命令禁用");
        return "PASS v2-auth: real Windows Avalonia Window and platform handle; three independent Feature forms; native TextBox inputs and Button automation; command/program validation; validated IO snapshot; editable inputs during Submit loading; duplicate rejected with Busy preserved; success/business failure/swallowed cancellation/fault; safe error text; business service off UI thread; control/PropertyChanged/CanExecuteChanged callbacks on UI thread; await commits without waiting for projection; disposed View disconnected; PNG=" + imagePath;
    }

    private static async Task VerifyFormAsync<TState>(Feature<TState> feature, FeatureProjection<TState> projection,
        AuthFormFeedback<TState> feedback, TextBox[] inputs, string[] values,
        Func<CancellationToken, Task<OperationResult<AuthResult>>> submit, Func<TState, string> name,
        Func<TState, AuthResult?> result, VerificationAuthService service) where TState : notnull
    {
        OperationCommand<AuthResult> command = (OperationCommand<AuthResult>)feedback.SubmitButton.Command!;
        bool correctThread = true;
        int uiThread = Environment.CurrentManagedThreadId;
        void RecordThread() => correctThread &= Dispatcher.UIThread.CheckAccess() && Environment.CurrentManagedThreadId == uiThread;
        projection.PropertyChanged += (_, _) => RecordThread();
        command.CanExecuteChanged += (_, _) => RecordThread();
        foreach (AvaloniaObject control in inputs.Cast<AvaloniaObject>().Concat([feedback.SubmitButton, feedback.Loading, feedback.Error, feedback.Result]))
            control.PropertyChanged += (_, _) => RecordThread();

        int calls = service.Calls;
        Require(!feedback.SubmitButton.IsEffectivelyEnabled && !command.CanExecute(null), "无效初始输入禁用原生按钮");
        command.Execute(null);
        Require((await command.Execution!).Kind == OperationResultKind.Rejected, "直接 UI 命令执行仍受启动验证");
        Require((await submit(CancellationToken.None)).Kind == OperationResultKind.Rejected, "程序提交执行同一验证");
        await FlushAsync();
        Require(service.Calls == calls && !string.IsNullOrEmpty(feedback.Error.Text), "无效提交不调用服务且快照错误可见");

        for (int index = 0; index < inputs.Length; index++) inputs[index].SetCurrentValue(TextBox.TextProperty, values[index]);
        await FlushAsync();
        Require(command.CanExecute(null) && feedback.SubmitButton.IsEffectivelyEnabled, "原生输入使按钮反馈可用");
        TaskCompletionSource<(string Name, string Password, string? Email)> entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<AuthResult> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        service.Handler = (userName, password, email, _) =>
        {
            entered.SetResult((userName, password, email));
            return response.Task;
        };
        Invoke(feedback.SubmitButton);
        await FlushAsync();
        Task<OperationResult<AuthResult>> execution = command.Execution!;
        (string sampledName, string sampledPassword, string? sampledEmail) = await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Require(sampledName == "neo" && sampledPassword == "abc123" && (inputs.Length != 4 || sampledEmail == "neo@example.com"), "IO 使用验证过的开始快照");
        await FlushAsync();
        Require(feedback.Loading.IsVisible && !feedback.SubmitButton.IsEffectivelyEnabled, "仅 Submit 展示原生加载表现");
        Require(inputs.All(static input => input.IsEnabled), "慢 IO 时其他输入保持可编辑");
        inputs[0].SetCurrentValue(TextBox.TextProperty, "new-edit");
        await FlushAsync();
        command.Execute(null);
        Require((await command.Execution!).Kind == OperationResultKind.Rejected, "运行中默认拒绝重复提交");
        await FlushAsync();
        Require(feature.Snapshot.OperationStates["SubmitAsync"].IsRunning && feedback.Loading.IsVisible, "重复拒绝不能清空有效 Busy");
        Require(service.Calls == calls + 1, "重复提交不启动第二次 IO");
        long shownVersion = projection.Snapshot.Version;
        response.SetResult(AuthResult.Success("Neo"));
        Require(execution.Wait(TimeSpan.FromSeconds(10)), "操作完成不等待 UI 绘制");
        Require(execution.Result.Kind == OperationResultKind.Completed && result(feature.Snapshot.State) == AuthResult.Success("Neo"), "await 边界已提交业务结果");
        Require(name(feature.Snapshot.State) == "new-edit" && projection.Snapshot.Version == shownVersion, "反馈保留新编辑且 await 不等待投影");
        await FlushAsync();
        Require(feedback.Result.Text == "成功：Neo" && !feedback.Loading.IsVisible && inputs[0].Text == "new-edit", "成功与继续编辑来自已提交快照");

        inputs[0].SetCurrentValue(TextBox.TextProperty, "neo");
        await FlushAsync();
        entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        service.Handler = (userName, password, email, _) => { entered.SetResult((userName, password, email)); return response.Task; };
        execution = submit(CancellationToken.None);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await FlushAsync();
        Require(feedback.Loading.IsVisible && feedback.Result.Text == string.Empty, "重新启动时隐藏历史成功结果");
        response.SetResult(AuthResult.Failure("服务器拒绝。"));
        Require((await execution.WaitAsync(TimeSpan.FromSeconds(10))).Kind == OperationResultKind.Completed, "业务失败属于正常完成");
        await FlushAsync();
        Require(feedback.Error.Text == "服务器拒绝。" && !feedback.Loading.IsVisible, "业务失败原生错误展示");

        using CancellationTokenSource cancellation = new();
        TaskCompletionSource canceledEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource never = new(TaskCreationOptions.RunContinuationsAsynchronously);
        service.Handler = async (_, _, _, token) =>
        {
            canceledEntered.SetResult();
            try { await never.Task.WaitAsync(token); }
            catch (OperationCanceledException) { return AuthResult.Failure("请求超时或已取消。"); }
            return AuthResult.Success("unexpected");
        };
        execution = submit(cancellation.Token);
        await canceledEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await FlushAsync();
        Require(feedback.Error.Text == string.Empty && feedback.Loading.IsVisible, "取消前重新启动隐藏历史错误");
        cancellation.Cancel();
        Require((await execution.WaitAsync(TimeSpan.FromSeconds(10))).Kind == OperationResultKind.Canceled, "服务吞取消后仍返回执行取消");
        await FlushAsync();
        Require(feedback.Error.Text == "操作已取消。" && feedback.Result.Text == string.Empty && !feedback.Loading.IsVisible, "取消反馈优先于旧业务结果");

        service.Handler = static (_, _, _, _) => throw new InvalidOperationException("PRIVATE_PASSWORD_PAYLOAD");
        Require((await submit(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10))).Kind == OperationResultKind.Faulted, "异常服务返回结构化故障");
        await FlushAsync();
        Require(feedback.Error.Text == "服务暂时不可用，请稍后重试。" && !feedback.Error.Text.Contains("PRIVATE", StringComparison.Ordinal)
            && feedback.Result.Text == string.Empty && !feedback.Loading.IsVisible, "故障安全展示优先于旧业务结果");
        Require(correctThread, "全部原生控件与投影命令通知均在 UI 线程");
    }

    private static void Invoke(Button button) => ((IInvokeProvider)new ButtonAutomationPeer(button)).Invoke();

    private static async Task FlushAsync() => await Dispatcher.UIThread.InvokeAsync(static () => { }, DispatcherPriority.Background);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

internal sealed class VerificationAuthService : IAuthService
{
    private int calls;
    internal int Calls => Volatile.Read(ref calls);
    internal Func<string, string, string?, CancellationToken, Task<AuthResult>> Handler { get; set; }
        = static (_, _, _, _) => throw new InvalidOperationException("无效输入不能调用服务。");

    /// <summary>执行真实窗口验收的可控登录服务。</summary>
    /// <param name="userName">用户名。</param>
    /// <param name="password">密码。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>可控业务结果。</returns>
    public Task<AuthResult> LoginAsync(string userName, string password, CancellationToken cancellationToken)
        => Invoke(userName, password, null, cancellationToken);

    /// <summary>执行真实窗口验收的可控注册服务。</summary>
    /// <param name="userName">用户名。</param>
    /// <param name="email">邮箱。</param>
    /// <param name="password">密码。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>可控业务结果。</returns>
    public Task<AuthResult> RegisterAsync(string userName, string email, string password, CancellationToken cancellationToken)
        => Invoke(userName, password, email, cancellationToken);

    /// <summary>执行真实窗口验收的可控重置密码服务。</summary>
    /// <param name="userName">用户名。</param>
    /// <param name="newPassword">新密码。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>可控业务结果。</returns>
    public Task<AuthResult> ResetPasswordAsync(string userName, string newPassword, CancellationToken cancellationToken)
        => Invoke(userName, newPassword, null, cancellationToken);

    private Task<AuthResult> Invoke(string name, string password, string? email, CancellationToken token)
    {
        if (Dispatcher.UIThread.CheckAccess()) throw new InvalidOperationException("认证 IO 必须在后台执行。");
        Interlocked.Increment(ref calls);
        return Handler(name, password, email, token);
    }
}
