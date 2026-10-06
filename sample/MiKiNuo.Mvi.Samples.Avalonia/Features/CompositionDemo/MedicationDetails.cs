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
/// <summary>不可变的已选明细。</summary>
public sealed record MedicationDetailsState : IMviState
{
    /// <summary>获取已选择名称。</summary>
    public ImmutableArray<string> Names { get; init; } = [];
    /// <summary>获取初始状态。</summary>
    public static MedicationDetailsState Initial { get; } = new();
}
/// <summary>接收明确名称的本地意图。</summary>
/// <param name="Name">需要加入明细的演示名称。</param>
public sealed record MedicationDetailsIntent(string Name) : IMviIntent;
/// <summary>新增名称的内部状态转换。</summary>
/// <param name="Name">本次追加到明细列表的名称。</param>
public sealed record MedicationDetailsMutation(string Name) : IMviMutation<MedicationDetailsState>;
/// <summary>纯粹更新不可变明细列表。</summary>
public sealed partial class MedicationDetailsReducer : MviReducerBase<MedicationDetailsState>
{
    [MviReduce(typeof(MedicationDetailsMutation))]
    private static MedicationDetailsState Add(MedicationDetailsState state, MedicationDetailsMutation change)
        => state with { Names = state.Names.Add(change.Name) };
}
/// <summary>明细路由和本地意图共用处理器，不读取其他组件 Store。</summary>
[MviFeature]
public sealed partial class MedicationDetailsHandler(MviMediatorEndpoint endpoint) : MviIntentHandler<MedicationDetailsState, MedicationDetailsIntent>, IDisposable
{
    // 同一明细实例串行处理新增与事实发布，确保版本和名称来自同一次提交。
    private readonly SemaphoreSlim _operations = new(1, 1);
    [MviHandle(typeof(MedicationDetailsIntent))]
    private async ValueTask AddAsync(MedicationDetailsIntent intent, IIntentContext<MedicationDetailsState> context, CancellationToken cancellationToken)
    {
        await _operations.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            context.Reduce(new MedicationDetailsMutation(intent.Name));
            MviNotificationReport report = await endpoint.PublishAsync(new MedicineAdded(intent.Name, context.State.Names.Length), cancellationToken).ConfigureAwait(false);
            if (report.Deliveries.Any(static delivery => delivery.Status != MviNotificationDeliveryStatus.Accepted))
                throw new InvalidOperationException("明细已更新，但摘要未全部接纳；请重新查询摘要。");
        }
        finally { _operations.Release(); }
    }
    /// <summary>由 Feature 所有者在 Store 排空后释放同步资源。</summary>
    public void Dispose() { _operations.Dispose(); GC.SuppressFinalize(this); }
    /// <summary>经范围内路由接收名称，等待本地操作完成。</summary>
    [MviRouteHandler(typeof(SubmitMedicine))]
    internal async ValueTask<MedicineSelectionResult> HandleSubmitAsync(SubmitMedicine request, CancellationToken cancellationToken)
    {
        await DispatchIntentAsync(new MedicationDetailsIntent(request.Name), cancellationToken).ConfigureAwait(false);
        return new(request.Name);
    }
}
