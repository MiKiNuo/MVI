using MiKiNuo.Mvi.Application.MVI.IntentHandler;
using MiKiNuo.Mvi.Application.MVI.Store;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示任务意图处理器。
/// </summary>
public sealed class MissionIntentHandler
    : IMviIntentHandler<MissionState, MissionIntent, MissionEffect>
{
    private readonly ILobbyApiService _apiService;
    private readonly IMviStore<PlayerState, PlayerIntent, PlayerEffect> _playerStore;
    private readonly IMviStore<HeroRosterState, HeroRosterIntent, HeroRosterEffect> _heroRosterStore;
    private readonly IMviStore<InventoryState, InventoryIntent, InventoryEffect> _inventoryStore;

    /// <summary>
    /// 初始化任务意图处理器。
    /// </summary>
    /// <param name="apiService">大厅后端 API 服务。</param>
    /// <param name="playerStore">玩家状态存储。</param>
    /// <param name="heroRosterStore">英雄队伍状态存储。</param>
    /// <param name="inventoryStore">背包仓库状态存储。</param>
    public MissionIntentHandler(
        ILobbyApiService apiService,
        IMviStore<PlayerState, PlayerIntent, PlayerEffect> playerStore,
        IMviStore<HeroRosterState, HeroRosterIntent, HeroRosterEffect> heroRosterStore,
        IMviStore<InventoryState, InventoryIntent, InventoryEffect> inventoryStore)
    {
        ArgumentNullException.ThrowIfNull(apiService);
        ArgumentNullException.ThrowIfNull(playerStore);
        ArgumentNullException.ThrowIfNull(heroRosterStore);
        ArgumentNullException.ThrowIfNull(inventoryStore);
        _apiService = apiService;
        _playerStore = playerStore;
        _heroRosterStore = heroRosterStore;
        _inventoryStore = inventoryStore;
    }

    /// <summary>
    /// 处理意图并产生后续意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>后续意图集合。</returns>
    public async ValueTask<IReadOnlyList<MissionIntent>> HandleAsync(
        MissionState state,
        MissionIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        switch (intent)
        {
            case MissionIntent.Accept accept:
                return await HandleAcceptAsync(state, accept, cancellationToken).ConfigureAwait(false);
            case MissionIntent.Complete:
                return await HandleCompleteAsync(state, cancellationToken).ConfigureAwait(false);
            default:
                return Array.Empty<MissionIntent>();
        }
    }

    private async ValueTask<IReadOnlyList<MissionIntent>> HandleAcceptAsync(
        MissionState state,
        MissionIntent.Accept accept,
        CancellationToken cancellationToken)
    {
        int currentStamina = _playerStore.CurrentState.Stamina;
        int heroPower = _heroRosterStore.CurrentState.HeroTeamPower;
        int potionCount = _inventoryStore.CurrentState.PotionCount;

        AcceptMissionResult result = await _apiService
            .AcceptMissionAsync(
                accept.Spec.MissionName,
                accept.Spec.StaminaCost,
                accept.Spec.BaseReward,
                heroPower,
                currentStamina,
                cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success)
        {
            return new MissionIntent[]
            {
                new MissionIntent.AcceptFailed(result.ErrorMessage ?? "接受任务失败。"),
            };
        }

        string readyText = await _apiService
            .BuildBattleReadyTextAsync(
                accept.Spec.MissionName,
                heroPower,
                result.NewStamina,
                potionCount,
                cancellationToken)
            .ConfigureAwait(false);
        return new MissionIntent[]
        {
            new MissionIntent.Accepted(
                accept.Spec.MissionName,
                accept.Spec.StaminaCost,
                result.Reward,
                result.NewStamina,
                readyText),
        };
    }

    private async ValueTask<IReadOnlyList<MissionIntent>> HandleCompleteAsync(
        MissionState state,
        CancellationToken cancellationToken)
    {
        int heroPower = _heroRosterStore.CurrentState.HeroTeamPower;
        int currentStamina = _playerStore.CurrentState.Stamina;
        int potionCount = _inventoryStore.CurrentState.PotionCount;
        string selectedMission = state.SelectedMission;

        int reward = await _apiService
            .CompleteMissionAsync(heroPower, cancellationToken)
            .ConfigureAwait(false);
        string readyText = await _apiService
            .BuildBattleReadyTextAsync(
                selectedMission,
                heroPower,
                currentStamina,
                potionCount,
                cancellationToken)
            .ConfigureAwait(false);
        return new MissionIntent[] { new MissionIntent.Completed(reward, readyText) };
    }
}
