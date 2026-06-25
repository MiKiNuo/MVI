using System;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;
using ZLinq;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

/// <summary>
/// 表示仪表板卡片意图处理器。
/// </summary>
public sealed class CardIntentHandler
    : IMviIntentHandler<CardState, CardIntent, CardEffect>
{
    private readonly IReadOnlyDictionary<PageKey, CardDefinition> _definitions;

    /// <summary>
    /// 初始化仪表板卡片意图处理器。
    /// </summary>
    /// <param name="definitions">卡片定义字典。</param>
    public CardIntentHandler(IReadOnlyDictionary<PageKey, CardDefinition> definitions)
    {
        _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
    }

    /// <summary>
    /// 处理意图并产生动作副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>动作副作用集合。</returns>
    public ValueTask<IReadOnlyList<CardEffect>> HandleAsync(
        CardState state,
        CardIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        IReadOnlyList<CardEffect> effects = intent switch
        {
            CardIntent.ExecutePrimaryAction => new CardEffect[]
            {
                new CardEffect.RequestPrimaryWorkflow($"{state.Title}：{state.PrimaryActionText}"),
            },
            CardIntent.ExecuteSecondaryAction => new CardEffect[]
            {
                new CardEffect.RequestSecondaryWorkflow($"{state.Title}：{state.SecondaryActionText}"),
            },
            CardIntent.SubmitForm => HandleSubmitForm(state),
            _ => Array.Empty<CardEffect>(),
        };
        return new ValueTask<IReadOnlyList<CardEffect>>(effects);
    }

    private IReadOnlyList<CardEffect> HandleSubmitForm(CardState state)
    {
        CardDefinition? definition = ResolveDefinition(state);
        if (definition is null || !definition.IsFormCard || definition.Validator is null)
        {
            return Array.Empty<CardEffect>();
        }

        (bool canSubmit, string _, string _) = definition.Validator(state.FormValues);
        if (!canSubmit)
        {
            return Array.Empty<CardEffect>();
        }

        string contextText = state.FormValues.AsValueEnumerable()
            .Select(static value => $"{value.Key}={value.Value}")
            .JoinToString("；");
        return new CardEffect[] { new CardEffect.RequestFormSubmission(state.FormValues, contextText) };
    }

    private CardDefinition? ResolveDefinition(CardState state)
    {
        return _definitions.TryGetValue(state.PageKey, out CardDefinition? definition) ? definition : null;
    }
}
