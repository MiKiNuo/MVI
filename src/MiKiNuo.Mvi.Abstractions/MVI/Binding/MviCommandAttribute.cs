namespace MiKiNuo.Mvi.Abstractions.MVI.Binding;
/// <summary>声明命令的 Intent 类型与快照来源，不绑定 ViewModel 或 Feature 转发方法。</summary>
/// <param name="intentType">要构造的具体 Intent。</param>
/// <param name="inputs">按构造参数顺序捕获的 ViewModel 属性；为空时使用无参或单 payload 构造。</param>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class MviCommandAttribute(Type intentType, params string[] inputs) : Attribute
{
    /// <summary>获取 Intent 类型。</summary>
    public Type IntentType { get; } = intentType;
    /// <summary>获取快照属性名称。</summary>
    public string[] Inputs { get; } = inputs;
    /// <summary>获取或设置 ViewModel 上的布尔条件属性；State.X 也可作为显式来源。</summary>
    public string? CanExecuteProperty { get; set; }
    /// <summary>获取或设置 payload 的类型，用于无 Inputs 的一参 Intent。</summary>
    public Type? PayloadType { get; set; }
    /// <summary>获取或设置捕获快照后清空的本地 string 属性；默认不清空。</summary>
    public string[] ClearAfterCapture { get; set; } = [];
}
