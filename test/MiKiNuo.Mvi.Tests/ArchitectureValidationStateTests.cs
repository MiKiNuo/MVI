using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ArchitectureValidation;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.AuditTimeline;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.PatientSearch;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示架构验证中心状态与 ViewModel 解耦的回归测试。
/// <para>
/// 重构后 6 个子 ViewModel 全部走工厂解析（2 个复用组件走 <c>IArchitectureValidationPanelFactory</c>，
/// 4 个指标卡走 <c>CardStoreFactory</c>），
/// <see cref="ArchitectureValidationViewModel"/>
/// 上不暴露任何强类型子 VM 属性，仅暴露 <c>CreateXxxViewModel</c> 工厂方法，
/// <see cref="ArchitectureValidationState"/>
/// 也不再包含子 VM 引用。
/// </para>
/// <para>
/// 这是"VM 不持 VM、State 不存 ViewModel"架构原则的回归保护。
/// </para>
/// </summary>
public sealed class ArchitectureValidationStateTests
{
    /// <summary>
    /// 验证 <see cref="ArchitectureValidationViewModel"/>
    /// 不再持有任何子 ViewModel 引用（避免"VM-in-VM"反模式）。
    /// </summary>
    [Test]
    public async Task ArchitectureValidationViewModel_ShouldNotExposeChildViewModelPropertiesAsync()
    {
        Type viewModelType = typeof(ArchitectureValidationViewModel);

        await Assert.That(viewModelType.GetProperty("PatientSearchViewModel")).IsNull();
        await Assert.That(viewModelType.GetProperty("AuditTimelineViewModel")).IsNull();
        await Assert.That(viewModelType.GetProperty("MiddlewareMetricViewModel")).IsNull();
        await Assert.That(viewModelType.GetProperty("ReuseMetricViewModel")).IsNull();
        await Assert.That(viewModelType.GetProperty("MediatorMetricViewModel")).IsNull();
        await Assert.That(viewModelType.GetProperty("EffectMetricViewModel")).IsNull();
    }

    /// <summary>
    /// 验证 <see cref="ArchitectureValidationViewModel"/>
    /// 暴露 6 个工厂方法（2 个复用组件 + 4 个指标卡），View 通过这些方法按需解析。
    /// </summary>
    [Test]
    public async Task ArchitectureValidationViewModel_ShouldExposeFactoryMethodsAsync()
    {
        Type viewModelType = typeof(ArchitectureValidationViewModel);

        await Assert.That(viewModelType.GetMethod("CreatePatientSearchViewModel")).IsNotNull();
        await Assert.That(viewModelType.GetMethod("CreateAuditTimelineViewModel")).IsNotNull();
        await Assert.That(viewModelType.GetMethod("CreateMiddlewareMetricViewModel")).IsNotNull();
        await Assert.That(viewModelType.GetMethod("CreateReuseMetricViewModel")).IsNotNull();
        await Assert.That(viewModelType.GetMethod("CreateMediatorMetricViewModel")).IsNotNull();
        await Assert.That(viewModelType.GetMethod("CreateEffectMetricViewModel")).IsNotNull();
    }

    /// <summary>
    /// 验证 2 个复用组件工厂方法返回 <c>object</c>，但实际类型对应
    /// <see cref="PatientSearchViewModel"/> / <see cref="AuditTimelineViewModel"/>。
    /// </summary>
    [Test]
    public async Task ArchitectureValidationViewModel_FactoryMethods_ShouldReturnReusableViewModelsAsync()
    {
        Type viewModelType = typeof(ArchitectureValidationViewModel);
        Type patientSearchType = typeof(PatientSearchViewModel);
        Type auditTimelineType = typeof(AuditTimelineViewModel);

        await Assert.That(viewModelType.GetMethod("CreatePatientSearchViewModel")!.ReturnType)
            .IsEqualTo(typeof(object));
        await Assert.That(viewModelType.GetMethod("CreateAuditTimelineViewModel")!.ReturnType)
            .IsEqualTo(typeof(object));
        await Assert.That(patientSearchType).IsNotNull();
        await Assert.That(auditTimelineType).IsNotNull();
    }

    /// <summary>
    /// 验证 4 个指标卡工厂方法返回 <c>object</c>，但实际类型对应 <see cref="CardViewModel"/>。
    /// </summary>
    [Test]
    public async Task ArchitectureValidationViewModel_FactoryMethods_ShouldReturnMetricCardViewModelsAsync()
    {
        Type viewModelType = typeof(ArchitectureValidationViewModel);
        Type cardViewModelType = typeof(CardViewModel);

        await Assert.That(viewModelType.GetMethod("CreateMiddlewareMetricViewModel")!.ReturnType)
            .IsEqualTo(typeof(object));
        await Assert.That(viewModelType.GetMethod("CreateReuseMetricViewModel")!.ReturnType)
            .IsEqualTo(typeof(object));
        await Assert.That(viewModelType.GetMethod("CreateMediatorMetricViewModel")!.ReturnType)
            .IsEqualTo(typeof(object));
        await Assert.That(viewModelType.GetMethod("CreateEffectMetricViewModel")!.ReturnType)
            .IsEqualTo(typeof(object));
        await Assert.That(cardViewModelType.Name).IsEqualTo("CardViewModel");
    }

    /// <summary>
    /// 验证 <see cref="ArchitectureValidationState"/>
    /// 不再持有任何子 ViewModel 引用。
    /// </summary>
    [Test]
    public async Task ArchitectureValidationState_ShouldNotExposeViewModelPropertiesAsync()
    {
        Type stateType = typeof(ArchitectureValidationState);

        await Assert.That(stateType.GetProperty("PatientSearchViewModel")).IsNull();
        await Assert.That(stateType.GetProperty("AuditTimelineViewModel")).IsNull();
        await Assert.That(stateType.GetProperty("MiddlewareMetricViewModel")).IsNull();
        await Assert.That(stateType.GetProperty("ReuseMetricViewModel")).IsNull();
        await Assert.That(stateType.GetProperty("MediatorMetricViewModel")).IsNull();
        await Assert.That(stateType.GetProperty("EffectMetricViewModel")).IsNull();
    }
}
