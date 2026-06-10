using MiKiNuo.Mvi.Presentation.ViewRegistry;
using MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

namespace MiKiNuo.Mvi.Samples.Avalonia.Composition;

/// <summary>
/// 表示示例应用组合根。
/// </summary>
public sealed class SampleCompositionRoot
{
    private readonly SampleGeneratedContainer _container;
    private readonly EventBindingWorkbenchComposition _eventBindingWorkbenchComposition;

    /// <summary>
    /// 初始化示例应用组合根。
    /// </summary>
    public SampleCompositionRoot()
    {
        _container = new SampleGeneratedContainer();
        _eventBindingWorkbenchComposition = EventBindingWorkbenchComposition.Create();
        // 容器自身无法枚举 EventBindingWorkbench 子组件 Store/Mediator 装配逻辑，
        // 因此由组合根显式注入工作台 ViewModel，供 IShellPageFactory 按键解析。
        _container.SetEventBindingWorkbenchViewModel(_eventBindingWorkbenchComposition.WorkbenchViewModel);
    }

    /// <summary>
    /// 创建主窗口。
    /// </summary>
    /// <returns>主窗口。</returns>
    public MainWindow CreateMainWindow()
    {
        AppShellViewModel shellViewModel = _container.Resolve<AppShellViewModel>();
        IMviViewRegistry viewRegistry = _container.Resolve<IMviViewRegistry>();
        return new MainWindow(shellViewModel, viewRegistry, _eventBindingWorkbenchComposition);
    }
}
