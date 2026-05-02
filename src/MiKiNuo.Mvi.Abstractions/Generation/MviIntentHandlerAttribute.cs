namespace MiKiNuo.Mvi.Abstractions.Generation;

/// <summary>
/// 标记当前类型为指定意图的处理器。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MviIntentHandlerAttribute : Attribute
{
    /// <summary>
    /// 初始化意图处理器特性。
    /// </summary>
    /// <param name="intentType">意图类型。</param>
    public MviIntentHandlerAttribute(Type intentType)
    {
        IntentType = intentType;
    }

    /// <summary>
    /// 获取意图类型。
    /// </summary>
    public Type IntentType { get; }
}
