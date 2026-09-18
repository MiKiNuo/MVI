using System.Collections.Immutable;
using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.DI;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.Mediator;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.CompositionDemo;

/// <summary>只记录名称与计数的明细状态。</summary>
public sealed record MedicationDetailsState : IMviState
{
    /// <summary>获取已选择名称的不可变列表。</summary>
    public ImmutableArray<string> Names { get; init; } = [];

    /// <summary>获取初始状态。</summary>
    public static MedicationDetailsState Initial { get; } = new();
}

/// <summary>明细自己的意图。</summary>
public abstract partial record MedicationDetailsIntent : IMviIntent
{
    /// <summary>接收已经选中的演示名称。</summary>
    /// <param name="Name">演示名称。</param>
    public sealed partial record Add(string Name) : MedicationDetailsIntent;
}

/// <summary>明细提交后发布已发生事实。</summary>
/// <param name="Change">已发生的版本化变化。</param>
public sealed record MedicationDetailsEffect(MedicineAdded Change) : IMviEffect;

/// <summary>明细状态的唯一修改入口。</summary>
[MviFeature]
public sealed partial class MedicationDetailsReducer
    : MviReducerBase<MedicationDetailsState, MedicationDetailsIntent, MedicationDetailsEffect>
{
    /// <summary>规约明细自己的名称列表，并声明版本化事实副作用。</summary>
    [MviReduce(typeof(MedicationDetailsIntent.Add))]
    private MviReduceResult<MedicationDetailsState, MedicationDetailsEffect> HandleAdd(
        MedicationDetailsState state,
        MedicationDetailsIntent.Add intent)
    {
        return WithEffect(
            state with { Names = state.Names.Add(intent.Name) },
            new(new(intent.Name, state.Names.Length + 1)));
    }
}

/// <summary>明细独占的副作用分发器：发布版本化事实，并承载名称接收路由。</summary>
public sealed partial class MedicationDetailsEffectDispatcher(MviMediatorEndpoint endpoint)
    : MviEffectDispatcherBase<MedicationDetailsIntent, MedicationDetailsEffect>
{
    [MviEffect(typeof(MedicationDetailsEffect))]
    private async ValueTask HandleAsync(MedicationDetailsEffect effect, CancellationToken cancellationToken)
    {
        MviNotificationReport report = await endpoint.PublishAsync(effect.Change, cancellationToken);
        if (report.Deliveries.Any(delivery => delivery.Status != MviNotificationDeliveryStatus.Accepted))
            throw new InvalidOperationException("明细已更新，但摘要未能全部接纳，请重新查询摘要。");
    }

    /// <summary>处理名称提交请求：回流本地新增意图并返回明确接收结果。</summary>
    /// <param name="request">名称提交请求。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>已接收名称的结果。</returns>
    [MviRouteHandler(typeof(SubmitMedicine))]
    internal async ValueTask<MedicineSelectionResult> HandleSubmitAsync(
        SubmitMedicine request,
        CancellationToken cancellationToken)
    {
        await DispatchIntentAsync(new MedicationDetailsIntent.Add(request.Name), cancellationToken);
        return new MedicineSelectionResult(request.Name);
    }
}
