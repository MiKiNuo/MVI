using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.DI;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.Mediator;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.CompositionDemo;

/// <summary>明细已接收名称的版本化事实。</summary>
/// <param name="Name">演示名称。</param>
/// <param name="Version">来源明细的版本。</param>
public sealed record MedicineAdded(string Name, long Version) : IMviNotification;

/// <summary>组合 MVI 自己的摘要状态。</summary>
/// <param name="LastMedicine">最近一次收到的名称。</param>
/// <param name="Version">最近接受的摘要版本。</param>
public sealed record PrescriptionWorkspaceState(string? LastMedicine = null, long Version = 0) : IMviState
{
    /// <summary>获取初始状态。</summary>
    public static PrescriptionWorkspaceState Initial { get; } = new();
}

/// <summary>父实例自己的摘要意图。</summary>
public abstract partial record PrescriptionWorkspaceIntent : IMviIntent
{
    /// <summary>经中介者取得事实后的摘要更新意图。</summary>
    /// <param name="Change">已发生的版本化变化。</param>
    public sealed partial record SummaryChanged(MedicineAdded Change) : PrescriptionWorkspaceIntent;
}

/// <summary>父级只维护自己的摘要，不访问子 Store。</summary>
[MviFeature]
public sealed partial class PrescriptionWorkspaceReducer
    : MviReducerBase<PrescriptionWorkspaceState, PrescriptionWorkspaceIntent, UnitEffect>
{
    /// <summary>忽略过期摘要，提交父级状态。</summary>
    [MviReduce(typeof(PrescriptionWorkspaceIntent.SummaryChanged))]
    private MviReduceResult<PrescriptionWorkspaceState, UnitEffect> HandleSummaryChanged(
        PrescriptionWorkspaceState state,
        PrescriptionWorkspaceIntent.SummaryChanged intent)
    {
        return Unchanged(intent.Change.Version > state.Version
            ? new(intent.Change.Name, intent.Change.Version)
            : state);
    }
}

/// <summary>父级的对外协调点：接纳明细发布的版本化事实。</summary>
public sealed partial class PrescriptionWorkspaceEffectDispatcher
    : MviEffectDispatcherBase<PrescriptionWorkspaceIntent, UnitEffect>
{
    /// <summary>接纳明细事实通知：同步接纳摘要更新意图，拒绝时抛出异常。</summary>
    /// <param name="notification">明细已接收名称的版本化事实。</param>
    [MviNotificationAcceptor(typeof(MedicineAdded))]
    internal void OnMedicineAdded(MedicineAdded notification)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (!TryAcceptIntent(new PrescriptionWorkspaceIntent.SummaryChanged(notification)))
        {
            throw new InvalidOperationException("工作区已关闭或通知队列已满。");
        }
    }
}
