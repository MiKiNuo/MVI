namespace MiKiNuo.Mvi.Domain.MVI.Business;

/// <summary>
/// 表示携带后续意图的业务结果。
/// </summary>
/// <typeparam name="TIntent">意图类型。</typeparam>
public sealed class FollowUpIntentResult<TIntent> : IMviBusinessResult
{
    /// <summary>获取后续意图。</summary>
    public TIntent Intent { get; }

    /// <summary>初始化业务结果。</summary>
    /// <param name="intent">后续意图。</param>
    public FollowUpIntentResult(TIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        Intent = intent;
    }
}
