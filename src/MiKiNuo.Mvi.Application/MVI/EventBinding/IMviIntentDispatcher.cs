using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Application.MVI.EventBinding;

/// <summary>
/// 表示 Intent 派发器接口，供 View 层把事件映射为 Intent 后派发到 Store。
/// </summary>
/// <remarks>
/// <see cref="MviComponent"/> 显式实现本接口；
/// View 通过 <see cref="MviComponent.GetIntentDispatcher"/> 获取实例，
/// 配合 <see cref="EventBinding{TEvent}"/> 把控件事件转换为 Intent 派发。
/// </remarks>
public interface IMviIntentDispatcher
{
    /// <summary>
    /// 派发 Intent 到 Store。
    /// </summary>
    /// <param name="intent">意图。</param>
    public void Dispatch(IMviIntent intent);
}
