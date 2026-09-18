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

/// <summary>提交演示药品名称，不包含任何医疗规则。</summary>
/// <param name="Name">演示名称。</param>
public sealed record SubmitMedicine(string Name) : IMviRequest<MedicineSelectionResult>;

/// <summary>明确表示已完成的名称接收结果。</summary>
/// <param name="Name">已接收名称。</param>
public sealed record MedicineSelectionResult(string Name);

/// <summary>药品检索自己的状态。</summary>
public sealed record MedicineSearchState : IMviState
{
    /// <summary>获取匹配的演示名称。</summary>
    public ImmutableArray<string> Results { get; init; } = [];
    /// <summary>获取选择请求是否执行中。</summary>
    public bool IsBusy { get; init; }
    /// <summary>获取明确完成的选择次数。</summary>
    public int CompletedSelections { get; init; }
    /// <summary>获取操作错误。</summary>
    public string? Error { get; init; }

    /// <summary>获取初始状态。</summary>
    public static MedicineSearchState Initial { get; } = new();
}

/// <summary>药品检索意图。</summary>
public abstract partial record MedicineSearchIntent : IMviIntent
{
    /// <summary>按名称检索。</summary>
    /// <param name="Text">检索文本。</param>
    public sealed partial record Search(string Text) : MedicineSearchIntent;
    /// <summary>提交名称选择。</summary>
    /// <param name="Name">检索结果中的名称。</param>
    public sealed partial record Choose(string Name) : MedicineSearchIntent;
    /// <summary>接收已完成结果。</summary>
    public sealed partial record Completed : MedicineSearchIntent;
    /// <summary>接收失败结果。</summary>
    /// <param name="Message">失败信息。</param>
    public sealed partial record Failed(string Message) : MedicineSearchIntent;
}

/// <summary>提交选择的副作用。</summary>
/// <param name="Name">选中的名称。</param>
public sealed record MedicineSearchEffect(string Name) : IMviEffect;

/// <summary>仅规约本实例的检索与选择状态。</summary>
[MviFeature]
public sealed partial class MedicineSearchReducer
    : MviReducerBase<MedicineSearchState, MedicineSearchIntent, MedicineSearchEffect>
{
    private static readonly ImmutableArray<string> Catalog = ["演示药品甲", "演示药品乙"];

    /// <summary>规约检索意图。</summary>
    [MviReduce(typeof(MedicineSearchIntent.Search))]
    private MviReduceResult<MedicineSearchState, MedicineSearchEffect> HandleSearch(
        MedicineSearchState state,
        MedicineSearchIntent.Search intent)
    {
        return Unchanged(state with
        {
            Results = [.. Catalog.Where(name => name.Contains(intent.Text, StringComparison.Ordinal))],
        });
    }

    /// <summary>规约选择意图：守卫空闲后声明提交副作用。</summary>
    [MviReduce(typeof(MedicineSearchIntent.Choose), Guard = nameof(CanChoose))]
    private MviReduceResult<MedicineSearchState, MedicineSearchEffect> HandleChoose(
        MedicineSearchState state,
        MedicineSearchIntent.Choose intent)
    {
        return state.Results.Contains(intent.Name)
            ? WithEffect(state with { IsBusy = true, Error = null }, new(intent.Name))
            : Unchanged(state);
    }

    /// <summary>规约完成回流意图。</summary>
    [MviReduce(typeof(MedicineSearchIntent.Completed))]
    private MviReduceResult<MedicineSearchState, MedicineSearchEffect> HandleCompleted(
        MedicineSearchState state,
        MedicineSearchIntent.Completed intent)
    {
        return Unchanged(state with { IsBusy = false, CompletedSelections = state.CompletedSelections + 1 });
    }

    /// <summary>规约失败回流意图。</summary>
    [MviReduce(typeof(MedicineSearchIntent.Failed))]
    private MviReduceResult<MedicineSearchState, MedicineSearchEffect> HandleFailed(
        MedicineSearchState state,
        MedicineSearchIntent.Failed intent)
    {
        return Unchanged(state with { IsBusy = false, Error = intent.Message });
    }

    private static bool CanChoose(MedicineSearchState state) => !state.IsBusy;
}

/// <summary>只依赖通信契约，可以原样运行在独立宿主或处方组合内。</summary>
public sealed partial class MedicineSearchEffectDispatcher(IMviMediator mediator)
    : MviEffectDispatcherBase<MedicineSearchIntent, MedicineSearchEffect>
{
    [MviEffect(typeof(MedicineSearchEffect))]
    private async ValueTask SubmitAsync(MedicineSearchEffect effect, CancellationToken cancellationToken)
    {
        try
        {
            MedicineSelectionResult result = await mediator.SendAsync(new SubmitMedicine(effect.Name), cancellationToken);
            if (result.Name != effect.Name) throw new InvalidOperationException("接收结果与选择名称不一致。");
            await DispatchIntentAsync(new MedicineSearchIntent.Completed(), cancellationToken);
        }
        catch (Exception exception)
        {
            await DispatchIntentAsync(new MedicineSearchIntent.Failed(exception.Message), CancellationToken.None);
            throw;
        }
    }
}
