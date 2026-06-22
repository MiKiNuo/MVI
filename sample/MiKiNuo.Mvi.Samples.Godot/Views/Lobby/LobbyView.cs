using System;
using global::Godot;
using MiKiNuo.Mvi.Application.DI;
using MiKiNuo.Mvi.Presentation.Disposables;
using MiKiNuo.Mvi.Presentation.Slot;
using MiKiNuo.Mvi.Platforms.Godot.Binding;
using MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

namespace MiKiNuo.Mvi.Samples.Godot.Views.Lobby;

/// <summary>
/// 表示 Godot 游戏大厅组合 View。
/// <para>
/// 3 个常驻 chrome 子 View（玩家头部 / 大厅菜单 / 活动日志）与 1 个互斥面板 View
/// 全部以 <c>[MviSlot]</c> 字段声明，由 <c>MviCompositeSlotBindingGenerator</c> 在编译期
/// emit <c>OnBindSlots</c> override：</para>
/// <list type="bullet">
/// <item>3 个常驻 chrome 槽位：通过 <see cref="LobbyViewModel.CreateHeaderViewModel"/> /
///   <see cref="LobbyViewModel.CreateMenuViewModel"/> /
///   <see cref="LobbyViewModel.CreateActivityLogViewModel"/> 工厂按需解析，
///   <b>不</b>订阅属性变化（一次性绑定）。</item>
/// <item>1 个互斥面板槽位：通过 <see cref="LobbyViewModel.CreateCurrentPanelViewModel"/>
///   解析，并订阅 <see cref="LobbyViewModel.CurrentPanel"/> 属性变化时重新解析——
///   菜单切换会触发 <c>CurrentPanel</c> 变化并重渲。</item>
/// </list>
/// <para>
/// 视图不再持有任何"5 个互斥面板 GetNode + switch 路由"逻辑：源生成器在
/// <c>OnBindSlots</c> 末尾走 <c>IMviViewRegistry.CreateView</c> +
/// <c>slot.AddChild((Node)view)</c>，把 <see cref="LobbyViewModel.CurrentPanel"/>
/// 变化驱动的新 View 挂到 <c>_panelSlot</c> 控件上。
/// </para>
/// <para>
/// 5 个互斥面板的 View 通过 <c>[MviGodotView]</c> 注册到 <c>IGodotMviViewRegistry</c>，
/// 由 <see cref="GodotMviViewRegistryAdapter"/> 按 <c>{Name}ViewModel</c> 解析为
/// <c>{Name}View</c> 注册键——约定优于配置。
/// </para>
/// </summary>
public sealed partial class LobbyView : GodotMviControlView<LobbyViewModel>
{
    /// <summary>
    /// 玩家头部槽位：常驻 chrome 子 View，绑定时通过 <c>CreateHeaderViewModel()</c> 解析。
    /// </summary>
    [MviSlot(typeof(global::MiKiNuo.Mvi.Samples.Godot.Views.Lobby.PlayerHeader.PlayerHeaderView), factory: nameof(LobbyViewModel.CreateHeaderViewModel))]
    private Control? _headerSlot;

    /// <summary>
    /// 大厅菜单槽位：常驻 chrome 子 View，绑定时通过 <c>CreateMenuViewModel()</c> 解析。
    /// </summary>
    [MviSlot(typeof(global::MiKiNuo.Mvi.Samples.Godot.Views.Lobby.Menu.LobbyMenuView), factory: nameof(LobbyViewModel.CreateMenuViewModel))]
    private Control? _menuSlot;

    /// <summary>
    /// 活动日志槽位：常驻 chrome 子 View，绑定时通过 <c>CreateActivityLogViewModel()</c> 解析。
    /// </summary>
    [MviSlot(typeof(global::MiKiNuo.Mvi.Samples.Godot.Views.Lobby.ActivityLog.ActivityLogView), factory: nameof(LobbyViewModel.CreateActivityLogViewModel))]
    private Control? _activityLogSlot;

    /// <summary>
    /// 当前面板槽位：随 <see cref="LobbyViewModel.CurrentPanel"/> 变化重新解析。
    /// 源生成器会订阅 <c>CurrentPanel</c> 的 PropertyChanged，触发 Clear + AddChild 重新挂载。
    /// </summary>
    [MviSlot(
        typeof(object),
        factory: nameof(LobbyViewModel.CreateCurrentPanelViewModel),
        nameof(LobbyViewModel.CurrentPanel))]
    private Control? _panelSlot;

    /// <summary>
    /// 初始化游戏大厅组合视图。
    /// <para>
    /// 4 个 [MviSlot] 槽位字段在构造时通过 <c>GetNode&lt;Control&gt;</c> 取到 .tscn 中
    /// 对应的 ContentSlot Control 节点；源生成器会在编译期 override <c>OnBindSlots</c>
    /// 把对应子 View 挂到这些控件上。
    /// </para>
    /// </summary>
    public LobbyView()
    {
        _headerSlot = GetNode<Control>("Root/PlayerHeaderSlot")
            ?? throw new InvalidOperationException("无法找到 Root/PlayerHeaderSlot。");
        _menuSlot = GetNode<Control>("Root/Body/LobbyMenuSlot")
            ?? throw new InvalidOperationException("无法找到 Root/Body/LobbyMenuSlot。");
        _activityLogSlot = GetNode<Control>("Root/ActivityLogSlot")
            ?? throw new InvalidOperationException("无法找到 Root/ActivityLogSlot。");
        _panelSlot = GetNode<Control>("Root/Body/Content/CurrentPanelSlot")
            ?? throw new InvalidOperationException("无法找到 Root/Body/Content/CurrentPanelSlot。");
    }

    /// <summary>
    /// 执行具体 View 的绑定逻辑。
    /// </summary>
    /// <param name="viewModel">当前 ViewModel。</param>
    /// <param name="bindings">绑定生命周期集合。</param>
    protected override void OnBind(LobbyViewModel viewModel, MviDisposableBag bindings)
    {
        // LobbyView 自身没有手动状态绑定逻辑——4 个 [MviSlot] 槽位的所有绑定细节
        // （子 ViewModel 解析、CreateView、AddChild、PropertyChanged 订阅、Unbind 时清理）
        // 全部由 MviCompositeSlotBindingGenerator 在编译期 emit 到 OnBindSlots override。
        // 本方法保留为空 hook，仅作为未来需要叠加手动绑定时的扩展点。
        _ = viewModel;
        _ = bindings;
    }
}
