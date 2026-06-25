using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;

/// <summary>
/// 表示 Dashboard 壳规约器。
/// </summary>
public sealed partial class DashboardReducer
    : MviReducerBase<DashboardState, DashboardIntent, DashboardEffect>
{
    /// <summary>
    /// 处理显示业务页面意图。
    /// </summary>
    [MviReduce(typeof(DashboardIntent.ShowPage))]
    private static MviReduceResult<DashboardState, DashboardEffect> HandleShowPage(
        DashboardState state,
        DashboardIntent.ShowPage intent)
    {
        return MviReduceResult.State<DashboardState, DashboardEffect>(
            state with
            {
                CurrentPageKey = intent.PageKey,
                CurrentPageTitle = intent.PageTitle,
                CurrentPageDescription = intent.PageDescription,
            });
    }
}
