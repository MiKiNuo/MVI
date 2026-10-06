using MiKiNuo.Mvi.Abstractions.DI;
using MiKiNuo.Mvi.Binding.ViewModel;
namespace MiKiNuo.Mvi.Samples.Avalonia.Features.CompositionDemo;
/// <summary>检索绑定模型，仅使用框架的只读 State 和派发入口。</summary>
public sealed partial class MedicineSearchViewModel : MviViewModelBase<MedicineSearchState, MedicineSearchIntent> { }
/// <summary>明细绑定模型。</summary>
public sealed partial class MedicationDetailsViewModel : MviViewModelBase<MedicationDetailsState, MedicationDetailsIntent> { }
/// <summary>工作区绑定模型。</summary>
public sealed partial class PrescriptionWorkspaceViewModel : MviViewModelBase<PrescriptionWorkspaceState, PrescriptionWorkspaceIntent> { }
/// <summary>独立检索组合：相同 Feature 可以由不同宿主装配。</summary>
[MviComposition(typeof(MedicationDetailsHandler), typeof(MedicineSearchHandler))]
public sealed partial class MedicineSearchComposition { }
/// <summary>处方组合：范围内明细事实连接到父级摘要。</summary>
[MviComposition(typeof(PrescriptionWorkspaceHandler), typeof(MedicationDetailsHandler), typeof(MedicineSearchHandler))]
public sealed partial class PrescriptionComposition { }
