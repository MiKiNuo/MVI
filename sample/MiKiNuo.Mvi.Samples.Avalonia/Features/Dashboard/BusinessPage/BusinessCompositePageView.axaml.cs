using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Presentation.Platforms.Avalonia.Slot;
using MiKiNuo.Mvi.Presentation.Platforms.Avalonia.ViewRegistry;
using MiKiNuo.Mvi.Presentation.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.BusinessPage;

/// <summary>
/// 表示生产业务组合页面视图。
/// </summary>
public sealed partial class BusinessCompositePageView : MviAvaloniaView<BusinessCompositePageViewModel>
{
    private readonly IMviViewRegistry _viewRegistry;
    private readonly Grid _inpatientLayout;
    private readonly Grid _labLayout;
    private readonly Grid _pharmacyLayout;
    private readonly Grid _qualityLayout;

    /// <summary>
    /// 初始化生产业务组合页面视图。
    /// </summary>
    /// <param name="viewRegistry">视图注册表。</param>
    public BusinessCompositePageView(IMviViewRegistry viewRegistry)
    {
        ArgumentNullException.ThrowIfNull(viewRegistry);

        _viewRegistry = viewRegistry;
        AvaloniaXamlLoader.Load(this);
        _inpatientLayout = FindRequired<Grid>("InpatientLayout");
        _labLayout = FindRequired<Grid>("LabLayout");
        _pharmacyLayout = FindRequired<Grid>("PharmacyLayout");
        _qualityLayout = FindRequired<Grid>("QualityLayout");
    }

    /// <inheritdoc />
    public new void Bind(BusinessCompositePageViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        base.Bind(viewModel);
        RenderBusinessLayout(viewModel);
    }

    private void RenderBusinessLayout(BusinessCompositePageViewModel viewModel)
    {
        HideAllLayouts();

        switch (viewModel.PageLayout)
        {
            case "Inpatient":
                RenderInpatientLayout(viewModel);
                break;
            case "Lab":
                RenderLabLayout(viewModel);
                break;
            case "Pharmacy":
                RenderPharmacyLayout(viewModel);
                break;
            case "Quality":
                RenderQualityLayout(viewModel);
                break;
            default:
                RenderInpatientLayout(viewModel);
                break;
        }
    }

    private void HideAllLayouts()
    {
        _inpatientLayout.IsVisible = false;
        _labLayout.IsVisible = false;
        _pharmacyLayout.IsVisible = false;
        _qualityLayout.IsVisible = false;
    }

    private void RenderInpatientLayout(BusinessCompositePageViewModel viewModel)
    {
        _inpatientLayout.IsVisible = true;
        SetSlotContent("InpatientBedOverviewSlot", viewModel.PrimaryPanelViewModel);
        SetSlotContent("InpatientAdmissionSlot", viewModel.SecondaryPanelViewModel);
        SetSlotContent("InpatientNursingSlot", viewModel.TertiaryPanelViewModel);
        SetSlotContent("InpatientRiskSlot", viewModel.QuaternaryPanelViewModel);
    }

    private void RenderLabLayout(BusinessCompositePageViewModel viewModel)
    {
        _labLayout.IsVisible = true;
        SetSlotContent("LabOrderSlot", viewModel.PrimaryPanelViewModel);
        SetSlotContent("LabSpecimenSlot", viewModel.SecondaryPanelViewModel);
        SetSlotContent("LabCriticalSlot", viewModel.TertiaryPanelViewModel);
        SetSlotContent("LabTatSlot", viewModel.QuaternaryPanelViewModel);
    }

    private void RenderPharmacyLayout(BusinessCompositePageViewModel viewModel)
    {
        _pharmacyLayout.IsVisible = true;
        SetSlotContent("PharmacyPrescriptionSlot", viewModel.PrimaryPanelViewModel);
        SetSlotContent("PharmacyStockSlot", viewModel.SecondaryPanelViewModel);
        SetSlotContent("PharmacyReplenishmentSlot", viewModel.TertiaryPanelViewModel);
        SetSlotContent("PharmacySafetySlot", viewModel.QuaternaryPanelViewModel);
    }

    private void RenderQualityLayout(BusinessCompositePageViewModel viewModel)
    {
        _qualityLayout.IsVisible = true;
        SetSlotContent("QualityKpiSlot", viewModel.PrimaryPanelViewModel);
        SetSlotContent("QualityAuditSlot", viewModel.SecondaryPanelViewModel);
        SetSlotContent("QualityRiskSlot", viewModel.TertiaryPanelViewModel);
        SetSlotContent("QualityRectificationSlot", viewModel.QuaternaryPanelViewModel);
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
