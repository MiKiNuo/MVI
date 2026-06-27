using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示活动日志 ViewModel。
/// </summary>
public sealed partial class ActivityLogViewModel : MviViewModelBase<ActivityLogState, ActivityLogIntent, ActivityLogEffect>
{
    /// <summary>
    /// 初始化活动日志 ViewModel。
    /// </summary>
    /// <param name="store">活动日志状态存储。</param>
    /// <param name="uiDispatcher">UI 调度器。</param>
    public ActivityLogViewModel(IMviStore<ActivityLogState, ActivityLogIntent, ActivityLogEffect> store, IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
    }

    /// <summary>获取活动日志。</summary>
    [MviBind(nameof(ActivityLogState.ActivityLog))]
    public partial string ActivityLog { get; private set; }
}
