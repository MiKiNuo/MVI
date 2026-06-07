using System.Threading.Tasks;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.PatientRegistry;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 <see cref="CardReducer"/> 在 <c>ApplyPatientAdmitted</c> 路径上的归约测试。
/// 验证 <c>CardIntent.ApplyPatientAdmitted(Patient)</c> 携带的强类型患者能写入 <c>CardState.RecentAdmittedPatient</c>，
/// 并同时刷新 StatusText / DetailText / ActionLog 形成可观测的状态变更。
/// </summary>
public sealed class CardReducerApplyPatientAdmittedTests
{
    /// <summary>
    /// 验证 ApplyPatientAdmitted 归约把 Patient 写入 RecentAdmittedPatient 字段。
    /// </summary>
    [Test]
    public async Task ApplyPatientAdmitted_StoresPatientInRecentAdmittedPatientAsync()
    {
        CardReducer reducer = new();
        CardState state = NewBedOverviewState();
        Patient patient = new(
            Id: Guid.NewGuid(),
            Name: "张三",
            Age: 68,
            Diagnosis: "急性心衰",
            BedNo: "A12-08",
            NurseNote: "过敏史：青霉素",
            AdmittedAt: DateTimeOffset.Parse("2026-06-06T10:30:00+08:00"));

        MviReduceResult<CardState, CardEffect> result = reducer.Reduce(
            state,
            new CardIntent.ApplyPatientAdmitted(patient));

        await Assert.That(result.State.RecentAdmittedPatient).IsNotNull();
        await Assert.That(result.State.RecentAdmittedPatient!.Name).IsEqualTo("张三");
        await Assert.That(result.State.RecentAdmittedPatient.BedNo).IsEqualTo("A12-08");
        await Assert.That(result.State.RecentAdmittedPatient.Diagnosis).IsEqualTo("急性心衰");
    }

    /// <summary>
    /// 验证 ApplyPatientAdmitted 归约会刷新 StatusText/DetailText/ActionLog，让用户看到新患者流入。
    /// </summary>
    [Test]
    public async Task ApplyPatientAdmitted_RefreshesStatusDetailActionLogAsync()
    {
        CardReducer reducer = new();
        CardState state = NewBedOverviewState();
        Patient patient = new(
            Id: Guid.NewGuid(),
            Name: "李四",
            Age: 55,
            Diagnosis: "高血压",
            BedNo: "B05-12",
            NurseNote: null,
            AdmittedAt: DateTimeOffset.Parse("2026-06-06T11:00:00+08:00"));

        MviReduceResult<CardState, CardEffect> result = reducer.Reduce(
            state,
            new CardIntent.ApplyPatientAdmitted(patient));

        await Assert.That(result.State.StatusText).Contains("李四");
        await Assert.That(result.State.DetailText).Contains("B05-12");
        await Assert.That(result.State.DetailText).Contains("高血压");
        await Assert.That(result.State.ActionLog).Contains("李四");
    }

    /// <summary>
    /// 验证 ApplyPatientAdmitted 归约不产生 Effect（仅修改 state），由 EffectDispatcher 负责派发 ApplyPatientAdmitted 给兄弟。
    /// </summary>
    [Test]
    public async Task ApplyPatientAdmitted_ProducesNoEffectAsync()
    {
        CardReducer reducer = new();
        CardState state = NewBedOverviewState();
        Patient patient = new(
            Id: Guid.NewGuid(),
            Name: "王五",
            Age: null,
            Diagnosis: "发热",
            BedNo: "C03-21",
            NurseNote: null,
            AdmittedAt: DateTimeOffset.Now);

        MviReduceResult<CardState, CardEffect> result = reducer.Reduce(
            state,
            new CardIntent.ApplyPatientAdmitted(patient));

        await Assert.That(result.Effects).IsEmpty();
    }

    private static CardState NewBedOverviewState()
    {
        CardDefinition definition = DashboardCardRegistry.GetDefinition(PageKey.BedOverview)!;
        return CardState.FromDefinition(definition);
    }
}
