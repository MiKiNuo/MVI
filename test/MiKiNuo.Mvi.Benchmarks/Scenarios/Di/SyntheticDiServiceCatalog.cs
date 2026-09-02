using MiKiNuo.Mvi.Application.DI;
using MiKiNuo.Mvi.Domain.DI;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Di;

/// <summary>
/// 表示 DI 深度扫描位置：对应生成容器 if-else 解析链中的相对深度。
/// </summary>
public enum DiScanPosition
{
    /// <summary>
    /// 链首：匹配目标生命周期的最早注册项，解析比较次数最少。
    /// </summary>
    First,

    /// <summary>
    /// 链中：最接近描述符列表中点的匹配项，代表典型解析深度。
    /// </summary>
    Middle,

    /// <summary>
    /// 链尾：匹配目标生命周期的最晚注册项，解析比较次数最多。
    /// </summary>
    Last
}

/// <summary>
/// 表示合成 DI 服务目录：为深度扫描基准提供描述符挑选规则，
/// 同时作为冒烟测试与基准方法共享的单一事实来源。
/// </summary>
public static class SyntheticDiServiceCatalog
{
    /// <summary>
    /// 获取合成服务总数：300 个，用于提供足够的解析链深度。
    /// </summary>
    public const int SyntheticServiceCount = 300;

    /// <summary>
    /// 判断描述符是否为合成服务。
    /// </summary>
    /// <param name="descriptor">服务描述符。</param>
    /// <returns>是合成服务返回 true。</returns>
    public static bool IsSyntheticService(MviServiceDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.ServiceType.Name.StartsWith("SyntheticService", StringComparison.Ordinal);
    }

    /// <summary>
    /// 按深度位置与生命周期挑选描述符。
    /// <para>
    /// 挑选基于描述符列表（即生成代码 if-else 链的真实顺序），
    /// 不依赖源码声明顺序：First 取匹配生命周期的最小下标，
    /// Last 取最大下标，Middle 取最接近列表中点者（并列取较小下标）。
    /// </para>
    /// </summary>
    /// <param name="descriptors">生成容器的服务描述符列表。</param>
    /// <param name="position">深度扫描位置。</param>
    /// <param name="lifetime">目标生命周期。</param>
    /// <returns>匹配的服务描述符。</returns>
    /// <exception cref="InvalidOperationException">列表中不存在匹配生命周期的服务时抛出。</exception>
    public static MviServiceDescriptor PickDescriptor(
        IReadOnlyList<MviServiceDescriptor> descriptors,
        DiScanPosition position,
        ServiceLifetime lifetime)
    {
        ArgumentNullException.ThrowIfNull(descriptors);

        int bestIndex = -1;
        int bestDistance = int.MaxValue;
        int middleTarget = descriptors.Count / 2;

        for (int index = 0; index < descriptors.Count; index++)
        {
            if (descriptors[index].Lifetime != lifetime)
            {
                continue;
            }

            int distance;
            switch (position)
            {
                case DiScanPosition.First:
                    distance = index;
                    break;
                case DiScanPosition.Last:
                    distance = descriptors.Count - 1 - index;
                    break;
                default:
                    distance = Math.Abs(index - middleTarget);
                    break;
            }

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = index;
            }
        }

        if (bestIndex < 0)
        {
            throw new InvalidOperationException(
                $"描述符列表中不存在生命周期为 {lifetime} 的服务，无法完成深度扫描挑选。");
        }

        return descriptors[bestIndex];
    }
}
