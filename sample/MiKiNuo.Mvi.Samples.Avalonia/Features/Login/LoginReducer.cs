namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
/// <summary>仅处理 LoginMutation 的纯状态转换。</summary>
public sealed partial class LoginReducer : MviReducerBase<LoginState>
{
    [MviReduce(typeof(LoginMutation.Started))]
    private static LoginState Start(LoginState state, LoginMutation.Started _) => state with { IsBusy = true, ErrorMessage = null, Notice = null };
    [MviReduce(typeof(LoginMutation.Failed))]
    private static LoginState Fail(LoginState state, LoginMutation.Failed change) => state with { IsBusy = false, ErrorMessage = change.Message };
    [MviReduce(typeof(LoginMutation.Finished))]
    private static LoginState Finish(LoginState state, LoginMutation.Finished change) => state with { IsBusy = false, Notice = change.Notice };
}
