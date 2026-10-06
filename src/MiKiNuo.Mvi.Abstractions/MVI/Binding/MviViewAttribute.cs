namespace MiKiNuo.Mvi.Abstractions.MVI.Binding;
/// <summary>为现有平台控件生成 IMviView 接口实现，不更换其控件基类。</summary>
/// <param name="viewModelType">借用的绑定模型类型。</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MviViewAttribute(Type viewModelType) : Attribute
{
    /// <summary>获取绑定模型类型。</summary>
    public Type ViewModelType { get; } = viewModelType;
}
