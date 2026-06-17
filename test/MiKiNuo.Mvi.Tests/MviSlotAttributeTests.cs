using System.Reflection;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 <see cref="MiKiNuo.Mvi.Presentation.Slot.MviSlotAttribute"/> 的单元测试。
/// </summary>
public sealed class MviSlotAttributeTests
{
    /// <summary>
    /// 验证特性构造时传入的子 View 类型与观察属性集合能够被原样读出。
    /// </summary>
    [Test]
    public async Task Constructor_Should_StoreChildViewTypeAndObservesAsync()
    {
        Type childViewType = typeof(SampleMarkerView);
        string[] observes = new[] { "DisplayName", "CurrentPageKey" };
        MiKiNuo.Mvi.Presentation.Slot.MviSlotAttribute attribute =
            new(childViewType, observes);

        await Assert.That(attribute.ChildViewType).IsEqualTo(childViewType);
        await Assert.That(attribute.Observes).IsEquivalentTo(observes);
    }

    /// <summary>
    /// 验证不传 <c>Observes</c> 时默认不观察任何属性（一次性绑定）。
    /// </summary>
    [Test]
    public async Task Constructor_Should_DefaultObservesToEmptyArrayAsync()
    {
        MiKiNuo.Mvi.Presentation.Slot.MviSlotAttribute attribute =
            new(typeof(SampleMarkerView));

        await Assert.That(attribute.Observes).IsEmpty();
    }

    /// <summary>
    /// 验证特性只能标在字段上、且 <c>AllowMultiple = false</c>、<c>Inherited = false</c>。
    /// </summary>
    [Test]
    public async Task AttributeUsage_Should_TargetFieldOnlyOnceAndNotInheritedAsync()
    {
        AttributeUsageAttribute usage = typeof(MiKiNuo.Mvi.Presentation.Slot.MviSlotAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>()!;

        await Assert.That(usage.ValidOn).IsEqualTo(AttributeTargets.Field);
        await Assert.That(usage.AllowMultiple).IsFalse();
        await Assert.That(usage.Inherited).IsFalse();
    }

    /// <summary>
    /// 验证构造函数对 <c>null</c> View 类型入参会抛出 <see cref="ArgumentNullException"/>。
    /// </summary>
    [Test]
    public async Task Constructor_Should_ThrowOnNullChildViewTypeAsync()
    {
        Action act = () => _ = new MiKiNuo.Mvi.Presentation.Slot.MviSlotAttribute(null!);

        await Assert.That(act).Throws<ArgumentNullException>();
    }

    /// <summary>
    /// 验证 <c>Observes</c> 数组中包含空字符串或重复项时会被规范化（去重并剔除空值）。
    /// </summary>
    [Test]
    public async Task Constructor_Should_DeduplicateAndStripEmptyObservesAsync()
    {
        string[] observes = new[] { "DisplayName", "", "DisplayName", "CurrentPageKey" };
        MiKiNuo.Mvi.Presentation.Slot.MviSlotAttribute attribute =
            new(typeof(SampleMarkerView), observes);

        await Assert.That(attribute.Observes).IsEquivalentTo(new[] { "DisplayName", "CurrentPageKey" });
    }

    /// <summary>
    /// 表示仅用于类型反射的标记型子 View。
    /// </summary>
    private sealed class SampleMarkerView
    {
    }
}
