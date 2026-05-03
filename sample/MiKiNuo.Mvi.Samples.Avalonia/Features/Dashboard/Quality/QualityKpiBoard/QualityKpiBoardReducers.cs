using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Quality.QualityKpiBoard;

/// <summary>
/// 表示质量 KPI MVI规约器。
/// </summary>
public sealed partial class QualityKpiBoardReducer
    : MviReducerBase<QualityKpiBoardState, QualityKpiBoardIntent, QualityKpiBoardEffect>
{
    /// <summary>
    /// 处理主业务动作意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">主业务动作意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<QualityKpiBoardState, QualityKpiBoardEffect> Reduce(
        QualityKpiBoardState state,
        QualityKpiBoardIntent.ExecutePrimaryAction intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        QualityKpiBoardState nextState = state with
        {
            StatusText = $"已发起：{state.PrimaryActionText}",
            ActionLog = $"{state.Title} -> {state.PrimaryActionText} -> 等待 Mediator 协调父页面和兄弟 MVI。"
        };

        return MviReduceResult.StateAndEffect<QualityKpiBoardState, QualityKpiBoardEffect>(
            nextState,
            new QualityKpiBoardEffect.RequestPrimaryWorkflow($"{state.Title}：{state.PrimaryActionText}"));
    }

    /// <summary>
    /// 处理辅助业务动作意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">辅助业务动作意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<QualityKpiBoardState, QualityKpiBoardEffect> Reduce(
        QualityKpiBoardState state,
        QualityKpiBoardIntent.ExecuteSecondaryAction intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        QualityKpiBoardState nextState = state with
        {
            StatusText = $"已发起：{state.SecondaryActionText}",
            ActionLog = $"{state.Title} -> {state.SecondaryActionText} -> 等待 Mediator 协调副作用。"
        };

        return MviReduceResult.StateAndEffect<QualityKpiBoardState, QualityKpiBoardEffect>(
            nextState,
            new QualityKpiBoardEffect.RequestSecondaryWorkflow($"{state.Title}：{state.SecondaryActionText}"));
    }

    /// <summary>
    /// 处理外部更新意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">外部更新意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<QualityKpiBoardState, QualityKpiBoardEffect> Reduce(
        QualityKpiBoardState state,
        QualityKpiBoardIntent.ApplyExternalUpdate intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<QualityKpiBoardState, QualityKpiBoardEffect>(state with
        {
            DetailText = intent.Message,
            ActionLog = $"收到父页面或兄弟 MVI 更新：{intent.Message}"
        });
    }
}
