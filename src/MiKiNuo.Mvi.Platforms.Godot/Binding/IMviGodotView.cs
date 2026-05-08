namespace MiKiNuo.Mvi.Platforms.Godot.Binding;

/// <summary>
/// 表示 Godot MVI View 的只读 ViewModel 访问契约。
/// </summary>
/// <typeparam name="TViewModel">ViewModel 类型。</typeparam>
public interface IMviGodotView<out TViewModel>
    where TViewModel : class
{
    /// <summary>
    /// 获取当前绑定的 ViewModel。
    /// </summary>
    public TViewModel? ViewModel { get; }
}
