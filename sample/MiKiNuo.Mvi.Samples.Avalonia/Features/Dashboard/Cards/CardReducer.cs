using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using ZLinq;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

/// <summary>
/// 表示仪表板卡片的统一归约器。
/// 处理器使用 <c>[MviReduce]</c> 标注的私有方法，由 MVI 源代码生成器发出 dispatch 逻辑。
/// 内部根据 state.PageKey 检索 DashboardCardRegistry 中的 CardDefinition，从而根据 16 个不同卡片执行差异化状态更新。
/// 设计为非泛型：1 个 reducer 覆盖 16 个 PageKey（详见 .gsd/DECISIONS.md 2026-06-02 第 5 轮 grill）。
/// </summary>
public sealed partial class CardReducer
    : MviReducerBase<CardState, CardIntent, CardEffect>
{
    /// <summary>处理 ExecutePrimaryAction。</summary>
    [MviReduce]
    private MviReduceResult<CardState, CardEffect> Reduce(
        CardState state,
        CardIntent.ExecutePrimaryAction intent)
    {
        ArgumentNullException.ThrowIfNull(state);

        CardState nextState = state with
        {
            StatusText = $"已发起：{state.PrimaryActionText}",
            ActionLog = $"{state.Title} -> {state.PrimaryActionText} -> 等待 Mediator 协调父页面和兄弟卡片。",
        };

        return MviReduceResult.StateAndEffect<CardState, CardEffect>(
            nextState,
            new CardEffect.RequestPrimaryWorkflow($"{state.Title}：{state.PrimaryActionText}"));
    }

    /// <summary>处理 ExecuteSecondaryAction。</summary>
    [MviReduce]
    private MviReduceResult<CardState, CardEffect> Reduce(
        CardState state,
        CardIntent.ExecuteSecondaryAction intent)
    {
        ArgumentNullException.ThrowIfNull(state);

        CardState nextState = state with
        {
            StatusText = $"已发起：{state.SecondaryActionText}",
            ActionLog = $"{state.Title} -> {state.SecondaryActionText} -> 等待 Mediator 协调副作用。",
        };

        return MviReduceResult.StateAndEffect<CardState, CardEffect>(
            nextState,
            new CardEffect.RequestSecondaryWorkflow($"{state.Title}：{state.SecondaryActionText}"));
    }

    /// <summary>处理 ApplyExternalUpdate。</summary>
    [MviReduce]
    private MviReduceResult<CardState, CardEffect> Reduce(
        CardState state,
        CardIntent.ApplyExternalUpdate intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<CardState, CardEffect>(state with
        {
            DetailText = intent.Message,
            ActionLog = $"收到父页面或兄弟卡片更新：{intent.Message}",
        });
    }

    /// <summary>处理 SetFormField。</summary>
    [MviReduce]
    private MviReduceResult<CardState, CardEffect> Reduce(
        CardState state,
        CardIntent.SetFormField intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        CardDefinition? definition = DashboardCardRegistry.GetDefinition(state.PageKey);
        if (definition is null || !definition.IsFormCard)
        {
            return MviReduceResult.State<CardState, CardEffect>(state);
        }

        CardState nextState = state.WithFormValue(intent.Key, intent.Value);
        if (definition.Validator is null)
        {
            return MviReduceResult.State<CardState, CardEffect>(nextState);
        }

        (bool _, string statusText, string actionLog) = definition.Validator(nextState.FormValues);
        return MviReduceResult.State<CardState, CardEffect>(nextState with
        {
            StatusText = statusText,
            ActionLog = actionLog,
        });
    }

    /// <summary>处理 SubmitForm。</summary>
    [MviReduce]
    private MviReduceResult<CardState, CardEffect> Reduce(
        CardState state,
        CardIntent.SubmitForm intent)
    {
        ArgumentNullException.ThrowIfNull(state);

        CardDefinition? definition = DashboardCardRegistry.GetDefinition(state.PageKey);
        if (definition is null || !definition.IsFormCard || definition.Validator is null)
        {
            return MviReduceResult.State<CardState, CardEffect>(state);
        }

        (bool canSubmit, string statusText, string actionLog) = definition.Validator(state.FormValues);
        if (!canSubmit)
        {
            return MviReduceResult.State<CardState, CardEffect>(state with
            {
                StatusText = statusText,
                ActionLog = actionLog,
                FormErrorMessage = statusText,
            });
        }

        string contextText = state.FormValues.AsValueEnumerable()
            .Select(static value => $"{value.Key}={value.Value}")
            .JoinToString("；");

        CardState nextState = state with
        {
            StatusText = $"{definition.Title} 已提交，等待兄弟卡片处理",
            ActionLog = $"{definition.SourceDisplayName} 提交 -> {contextText} -> 通过 Mediator 分发。",
        };

        return MviReduceResult.StateAndEffect<CardState, CardEffect>(
            nextState,
            new CardEffect.RequestFormSubmission(contextText));
    }
}
