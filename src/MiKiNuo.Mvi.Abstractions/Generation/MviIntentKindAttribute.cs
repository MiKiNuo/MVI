namespace MiKiNuo.Mvi.Abstractions.Generation;

/// <summary>
/// 标记当前意图类型的源生成编号。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MviIntentKindAttribute : Attribute
{
    /// <summary>
    /// 初始化意图编号特性。
    /// </summary>
    /// <param name="kind">意图编号。</param>
    public MviIntentKindAttribute(int kind)
    {
        Kind = kind;
    }

    /// <summary>
    /// 获取意图编号。
    /// </summary>
    public int Kind { get; }
}
