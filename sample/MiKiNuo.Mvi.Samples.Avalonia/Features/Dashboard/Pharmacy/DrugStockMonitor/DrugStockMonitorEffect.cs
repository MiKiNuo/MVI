using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Pharmacy.DrugStockMonitor;

/// <summary>
/// 表示库存监控 MVI副作用。
/// </summary>
public abstract partial record DrugStockMonitorEffect : IMviEffect
{
    /// <summary>
    /// 表示请求主业务工作流副作用。
    /// </summary>
    /// <param name="ContextText">业务上下文。</param>
    public sealed partial record RequestPrimaryWorkflow(string ContextText) : DrugStockMonitorEffect;

    /// <summary>
    /// 表示请求辅助业务工作流副作用。
    /// </summary>
    /// <param name="ContextText">业务上下文。</param>
    public sealed partial record RequestSecondaryWorkflow(string ContextText) : DrugStockMonitorEffect;
}
