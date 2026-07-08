using MiKiNuo.Mvi.Application.MVI.IntentHandler;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Domain.MVI.Business;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示战斗准备意图处理器。
/// </summary>
public sealed class BattlePrepIntentHandler
    : MviIntentHandlerBase<BattlePrepState, BattlePrepIntent, BattlePrepEffect>
{
    private readonly ILobbyApiService _apiService;
    private readonly IMviStore<PlayerState, PlayerIntent, PlayerEffect> _playerStore;
    private readonly IMviStore<MissionState, MissionIntent, MissionEffect> _missionStore;
    private readonly IMviStore<HeroRosterState, HeroRosterIntent, HeroRosterEffect> _heroRosterStore;
    private readonly IMviStore<InventoryState, InventoryIntent, InventoryEffect> _inventoryStore;

    /// <summary>
    /// 初始化战斗准备意图处理器。
    /// </summary>
    /// <param name="apiService">大厅后端 API 服务。</param>
    /// <param name="playerStore">玩家状态存储。</param>
    /// <param name="missionStore">任务状态存储。</param>
    /// <param name="heroRosterStore">英雄队伍状态存储。</param>
    /// <param name="inventoryStore">背包仓库状态存储。</param>
    public BattlePrepIntentHandler(
        ILobbyApiService apiService,
        IMviStore<PlayerState, PlayerIntent, PlayerEffect> playerStore,
        IMviStore<MissionState, MissionIntent, MissionEffect> missionStore,
        IMviStore<HeroRosterState, HeroRosterIntent, HeroRosterEffect> heroRosterStore,
        IMviStore<InventoryState, InventoryIntent, InventoryEffect> inventoryStore)
    {
        _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        _playerStore = playerStore ?? throw new ArgumentNullException(nameof(playerStore));
        _missionStore = missionStore ?? throw new ArgumentNullException(nameof(missionStore));
        _heroRosterStore = heroRosterStore ?? throw new ArgumentNullException(nameof(heroRosterStore));
        _inventoryStore = inventoryStore ?? throw new ArgumentNullException(nameof(inventoryStore));
    }

    /// <summary>
    /// 处理具体业务逻辑。
    /// </summary>
    /// <param name="state">当前状态（已通过 null 检查）。</param>
    /// <param name="intent">用户意图（已通过 null 检查）。</param>
    /// <param name="cancellationToken">取消标记（已通过检查）。</param>
    /// <returns>业务结果;无业务时返回 null。</returns>
    protected override async ValueTask<IMviBusinessResult?> HandleCoreAsync(
        BattlePrepState state,
        BattlePrepIntent intent,
        CancellationToken cancellationToken)
    {
        switch (intent)
        {
            case BattlePrepIntent.PrepareBattle:
                return await HandlePrepareBattleAsync(cancellationToken).ConfigureAwait(false);
            default:
                return null;
        }
    }

    private async ValueTask<IMviBusinessResult?> HandlePrepareBattleAsync(
        CancellationToken cancellationToken)
    {
        string selectedMission = _missionStore.CurrentState.SelectedMission;
        int heroPower = _heroRosterStore.CurrentState.HeroTeamPower;
        int stamina = _playerStore.CurrentState.Stamina;
        int potionCount = _inventoryStore.CurrentState.PotionCount;

        string readyText = await _apiService
            .BuildBattleReadyTextAsync(selectedMission, heroPower, stamina, potionCount, cancellationToken)
            .ConfigureAwait(false);
        return new FollowUpIntentResult<BattlePrepIntent>(
            new BattlePrepIntent.BattlePrepared(readyText));
    }
}
