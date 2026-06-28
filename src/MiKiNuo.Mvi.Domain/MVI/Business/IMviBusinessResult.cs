namespace MiKiNuo.Mvi.Domain.MVI.Business;

/// <summary>
/// 表示异步业务处理结果标记接口。
/// </summary>
/// <remarks>
/// IntentHandler 执行异步业务后产出此接口的实现,
/// 由 Store 传递给 Reducer 消费,产出新状态与副作用。
/// </remarks>
public interface IMviBusinessResult
{
}
