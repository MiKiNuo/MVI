using MiKiNuo.Mvi.Application.MVI.IntentHandler;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Domain.MVI.Business;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示锻造工坊意图处理器。
/// </summary>
public sealed class ForgeLabIntentHandler
    : MviIntentHandlerBase<UnitState, ForgeLabIntent, ForgeLabEffect>
{
    private readonly ILobbyApiService _apiService;
    private readonly IMviStore<PlayerState, PlayerIntent, PlayerEffect> _playerStore;
    private readonly IMviStore<MissionState, MissionIntent, MissionEffect> _missionStore;
    private readonly IMviStore<HeroRosterState, HeroRosterIntent, HeroRosterEffect> _heroRosterStore;
    private readonly IMviStore<InventoryState, InventoryIntent, InventoryEffect> _inventoryStore;

    /// <summary>
    /// 初始化锻造工坊意图处理器。
    /// </summary>
    /// <param name="apiService">大厅后端 API 服务。</param>
    /// <param name="playerStore">玩家状态存储。</param>
    /// <param name="missionStore">任务状态存储。</param>
    /// <param name="heroRosterStore">英雄队伍状态存储。</param>
    /// <param name="inventoryStore">背包仓库状态存储。</param>
    public ForgeLabIntentHandler(
        ILobbyApiService apiService,
        IMviStore<PlayerState, PlayerIntent, PlayerEffect> playerStore,
        IMviStore<MissionState, MissionIntent, MissionEffect> missionStore,
        IMviStore<HeroRosterState, HeroRosterIntent, HeroRosterEffect> heroRosterStore,
        IMviStore<InventoryState, InventoryIntent, InventoryEffect> inventoryStore)
    {
        ArgumentNullException.ThrowIfNull(apiService);
        _apiService = apiService;
        ArgumentNullException.ThrowIfNull(playerStore);
        _playerStore = playerStore;
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
        UnitState state,
        ForgeLabIntent intent,
        CancellationToken cancellationToken)
    {
        switch (intent)
        {
            case ForgeLabIntent.Forge forge:
                return await HandleForgeAsync(forge, cancellationToken).ConfigureAwait(false);
            default:
                return null;
        }
    }

    private async ValueTask<IMviBusinessResult?> HandleForgeAsync(
        ForgeLabIntent.Forge forge,
        CancellationToken cancellationToken)
    {
        int heroPower = _heroRosterStore.CurrentState.HeroTeamPower;
        int oreCount = _inventoryStore.CurrentState.OreCount;
        int crystalCount = _inventoryStore.CurrentState.CrystalCount;
        int potionCount = _inventoryStore.CurrentState.PotionCount;
        int stamina = _playerStore.CurrentState.Stamina;
        string selectedMission = _missionStore.CurrentState.SelectedMission;

        ForgeResult result = await _apiService
            .ForgeAsync(
                forge.Spec.ItemName,
                forge.Spec.OreCost,
                forge.Spec.CrystalCost,
                forge.Spec.PowerBonus,
                heroPower,
                oreCount,
                crystalCount,
                cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success)
        {
            return new FollowUpIntentResult<ForgeLabIntent>(
                new ForgeLabIntent.ForgeFailed(result.ErrorMessage ?? "锻造失败。"));
        }

        int newPower = heroPower + forge.Spec.PowerBonus;
        string readyText = await _apiService
            .BuildBattleReadyTextAsync(selectedMission, newPower, stamina, potionCount, cancellationToken)
            .ConfigureAwait(false);
        return new FollowUpIntentResult<ForgeLabIntent>(
            new ForgeLabIntent.Forged(
                forge.Spec.ItemName,
                forge.Spec.OreCost,
                forge.Spec.CrystalCost,
                forge.Spec.PowerBonus,
                result.ForgeScore,
                readyText));
    }
}
