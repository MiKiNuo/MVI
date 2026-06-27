using System;
using System.Collections.Generic;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示游戏大厅可切换面板 ViewModel 的工厂实现。
/// <para>
/// 5 个互斥面板（任务大厅/英雄队伍/背包仓库/锻造工坊/战斗准备）由各自独立 Store 驱动；
/// 本工厂缓存为单例避免每次切换面板都重新构造。
/// </para>
/// </summary>
public sealed class LobbyPanelFactory : ILobbyPanelFactory
{
    private readonly IReadOnlyDictionary<LobbyPanel, object> _panelViewModels;

    /// <summary>
    /// 初始化游戏大厅面板 ViewModel 工厂。
    /// </summary>
    /// <param name="missionBoardViewModel">任务大厅 ViewModel。</param>
    /// <param name="heroRosterViewModel">英雄队伍 ViewModel。</param>
    /// <param name="inventoryViewModel">背包仓库 ViewModel。</param>
    /// <param name="forgeLabViewModel">锻造工坊 ViewModel。</param>
    /// <param name="battlePrepViewModel">战斗准备 ViewModel。</param>
    public LobbyPanelFactory(
        MissionBoardViewModel missionBoardViewModel,
        HeroRosterViewModel heroRosterViewModel,
        InventoryViewModel inventoryViewModel,
        ForgeLabViewModel forgeLabViewModel,
        BattlePrepViewModel battlePrepViewModel)
    {
        ArgumentNullException.ThrowIfNull(missionBoardViewModel);
        ArgumentNullException.ThrowIfNull(heroRosterViewModel);
        ArgumentNullException.ThrowIfNull(inventoryViewModel);
        ArgumentNullException.ThrowIfNull(forgeLabViewModel);
        ArgumentNullException.ThrowIfNull(battlePrepViewModel);

        _panelViewModels = new Dictionary<LobbyPanel, object>
        {
            [LobbyPanel.MissionBoard] = missionBoardViewModel,
            [LobbyPanel.HeroRoster] = heroRosterViewModel,
            [LobbyPanel.Inventory] = inventoryViewModel,
            [LobbyPanel.ForgeLab] = forgeLabViewModel,
            [LobbyPanel.BattlePrep] = battlePrepViewModel,
        };
    }

    /// <summary>
    /// 根据面板枚举解析面板 ViewModel。
    /// </summary>
    /// <param name="panel">面板枚举。</param>
    /// <returns>面板 ViewModel；未识别时返回 null。</returns>
    public object? CreatePanel(LobbyPanel panel)
    {
        return _panelViewModels.TryGetValue(panel, out object? viewModel) ? viewModel : null;
    }
}
