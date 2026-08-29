using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

/// <summary>
/// 表示仪表板卡片意图处理器。
/// 卡片无异步业务,返回null。
/// </summary>
public sealed class CardIntentHandler
    : MviIntentHandlerBase<CardState, CardIntent>
{
    /// <summary>处理具体业务逻辑。</summary>
    /// <param name="state">当前状态（已通过 null 检查）。</param>
    /// <param name="intent">用户意图（已通过 null 检查）。</param>
    /// <param name="cancellationToken">取消标记（已通过检查）。</param>
    /// <returns>后续意图;无后续工作时返回 null。</returns>
    protected override async ValueTask<CardIntent?> HandleCoreAsync(
        CardState state,
        CardIntent intent,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return null;
    }
}
