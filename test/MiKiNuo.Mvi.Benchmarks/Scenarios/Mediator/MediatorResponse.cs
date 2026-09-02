namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mediator;

/// <summary>
/// 表示中介者基准场景的响应：携带请求原值用于对账。
/// </summary>
/// <param name="Value">响应载荷（回显请求值）。</param>
public sealed record MediatorResponse(int Value);
