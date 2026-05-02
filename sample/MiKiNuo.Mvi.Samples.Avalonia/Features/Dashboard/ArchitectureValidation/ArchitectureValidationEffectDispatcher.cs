using MiKiNuo.Mvi.Application.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ArchitectureValidation;

/// <summary>
/// 表示架构验证中心副作用分发器。
/// </summary>
public sealed class ArchitectureValidationEffectDispatcher : IMviEffectDispatcher<ArchitectureValidationEffect>
{
    /// <inheritdoc />
    public ValueTask DispatchAsync(ArchitectureValidationEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);

        return ValueTask.CompletedTask;
    }
}
