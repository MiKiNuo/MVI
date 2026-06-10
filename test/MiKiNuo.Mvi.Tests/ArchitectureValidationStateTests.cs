using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ArchitectureValidation;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.AuditTimeline;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.PatientSearch;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示架构验证中心状态强类型化的回归测试。
/// 避免再次退化为 <c>object</c> 字段，丢失 ViewModel 静态类型。
/// <para>
/// 重构后 6 个子 ViewModel 仅暴露在 <see cref="ArchitectureValidationViewModel"/> 上（构造函数注入），
/// <see cref="ArchitectureValidationState"/> 仅保留 3 个派生字段，不再包含子 VM 引用。
/// </para>
/// </summary>
public sealed class ArchitectureValidationStateTests
{
    /// <summary>
    /// 验证 <see cref="ArchitectureValidationViewModel"/> 的 6 个子 ViewModel 暴露为强类型属性，
    /// 不再使用 <c>object</c>。
    /// </summary>
    [Test]
    public async Task ArchitectureValidationViewModel_ShouldExposeStronglyTypedViewModelPropertiesAsync()
    {
        Type viewModelType = typeof(ArchitectureValidationViewModel);

        await Assert.That(await GetPropertyTypeAsync(viewModelType, nameof(ArchitectureValidationViewModel.PatientSearchViewModel)))
            .IsEqualTo(typeof(PatientSearchViewModel));
        await Assert.That(await GetPropertyTypeAsync(viewModelType, nameof(ArchitectureValidationViewModel.AuditTimelineViewModel)))
            .IsEqualTo(typeof(AuditTimelineViewModel));
        await Assert.That(await GetPropertyTypeAsync(viewModelType, nameof(ArchitectureValidationViewModel.MiddlewareMetricViewModel)))
            .IsEqualTo(typeof(CardViewModel));
        await Assert.That(await GetPropertyTypeAsync(viewModelType, nameof(ArchitectureValidationViewModel.ReuseMetricViewModel)))
            .IsEqualTo(typeof(CardViewModel));
        await Assert.That(await GetPropertyTypeAsync(viewModelType, nameof(ArchitectureValidationViewModel.MediatorMetricViewModel)))
            .IsEqualTo(typeof(CardViewModel));
        await Assert.That(await GetPropertyTypeAsync(viewModelType, nameof(ArchitectureValidationViewModel.EffectMetricViewModel)))
            .IsEqualTo(typeof(CardViewModel));
    }

    /// <summary>
    /// 验证 <see cref="ArchitectureValidationState"/> 不再持有任何子 ViewModel 引用。
    /// 这是"State 不存 ViewModel"架构原则的回归保护。
    /// </summary>
    [Test]
    public async Task ArchitectureValidationState_ShouldNotExposeViewModelPropertiesAsync()
    {
        Type stateType = typeof(ArchitectureValidationState);

        await Assert.That(stateType.GetProperty(nameof(ArchitectureValidationViewModel.PatientSearchViewModel))).IsNull();
        await Assert.That(stateType.GetProperty(nameof(ArchitectureValidationViewModel.AuditTimelineViewModel))).IsNull();
        await Assert.That(stateType.GetProperty(nameof(ArchitectureValidationViewModel.MiddlewareMetricViewModel))).IsNull();
        await Assert.That(stateType.GetProperty(nameof(ArchitectureValidationViewModel.ReuseMetricViewModel))).IsNull();
        await Assert.That(stateType.GetProperty(nameof(ArchitectureValidationViewModel.MediatorMetricViewModel))).IsNull();
        await Assert.That(stateType.GetProperty(nameof(ArchitectureValidationViewModel.EffectMetricViewModel))).IsNull();
    }

    private static async Task<Type> GetPropertyTypeAsync(Type owner, string propertyName)
    {
        await Task.Yield();
        return owner.GetProperty(propertyName)!.PropertyType;
    }
}
