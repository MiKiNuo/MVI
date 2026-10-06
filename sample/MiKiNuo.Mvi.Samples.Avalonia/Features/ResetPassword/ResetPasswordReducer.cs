namespace MiKiNuo.Mvi.Samples.Avalonia.Features.ResetPassword;
/// <summary>仅处理 ResetPasswordMutation 的纯状态转换。</summary>
public sealed partial class ResetPasswordReducer : MviReducerBase<ResetPasswordState>
{
    [MviReduce(typeof(ResetPasswordMutation.Started))]
    private static ResetPasswordState Start(ResetPasswordState state, ResetPasswordMutation.Started _) => state with { IsBusy = true, ErrorMessage = null, Notice = null };
    [MviReduce(typeof(ResetPasswordMutation.Failed))]
    private static ResetPasswordState Fail(ResetPasswordState state, ResetPasswordMutation.Failed change) => state with { IsBusy = false, ErrorMessage = change.Message };
    [MviReduce(typeof(ResetPasswordMutation.Finished))]
    private static ResetPasswordState Finish(ResetPasswordState state, ResetPasswordMutation.Finished change) => state with { IsBusy = false, Notice = change.Notice };
}
