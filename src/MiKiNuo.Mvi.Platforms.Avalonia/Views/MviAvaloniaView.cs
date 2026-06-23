using Avalonia;
using Avalonia.Controls;
using MiKiNuo.Mvi.Application.DI;
using MiKiNuo.Mvi.Presentation.Disposables;

namespace MiKiNuo.Mvi.Platforms.Avalonia.Views;

/// <summary>
/// 表示 Avalonia 平台 MVI 视图基类。
/// 内部使用 <see cref="MviDisposableBag"/> 收集 View 绑定生命周期内的可释放资源，
/// 与 Godot <c>GodotMviControlView</c> 共用同一套释放语义（重入、Dispose-after-Add 竞态等）。
/// </summary>
/// <typeparam name="TViewModel">视图模型类型。</typeparam>
public abstract class MviAvaloniaView<TViewModel> : UserControl
    where TViewModel : class
{
    private MviDisposableBag? _bindings;

    /// <summary>
    /// 获取强类型 ViewModel。
    /// </summary>
    protected TViewModel ViewModel
    {
        get
        {
            if (DataContext is TViewModel viewModel)
            {
                return viewModel;
            }

            throw new InvalidOperationException($"当前视图未绑定 ViewModel：{typeof(TViewModel).FullName}");
        }
    }

    /// <summary>
    /// 绑定 ViewModel；保留旧版 1 参数入口以兼容"手写 Bind"或"无槽位"场景。
    /// <para>
    /// 此重载 <b>不</b> 触发组合模式槽位绑定钩子（<see cref="OnBindSlots"/>）。
    /// 需要源生成器 emit 槽位绑定逻辑的 View，必须使用带 <see cref="IMviResolver"/> 的 2 参数重载。
    /// </para>
    /// </summary>
    /// <param name="viewModel">视图模型。</param>
    public void Bind(TViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        ClearBindings();
        DataContext = viewModel;
        OnBind(viewModel, EnsureBindings());
    }

    /// <summary>
    /// 绑定 ViewModel 与子组件解析容器。
    /// <para>
    /// 走完整绑定流程：清空旧绑定、设置 <c>DataContext</c>、调用 <see cref="OnBind"/> 与 <see cref="OnBindSlots"/> 钩子；
    /// 源生成器会在子类中 <c>override</c> <see cref="OnBindSlots"/> 以驱动 [MviSlot] 字段绑定。
    /// </para>
    /// </summary>
    /// <param name="viewModel">视图模型。</param>
    /// <param name="resolver">用于解析子 ViewModel 与子 View 的 <see cref="IMviResolver"/>；不能为 <c>null</c>。</param>
    public void Bind(TViewModel viewModel, IMviResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(resolver);

        ClearBindings();
        DataContext = viewModel;
        MviDisposableBag bindings = EnsureBindings();
        OnBind(viewModel, bindings);

        // 触发组合模式槽位绑定钩子；源生成器会 emit override 实现
        // —— 扫描子类的 [MviSlot] 字段，按需解析子 ViewModel 并写入 MviSlotHost。
        OnBindSlots(viewModel, bindings, resolver);
    }

    /// <summary>
    /// View 绑定 ViewModel 时的事件绑定扩展点。
    /// <para>
    /// 子类 <c>override</c> 本方法，通过 <c>ToEventSource().EventName</c> 获取
    /// <see cref="Application.MVI.EventBinding.IEventSource{TEvent}"/>，
    /// 调用 <see cref="Presentation.Binding.EventBindingExtensions.BindTo{TEvent}"/> 把事件映射为 Intent 并注册到 <paramref name="bindings"/>。
    /// 基类提供空实现；不依赖事件绑定的 View 得到零成本默认行为。
    /// </para>
    /// </summary>
    /// <param name="viewModel">当前绑定的视图模型。</param>
    /// <param name="bindings">随 View 生命周期释放的可释放资源集合。</param>
    protected virtual void OnBind(TViewModel viewModel, MviDisposableBag bindings)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(bindings);
    }

    /// <summary>
    /// 由源生成器在子类中 <c>override</c> 实现的组合模式槽位绑定钩子。
    /// <para>
    /// 基类提供空实现；不依赖源生成器的 View（如无 [MviSlot] 字段）会得到一个零成本的默认行为。
    /// </para>
    /// </summary>
    /// <param name="viewModel">当前绑定的视图模型。</param>
    /// <param name="bindings">随 View 生命周期释放的可释放资源集合。</param>
    /// <param name="resolver">由 <c>Bind</c> 传入的 <see cref="IMviResolver"/>；源生成器用它解析子 ViewModel 与子 View。</param>
    protected virtual void OnBindSlots(TViewModel viewModel, MviDisposableBag bindings, IMviResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(resolver);
    }

    /// <summary>
    /// 脱离可视树时清理绑定资源。
    /// </summary>
    /// <param name="e">可视树附件事件参数。</param>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ClearBindings();
        base.OnDetachedFromVisualTree(e);
    }

    private MviDisposableBag EnsureBindings()
    {
        return _bindings ??= new MviDisposableBag();
    }

    private void ClearBindings()
    {
        _bindings?.Dispose();
        _bindings = null;
    }
}
