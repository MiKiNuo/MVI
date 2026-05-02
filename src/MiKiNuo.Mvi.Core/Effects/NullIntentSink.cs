using MiKiNuo.Mvi.Abstractions;

namespace MiKiNuo.Mvi.Core.Effects;

/// <summary>
/// 表示忽略所有后续意图的空意图接收器。
/// </summary>
/// <typeparam name="TIntent">意图类型。</typeparam>
public sealed class NullIntentSink<TIntent> : IIntentSink<TIntent>
    where TIntent : IMviIntent
{
    /// <inheritdoc />
    public ValueTask DispatchAsync(TIntent intent, CancellationToken cancellationToken)
    {
        return default;
    }
}
