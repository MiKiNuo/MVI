namespace MiKiNuo.Mvi.Domain.MVI.Middleware;

/// <summary>
/// 表示 MVI 中间件注册特性，用于源生成器识别中间件顺序。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MviMiddlewareAttribute : Attribute
{
    /// <summary>
    /// 获取或设置中间件执行顺序。
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 获取或设置中间件所属功能键。
    /// </summary>
    public string FeatureKey { get; set; } = string.Empty;
}
