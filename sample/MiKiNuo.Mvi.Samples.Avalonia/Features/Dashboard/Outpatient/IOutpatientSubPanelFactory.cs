using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalEditor;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalReminder;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.PatientQueue;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient;

/// <summary>
/// 表示门诊工作站 3 个子组件 ViewModel（候诊队列 / 电子病历编辑 / 临床提醒）的工厂。
/// <para>
/// 父 <see cref="OutpatientWorkstationViewModel"/> 仅持此工厂（不直接持有任何子 VM 引用），
/// 由 <see cref="OutpatientWorkstationViewModel.CreateQueueViewModel"/>、
/// <see cref="OutpatientWorkstationViewModel.CreateEditorViewModel"/>、
/// <see cref="OutpatientWorkstationViewModel.CreateReminderViewModel"/> 等方法按需解析。
/// </para>
/// <para>
/// 实现内部在构造时一次性缓存 3 个子 VM，避免父 VM 长期持有可变子 VM 引用导致的
/// "VM-in-VM"反模式（State 体积膨胀、缓存无效状态、序列化失真）。
/// 3 个子组件均不参与父 Store 的状态计算，因此工厂缓存与按需解析在语义上等价。
/// </para>
/// </summary>
public interface IOutpatientSubPanelFactory
{
    /// <summary>
    /// 解析候诊队列子组件 ViewModel。
    /// </summary>
    /// <returns>候诊队列 <see cref="PatientQueueViewModel"/> 实例（缓存）。</returns>
    public object CreateQueueViewModel();

    /// <summary>
    /// 解析电子病历编辑子组件 ViewModel。
    /// </summary>
    /// <returns>电子病历编辑 <see cref="ClinicalEditorViewModel"/> 实例（缓存）。</returns>
    public object CreateEditorViewModel();

    /// <summary>
    /// 解析临床提醒子组件 ViewModel。
    /// </summary>
    /// <returns>临床提醒 <see cref="ClinicalReminderViewModel"/> 实例（缓存）。</returns>
    public object CreateReminderViewModel();
}
