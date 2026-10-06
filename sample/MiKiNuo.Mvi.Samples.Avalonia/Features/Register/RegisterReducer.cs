namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Register;
/// <summary>仅处理 RegisterMutation 的纯状态转换。</summary>
public sealed partial class RegisterReducer : MviReducerBase<RegisterState>
{
    [MviReduce(typeof(RegisterMutation.Started))]
    private static RegisterState Start(RegisterState state, RegisterMutation.Started _) => state with { IsBusy = true, ErrorMessage = null, Notice = null };
    [MviReduce(typeof(RegisterMutation.Failed))]
    private static RegisterState Fail(RegisterState state, RegisterMutation.Failed change) => state with { IsBusy = false, ErrorMessage = change.Message };
    [MviReduce(typeof(RegisterMutation.Finished))]
    private static RegisterState Finish(RegisterState state, RegisterMutation.Finished change) => state with { IsBusy = false, Notice = change.Notice };
}
