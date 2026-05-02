namespace MiKiNuo.Mvi.Domain.MVI.Feature;

/// <summary>
/// 表示 MVI 功能模块注册特性，用于源生成器识别完整的 MVI Feature。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MviFeatureModuleAttribute : Attribute
{
    /// <summary>
    /// 初始化 MVI 功能模块注册特性。
    /// </summary>
    /// <param name="featureKey">功能模块键。</param>
    public MviFeatureModuleAttribute(string featureKey)
    {
        FeatureKey = string.IsNullOrWhiteSpace(featureKey)
            ? throw new ArgumentException("功能模块键不能为空。", nameof(featureKey))
            : featureKey;
    }

    /// <summary>
    /// 获取功能模块键。
    /// </summary>
    public string FeatureKey { get; }

    /// <summary>
    /// 获取或设置功能模块显示名称。
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
}
