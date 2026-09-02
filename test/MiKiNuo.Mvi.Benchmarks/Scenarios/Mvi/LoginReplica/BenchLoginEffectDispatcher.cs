using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.LoginReplica;

/// <summary>
/// 表示登录复刻基准场景副作用分发器：执行假认证并回流结果意图，无任何 IO。
/// </summary>
public sealed partial class BenchLoginEffectDispatcher
    : MviEffectDispatcherBase<BenchLoginIntent, BenchLoginEffect>
{
    private readonly IBenchAuthService _authService;

    /// <summary>
    /// 初始化登录复刻基准场景副作用分发器。
    /// </summary>
    /// <param name="authService">假认证服务。</param>
    public BenchLoginEffectDispatcher(IBenchAuthService authService)
    {
        ArgumentNullException.ThrowIfNull(authService);
        _authService = authService;
    }

    /// <summary>
    /// 获取已执行的副作用总数。
    /// </summary>
    public int HandledCount { get; private set; }

    /// <summary>
    /// 执行假认证并回流成功/失败意图。
    /// </summary>
    [MviEffect(typeof(BenchLoginEffect.PerformLogin))]
    private async ValueTask HandlePerformLogin(
        BenchLoginEffect.PerformLogin effect,
        CancellationToken cancellationToken)
    {
        HandledCount++;

        BenchAuthResult result = await _authService
            .LoginAsync(effect.UserName, effect.Password, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccess && result.DisplayName is not null)
        {
            await DispatchIntentAsync(
                new BenchLoginIntent.Succeeded(result.DisplayName),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        await DispatchIntentAsync(
            new BenchLoginIntent.Failed(result.ErrorMessage ?? "登录失败。"),
            cancellationToken).ConfigureAwait(false);
    }
}
