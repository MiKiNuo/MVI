using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Business;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;

/// <summary>
/// 表示 Dashboard 壳规约器。
/// </summary>
public sealed partial class DashboardReducer
    : MviReducerBase<DashboardState, DashboardIntent, UnitEffect>
{
    /// <summary>
    /// 处理显示业务页面意图。
    /// </summary>
    [MviReduce(typeof(DashboardIntent.ShowPage))]
    private MviReduceResult<DashboardState, UnitEffect> HandleShowPage(
        DashboardState state,
        DashboardIntent.ShowPage intent,
        IMviBusinessResult? result)
    {
        return Unchanged(
            state with
            {
                CurrentPageKey = intent.PageKey,
                CurrentPageTitle = intent.PageTitle,
                CurrentPageDescription = intent.PageDescription,
            });
    }
}
