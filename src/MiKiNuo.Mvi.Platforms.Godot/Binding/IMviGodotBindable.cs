namespace MiKiNuo.Mvi.Platforms.Godot.Binding;

/// <summary>
/// 表示可以绑定和解绑 ViewModel 的 Godot MVI View。
/// </summary>
/// <typeparam name="TViewModel">ViewModel 类型。</typeparam>
public interface IMviGodotBindable<TViewModel> : IMviGodotView<TViewModel>
    where TViewModel : class
{
    /// <summary>
    /// 绑定 ViewModel。
    /// </summary>
    /// <param name="viewModel">需要绑定的 ViewModel。</param>
    public void Bind(TViewModel viewModel);

    /// <summary>
    /// 解绑当前 ViewModel。
    /// </summary>
    public void Unbind();
}
