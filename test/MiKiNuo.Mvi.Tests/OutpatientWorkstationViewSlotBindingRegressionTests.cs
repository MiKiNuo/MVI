using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.VisualTree;
using MiKiNuo.Mvi.Presentation.ViewRegistry;
using MiKiNuo.Mvi.Samples.Avalonia.Composition;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 回归测试：<c>OutpatientWorkstationView</c> 通过 <see cref="IMviViewRegistry"/> 创建后，
/// 3 个槽位（QueueSlot / EditorSlot / ReminderSlot）必须被填入对应子 View，
/// 否则右侧组合页面只显示标题和描述，正文为空白。
/// <para>
/// 这正是用户截图"右侧的组合界面不显示"症状的根因复现：
/// <c>OutpatientWorkstationView</c> 用了"手写 <c>new Bind(viewModel)</c>" 旧模式，
/// 而容器生成代码调用的是基类 2-arg <c>Bind(viewModel, resolver)</c>，
/// 导致手写的子组件创建逻辑被跳过。
/// </para>
/// </summary>
public sealed class OutpatientWorkstationViewSlotBindingRegressionTests
{
    private static readonly HeadlessUnitTestSession Session = HeadlessUnitTestSession.StartNew(typeof(HeadlessTestApp));

    /// <summary>
    /// 验证 <c>OutpatientWorkstationView</c> 的 3 个槽位在通过 <see cref="IMviViewRegistry"/>
    /// 走通 <c>CreateView</c> 后 <c>Content</c> 不为 <c>null</c>，即子 View 已被挂载。
    /// </summary>
    [Test]
    public async Task OutpatientWorkstationView_Slots_Should_BePopulatedAfterCreateViewAsync()
    {
        (object? queueContent, object? editorContent, object? reminderContent) = await Session.Dispatch(
            () =>
            {
                SampleGeneratedContainer container = new();
                container.NavigateToDashboardAsync("测试用户").AsTask().GetAwaiter().GetResult();
                DashboardViewModel dashboardViewModel = container.Resolve<DashboardViewModel>();
                IMviViewRegistry viewRegistry = container.Resolve<IMviViewRegistry>();

                // 构造 OutpatientWorkstationViewModel：通过 dashboard.CreateCurrentPageViewModel() 解析
                // （与 DashboardView 的 _pageSlot 走同一路径，确保 PageKey 正确）。
                object pageViewModel = dashboardViewModel.CreateCurrentPageViewModel()!;
                OutpatientWorkstationViewModel outpatientViewModel = (OutpatientWorkstationViewModel)pageViewModel;

                // 通过 ViewRegistry 走完整流程：构造 View + Bind(viewModel, resolver)。
                object viewObject = viewRegistry.CreateView(outpatientViewModel);
                OutpatientWorkstationView view = (OutpatientWorkstationView)viewObject;
                ContentControl queueSlot = view.FindControl<ContentControl>("QueueSlot")
                    ?? throw new InvalidOperationException("OutpatientWorkstationView 未声明 QueueSlot 节点。");
                ContentControl editorSlot = view.FindControl<ContentControl>("EditorSlot")
                    ?? throw new InvalidOperationException("OutpatientWorkstationView 未声明 EditorSlot 节点。");
                ContentControl reminderSlot = view.FindControl<ContentControl>("ReminderSlot")
                    ?? throw new InvalidOperationException("OutpatientWorkstationView 未声明 ReminderSlot 节点。");

                return (queueSlot.Content, editorSlot.Content, reminderSlot.Content);
            },
            CancellationToken.None);

        await Assert.That(queueContent).IsNotNull();
        await Assert.That(editorContent).IsNotNull();
        await Assert.That(reminderContent).IsNotNull();
    }
}
