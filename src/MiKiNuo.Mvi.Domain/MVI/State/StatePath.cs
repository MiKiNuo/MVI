namespace MiKiNuo.Mvi.Domain.MVI.State;

/// <summary>
/// 表示从 MVI 状态根到目标值的强类型、无反射访问路径。
/// </summary>
/// <remarks>
/// 借鉴 ProMvvm 的 PropertyPath 设计，但面向不可变状态快照：
/// 状态每次整体替换，因此不需要属性名匹配与嵌套重订阅，
/// 仅保留强类型 Getter 与用于诊断、录制与 DevTools 的显示路径。
/// 路径实例通常由源生成器产出（如 DashboardStatePaths.Machine.Speed）。
/// </remarks>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TValue">路径终点值类型。</typeparam>
public readonly struct StatePath<TState, TValue>
    where TState : IMviState
{
    private readonly Func<TState, TValue>? _getter;

    /// <summary>
    /// 初始化状态访问路径。
    /// </summary>
    /// <param name="displayPath">用于诊断与录制的显示路径，例如 "Machine.Speed"。</param>
    /// <param name="getter">强类型取值委托。</param>
    public StatePath(string displayPath, Func<TState, TValue> getter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayPath);
        ArgumentNullException.ThrowIfNull(getter);

        DisplayPath = displayPath;
        _getter = getter;
    }

    /// <summary>
    /// 获取用于诊断与录制的显示路径。
    /// </summary>
    public string DisplayPath { get; }

    /// <summary>
    /// 获取强类型取值委托。
    /// </summary>
    /// <exception cref="InvalidOperationException">路径为默认结构体实例时抛出。</exception>
    public Func<TState, TValue> Getter =>
        _getter ?? throw new InvalidOperationException("StatePath 为默认实例，未初始化取值委托。");

    /// <summary>
    /// 从给定状态快照中取出路径终点值。
    /// </summary>
    /// <param name="state">状态快照。</param>
    /// <returns>路径终点值。</returns>
    public TValue GetValue(TState state) => Getter(state);

    /// <summary>
    /// 创建状态访问路径。
    /// </summary>
    /// <param name="displayPath">用于诊断与录制的显示路径。</param>
    /// <param name="getter">强类型取值委托。</param>
    /// <returns>状态访问路径。</returns>
    public static StatePath<TState, TValue> Create(string displayPath, Func<TState, TValue> getter) =>
        new(displayPath, getter);
}
