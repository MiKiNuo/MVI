using System;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Domain.MVI.Business;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示玩家资料意图处理器。
/// </summary>
public sealed class PlayerIntentHandler
    : MviIntentHandlerBase<PlayerState, PlayerIntent, PlayerEffect>
{
    private readonly ILobbyApiService _apiService;
    private readonly IMviStore<MissionState, MissionIntent, MissionEffect> _missionStore;
    private readonly IMviStore<HeroRosterState, HeroRosterIntent, HeroRosterEffect> _heroRosterStore;
    private readonly IMviStore<InventoryState, InventoryIntent, InventoryEffect> _inventoryStore;

    /// <summary>初始化玩家资料意图处理器。</summary>
    /// <param name="apiService">大厅后端 API 服务。</param>
    /// <param name="missionStore">任务大厅状态存储。</param>
    /// <param name="heroRosterStore">英雄队伍状态存储。</param>
    /// <param name="inventoryStore">背包仓库状态存储。</param>
    public PlayerIntentHandler(
        ILobbyApiService apiService,
        IMviStore<MissionState, MissionIntent, MissionEffect> missionStore,
        IMviStore<HeroRosterState, HeroRosterIntent, HeroRosterEffect> heroRosterStore,
        IMviStore<InventoryState, InventoryIntent, InventoryEffect> inventoryStore)
    {
        ArgumentNullException.ThrowIfNull(apiService);
        _apiService = apiService;
        ArgumentNullException.ThrowIfNull(missionStore);
        _missionStore = missionStore;
        ArgumentNullException.ThrowIfNull(heroRosterStore);
        _heroRosterStore = heroRosterStore;
        ArgumentNullException.ThrowIfNull(inventoryStore);
        _inventoryStore = inventoryStore;
    }

    /// <summary>
    /// 处理具体业务逻辑。
    /// </summary>
    /// <param name="state">当前状态（已通过 null 检查）。</param>
    /// <param name="intent">用户意图（已通过 null 检查）。</param>
    /// <param name="cancellationToken">取消标记（已通过检查）。</param>
    /// <returns>业务结果;无业务时返回 null。</returns>
    protected override async ValueTask<IMviBusinessResult?> HandleCoreAsync(
        PlayerState state,
        PlayerIntent intent,
        CancellationToken cancellationToken)
    {
        switch (intent)
        {
            case PlayerIntent.SetPlayer setPlayer:
                return await HandleSetPlayerAsync(setPlayer, cancellationToken).ConfigureAwait(false);
            default:
                return null;
        }
    }

    private async ValueTask<IMviBusinessResult?> HandleSetPlayerAsync(
        PlayerIntent.SetPlayer intent,
        CancellationToken cancellationToken)
    {
        string selectedMission = _missionStore.CurrentState.SelectedMission;
        int heroPower = _heroRosterStore.CurrentState.HeroTeamPower;
        int potionCount = _inventoryStore.CurrentState.PotionCount;
        string readyText = await _apiService
            .BuildBattleReadyTextAsync(
                selectedMission,
                heroPower,
                intent.Profile.Stamina,
                potionCount,
                cancellationToken)
            .ConfigureAwait(false);
        return new FollowUpIntentResult<PlayerIntent>(new PlayerIntent.PlayerSet(readyText));
    }
}
