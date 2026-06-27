using System;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;
using MiKiNuo.Mvi.Application.MVI.Store;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示玩家资料意图处理器。
/// </summary>
public sealed class PlayerIntentHandler
    : IMviIntentHandler<PlayerState, PlayerIntent, PlayerEffect>
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
        _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        _missionStore = missionStore ?? throw new ArgumentNullException(nameof(missionStore));
        _heroRosterStore = heroRosterStore ?? throw new ArgumentNullException(nameof(heroRosterStore));
        _inventoryStore = inventoryStore ?? throw new ArgumentNullException(nameof(inventoryStore));
    }

    /// <summary>处理意图并产生后续意图。</summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>后续意图集合。</returns>
    public async ValueTask<IReadOnlyList<PlayerIntent>> HandleAsync(
        PlayerState state,
        PlayerIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        switch (intent)
        {
            case PlayerIntent.SetPlayer setPlayer:
                return await HandleSetPlayerAsync(setPlayer, cancellationToken).ConfigureAwait(false);
            default:
                return Array.Empty<PlayerIntent>();
        }
    }

    private async ValueTask<IReadOnlyList<PlayerIntent>> HandleSetPlayerAsync(
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
        return new PlayerIntent[] { new PlayerIntent.PlayerSet(readyText) };
    }
}
