using System.Globalization;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示活动日志规约器。
/// </summary>
public sealed partial class ActivityLogReducer
    : MviReducerBase<ActivityLogState, ActivityLogIntent, ActivityLogEffect>
{
    /// <summary>处理追加日志条目。</summary>
    [MviReduce(typeof(ActivityLogIntent.AppendEntry))]
    private MviReduceResult<ActivityLogState, ActivityLogEffect> HandleAppendEntry(
        ActivityLogState state,
        ActivityLogIntent.AppendEntry intent)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        string newLog = string.Concat(state.ActivityLog, "[", timestamp, "] ", intent.Message, Environment.NewLine);
        ActivityLogState newState = state with { ActivityLog = newLog };
        return MviReduceResult.StateAndEffect<ActivityLogState, ActivityLogEffect>(
            newState,
            new ActivityLogEffect.Trace("ActivityLog AppendEntry"));
    }
}
