using System.Collections.Immutable;
using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.CompositionDemo;

/// <summary>只记录名称与计数的明细状态。</summary>
public sealed record MedicationDetailsState : IMviState
{
    /// <summary>获取已选择名称的不可变列表。</summary>
    public ImmutableArray<string> Names { get; init; } = [];
}

/// <summary>明细自己的意图。</summary>
public abstract record MedicationDetailsIntent : IMviIntent
{
    /// <summary>接收已经选中的演示名称。</summary>
    /// <param name="Name">演示名称。</param>
    public sealed record Add(string Name) : MedicationDetailsIntent;
}

/// <summary>明细提交后发布已发生事实。</summary>
/// <param name="Change">已发生的版本化变化。</param>
public sealed record MedicationDetailsEffect(MedicineAdded Change) : IMviEffect;

/// <summary>明细状态的唯一修改入口。</summary>
internal sealed class MedicationDetailsReducer : IMviReducer<MedicationDetailsState, MedicationDetailsIntent, MedicationDetailsEffect>
{
    /// <summary>规约明细自己的名称列表。</summary>
    /// <param name="state">本实例状态。</param>
    /// <param name="intent">本实例意图。</param>
    /// <returns>新状态与副作用。</returns>
    public MviReduceResult<MedicationDetailsState, MedicationDetailsEffect> Reduce(MedicationDetailsState state, MedicationDetailsIntent intent)
        => intent is MedicationDetailsIntent.Add add
            ? new(state with { Names = state.Names.Add(add.Name) }, [new(new(add.Name, state.Names.Length + 1))])
            : new(state, []);
}

/// <summary>明细独占的副作用分发器。</summary>
internal sealed partial class MedicationDetailsEffectDispatcher(MviMediatorEndpoint endpoint) : MviEffectDispatcherBase<MedicationDetailsIntent, MedicationDetailsEffect>
{
    [MviEffect(typeof(MedicationDetailsEffect))]
    private async ValueTask HandleAsync(MedicationDetailsEffect effect, CancellationToken cancellationToken)
    {
        MviNotificationReport report = await endpoint.PublishAsync(effect.Change, cancellationToken);
        if (report.Deliveries.Any(delivery => delivery.Status != MviNotificationDeliveryStatus.Accepted))
            throw new InvalidOperationException("明细已更新，但摘要未能全部接纳，请重新查询摘要。");
    }
}
