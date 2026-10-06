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
/// <summary>按通信契约提交演示名称，不涉及医疗规则。</summary>
/// <param name="Name">提交给明细组件的演示名称。</param>
public sealed record SubmitMedicine(string Name) : IMviRequest<MedicineSelectionResult>;
/// <summary>明确表示接收完成的返回值。</summary>
/// <param name="Name">明细组件确认已接收的名称。</param>
public sealed record MedicineSelectionResult(string Name);
/// <summary>本实例独立的检索状态。</summary>
public sealed record MedicineSearchState : IMviState
{
    /// <summary>获取检索结果。</summary>
    public ImmutableArray<string> Results { get; init; } = [];
    /// <summary>获取提交状态。</summary>
    public bool IsBusy { get; init; }
    /// <summary>获取已完成提交数量。</summary>
    public int CompletedSelections { get; init; }
    /// <summary>获取错误信息。</summary>
    public string? Error { get; init; }
    /// <summary>获取初始状态。</summary>
    public static MedicineSearchState Initial { get; } = new();
}
/// <summary>检索实例接受的用户操作。</summary>
public abstract record MedicineSearchIntent : IMviIntent
{
    /// <summary>检索名称。</summary>
    /// <param name="Text">用于筛选演示名称的检索文本。</param>
    public sealed record Search(string Text) : MedicineSearchIntent;
    /// <summary>提交选中项。</summary>
    /// <param name="Name">用户选中并请求提交的名称。</param>
    public sealed record Choose(string Name) : MedicineSearchIntent;
}
/// <summary>内部状态转换，不作为用户意图暴露。</summary>
public abstract record MedicineSearchMutation : IMviMutation<MedicineSearchState>
{
    /// <summary>替换结果列表。</summary>
    /// <param name="Items">替换当前检索结果的不可变名称集合。</param>
    public sealed record Results(ImmutableArray<string> Items) : MedicineSearchMutation;
    /// <summary>接纳一次提交。</summary>
    public sealed record Started : MedicineSearchMutation;
    /// <summary>完成一次提交或记录失败。</summary>
    /// <param name="Success">本次选择是否成功提交。</param>
    /// <param name="Error">失败时的错误说明；成功时为空。</param>
    public sealed record Finished(bool Success, string? Error = null) : MedicineSearchMutation;
}
/// <summary>只转换检索状态的纯函数集合。</summary>
public sealed partial class MedicineSearchReducer : MviReducerBase<MedicineSearchState>
{
    [MviReduce(typeof(MedicineSearchMutation.Results))]
    private static MedicineSearchState Results(MedicineSearchState state, MedicineSearchMutation.Results change)
        => state with { Results = change.Items };
    [MviReduce(typeof(MedicineSearchMutation.Started))]
    private static MedicineSearchState Start(MedicineSearchState state, MedicineSearchMutation.Started change)
        => state with { IsBusy = true, Error = null };
    [MviReduce(typeof(MedicineSearchMutation.Finished))]
    private static MedicineSearchState Finish(MedicineSearchState state, MedicineSearchMutation.Finished change)
        => state with { IsBusy = false, CompletedSelections = state.CompletedSelections + (change.Success ? 1 : 0), Error = change.Error };
}
/// <summary>独立宿主和嵌套组合共用同一处理器，只依赖通信契约。</summary>
[MviFeature]
public sealed partial class MedicineSearchHandler(IMviMediator mediator) : MviIntentHandler<MedicineSearchState, MedicineSearchIntent>
{
    private static readonly ImmutableArray<string> Catalog = ["演示药品甲", "演示药品乙"];
    [MviHandle(typeof(MedicineSearchIntent.Search))]
    private static ValueTask SearchAsync(MedicineSearchIntent.Search intent, IIntentContext<MedicineSearchState> context, CancellationToken cancellationToken)
    {
        context.Reduce(new MedicineSearchMutation.Results([.. Catalog.Where(name => name.Contains(intent.Text, StringComparison.Ordinal))]));
        return ValueTask.CompletedTask;
    }
    [MviHandle(typeof(MedicineSearchIntent.Choose))]
    private async ValueTask ChooseAsync(MedicineSearchIntent.Choose intent, IIntentContext<MedicineSearchState> context, CancellationToken cancellationToken)
    {
        if (!context.TryReduce(state => !state.IsBusy && state.Results.Contains(intent.Name), new MedicineSearchMutation.Started())) return;
        bool success = false;
        string? error = null;
        try
        {
            MedicineSelectionResult result = await mediator.SendAsync(new SubmitMedicine(intent.Name), cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (result.Name != intent.Name) throw new InvalidOperationException("接收结果与选择名称不一致。");
            success = true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException) { error = exception.Message; throw; }
        finally { context.TryReduce(static state => state.IsBusy, new MedicineSearchMutation.Finished(success, error)); }
    }
}
