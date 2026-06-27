using System;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;
using MiKiNuo.Mvi.Application.MVI.Store;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示英雄队伍意图处理器。
/// </summary>
public sealed class HeroRosterIntentHandler
    : IMviIntentHandler<HeroRosterState, HeroRosterIntent, HeroRosterEffect>
{
    private readonly ILobbyApiService _apiService;
    private readonly IMviStore<PlayerState, PlayerIntent, PlayerEffect> _playerStore;
    private readonly IMviStore<MissionState, MissionIntent, MissionEffect> _missionStore;
    private readonly IMviStore<InventoryState, InventoryIntent, InventoryEffect> _inventoryStore;

    /// <summary>
    /// 初始化英雄队伍意图处理器。
    /// </summary>
    /// <param name="apiService">大厅后端 API 服务。</param>
    /// <param name="playerStore">玩家状态存储。</param>
    /// <param name="missionStore">任务状态存储。</param>
    /// <param name="inventoryStore">背包状态存储。</param>
    public HeroRosterIntentHandler(
        ILobbyApiService apiService,
        IMviStore<PlayerState, PlayerIntent, PlayerEffect> playerStore,
        IMviStore<MissionState, MissionIntent, MissionEffect> missionStore,
        IMviStore<InventoryState, InventoryIntent, InventoryEffect> inventoryStore)
    {
        _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        _playerStore = playerStore ?? throw new ArgumentNullException(nameof(playerStore));
        _missionStore = missionStore ?? throw new ArgumentNullException(nameof(missionStore));
        _inventoryStore = inventoryStore ?? throw new ArgumentNullException(nameof(inventoryStore));
    }

    /// <summary>
    /// 处理意图并产生后续意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>后续意图集合。</returns>
    public async ValueTask<IReadOnlyList<HeroRosterIntent>> HandleAsync(
        HeroRosterState state,
        HeroRosterIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        switch (intent)
        {
            case HeroRosterIntent.Train train:
                return await HandleTrainAsync(state, train, cancellationToken).ConfigureAwait(false);
            default:
                return Array.Empty<HeroRosterIntent>();
        }
    }

    private async ValueTask<IReadOnlyList<HeroRosterIntent>> HandleTrainAsync(
        HeroRosterState state,
        HeroRosterIntent.Train train,
        CancellationToken cancellationToken)
    {
        string heroName = train.Kind switch
        {
            HeroKind.Warrior => "战士",
            HeroKind.Mage => "法师",
            HeroKind.Archer => "弓手",
            _ => throw new ArgumentOutOfRangeException(nameof(train), train.Kind, "无效的英雄种类。"),
        };
        int currentLevel = train.Kind switch
        {
            HeroKind.Warrior => state.WarriorLevel,
            HeroKind.Mage => state.MageLevel,
            HeroKind.Archer => state.ArcherLevel,
            _ => throw new ArgumentOutOfRangeException(nameof(train), train.Kind, "无效的英雄种类。"),
        };
        int currentGold = _playerStore.CurrentState.Gold;
        TrainHeroResult result = await _apiService
            .TrainHeroAsync(heroName, currentLevel, currentGold, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success)
        {
            return new HeroRosterIntent[]
            {
                new HeroRosterIntent.TrainFailed(result.ErrorMessage ?? "训练失败。"),
            };
        }

        HeroRosterState leveledRoster = ApplyHeroLevel(state, train.Kind, result.NewLevel);
        int nextPower = CalculateHeroPower(leveledRoster.WarriorLevel, leveledRoster.MageLevel, leveledRoster.ArcherLevel);
        string selectedMission = _missionStore.CurrentState.SelectedMission;
        int stamina = _playerStore.CurrentState.Stamina;
        int potionCount = _inventoryStore.CurrentState.PotionCount;
        string readyText = await _apiService
            .BuildBattleReadyTextAsync(selectedMission, nextPower, stamina, potionCount, cancellationToken)
            .ConfigureAwait(false);
        return new HeroRosterIntent[]
        {
            new HeroRosterIntent.Trained(train.Kind, heroName, result.NewLevel, result.Cost, readyText),
        };
    }

    private static HeroRosterState ApplyHeroLevel(HeroRosterState roster, HeroKind kind, int newLevel)
    {
        return kind switch
        {
            HeroKind.Warrior => roster with { WarriorLevel = newLevel },
            HeroKind.Mage => roster with { MageLevel = newLevel },
            HeroKind.Archer => roster with { ArcherLevel = newLevel },
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "无效的英雄种类。"),
        };
    }

    private static int CalculateHeroPower(int warriorLevel, int mageLevel, int archerLevel)
    {
        return warriorLevel * 12 + mageLevel * 15 + archerLevel * 10;
    }
}
