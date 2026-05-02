using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.BusinessPage;

/// <summary>
/// 表示生产业务组合页面 ViewModel。
/// </summary>
public sealed partial class BusinessCompositePageViewModel
    : MviViewModelBase<BusinessCompositePageState, BusinessCompositePageIntent, BusinessCompositePageEffect>
{
    /// <summary>
    /// 初始化生产业务组合页面 ViewModel。
    /// </summary>
    /// <param name="store">业务页面状态存储。</param>
    public BusinessCompositePageViewModel(IMviStore<BusinessCompositePageState, BusinessCompositePageIntent, BusinessCompositePageEffect> store)
        : base(store)
    {
    }

    /// <summary>
    /// 获取场景标题。
    /// </summary>
    [MviBind(nameof(BusinessCompositePageState.ScenarioTitle))]
    public partial string ScenarioTitle { get; private set; }

    /// <summary>
    /// 获取场景摘要。
    /// </summary>
    [MviBind(nameof(BusinessCompositePageState.ScenarioSummary))]
    public partial string ScenarioSummary { get; private set; }

    /// <summary>
    /// 获取主业务子组件 ViewModel。
    /// </summary>
    [MviBind(nameof(BusinessCompositePageState.PrimaryPanelViewModel))]
    public partial object PrimaryPanelViewModel { get; private set; }

    /// <summary>
    /// 获取第二业务子组件 ViewModel。
    /// </summary>
    [MviBind(nameof(BusinessCompositePageState.SecondaryPanelViewModel))]
    public partial object SecondaryPanelViewModel { get; private set; }

    /// <summary>
    /// 获取第三业务子组件 ViewModel。
    /// </summary>
    [MviBind(nameof(BusinessCompositePageState.TertiaryPanelViewModel))]
    public partial object TertiaryPanelViewModel { get; private set; }

    /// <summary>
    /// 获取第四业务子组件 ViewModel。
    /// </summary>
    [MviBind(nameof(BusinessCompositePageState.QuaternaryPanelViewModel))]
    public partial object QuaternaryPanelViewModel { get; private set; }


    /// <summary>
    /// 获取页面布局键。
    /// </summary>
    [MviBind(nameof(BusinessCompositePageState.PageLayout))]
    public partial string PageLayout { get; private set; }

    /// <summary>
    /// 获取当前业务上下文。
    /// </summary>
    [MviBind(nameof(BusinessCompositePageState.ActiveContext))]
    public partial string ActiveContext { get; private set; }

    /// <summary>
    /// 获取当前流程状态。
    /// </summary>
    [MviBind(nameof(BusinessCompositePageState.FlowStatus))]
    public partial string FlowStatus { get; private set; }

    /// <summary>
    /// 获取父子 MVI 与子子 MVI 交互日志。
    /// </summary>
    [MviBind(nameof(BusinessCompositePageState.InteractionLog))]
    public partial string InteractionLog { get; private set; }
}
