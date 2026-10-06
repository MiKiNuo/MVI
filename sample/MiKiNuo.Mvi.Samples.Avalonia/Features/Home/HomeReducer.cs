namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Home;
/// <summary>大厅的纯状态转换。</summary>
public sealed partial class HomeReducer : MviReducerBase<HomeState>
{
    [MviReduce(typeof(HomeMutation))]
    private static HomeState Change(HomeState state, HomeMutation change) => new(change.DisplayName);
}
