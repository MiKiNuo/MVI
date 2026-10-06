namespace MiKiNuo.Mvi.Abstractions.MVI.Binding;
/// <summary>
/// 声明绑定属性。无状态来源的可写属性是本地输入；只读属性默认投影同名状态。
/// 显式指定状态来源的可写属性必须同时指定输入 Intent，绝不直接修改 Store。
/// </summary>
/// <param name="stateProperty">可选的状态来源属性。</param>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class MviBindAttribute(string? stateProperty = null) : Attribute
{
    /// <summary>获取显式状态来源；为空时按访问器规则判断。</summary>
    public string? StateProperty { get; } = stateProperty;
    /// <summary>获取或设置写入时派发的单参数 Intent 类型。</summary>
    public Type? IntentType { get; set; }
    /// <summary>获取或设置敏感输入标记；仅允许本地 string 输入，不表示内存擦除。</summary>
    public bool Sensitive { get; set; }
}
