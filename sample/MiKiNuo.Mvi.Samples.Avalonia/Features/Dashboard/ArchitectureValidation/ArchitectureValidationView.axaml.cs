using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Slot;
using MiKiNuo.Mvi.Presentation.ViewRegistry;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ArchitectureValidation;

/// <summary>
/// 表示架构验证中心视图。
/// 6 个子组件 ViewModel 由 <see cref="ArchitectureValidationViewModel"/> 工厂方法按需解析，
/// 再由 <see cref="IMviViewRegistry"/> 创建具体 View，View 不再依赖父 VM 上的强类型子 VM 属性。
/// </summary>
public sealed partial class ArchitectureValidationView : MviAvaloniaView<ArchitectureValidationViewModel>
{
    private readonly IMviViewRegistry _viewRegistry;
    private readonly MviSlotHost _patientSearchSlot;
    private readonly MviSlotHost _middlewareMetricSlot;
    private readonly MviSlotHost _reuseMetricSlot;
    private readonly MviSlotHost _mediatorMetricSlot;
    private readonly MviSlotHost _effectMetricSlot;
    private readonly MviSlotHost _auditTimelineSlot;

    /// <summary>
    /// 初始化架构验证中心视图。
    /// </summary>
    /// <param name="viewRegistry">视图注册表。</param>
    public ArchitectureValidationView(IMviViewRegistry viewRegistry)
    {
        ArgumentNullException.ThrowIfNull(viewRegistry);

        _viewRegistry = viewRegistry;
        AvaloniaXamlLoader.Load(this);
        _patientSearchSlot = FindRequiredSlot("PatientSearchSlot");
        _middlewareMetricSlot = FindRequiredSlot("MiddlewareMetricSlot");
        _reuseMetricSlot = FindRequiredSlot("ReuseMetricSlot");
        _mediatorMetricSlot = FindRequiredSlot("MediatorMetricSlot");
        _effectMetricSlot = FindRequiredSlot("EffectMetricSlot");
        _auditTimelineSlot = FindRequiredSlot("AuditTimelineSlot");
    }

    /// <inheritdoc />
    public new void Bind(ArchitectureValidationViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        base.Bind(viewModel);
        _patientSearchSlot.Content = _viewRegistry.CreateView(viewModel.CreatePatientSearchViewModel(viewModel.ActiveContext));
        _middlewareMetricSlot.Content = _viewRegistry.CreateView(viewModel.CreateMiddlewareMetricViewModel());
        _reuseMetricSlot.Content = _viewRegistry.CreateView(viewModel.CreateReuseMetricViewModel());
        _mediatorMetricSlot.Content = _viewRegistry.CreateView(viewModel.CreateMediatorMetricViewModel());
        _effectMetricSlot.Content = _viewRegistry.CreateView(viewModel.CreateEffectMetricViewModel());
        _auditTimelineSlot.Content = _viewRegistry.CreateView(viewModel.CreateAuditTimelineViewModel(viewModel.ActiveContext));
    }

    private MviSlotHost FindRequiredSlot(string name)
    {
        return this.FindControl<MviSlotHost>(name)
            ?? throw new InvalidOperationException($"无法找到名为 {name} 的控件。");
    }
}
