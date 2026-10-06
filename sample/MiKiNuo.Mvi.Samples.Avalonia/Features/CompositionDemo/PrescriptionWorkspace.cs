using System.Collections.Immutable;
using MiKiNuo.Mvi.Abstractions.DI;
using MiKiNuo.Mvi.Abstractions.MVI.Intent;
using MiKiNuo.Mvi.Abstractions.MVI.Mediator;
using MiKiNuo.Mvi.Abstractions.MVI.Mutation;
using MiKiNuo.Mvi.Abstractions.MVI.Reducer;
using MiKiNuo.Mvi.Abstractions.MVI.State;
using MiKiNuo.Mvi.Runtime.MVI.Intent;
using MiKiNuo.Mvi.Runtime.MVI.Mediator;
using MiKiNuo.Mvi.Runtime.MVI.Reducer;
namespace MiKiNuo.Mvi.Samples.Avalonia.Features.CompositionDemo;
/// <summary>版本化明细事实。</summary>
/// <param name="Name">已加入明细的演示名称。</param>
/// <param name="Version">本次明细提交对应的递增版本。</param>
public sealed record MedicineAdded(string Name, long Version) : IMviNotification;
/// <summary>父组件自己的摘要，不持有子组件状态。</summary>
/// <param name="LastMedicine">最近一次接纳的演示名称；尚未接纳时为空。</param>
/// <param name="Version">摘要已接纳的最新明细版本；初始为零。</param>
public sealed record PrescriptionWorkspaceState(string? LastMedicine = null, long Version = 0) : IMviState
{
    /// <summary>获取初始状态。</summary>
    public static PrescriptionWorkspaceState Initial { get; } = new();
}
/// <summary>由明确订阅转换成本地操作。</summary>
/// <param name="Change">通过范围内订阅收到的版本化明细事实。</param>
public sealed record PrescriptionWorkspaceIntent(MedicineAdded Change) : IMviIntent;
/// <summary>更新摘要的内部转换。</summary>
/// <param name="Change">用于更新父组件摘要的版本化明细事实。</param>
public sealed record PrescriptionWorkspaceMutation(MedicineAdded Change) : IMviMutation<PrescriptionWorkspaceState>;
/// <summary>只接受比当前更新的摘要版本。</summary>
public sealed partial class PrescriptionWorkspaceReducer : MviReducerBase<PrescriptionWorkspaceState>
{
    [MviReduce(typeof(PrescriptionWorkspaceMutation))]
    private static PrescriptionWorkspaceState Update(PrescriptionWorkspaceState state, PrescriptionWorkspaceMutation change)
        => change.Change.Version > state.Version ? new(change.Change.Name, change.Change.Version) : state;
}
/// <summary>通过范围内通知接纳本地摘要更新。</summary>
[MviFeature]
public sealed partial class PrescriptionWorkspaceHandler : MviIntentHandler<PrescriptionWorkspaceState, PrescriptionWorkspaceIntent>
{
    [MviHandle(typeof(PrescriptionWorkspaceIntent))]
    private static ValueTask UpdateAsync(PrescriptionWorkspaceIntent intent, IIntentContext<PrescriptionWorkspaceState> context, CancellationToken cancellationToken)
    {
        context.Reduce(new PrescriptionWorkspaceMutation(intent.Change));
        return ValueTask.CompletedTask;
    }
    /// <summary>只承诺接纳，完成状态通过本实例 State 观察。</summary>
    [MviNotificationAcceptor(typeof(MedicineAdded))]
    internal void OnMedicineAdded(MedicineAdded notification)
    {
        if (!TryAcceptIntent(new PrescriptionWorkspaceIntent(notification))) throw new InvalidOperationException("工作区已关闭或通知队列已满。");
    }
}
