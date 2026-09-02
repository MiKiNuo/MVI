using BenchmarkDotNet.Attributes;
using MiKiNuo.Mvi.Benchmarks.Scenarios.Mediator;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mediator;

/// <summary>
/// 表示中介者路由规模扫描基准：
/// 对照"直连处理委托"（基线）与"经 MviMediator.SendAsync 发送链尾目标请求"，
/// 路由表规模扫描（1 条 vs 300 条）验证哈希查表不随表大小退化——
/// 与 DI 深度扫描的 if-else 线性链形成"表 vs 链"对照。
/// </summary>
[MemoryDiagnoser]
public class MediatorSendBenchmarks
{
    private MediatorScenario _scenario = null!;

    /// <summary>
    /// 获取或设置路由表规模。
    /// </summary>
    [Params(1, SyntheticMediatorRouteCatalog.SyntheticRouteCount)]
    public int RouteCount { get; set; }

    /// <summary>
    /// 按路由规模构建中介者场景。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _scenario = new MediatorScenario(RouteCount);
    }

    /// <summary>
    /// 直连处理委托：不经中介者的物理上限基线。
    /// </summary>
    /// <returns>表示直连调用过程的任务。</returns>
    [Benchmark(Baseline = true)]
    public async Task DirectHandlerSendAsync()
    {
        await _scenario.SendDirectAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// 经中介者发送：null 检查 + 类型查找 + 响应类型校验 + 包装委托调用。
    /// </summary>
    /// <returns>表示中介者发送过程的任务。</returns>
    [Benchmark]
    public async Task MediatorSendAsync()
    {
        await _scenario.SendAsync().ConfigureAwait(false);
    }
}
