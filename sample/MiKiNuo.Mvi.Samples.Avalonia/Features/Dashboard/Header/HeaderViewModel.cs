using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Header;

/// <summary>
/// 表示 Dashboard 头部组件 ViewModel。
/// </summary>
public sealed partial class HeaderViewModel
    : MviViewModelBase<HeaderState, HeaderIntent, HeaderEffect>
{
    /// <summary>
    /// 初始化 Dashboard 头部组件 ViewModel。
    /// </summary>
    /// <param name="store">头部组件状态存储。</param>
    public HeaderViewModel(IMviStore<HeaderState, HeaderIntent, HeaderEffect> store)
        : base(store)
    {
    }

    /// <summary>
    /// 获取标题。
    /// </summary>
    [MviBind(nameof(HeaderState.Title))]
    public partial string Title { get; private set; }

    /// <summary>
    /// 获取副标题。
    /// </summary>
    [MviBind(nameof(HeaderState.SubTitle))]
    public partial string SubTitle { get; private set; }
}
