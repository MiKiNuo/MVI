using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Slot;
using MiKiNuo.Mvi.Presentation.ViewRegistry;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ArchitectureValidation;

/// <summary>
/// 表示架构验证中心视图。
/// </summary>
public sealed partial class ArchitectureValidationView : MviAvaloniaView<ArchitectureValidationViewModel>
{
    private readonly IMviViewRegistry _viewRegistry;

    /// <summary>
    /// 初始化架构验证中心视图。
    /// </summary>
    /// <param name="viewRegistry">视图注册表。</param>
    public ArchitectureValidationView(IMviViewRegistry viewRegistry)
    {
        ArgumentNullException.ThrowIfNull(viewRegistry);

        _viewRegistry = viewRegistry;
        AvaloniaXamlLoader.Load(this);
    }

    /// <inheritdoc />
    public new void Bind(ArchitectureValidationViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        base.Bind(viewModel);
        SetSlotContent("PatientSearchSlot", viewModel.PatientSearchViewModel);
        SetSlotContent("AuditTimelineSlot", viewModel.AuditTimelineViewModel);
        SetSlotContent("MiddlewareMetricSlot", viewModel.MiddlewareMetricViewModel);
        SetSlotContent("ReuseMetricSlot", viewModel.ReuseMetricViewModel);
        SetSlotContent("MediatorMetricSlot", viewModel.MediatorMetricViewModel);
        SetSlotContent("EffectMetricSlot", viewModel.EffectMetricViewModel);
    }

    private void SetSlotContent(string slotName, object viewModel)
    {
        MviSlotHost slot = FindRequired<MviSlotHost>(slotName);
        slot.Content = _viewRegistry.CreateView(viewModel);
    }

    private TControl FindRequired<TControl>(string name)
        where TControl : Control
    {
        return this.FindControl<TControl>(name)
            ?? throw new InvalidOperationException($"无法找到名为 {name} 的控件。");
    }
}
