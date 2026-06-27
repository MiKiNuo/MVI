namespace MiKiNuo.Mvi.Domain.MVI.State;

/// <summary>
/// 表示无状态的空状态。
/// </summary>
public sealed partial record UnitState : IMviState
{
    /// <summary>获取空状态单例。</summary>
    public static UnitState Instance { get; } = new();
}
