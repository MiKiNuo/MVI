using System;
using global::Godot;
using MiKiNuo.Mvi.Presentation.Disposables;
using MiKiNuo.Mvi.Platforms.Godot.Binding;
using MiKiNuo.Mvi.Samples.Godot.Features.Lobby;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;
using MiKiNuo.Mvi.Samples.Godot.Views.Lobby.ActivityLog;
using MiKiNuo.Mvi.Samples.Godot.Views.Lobby.BattlePrep;
using MiKiNuo.Mvi.Samples.Godot.Views.Lobby.ForgeLab;
using MiKiNuo.Mvi.Samples.Godot.Views.Lobby.HeroRoster;
using MiKiNuo.Mvi.Samples.Godot.Views.Lobby.Inventory;
using MiKiNuo.Mvi.Samples.Godot.Views.Lobby.Menu;
using MiKiNuo.Mvi.Samples.Godot.Views.Lobby.MissionBoard;
using MiKiNuo.Mvi.Samples.Godot.Views.Lobby.PlayerHeader;

namespace MiKiNuo.Mvi.Samples.Godot.Views.Lobby;

/// <summary>
/// 表示 Godot 游戏大厅组合 View。
/// <para>
/// 3 个常驻子 View（玩家头部 / 大厅菜单 / 活动日志）由 <see cref="LobbyViewModel"/>
/// 通过 <see cref="ILobbyChromeFactory"/> 工厂方法按需解析。
/// 5 个互斥面板 View 通过 <see cref="LobbyViewModel.CreateCurrentPanelViewModel"/> 解析，
/// <c>CurrentPanel</c> 变化时按需重新绑定到当前面板，其他 4 个面板 VM 由 <see cref="ILobbyPanelFactory"/> 内部缓存。
/// </para>
/// </summary>
public partial class LobbyView : GodotMviControlView<LobbyViewModel>
{
    private Control? _currentPanelView;
    private object? _currentPanelViewModel;

    /// <inheritdoc />
    protected override void OnBind(LobbyViewModel viewModel, MviDisposableBag bindings)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(bindings);
        PlayerHeaderView headerView = GetNode<PlayerHeaderView>("Root/PlayerHeaderView");
        LobbyMenuView menuView = GetNode<LobbyMenuView>("Root/Body/LobbyMenuView");
        MissionBoardView missionView = GetNode<MissionBoardView>("Root/Body/Content/MissionBoardView");
        HeroRosterView heroView = GetNode<HeroRosterView>("Root/Body/Content/HeroRosterView");
        InventoryView inventoryView = GetNode<InventoryView>("Root/Body/Content/InventoryView");
        ForgeLabView forgeView = GetNode<ForgeLabView>("Root/Body/Content/ForgeLabView");
        BattlePrepView battleView = GetNode<BattlePrepView>("Root/Body/Content/BattlePrepView");
        ActivityLogView logView = GetNode<ActivityLogView>("Root/ActivityLogView");

        headerView.Bind((PlayerHeaderViewModel)viewModel.CreateHeaderViewModel());
        menuView.Bind((LobbyMenuViewModel)viewModel.CreateMenuViewModel());
        logView.Bind((ActivityLogViewModel)viewModel.CreateActivityLogViewModel());

        BindPropertyChanged(
            viewModel,
            nameof(LobbyViewModel.CurrentPanel),
            () => SwitchPanel(
                viewModel,
                missionView,
                heroView,
                inventoryView,
                forgeView,
                battleView),
            bindings);

        // 初始绑定：构造 View 时的 CurrentPanel（默认 MissionBoard）
        SwitchPanel(viewModel, missionView, heroView, inventoryView, forgeView, battleView);

        bindings.Add(() => UnbindCurrentPanel());
    }

    private void SwitchPanel(
        LobbyViewModel viewModel,
        MissionBoardView missionView,
        HeroRosterView heroView,
        InventoryView inventoryView,
        ForgeLabView forgeView,
        BattlePrepView battleView)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        object? panelViewModel = viewModel.CreateCurrentPanelViewModel();
        if (ReferenceEquals(panelViewModel, _currentPanelViewModel))
        {
            return;
        }

        UnbindCurrentPanel();
        _currentPanelViewModel = panelViewModel;
        if (panelViewModel is null)
        {
            _currentPanelView = null;
            return;
        }

        Control nextView = viewModel.CurrentPanel switch
        {
            LobbyPanelKeys.MissionBoard => ResolveView(missionView, panelViewModel, viewModel),
            LobbyPanelKeys.HeroRoster => ResolveView(heroView, panelViewModel, viewModel),
            LobbyPanelKeys.Inventory => ResolveView(inventoryView, panelViewModel, viewModel),
            LobbyPanelKeys.ForgeLab => ResolveView(forgeView, panelViewModel, viewModel),
            LobbyPanelKeys.BattlePrep => ResolveView(battleView, panelViewModel, viewModel),
            _ => throw new InvalidOperationException($"未识别的 Lobby 面板键：{viewModel.CurrentPanel}"),
        };
        _currentPanelView = nextView;
    }

    private static Control ResolveView(Control view, object viewModel, LobbyViewModel owner)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(owner);
        return view switch
        {
            MissionBoardView mission => BindPanel(mission, (MissionBoardViewModel)viewModel, owner),
            HeroRosterView hero => BindPanel(hero, (HeroRosterViewModel)viewModel, owner),
            InventoryView inventory => BindPanel(inventory, (InventoryViewModel)viewModel, owner),
            ForgeLabView forge => BindPanel(forge, (ForgeLabViewModel)viewModel, owner),
            BattlePrepView battle => BindPanel(battle, (BattlePrepViewModel)viewModel, owner),
            _ => throw new InvalidOperationException($"Lobby 面板 View 类型不支持：{view.GetType().FullName}"),
        };
    }

    private static TView BindPanel<TView, TViewModel>(TView view, TViewModel viewModel, LobbyViewModel owner)
        where TView : Control, IMviGodotBindable<TViewModel>
        where TViewModel : class
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(owner);
        view.Bind(viewModel);
        view.Visible = true;
        return view;
    }

    private void UnbindCurrentPanel()
    {
        if (_currentPanelView is null)
        {
            return;
        }

        _currentPanelView.Visible = false;
        if (_currentPanelView is MissionBoardView mission)
        {
            mission.Unbind();
        }
        else if (_currentPanelView is HeroRosterView hero)
        {
            hero.Unbind();
        }
        else if (_currentPanelView is InventoryView inventory)
        {
            inventory.Unbind();
        }
        else if (_currentPanelView is ForgeLabView forge)
        {
            forge.Unbind();
        }
        else if (_currentPanelView is BattlePrepView battle)
        {
            battle.Unbind();
        }

        _currentPanelView = null;
        _currentPanelViewModel = null;
    }
}
