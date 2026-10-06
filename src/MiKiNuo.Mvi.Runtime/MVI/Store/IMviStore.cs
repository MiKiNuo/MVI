using MiKiNuo.Mvi.Abstractions.MVI.Intent;
using MiKiNuo.Mvi.Abstractions.MVI.State;
using MiKiNuo.Mvi.Runtime.MVI.Intent;
using R3;
namespace MiKiNuo.Mvi.Runtime.MVI.Store;
/// <summary>组件自己的状态存储与唯一意图入口。状态只能通过内部上下文提交。</summary>
/// <typeparam name="TState">不可变状态。</typeparam>
/// <typeparam name="TIntent">用户或组件输入。</typeparam>
public interface IMviStore<TState, in TIntent> : IMviIntentSink<TIntent>
    where TState : IMviState where TIntent : IMviIntent
{
    /// <summary>获取已提交的最新状态。</summary>
    public TState CurrentState { get; }
    /// <summary>获取按提交顺序发布的状态流，新订阅者取得最近已发布状态。</summary>
    public Observable<TState> States { get; }
    /// <summary>获取已接纳通知的后台失败及观察者错误，不包含意图内容。</summary>
    public Observable<Exception> Errors { get; }
}
