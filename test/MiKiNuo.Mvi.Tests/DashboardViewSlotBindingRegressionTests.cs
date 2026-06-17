using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MiKiNuo.Mvi.Presentation.ViewRegistry;
using MiKiNuo.Mvi.Samples.Avalonia.Composition;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 回归测试：<c>DashboardView</c> 通过 <see cref="IMviViewRegistry"/> 创建后，
/// 其声明的 <c>[MviSlot]</c> 字段（Header / Menu / Page）必须被源生成器 emit 的
/// <c>OnBindSlots</c> 钩子填入对应子 View，否则左侧菜单 / 顶部头部 / 当前页面均为空。
/// <para>
/// 这正是用户截图"左侧菜单不显示"症状的根因复现：
/// <see cref="SampleGeneratedViewRegistry"/> 的 <c>CreateView</c> 调用的是 <c>view.Bind(viewModel)</c> 1-arg 重载，
/// 不会触发 <c>OnBindSlots</c>。
/// </para>
/// </summary>
public sealed class DashboardViewSlotBindingRegressionTests
{
    private static readonly HeadlessUnitTestSession Session = HeadlessUnitTestSession.StartNew(typeof(HeadlessTestApp));

    /// <summary>
    /// 验证 <c>DashboardView.MenuSlot</c> 在通过 <see cref="IMviViewRegistry"/> 走通 <c>CreateView</c> 后
    /// <c>Content</c> 不为 <c>null</c>，即菜单子 View 已被源生成器 emit 的 <c>OnBindSlots</c> 挂载上。
    /// </summary>
    [Test]
    public async Task DashboardView_MenuSlot_Should_BePopulatedAfterCreateViewAsync()
    {
        (object? menuContent, object? headerContent, object? pageContent) = await Session.Dispatch(
            () =>
            {
                SampleGeneratedContainer container = new();
                container.NavigateToDashboardAsync("测试用户").AsTask().GetAwaiter().GetResult();
                DashboardViewModel dashboardViewModel = container.Resolve<DashboardViewModel>();
                IMviViewRegistry viewRegistry = container.Resolve<IMviViewRegistry>();

                // 通过 ViewRegistry 走完整流程：构造 View + Bind —— 必须真正跑源生成器 emit 的 OnBindSlots。
                object viewObject = viewRegistry.CreateView(dashboardViewModel);
                DashboardView view = (DashboardView)viewObject;
                ContentControl menuSlot = view.FindControl<ContentControl>("MenuSlot")
                    ?? throw new InvalidOperationException("DashboardView 未声明 MenuSlot 节点。");
                ContentControl headerSlot = view.FindControl<ContentControl>("HeaderSlot")
                    ?? throw new InvalidOperationException("DashboardView 未声明 HeaderSlot 节点。");
                ContentControl pageSlot = view.FindControl<ContentControl>("PageSlot")
                    ?? throw new InvalidOperationException("DashboardView 未声明 PageSlot 节点。");

                return (menuSlot.Content, headerSlot.Content, pageSlot.Content);
            },
            CancellationToken.None);

        await Assert.That(menuContent).IsNotNull();
        await Assert.That(headerContent).IsNotNull();
        await Assert.That(pageContent).IsNotNull();
    }
}
