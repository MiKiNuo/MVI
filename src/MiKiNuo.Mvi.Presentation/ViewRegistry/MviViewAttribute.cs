namespace MiKiNuo.Mvi.Presentation.ViewRegistry;

/// <summary>
/// 表示将平台 View 类与其关联的 ViewModel 类型绑定的特性。
/// <para>
/// 源生成器扫描标有此特性的 View 类，生成组合 View 注册条目
/// （如 Avalonia 端 <c>MviAvaloniaViewRegistryGenerator</c> 已生成的 <c>IMviViewRegistry</c> 注册项，
/// 以及 Godot 端 <c>MviGodotViewRegistryGenerator</c> 已生成的对应注册项）的"显式版"声明。
/// </para>
/// <para>
/// 与 <c>MviAvaloniaView&lt;TViewModel&gt;</c> / <c>GodotMviControlView&lt;TViewModel&gt;</c> 泛型基类
/// 共同承担"View↔VM 强类型绑定"职责：泛型基类负责编译期类型校验，特性负责源生成器发现。
/// </para>
/// </summary>
/// <remarks>
/// 仅允许标在类上，每个 View 类只允许一个，不允许继承——避免重复绑定。
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MviViewAttribute : Attribute
{
    /// <summary>
    /// 创建 <see cref="MviViewAttribute"/> 实例。
    /// </summary>
    /// <param name="viewModelType">与此 View 关联的 ViewModel 类型。</param>
    /// <exception cref="ArgumentNullException"><paramref name="viewModelType"/> 为 <c>null</c>。</exception>
    public MviViewAttribute(Type viewModelType)
    {
        ArgumentNullException.ThrowIfNull(viewModelType);
        ViewModelType = viewModelType;
    }

    /// <summary>
    /// 获取与此 View 关联的 ViewModel 类型。
    /// </summary>
    public Type ViewModelType { get; }
}
