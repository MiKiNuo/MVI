using System;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;
using MiKiNuo.Mvi.Application.MVI.Store;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示背包仓库意图处理器。
/// </summary>
public sealed class InventoryIntentHandler
    : IMviIntentHandler<InventoryState, InventoryIntent, InventoryEffect>
{
    private readonly ILobbyApiService _apiService;
    private readonly IMviStore<PlayerState, PlayerIntent, PlayerEffect> _playerStore;
    private readonly IMviStore<MissionState, MissionIntent, MissionEffect> _missionStore;
    private readonly IMviStore<HeroRosterState, HeroRosterIntent, HeroRosterEffect> _heroRosterStore;

    /// <summary>
    /// 初始化背包仓库意图处理器。
    /// </summary>
    /// <param name="apiService">大厅后端 API 服务。</param>
    /// <param name="playerStore">玩家状态存储。</param>
    /// <param name="missionStore">任务状态存储。</param>
    /// <param name="heroRosterStore">英雄队伍状态存储。</param>
    public InventoryIntentHandler(
        ILobbyApiService apiService,
        IMviStore<PlayerState, PlayerIntent, PlayerEffect> playerStore,
        IMviStore<MissionState, MissionIntent, MissionEffect> missionStore,
        IMviStore<HeroRosterState, HeroRosterIntent, HeroRosterEffect> heroRosterStore)
    {
        _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        _playerStore = playerStore ?? throw new ArgumentNullException(nameof(playerStore));
        _missionStore = missionStore ?? throw new ArgumentNullException(nameof(missionStore));
        _heroRosterStore = heroRosterStore ?? throw new ArgumentNullException(nameof(heroRosterStore));
    }

    /// <summary>
    /// 处理意图并产生后续意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>后续意图集合。</returns>
    public async ValueTask<IReadOnlyList<InventoryIntent>> HandleAsync(
        InventoryState state,
        InventoryIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        switch (intent)
        {
            case InventoryIntent.UsePotion:
                return await HandleUsePotionAsync(state, cancellationToken).ConfigureAwait(false);
            case InventoryIntent.OpenGoldBox:
                return await HandleOpenGoldBoxAsync(cancellationToken).ConfigureAwait(false);
            default:
                return Array.Empty<InventoryIntent>();
        }
    }

    private async ValueTask<IReadOnlyList<InventoryIntent>> HandleUsePotionAsync(
        InventoryState state,
        CancellationToken cancellationToken)
    {
        int currentStamina = _playerStore.CurrentState.Stamina;
        UsePotionResult result = await _apiService
            .UsePotionAsync(state.PotionCount, currentStamina, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success)
        {
            return new InventoryIntent[]
            {
                new InventoryIntent.PotionUseFailed(result.ErrorMessage ?? "使用药水失败。"),
            };
        }

        string selectedMission = _missionStore.CurrentState.SelectedMission;
        int heroPower = _heroRosterStore.CurrentState.HeroTeamPower;
        string readyText = await _apiService
            .BuildBattleReadyTextAsync(selectedMission, heroPower, result.NewStamina, result.NewPotionCount, cancellationToken)
            .ConfigureAwait(false);
        return new InventoryIntent[]
        {
            new InventoryIntent.PotionUsed(result.NewPotionCount, result.NewStamina, readyText),
        };
    }

    private async ValueTask<IReadOnlyList<InventoryIntent>> HandleOpenGoldBoxAsync(CancellationToken cancellationToken)
    {
        int gold = await _apiService.OpenGoldBoxAsync(cancellationToken).ConfigureAwait(false);
        return new InventoryIntent[] { new InventoryIntent.GoldBoxOpened(gold) };
    }
}
