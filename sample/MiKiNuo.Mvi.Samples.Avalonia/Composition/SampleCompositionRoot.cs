using MiKiNuo.Mvi.Binding.Threading;
namespace MiKiNuo.Mvi.Samples.Avalonia.Composition;
/// <summary>应用组合根，只做装配，不重复手写各 Feature 的构造流程。</summary>
public sealed class SampleCompositionRoot
{
    /// <summary>创建根解析器。</summary>
    /// <param name="uiDispatcher">平台 UI 调度器。</param>
    public SampleCompositionRoot(IMviUiDispatcher uiDispatcher) => Container = new(uiDispatcher);
    /// <summary>获取根解析器，供 View 的槽位功能使用。</summary>
    public GeneratedMviContainer Container { get; }
    /// <summary>异步创建认证组合，不通过 Result/Wait 阻塞 UI。</summary>
    /// <returns>拥有各页面实例的组合。</returns>
    public ValueTask<AppComposition> CreateAsync() => Container.CreateAppCompositionAsync();
}
