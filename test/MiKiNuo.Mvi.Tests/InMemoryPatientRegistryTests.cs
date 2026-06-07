using System.Threading.Tasks;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.PatientRegistry;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 <see cref="InMemoryPatientRegistry"/> 的单元测试。
/// 验证注册表提供：Register（线程安全）→ GetSnapshot（不可变快照）→ Subscribe（R3 反应式订阅）三段契约。
/// </summary>
public sealed class InMemoryPatientRegistryTests
{
    /// <summary>
    /// 验证 Register 写入新患者后，GetSnapshot 立即能看到该患者，Count 增加 1。
    /// </summary>
    [Test]
    public async Task Register_ThenGetSnapshot_ContainsThePatientAsync()
    {
#pragma warning disable CA2000
        InMemoryPatientRegistry registry = new();
#pragma warning restore CA2000
        using IDisposable __ = registry;
        Patient patient = new(
            Id: Guid.NewGuid(),
            Name: "张三",
            Age: 68,
            Diagnosis: "急性心衰",
            BedNo: "A12-08",
            NurseNote: "过敏史：青霉素",
            AdmittedAt: DateTimeOffset.Now);

        registry.Register(patient);
        PatientRegistrySnapshot snapshot = registry.GetSnapshot();

        await Assert.That(snapshot.Patients.Count).IsEqualTo(1);
        await Assert.That(snapshot.Patients[0].Name).IsEqualTo("张三");
        await Assert.That(snapshot.Patients[0].BedNo).IsEqualTo("A12-08");
    }

    /// <summary>
    /// 验证多次 Register 累计，AdmittedCount 等于累计次数。
    /// </summary>
    [Test]
    public async Task Register_MultiplePatients_AccumulatesInOrderAsync()
    {
#pragma warning disable CA2000
        InMemoryPatientRegistry registry = new();
#pragma warning restore CA2000
        using IDisposable __ = registry;
        Patient first = NewPatient("张三", "A12-08");
        Patient second = NewPatient("李四", "B05-12");
        Patient third = NewPatient("王五", "C03-21");

        registry.Register(first);
        registry.Register(second);
        registry.Register(third);
        PatientRegistrySnapshot snapshot = registry.GetSnapshot();

        await Assert.That(snapshot.Patients.Count).IsEqualTo(3);
        await Assert.That(snapshot.Patients[0].Name).IsEqualTo("张三");
        await Assert.That(snapshot.Patients[1].Name).IsEqualTo("李四");
        await Assert.That(snapshot.Patients[2].Name).IsEqualTo("王五");
    }

    /// <summary>
    /// 验证 Subscribe 订阅后，每次 Register 都会触发回调推送新快照。
    /// </summary>
    [Test]
    public async Task Subscribe_OnRegister_PushesNewSnapshotAsync()
    {
#pragma warning disable CA2000
        InMemoryPatientRegistry registry = new();
#pragma warning restore CA2000
        using IDisposable __ = registry;
        List<PatientRegistrySnapshot> received = new();
        IDisposable subscription = registry.Subscribe(snapshot => received.Add(snapshot));

        registry.Register(NewPatient("张三", "A12-08"));
        registry.Register(NewPatient("李四", "B05-12"));
        subscription.Dispose();

        await Assert.That(received.Count).IsEqualTo(2);
        await Assert.That(received[0].Patients.Count).IsEqualTo(1);
        await Assert.That(received[0].Patients[0].Name).IsEqualTo("张三");
        await Assert.That(received[1].Patients.Count).IsEqualTo(2);
        await Assert.That(received[1].Patients[1].Name).IsEqualTo("李四");
    }

    /// <summary>
    /// 验证 GetSnapshot 返回的列表是只读快照：后续 Register 不会影响已经返回的快照内容。
    /// </summary>
    [Test]
    public async Task GetSnapshot_ReturnsImmutableSnapshot_NotBackingStoreReferenceAsync()
    {
#pragma warning disable CA2000
        InMemoryPatientRegistry registry = new();
#pragma warning restore CA2000
        using IDisposable __ = registry;
        registry.Register(NewPatient("张三", "A12-08"));
        PatientRegistrySnapshot firstSnapshot = registry.GetSnapshot();

        registry.Register(NewPatient("李四", "B05-12"));
        PatientRegistrySnapshot secondSnapshot = registry.GetSnapshot();

        await Assert.That(firstSnapshot.Patients.Count).IsEqualTo(1);
        await Assert.That(secondSnapshot.Patients.Count).IsEqualTo(2);
    }

    private static Patient NewPatient(string name, string bedNo) => new(
        Id: Guid.NewGuid(),
        Name: name,
        Age: null,
        Diagnosis: string.Empty,
        BedNo: bedNo,
        NurseNote: null,
        AdmittedAt: DateTimeOffset.Now);
}
