using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Reducer;
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
public sealed record PrescriptionWorkspaceState(string? LastMedicine = null, long Version = 0) : IMviState;

/// <summary>父实例自己的摘要更新意图。</summary>
/// <param name="Change">经中介者取得的事实。</param>
public sealed record PrescriptionWorkspaceIntent(MedicineAdded Change) : IMviIntent;

/// <summary>父级只维护自己的摘要，不访问子 Store。</summary>
internal sealed class PrescriptionWorkspaceReducer : IMviReducer<PrescriptionWorkspaceState, PrescriptionWorkspaceIntent, UnitEffect>
{
    /// <summary>忽略过期摘要，提交父级状态。</summary>
    /// <param name="state">父级状态。</param>
    /// <param name="intent">本地意图。</param>
    /// <returns>更新结果。</returns>
    public MviReduceResult<PrescriptionWorkspaceState, UnitEffect> Reduce(PrescriptionWorkspaceState state, PrescriptionWorkspaceIntent intent)
        => MviReduceResult.State<PrescriptionWorkspaceState, UnitEffect>(intent.Change.Version > state.Version
            ? new(intent.Change.Name, intent.Change.Version) : state);
}

/// <summary>父级的独占空副作用通道。</summary>
internal sealed class PrescriptionWorkspaceDispatcher : IMviEffectDispatcher<UnitEffect>
{
    /// <summary>完成无副作用操作。</summary>
    /// <param name="effect">空副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>完成任务。</returns>
    public ValueTask DispatchAsync(UnitEffect effect, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
}
