using System;
using System.Threading;
using System.Threading.Tasks;
using global::Godot;
using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示游戏大厅副作用分发器。
/// <para>
/// 处理 <see cref="LobbyEffect"/> 中的请求类副作用：调用
/// <see cref="ILobbyApiService"/> 执行异步业务逻辑，根据结果向 Store 派发结果意图。
/// </para>
/// </summary>
public sealed class LobbyEffectDispatcher : IMviEffectDispatcher<LobbyEffect>
{
    private readonly ILobbyApiService _apiService;
    private readonly Func<IMviStore<LobbyState, LobbyIntent, LobbyEffect>> _storeFactory;
    private IGameShellNavigator? _navigator;

    /// <summary>
    /// 初始化游戏大厅副作用分发器。
    /// </summary>
    /// <param name="apiService">大厅后端 API 服务。</param>
    /// <param name="storeFactory">Store 工厂，用于延迟获取 Store 引用以派发意图。</param>
    public LobbyEffectDispatcher(
        ILobbyApiService apiService,
        Func<IMviStore<LobbyState, LobbyIntent, LobbyEffect>> storeFactory)
    {
        _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        _storeFactory = storeFactory ?? throw new ArgumentNullException(nameof(storeFactory));
    }

    /// <summary>
    /// 设置游戏壳导航协调器。
    /// </summary>
    /// <param name="navigator">游戏壳导航协调器。</param>
    public void SetNavigator(IGameShellNavigator navigator)
    {
        _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
    }

    /// <summary>
    /// 分发副作用。
    /// </summary>
    /// <param name="effect">副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步分发过程的任务。</returns>
    public async ValueTask DispatchAsync(LobbyEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);
        cancellationToken.ThrowIfCancellationRequested();

