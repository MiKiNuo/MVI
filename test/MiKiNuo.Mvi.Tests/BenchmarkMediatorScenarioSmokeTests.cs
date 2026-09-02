using MiKiNuo.Mvi.Benchmarks.Scenarios.Mediator;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示基准项目中介者场景的冒烟测试：
/// 验证路由表在 1 条与 300 条规模下都能正确路由，且直连基线可用。
/// </summary>
public sealed class BenchmarkMediatorScenarioSmokeTests
{
    /// <summary>
    /// 验证仅注册目标路由时 SendAsync 正确命中并返回响应。
    /// </summary>
    [Test]
    public async Task MediatorScenario_WithSingleRoute_Should_SendToTargetAsync()
    {
        MediatorScenario scenario = new(1);

        MediatorResponse response = await scenario.SendAsync();

        await Assert.That(response.Value).IsEqualTo(SyntheticMediatorRouteCatalog.SyntheticRouteCount - 1);
        await Assert.That(scenario.RouteCount).IsEqualTo(1);
    }

    /// <summary>
    /// 验证注册全部 300 条路由后目标请求（链尾）仍被精确路由。
    /// </summary>
    [Test]
    public async Task MediatorScenario_WithAllRoutes_Should_RouteTargetRequestAsync()
    {
        MediatorScenario scenario = new(SyntheticMediatorRouteCatalog.SyntheticRouteCount);

        MediatorResponse response = await scenario.SendAsync();

        await Assert.That(response.Value).IsEqualTo(SyntheticMediatorRouteCatalog.SyntheticRouteCount - 1);
        await Assert.That(scenario.RouteCount).IsEqualTo(SyntheticMediatorRouteCatalog.SyntheticRouteCount);
    }

    /// <summary>
    /// 验证直连基线绕过中介者直接调用处理器并返回相同结果。
    /// </summary>
    [Test]
    public async Task MediatorScenario_DirectHandler_Should_BypassMediatorAsync()
    {
        MediatorScenario scenario = new(1);

        MediatorResponse response = await scenario.SendDirectAsync();

        await Assert.That(response.Value).IsEqualTo(SyntheticMediatorRouteCatalog.SyntheticRouteCount - 1);
    }

    /// <summary>
    /// 验证不支持的路由规模被构造函数拒绝。
    /// </summary>
    [Test]
    public async Task MediatorScenario_InvalidRouteCount_Should_BeRejectedAsync()
    {
        await Assert.That(() => new MediatorScenario(7)).Throws<ArgumentOutOfRangeException>();
    }
}
