using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;

/// <summary>
/// 表示 Dashboard 壳意图。
/// <para>
/// <see cref="ShowPage"/> 不再携带具体页面 ViewModel（消除 VM-in-Intent 反模式），
/// 仅携带 PageKey 判别器 + 展示元数据；View 层通过 <see cref="IDashboardPageFactory"/> 解析具体页面 VM。
/// </para>
/// </summary>
public abstract partial record DashboardIntent : IMviIntent
{
    /// <summary>
    /// 表示显示业务页面的意图。
    /// </summary>
    /// <param name="PageKey">页面键（"门诊工作站"/"住院床位"/...），由 View 层 <see cref="IDashboardPageFactory"/> 解析为具体页面 ViewModel。</param>
    /// <param name="PageTitle">页面标题。</param>
    /// <param name="PageDescription">页面说明。</param>
    public sealed partial record ShowPage(
        string PageKey,
        string PageTitle,
        string PageDescription) : DashboardIntent;
}
