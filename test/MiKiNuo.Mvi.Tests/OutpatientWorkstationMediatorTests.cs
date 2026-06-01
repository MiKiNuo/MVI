using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Samples.Avalonia.Composition;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Mediator;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalEditor;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalReminder;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.PatientQueue;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 验证门诊工作站患者选择后中介者路由到电子病历编辑和临床提醒。
/// </summary>
public sealed class OutpatientWorkstationMediatorTests
{
    /// <summary>
    /// 验证接诊患者后电子病历编辑 ViewModel 会加载患者上下文。
    /// </summary>
    [Test]
    public async Task CallNextPatient_Should_LoadPatientInClinicalEditorAsync()
    {
        SampleGeneratedContainer container = new();
        await container.NavigateToDashboardAsync("测试医生");

        DashboardViewModel dashboard = container.Resolve<DashboardViewModel>();
        OutpatientWorkstationViewModel workstation = (OutpatientWorkstationViewModel)dashboard.CurrentPageViewModel;
        PatientQueueViewModel queue = (PatientQueueViewModel)workstation.QueueViewModel;
        ClinicalEditorViewModel editor = (ClinicalEditorViewModel)workstation.ClinicalEditorViewModel;

        // 初始状态：未选择患者
        await Assert.That(editor.PatientName).IsEqualTo("未选择患者");

        // 执行接诊下一位
        await queue.CallNextCommand.ExecuteAsync(null);
        await Task.Delay(200);

        // 验证电子病历已加载患者
        await Assert.That(editor.PatientName).IsNotEqualTo("未选择患者");
        await Assert.That(editor.SaveMessage).Contains("已载入患者上下文");
    }

    /// <summary>
    /// 验证接诊患者后临床提醒 ViewModel 会加载患者提醒。
    /// </summary>
    [Test]
    public async Task CallNextPatient_Should_LoadClinicalRemindersAsync()
    {
        SampleGeneratedContainer container = new();
        await container.NavigateToDashboardAsync("测试医生");

        DashboardViewModel dashboard = container.Resolve<DashboardViewModel>();
        OutpatientWorkstationViewModel workstation = (OutpatientWorkstationViewModel)dashboard.CurrentPageViewModel;
        PatientQueueViewModel queue = (PatientQueueViewModel)workstation.QueueViewModel;
        ClinicalReminderViewModel reminder = (ClinicalReminderViewModel)workstation.ClinicalReminderViewModel;

        // 初始状态
        await Assert.That(reminder.PatientName).IsEqualTo("未选择患者");

        // 执行接诊下一位
        await queue.CallNextCommand.ExecuteAsync(null);
        await Task.Delay(200);

        // 验证临床提醒已加载
        await Assert.That(reminder.PatientName).IsNotEqualTo("未选择患者");
        await Assert.That(reminder.HasAlert).IsTrue();
    }

    /// <summary>
    /// 验证中介者直接发送 OpenPatientEncounterRequest 能路由到子 Store。
    /// </summary>
    [Test]
    public async Task Mediator_Should_RouteOpenPatientEncounterToChildStoresAsync()
    {
        SampleGeneratedContainer container = new();
        await container.NavigateToDashboardAsync("测试医生");

        DashboardViewModel dashboard = container.Resolve<DashboardViewModel>();
        OutpatientWorkstationViewModel workstation = (OutpatientWorkstationViewModel)dashboard.CurrentPageViewModel;
        ClinicalEditorViewModel editor = (ClinicalEditorViewModel)workstation.ClinicalEditorViewModel;
        ClinicalReminderViewModel reminder = (ClinicalReminderViewModel)workstation.ClinicalReminderViewModel;

        // 直接通过中介者发送请求
        IMviMediator mediator = container.Resolve<IMviMediator>();
        await mediator.SendAsync<OpenPatientEncounterRequest, PatientEncounterResponse>(
            new OpenPatientEncounterRequest("张三 · 男 · 35岁 · 发热"));
        await Task.Delay(200);

        // 验证电子病历和临床提醒均已加载
        await Assert.That(editor.PatientName).IsEqualTo("张三 · 男 · 35岁 · 发热");
        await Assert.That(reminder.PatientName).IsEqualTo("张三 · 男 · 35岁 · 发热");
        await Assert.That(reminder.HasAlert).IsTrue();
    }
}
