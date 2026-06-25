namespace MiKiNuo.Mvi.Domain.MVI.Reducer;

/// <summary>
/// 表示规约器方法与意图子类型的声明式映射。
/// </summary>
/// <param name="intentType">意图子类型。</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class MviReduceAttribute(Type intentType) : Attribute
{
    /// <summary>
    /// 获取意图子类型。
    /// </summary>
    public Type IntentType { get; } = intentType;

    /// <summary>
    /// 获取或设置守卫谓词方法名。
    /// </summary>
    public string? Guard { get; set; }
}
