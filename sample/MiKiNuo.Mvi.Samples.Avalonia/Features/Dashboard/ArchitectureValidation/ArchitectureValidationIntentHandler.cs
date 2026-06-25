using System;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ArchitectureValidation;

/// <summary>
/// 表示架构验证中心意图处理器。
/// </summary>
public sealed class ArchitectureValidationIntentHandler
    : IMviIntentHandler<ArchitectureValidationState, ArchitectureValidationIntent, ArchitectureValidationEffect>
{
    /// <summary>
    /// 处理意图并产生动作副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>动作副作用集合。</returns>
    public ValueTask<IReadOnlyList<ArchitectureValidationEffect>> HandleAsync(
        ArchitectureValidationState state,
        ArchitectureValidationIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);
        return new ValueTask<IReadOnlyList<ArchitectureValidationEffect>>(Array.Empty<ArchitectureValidationEffect>());
    }
}
