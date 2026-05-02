namespace MiKiNuo.Mvi.Infrastructure.DI;

/// <summary>
/// 表示 DI 服务未找到异常。
/// </summary>
public sealed class DiServiceNotFoundException : Exception
{
    /// <summary>
    /// 初始化 DI 服务未找到异常。
    /// </summary>
    /// <param name="serviceType">服务类型。</param>
    public DiServiceNotFoundException(Type serviceType)
        : base(CreateMessage(serviceType))
    {
        ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
    }

    /// <summary>
    /// 获取服务类型。
    /// </summary>
    public Type ServiceType { get; }

    private static string CreateMessage(Type serviceType)
    {
        if (serviceType is null)
        {
            throw new ArgumentNullException(nameof(serviceType));
        }

        return $"未找到服务：{serviceType.FullName}";
    }
}
