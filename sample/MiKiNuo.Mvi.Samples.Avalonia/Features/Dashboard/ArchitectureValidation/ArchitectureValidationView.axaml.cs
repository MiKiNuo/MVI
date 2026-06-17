using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Slot;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;
using MiKiNuo.Mvi.Presentation.Slot;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.AuditTimeline;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.PatientSearch;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ArchitectureValidation;

/// <summary>
/// 表示架构验证中心视图。
/// 6 个子组件 ViewModel 由 <see cref="ArchitectureValidationViewModel"/> 工厂方法按需解析，
/// 再由 ViewRegistry 创建具体 View，View 不再依赖父 VM 上的强类型子 VM 属性。
/// <para>
/// 6 个槽位通过 <c>[MviSlot]</c> 特性声明，
/// 由 <c>MviCompositeSlotBindingGenerator</c> 源生成器自动 emit <c>OnBindSlots</c> override。
/// </para>
/// </summary>
public sealed partial class ArchitectureValidationView : MviAvaloniaView<ArchitectureValidationViewModel>
{
    /// <summary>
    /// 患者检索槽位（由源生成器自动绑定）。
    /// </summary>
    [MviSlot(typeof(PatientSearchView), factory: nameof(ArchitectureValidationViewModel.CreatePatientSearchViewModel))]
    private MviSlotHost? _patientSearchSlot;

    /// <summary>
    /// 中间件指标卡片槽位（由源生成器自动绑定）。
    /// </summary>
    [MviSlot(typeof(CardView), factory: nameof(ArchitectureValidationViewModel.CreateMiddlewareMetricViewModel))]
    private MviSlotHost? _middlewareMetricSlot;

    /// <summary>
    /// 复用指标卡片槽位（由源生成器自动绑定）。
    /// </summary>
    [MviSlot(typeof(CardView), factory: nameof(ArchitectureValidationViewModel.CreateReuseMetricViewModel))]
    private MviSlotHost? _reuseMetricSlot;

    /// <summary>
    /// 中介者指标卡片槽位（由源生成器自动绑定）。
    /// </summary>
    [MviSlot(typeof(CardView), factory: nameof(ArchitectureValidationViewModel.CreateMediatorMetricViewModel))]
    private MviSlotHost? _mediatorMetricSlot;

    /// <summary>
    /// 副作用指标卡片槽位（由源生成器自动绑定）。
    /// </summary>
    [MviSlot(typeof(CardView), factory: nameof(ArchitectureValidationViewModel.CreateEffectMetricViewModel))]
    private MviSlotHost? _effectMetricSlot;

    /// <summary>
    /// 审计时间线槽位（由源生成器自动绑定）。
    /// </summary>
    [MviSlot(typeof(AuditTimelineView), factory: nameof(ArchitectureValidationViewModel.CreateAuditTimelineViewModel))]
    private MviSlotHost? _auditTimelineSlot;

    /// <summary>
    /// 初始化架构验证中心视图。
    /// </summary>
    public ArchitectureValidationView()
    {
        AvaloniaXamlLoader.Load(this);
        _patientSearchSlot = FindRequiredSlot("PatientSearchSlot");
        _middlewareMetricSlot = FindRequiredSlot("MiddlewareMetricSlot");
        _reuseMetricSlot = FindRequiredSlot("ReuseMetricSlot");
        _mediatorMetricSlot = FindRequiredSlot("MediatorMetricSlot");
        _effectMetricSlot = FindRequiredSlot("EffectMetricSlot");
        _auditTimelineSlot = FindRequiredSlot("AuditTimelineSlot");
    }

    private MviSlotHost FindRequiredSlot(string name)
    {
        return this.FindControl<MviSlotHost>(name)
            ?? throw new InvalidOperationException($"无法找到名为 {name} 的控件。");
    }
}
