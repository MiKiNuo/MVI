using R3;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.PatientRegistry;

/// <summary>
/// 表示仪表板跨卡片共享的患者注册表。
/// 入院登记卡片提交后调用 <see cref="Register"/>，床位总览 / 护理任务 / 病区风险 3 张卡片通过
/// <see cref="Subscribe"/> 订阅变更并更新本地 <c>CardState.RecentAdmittedPatient</c>。
/// <para>
/// 设计选择 in-memory 单例：示例是 MVI 数据流演示，不接入真实 HIS / 病历系统；
/// 生产落地时此接口保持稳定，实现可替换为基于数据库 / FHIR / 消息总线的版本。
/// </para>
/// </summary>
public interface IMviPatientRegistry
{
    /// <summary>注册一位新入院患者。</summary>
    /// <param name="patient">患者记录。重复 Id 注册将覆盖既有记录。</param>
    public void Register(Patient patient);

    /// <summary>获取当前注册表快照的不可变视图。</summary>
    /// <returns>当前快照；空注册表返回空 <see cref="PatientRegistrySnapshot.Patients"/> 列表。</returns>
    public PatientRegistrySnapshot GetSnapshot();

    /// <summary>订阅注册表变更。每次 <see cref="Register"/> 调用后推送一次新快照。</summary>
    /// <param name="onChange">接收快照的 R3 兼容回调（只关心 OnNext）。</param>
    /// <returns>取消订阅的 IDisposable 句柄。</returns>
    public IDisposable Subscribe(Action<PatientRegistrySnapshot> onChange);
}
