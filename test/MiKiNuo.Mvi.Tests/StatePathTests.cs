using MiKiNuo.Mvi.Domain.MVI.State;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 <see cref="StatePath{TState, TValue}"/> 的单元测试。
/// </summary>
public sealed class StatePathTests
{
    /// <summary>
    /// 验证 Create 创建的路径可通过 GetValue 取出状态中的值。
    /// </summary>
    [Test]
    public async Task GetValue_Should_ReturnPathValueAsync()
    {
        StatePath<PathSampleState, int> path = StatePath<PathSampleState, int>.Create(
            "Value",
            static state => state.Value);

        int value = path.GetValue(new PathSampleState(42));

        await Assert.That(value).IsEqualTo(42);
        await Assert.That(path.DisplayPath).IsEqualTo("Value");
    }

    /// <summary>
    /// 验证默认结构体实例访问 Getter 时抛出异常。
    /// </summary>
    [Test]
    public async Task Getter_Should_Throw_WhenDefaultInstanceAsync()
    {
        StatePath<PathSampleState, int> path = default;

        await Assert.That(() => path.Getter).Throws<InvalidOperationException>();
    }

    /// <summary>
    /// 验证构造参数为非法值时抛出异常。
    /// </summary>
    [Test]
    public async Task Constructor_Should_Throw_WhenArgumentsInvalidAsync()
    {
        await Assert.That(() => new StatePath<PathSampleState, int>(null!, static state => state.Value))
            .Throws<ArgumentNullException>();
        await Assert.That(() => new StatePath<PathSampleState, int>("Value", null!))
            .Throws<ArgumentNullException>();
    }
}

/// <summary>
/// 表示 StatePath 测试用示例状态。
/// </summary>
/// <param name="Value">示例值。</param>
public sealed record PathSampleState(int Value) : IMviState;