        switch (effect)
        {
            case LobbyEffect.Trace trace:
                GD.Print($"[Godot Game MVI Lobby Effect] {trace.Text}");
                break;
            case LobbyEffect.LogoutRequested:
                await HandleLogoutRequestedAsync(cancellationToken).ConfigureAwait(false);
                break;
            case LobbyEffect.RequestSetPlayer requestSetPlayer:
                await HandleRequestSetPlayerAsync(requestSetPlayer, cancellationToken).ConfigureAwait(false);
                break;
            case LobbyEffect.RequestAcceptMission requestAcceptMission:
                await HandleRequestAcceptMissionAsync(requestAcceptMission, cancellationToken).ConfigureAwait(false);
                break;
            case LobbyEffect.RequestCompleteMission:
                await HandleRequestCompleteMissionAsync(cancellationToken).ConfigureAwait(false);
                break;
            case LobbyEffect.RequestTrainHero requestTrainHero:
                await HandleRequestTrainHeroAsync(requestTrainHero, cancellationToken).ConfigureAwait(false);
                break;
            case LobbyEffect.RequestUsePotion:
                await HandleRequestUsePotionAsync(cancellationToken).ConfigureAwait(false);
                break;
            case LobbyEffect.RequestOpenGoldBox:
                await HandleRequestOpenGoldBoxAsync(cancellationToken).ConfigureAwait(false);
                break;
            case LobbyEffect.RequestForge requestForge:
                await HandleRequestForgeAsync(requestForge, cancellationToken).ConfigureAwait(false);
                break;
            case LobbyEffect.RequestPrepareBattle:
                await HandleRequestPrepareBattleAsync(cancellationToken).ConfigureAwait(false);
                break;
        }
    }

    private async ValueTask HandleLogoutRequestedAsync(CancellationToken cancellationToken)
    {
        GD.Print("[Godot Game MVI Lobby Effect] ReturnToLogin");
        if (_navigator is not null)
        {
            await _navigator.ReturnToLoginAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask HandleRequestSetPlayerAsync(
        LobbyEffect.RequestSetPlayer requestSetPlayer,
        CancellationToken cancellationToken)
    {
        IMviStore<LobbyState, LobbyIntent, LobbyEffect> store = _storeFactory();
        LobbyState currentState = store.CurrentState;
        string readyText = await _apiService
            .BuildBattleReadyTextAsync(
                currentState.Mission.SelectedMission,
                currentState.HeroRoster.HeroTeamPower,
                requestSetPlayer.Profile.Stamina,
                currentState.Inventory.PotionCount,
                cancellationToken)
            .ConfigureAwait(false);
        await store.DispatchAsync(
            new LobbyIntent.PlayerSet(readyText),
            cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask HandleRequestAcceptMissionAsync(
        LobbyEffect.RequestAcceptMission requestAcceptMission,
        CancellationToken cancellationToken)
    {
        IMviStore<LobbyState, LobbyIntent, LobbyEffect> store = _storeFactory();
        LobbyState currentState = store.CurrentState;
        AcceptMissionResult result = await _apiService
            .AcceptMissionAsync(
                requestAcceptMission.MissionName,
                requestAcceptMission.StaminaCost,
                requestAcceptMission.BaseReward,
                currentState.HeroRoster.HeroTeamPower,
                currentState.Player.Stamina,
                cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success)
        {
            await store.DispatchAsync(
                new LobbyIntent.MissionAcceptFailed(result.ErrorMessage ?? "接受任务失败。"),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        string readyText = await _apiService
            .BuildBattleReadyTextAsync(
                requestAcceptMission.MissionName,
                currentState.HeroRoster.HeroTeamPower,
                result.NewStamina,
                currentState.Inventory.PotionCount,
                cancellationToken)
            .ConfigureAwait(false);
        await store.DispatchAsync(
            new LobbyIntent.MissionAccepted(
                requestAcceptMission.MissionName,
                requestAcceptMission.StaminaCost,
                result.Reward,
                result.NewStamina,
                readyText),
            cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask HandleRequestCompleteMissionAsync(CancellationToken cancellationToken)
    {
        IMviStore<LobbyState, LobbyIntent, LobbyEffect> store = _storeFactory();
        LobbyState currentState = store.CurrentState;
        int reward = await _apiService
            .CompleteMissionAsync(currentState.HeroRoster.HeroTeamPower, cancellationToken)
            .ConfigureAwait(false);
        string readyText = await _apiService
            .BuildBattleReadyTextAsync(
                currentState.Mission.SelectedMission,
                currentState.HeroRoster.HeroTeamPower,
                currentState.Player.Stamina,
                currentState.Inventory.PotionCount,
                cancellationToken)
            .ConfigureAwait(false);
        await store.DispatchAsync(
            new LobbyIntent.MissionCompleted(reward, readyText),
            cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask HandleRequestTrainHeroAsync(
        LobbyEffect.RequestTrainHero requestTrainHero,
        CancellationToken cancellationToken)
    {
        IMviStore<LobbyState, LobbyIntent, LobbyEffect> store = _storeFactory();
        LobbyState currentState = store.CurrentState;
        TrainHeroResult result = await _apiService
            .TrainHeroAsync(
                requestTrainHero.HeroName,
                requestTrainHero.CurrentLevel,
                currentState.Player.Gold,
                cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success)
        {
            await store.DispatchAsync(
                new LobbyIntent.HeroTrainFailed(result.ErrorMessage ?? "训练失败。"),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        LobbyHeroRoster leveledRoster = ApplyHeroLevel(currentState.HeroRoster, requestTrainHero.Kind, result.NewLevel);
        int nextPower = CalculateHeroPower(leveledRoster.WarriorLevel, leveledRoster.MageLevel, leveledRoster.ArcherLevel);
        string readyText = await _apiService
            .BuildBattleReadyTextAsync(
                currentState.Mission.SelectedMission,
                nextPower,
                currentState.Player.Stamina,
                currentState.Inventory.PotionCount,
                cancellationToken)
            .ConfigureAwait(false);
        await store.DispatchAsync(
            new LobbyIntent.HeroTrained(
                requestTrainHero.Kind,
                requestTrainHero.HeroName,
                result.NewLevel,
                result.Cost,
                readyText),
            cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask HandleRequestUsePotionAsync(CancellationToken cancellationToken)
    {
        IMviStore<LobbyState, LobbyIntent, LobbyEffect> store = _storeFactory();
        LobbyState currentState = store.CurrentState;
        UsePotionResult result = await _apiService
            .UsePotionAsync(currentState.Inventory.PotionCount, currentState.Player.Stamina, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success)
        {
            await store.DispatchAsync(
                new LobbyIntent.PotionUseFailed(result.ErrorMessage ?? "使用药水失败。"),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        string readyText = await _apiService
            .BuildBattleReadyTextAsync(
                currentState.Mission.SelectedMission,
                currentState.HeroRoster.HeroTeamPower,
                result.NewStamina,
                result.NewPotionCount,
                cancellationToken)
            .ConfigureAwait(false);
        await store.DispatchAsync(
            new LobbyIntent.PotionUsed(result.NewPotionCount, result.NewStamina, readyText),
            cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask HandleRequestOpenGoldBoxAsync(CancellationToken cancellationToken)
    {
        IMviStore<LobbyState, LobbyIntent, LobbyEffect> store = _storeFactory();
        int gold = await _apiService.OpenGoldBoxAsync(cancellationToken).ConfigureAwait(false);
        await store.DispatchAsync(
            new LobbyIntent.GoldBoxOpened(gold),
            cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask HandleRequestForgeAsync(
        LobbyEffect.RequestForge requestForge,
        CancellationToken cancellationToken)
    {
        IMviStore<LobbyState, LobbyIntent, LobbyEffect> store = _storeFactory();
        LobbyState currentState = store.CurrentState;
        ForgeResult result = await _apiService
            .ForgeAsync(
                requestForge.ItemName,
                requestForge.OreCost,
                requestForge.CrystalCost,
                requestForge.PowerBonus,
                currentState.HeroRoster.HeroTeamPower,
                currentState.Inventory.OreCount,
                currentState.Inventory.CrystalCount,
                cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success)
        {
            await store.DispatchAsync(
                new LobbyIntent.ForgeFailed(result.ErrorMessage ?? "锻造失败。"),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        int newPower = currentState.HeroRoster.HeroTeamPower + requestForge.PowerBonus;
        string readyText = await _apiService
            .BuildBattleReadyTextAsync(
                currentState.Mission.SelectedMission,
                newPower,
                currentState.Player.Stamina,
                currentState.Inventory.PotionCount,
                cancellationToken)
            .ConfigureAwait(false);
        await store.DispatchAsync(
            new LobbyIntent.Forged(
                requestForge.ItemName,
                requestForge.OreCost,
                requestForge.CrystalCost,
                requestForge.PowerBonus,
                result.ForgeScore,
                readyText),
            cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask HandleRequestPrepareBattleAsync(CancellationToken cancellationToken)
    {
        IMviStore<LobbyState, LobbyIntent, LobbyEffect> store = _storeFactory();
        LobbyState currentState = store.CurrentState;
        string readyText = await _apiService
            .BuildBattleReadyTextAsync(
                currentState.Mission.SelectedMission,
                currentState.HeroRoster.HeroTeamPower,
                currentState.Player.Stamina,
                currentState.Inventory.PotionCount,
                cancellationToken)
            .ConfigureAwait(false);
        await store.DispatchAsync(
            new LobbyIntent.BattlePrepared(readyText),
            cancellationToken).ConfigureAwait(false);
    }

    private static LobbyHeroRoster ApplyHeroLevel(LobbyHeroRoster roster, HeroKind kind, int newLevel)
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
