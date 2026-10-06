using Avalonia.Controls;
using MiKiNuo.Mvi.Binding.Disposables;
using MiKiNuo.Mvi.Binding.Views;
using MiKiNuo.Mvi.Runtime.DI;
namespace MiKiNuo.Mvi.Platforms.Avalonia.Views;
/// <summary>可选的便利基类；与非继承式 View 共享同一个控制器，并非另一套绑定运行时。</summary>
/// <typeparam name="TViewModel">借用的模型。</typeparam>
public abstract class MviAvaloniaView<TViewModel> : UserControl, IMviView<TViewModel>, IMviAvaloniaViewBinding where TViewModel : class
{
    private AvaloniaViewBinding<TViewModel>? _binding;
    /// <summary>获取绑定模型。</summary>
    protected TViewModel ViewModel => _binding?.ViewModel ?? throw new InvalidOperationException("View 尚未绑定。");
    /// <summary>绑定模型与子组件解析器。</summary>
    /// <param name="viewModel">模型。</param>
    /// <param name="resolver">解析器。</param>
    public void Bind(TViewModel viewModel, IMviResolver resolver)
    {
        _binding ??= new(this, (model, bag, services) => { OnBind(model, bag); OnBindSlots(model, bag, services); });
        _binding.Bind(viewModel, resolver);
    }
    /// <summary>解绑，不销毁业务对象。</summary>
    public void Unbind() => _binding?.Unbind();
    /// <summary>安装本 View 的事件订阅。</summary>
    /// <param name="viewModel">模型。</param>
    /// <param name="bindings">本代绑定集合。</param>
    protected virtual void OnBind(TViewModel viewModel, MviDisposableBag bindings) { }
    /// <summary>供原有槽位生成器安装子组件视图。</summary>
    /// <param name="viewModel">模型。</param>
    /// <param name="bindings">本代绑定集合。</param>
    /// <param name="resolver">解析器。</param>
    protected virtual void OnBindSlots(TViewModel viewModel, MviDisposableBag bindings, IMviResolver resolver) { }
}
