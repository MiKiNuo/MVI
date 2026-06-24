using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Domain.DI;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.PatientRegistry;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

/// <summary>
/// 表示仪表板卡片 MVI Store 的工厂。
/// 每个 PageKey 持有一个 store，store 内部使用统一的 CardReducer + CardEffectDispatcher。
/// 通过 <see cref="DiServiceAttribute"/> 注册为 DI 单例，
/// 由生成容器 <c>SampleGeneratedContainer</c> 在首次解析时构造并复用同一份 16 个 CardViewModel。
/// </summary>
[DiService(ServiceLifetime.Singleton)]
public sealed class CardStoreFactory
{
    private readonly IMviMediator _mediator;
    private readonly IMviPatientRegistry _patientRegistry;
    private readonly IReadOnlyDictionary<PageKey, IMviStore<CardState, CardIntent, CardEffect>> _storesByKey;
    private readonly IReadOnlyDictionary<PageKey, CardViewModel> _viewModelsByKey;

    /// <summary>
    /// 初始化仪表板卡片 MVI Store 工厂。
    /// </summary>
    /// <param name="mediator">Mediator。</param>
    /// <param name="patientRegistry">跨卡片患者注册表（单例），用于持久化入院登记卡提交的 Patient。</param>
    public CardStoreFactory(IMviMediator mediator, IMviPatientRegistry patientRegistry)
    {
        ArgumentNullException.ThrowIfNull(mediator);
        ArgumentNullException.ThrowIfNull(patientRegistry);

        _mediator = mediator;
        _patientRegistry = patientRegistry;
        IReadOnlyDictionary<PageKey, CardDefinition> all = DashboardCardRegistry.All;

        // 1) 预创建 16 个 store：先放入可变字典（让所有 store 能看到同组兄弟），
        //    再以只读形式赋给 _storesByKey 字段，保证运行时不再修改。
        Dictionary<PageKey, IMviStore<CardState, CardIntent, CardEffect>> mutableStores = new(all.Count);
        foreach (KeyValuePair<PageKey, CardDefinition> pair in all)
        {
            mutableStores[pair.Key] = CreateStore(mediator, patientRegistry, pair.Value, mutableStores);
        }

        IReadOnlyDictionary<PageKey, IMviStore<CardState, CardIntent, CardEffect>> readonlyStores = mutableStores;
        _storesByKey = readonlyStores;
        _viewModelsByKey = _storesByKey.ToDictionary(
            pair => pair.Key,
            pair => new CardViewModel(pair.Value));
    }

    /// <summary>
    /// 根据 PageKey 获取已构造的 ViewModel。
    /// </summary>
    /// <param name="key">PageKey。</param>
    /// <returns>对应 CardViewModel。</returns>
    public CardViewModel GetViewModel(PageKey key)
    {
        return _viewModelsByKey[key];
    }

    /// <summary>
    /// 根据 PageKey 获取已构造的 store。
    /// </summary>
    /// <param name="key">PageKey。</param>
    /// <returns>对应 store。</returns>
    public IMviStore<CardState, CardIntent, CardEffect> GetStore(PageKey key)
    {
        return _storesByKey[key];
    }

    private static IMviStore<CardState, CardIntent, CardEffect> CreateStore(
        IMviMediator mediator,
        IMviPatientRegistry patientRegistry,
        CardDefinition definition,
        IReadOnlyDictionary<PageKey, IMviStore<CardState, CardIntent, CardEffect>> allStores)
    {
        return new MviMutationStore<CardState, CardIntent, CardMutation, CardEffect>(
            initialState: CardState.FromDefinition(definition),
            intentHandler: new CardIntentHandler(DashboardCardRegistry.All),
            reducer: new CardMutationReducer(),
            effectDispatcher: new CardEffectDispatcher(mediator, patientRegistry, definition.Key, allStores),
            middlewares: null);
    }
}
