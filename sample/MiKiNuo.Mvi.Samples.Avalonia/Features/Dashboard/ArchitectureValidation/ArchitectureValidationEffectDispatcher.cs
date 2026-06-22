using MiKiNuo.Mvi.Application.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ArchitectureValidation;

/// <summary>
/// 表示架构验证中心副作用分发器。
/// </summary>
public sealed class ArchitectureValidationEffectDispatcher : IMviEffectDispatcher<ArchitectureValidationEffect>
{
    /// <summary>
    /// 分发副作用。
    /// </summary>
    /// <param name="effect">副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步分发过程的任务。</returns>
    public ValueTask DispatchAsync(ArchitectureValidationEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);

        return ValueTask.CompletedTask;
    }
}
