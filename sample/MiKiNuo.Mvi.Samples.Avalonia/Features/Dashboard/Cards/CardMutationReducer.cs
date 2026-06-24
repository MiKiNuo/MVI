using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

/// <summary>
/// 表示仪表板卡片变更规约器。
/// </summary>
public sealed partial class CardMutationReducer
    : MviMutationReducerBase<CardState, CardMutation, CardEffect>
{
    /// <summary>应用设置状态文本变更。</summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<CardState, CardEffect> HandleSetStatusText(
        CardState state,
        CardMutation.SetStatusText mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<CardState, CardEffect>(state.Apply(mutation));
    }

    /// <summary>应用设置详情文本变更。</summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<CardState, CardEffect> HandleSetDetailText(
        CardState state,
        CardMutation.SetDetailText mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<CardState, CardEffect>(state.Apply(mutation));
    }

    /// <summary>应用设置动作日志变更。</summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<CardState, CardEffect> HandleSetActionLog(
        CardState state,
        CardMutation.SetActionLog mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<CardState, CardEffect>(state.Apply(mutation));
    }

    /// <summary>应用设置主动作可用性变更。</summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<CardState, CardEffect> HandleSetCanPrimaryAction(
        CardState state,
        CardMutation.SetCanPrimaryAction mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<CardState, CardEffect>(state.Apply(mutation));
    }

    /// <summary>应用设置辅助动作可用性变更。</summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<CardState, CardEffect> HandleSetCanSecondaryAction(
        CardState state,
        CardMutation.SetCanSecondaryAction mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<CardState, CardEffect>(state.Apply(mutation));
    }

    /// <summary>应用设置表单错误消息变更。</summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<CardState, CardEffect> HandleSetFormErrorMessage(
        CardState state,
        CardMutation.SetFormErrorMessage mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<CardState, CardEffect>(state.Apply(mutation));
    }

    /// <summary>应用设置表单字段值集合变更。</summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<CardState, CardEffect> HandleSetFormValues(
        CardState state,
        CardMutation.SetFormValues mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<CardState, CardEffect>(state.Apply(mutation));
    }

    /// <summary>应用设置最近入院患者变更。</summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<CardState, CardEffect> HandleSetRecentAdmittedPatient(
        CardState state,
        CardMutation.SetRecentAdmittedPatient mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<CardState, CardEffect>(state.Apply(mutation));
    }

    /// <summary>应用设置床位筛选维度变更。</summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<CardState, CardEffect> HandleSetCurrentBedFilter(
        CardState state,
        CardMutation.SetCurrentBedFilter mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<CardState, CardEffect>(state.Apply(mutation));
    }

    /// <summary>应用设置筛选床位数量变更。</summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<CardState, CardEffect> HandleSetFilteredBedCount(
        CardState state,
        CardMutation.SetFilteredBedCount mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<CardState, CardEffect>(state.Apply(mutation));
    }

    /// <summary>应用设置选中床位类型集合变更。</summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<CardState, CardEffect> HandleSetSelectedBedTypes(
        CardState state,
        CardMutation.SetSelectedBedTypes mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<CardState, CardEffect>(state.Apply(mutation));
    }

    /// <summary>应用设置选中床位状态集合变更。</summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<CardState, CardEffect> HandleSetSelectedBedStatuses(
        CardState state,
        CardMutation.SetSelectedBedStatuses mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<CardState, CardEffect>(state.Apply(mutation));
    }
}
