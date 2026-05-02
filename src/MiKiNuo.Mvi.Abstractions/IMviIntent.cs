namespace MiKiNuo.Mvi.Abstractions;

/// <summary>
/// 表示 MVI 意图标记接口。
/// </summary>
public interface IMviIntent
{
    /// <summary>
    /// 获取由源生成器分配的意图编号。
    /// </summary>
    int Kind { get; }
}
