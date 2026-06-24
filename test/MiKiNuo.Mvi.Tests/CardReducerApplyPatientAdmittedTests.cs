using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.PatientRegistry;
using R3;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 <see cref="CardIntentHandler"/> 在 <c>ApplyPatientAdmitted</c> 路径上的规约测试。
/// 验证 <c>CardIntent.ApplyPatientAdmitted(Patient)</c> 携带的强类型患者能写入 <c>CardState.RecentAdmittedPatient</c>，
/// 并同时刷新 StatusText / DetailText / ActionLog 形成可观测的状态变更。
/// </summary>
public sealed class CardReducerApplyPatientAdmittedTests
{
    /// <summary>
    /// 验证 ApplyPatientAdmitted 规约把 Patient 写入 RecentAdmittedPatient 字段。
    /// </summary>
    [Test]
    public async Task ApplyPatientAdmitted_StoresPatientInRecentAdmittedPatientAsync()
    {
        using MviMutationStore<CardState, CardIntent, CardMutation, CardEffect> store = CreateStore();
        Patient patient = new(
            Id: Guid.NewGuid(),
            Name: "张三",
            Age: 68,
            Diagnosis: "急性心衰",
            BedNo: "A12-08",
            NurseNote: "过敏史：青霉素",
            AdmittedAt: DateTimeOffset.Parse("2026-06-06T10:30:00+08:00"));

        await store.DispatchAsync(new CardIntent.ApplyPatientAdmitted(patient));

        await Assert.That(store.CurrentState.RecentAdmittedPatient).IsNotNull();
        await Assert.That(store.CurrentState.RecentAdmittedPatient!.Name).IsEqualTo("张三");
        await Assert.That(store.CurrentState.RecentAdmittedPatient.BedNo).IsEqualTo("A12-08");
        await Assert.That(store.CurrentState.RecentAdmittedPatient.Diagnosis).IsEqualTo("急性心衰");
    }

    /// <summary>
    /// 验证 ApplyPatientAdmitted 规约会刷新 StatusText/DetailText/ActionLog，让用户看到新患者流入。
    /// </summary>
    [Test]
    public async Task ApplyPatientAdmitted_RefreshesStatusDetailActionLogAsync()
    {
        using MviMutationStore<CardState, CardIntent, CardMutation, CardEffect> store = CreateStore();
        Patient patient = new(
            Id: Guid.NewGuid(),
            Name: "李四",
            Age: 55,
            Diagnosis: "高血压",
            BedNo: "B05-12",
            NurseNote: null,
            AdmittedAt: DateTimeOffset.Parse("2026-06-06T11:00:00+08:00"));

        await store.DispatchAsync(new CardIntent.ApplyPatientAdmitted(patient));

        await Assert.That(store.CurrentState.StatusText).Contains("李四");
        await Assert.That(store.CurrentState.DetailText).Contains("B05-12");
        await Assert.That(store.CurrentState.DetailText).Contains("高血压");
        await Assert.That(store.CurrentState.ActionLog).Contains("李四");
    }

    /// <summary>
    /// 验证 ApplyPatientAdmitted 规约不产生 Effect（仅修改 state），由 EffectDispatcher 负责派发 ApplyPatientAdmitted 给兄弟。
    /// </summary>
    [Test]
    public async Task ApplyPatientAdmitted_ProducesNoEffectAsync()
    {
        using MviMutationStore<CardState, CardIntent, CardMutation, CardEffect> store = CreateStore();
        List<CardEffect> effects = [];
        IDisposable subscription = store.Effects.Subscribe(e => effects.Add(e));
        Patient patient = new(
            Id: Guid.NewGuid(),
            Name: "王五",
            Age: null,
            Diagnosis: "发热",
            BedNo: "C03-21",
            NurseNote: null,
            AdmittedAt: DateTimeOffset.Now);

        await store.DispatchAsync(new CardIntent.ApplyPatientAdmitted(patient));

        await Assert.That(effects).IsEmpty();
        subscription.Dispose();
    }

    private static MviMutationStore<CardState, CardIntent, CardMutation, CardEffect> CreateStore()
    {
        CardDefinition definition = DashboardCardRegistry.GetDefinition(PageKey.BedOverview)!;
        CardState initial = CardState.FromDefinition(definition);
        return new MviMutationStore<CardState, CardIntent, CardMutation, CardEffect>(
            initial,
            new CardIntentHandler(DashboardCardRegistry.All),
            new CardMutationReducer(),
            new NoopCardEffectDispatcher());
    }

    private sealed class NoopCardEffectDispatcher : IMviEffectDispatcher<CardEffect>
    {
        /// <summary>
        /// 分发副作用。
        /// </summary>
        /// <param name="effect">副作用。</param>
        /// <param name="cancellationToken">取消标记。</param>
        /// <returns>表示异步分发过程的任务。</returns>
        public ValueTask DispatchAsync(CardEffect effect, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
