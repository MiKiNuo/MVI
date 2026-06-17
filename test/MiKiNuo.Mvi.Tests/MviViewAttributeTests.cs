using System.Reflection;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 <see cref="MiKiNuo.Mvi.Presentation.ViewRegistry.MviViewAttribute"/> 的单元测试。
/// </summary>
public sealed class MviViewAttributeTests
{
    /// <summary>
    /// 验证特性构造时传入的 ViewModel 类型能够被原样读出。
    /// </summary>
    [Test]
    public async Task Constructor_Should_StoreViewModelTypeAsync()
    {
        Type viewModelType = typeof(SampleMarkerViewModel);
        Attribute attribute = new MiKiNuo.Mvi.Presentation.ViewRegistry.MviViewAttribute(viewModelType);

        await Assert.That(attribute).IsTypeOf<MiKiNuo.Mvi.Presentation.ViewRegistry.MviViewAttribute>();
        await Assert.That(((MiKiNuo.Mvi.Presentation.ViewRegistry.MviViewAttribute)attribute).ViewModelType).IsEqualTo(viewModelType);
    }

    /// <summary>
    /// 验证特性只能打在类上、且 <c>AllowMultiple = false</c>、<c>Inherited = false</c>。
    /// </summary>
    [Test]
    public async Task AttributeUsage_Should_TargetClassOnlyOnceAndNotInheritedAsync()
    {
        AttributeUsageAttribute usage = typeof(MiKiNuo.Mvi.Presentation.ViewRegistry.MviViewAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>()!;

        await Assert.That(usage.ValidOn).IsEqualTo(AttributeTargets.Class);
        await Assert.That(usage.AllowMultiple).IsFalse();
        await Assert.That(usage.Inherited).IsFalse();
    }

    /// <summary>
    /// 验证特性构造函数对 <c>null</c> 入参会抛出 <see cref="ArgumentNullException"/>。
    /// </summary>
    [Test]
    public async Task Constructor_Should_ThrowOnNullViewModelTypeAsync()
    {
        Action act = () => _ = new MiKiNuo.Mvi.Presentation.ViewRegistry.MviViewAttribute(null!);

        await Assert.That(act).Throws<ArgumentNullException>();
    }

    /// <summary>
    /// 表示仅用于类型反射的标记型 ViewModel。
    /// </summary>
    private sealed class SampleMarkerViewModel
    {
    }
}
