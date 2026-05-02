namespace MiKiNuo.Mvi.Core.Mediator;

/// <summary>
/// 表示 MVI 组件地址。
/// </summary>
/// <param name="Value">地址值。</param>
public readonly record struct ComponentAddress(string Value)
{
    /// <summary>
    /// 获取根组件地址。
    /// </summary>
    public static ComponentAddress Root { get; } = new("root");

    /// <inheritdoc />
    public override string ToString()
    {
        return Value;
    }
}
