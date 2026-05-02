namespace MiKiNuo.Mvi.Application.MVI.Component;

/// <summary>
/// 表示 MVI 组件编号。
/// </summary>
/// <param name="Value">组件编号值。</param>
public readonly record struct MviComponentId(string Value)
{
    /// <inheritdoc />
    public override string ToString()
    {
        return Value;
    }
}
