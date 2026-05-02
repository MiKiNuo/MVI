namespace MiKiNuo.Mvi.Domain.MVI.Binding;

/// <summary>
/// 表示 MVI 绑定模式。
/// </summary>
public enum MviBindingMode
{
    /// <summary>
    /// 单向绑定，由状态流推送到 ViewModel。
    /// </summary>
    OneWay,

    /// <summary>
    /// 双向绑定，由 UI setter 生成 Intent，再通过状态流回推。
    /// </summary>
    TwoWay
}
