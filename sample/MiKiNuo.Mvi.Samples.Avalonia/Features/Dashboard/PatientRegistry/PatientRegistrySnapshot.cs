using R3;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.PatientRegistry;

/// <summary>
/// 表示患者注册表快照的不可变视图。
/// 任何对外暴露患者列表的接口（<see cref="IMviPatientRegistry.GetSnapshot"/>、Subscribe 推送）均应返回此结构，
/// 防止消费者持有对内部可变集合的引用后破坏注册表不变性。
/// </summary>
/// <param name="Patients">当前已入院的患者列表（按入院时间升序）。</param>
public sealed record PatientRegistrySnapshot(IReadOnlyList<Patient> Patients)
{
    /// <summary>获取已入院患者总数。</summary>
    public int AdmittedCount => Patients.Count;
}
