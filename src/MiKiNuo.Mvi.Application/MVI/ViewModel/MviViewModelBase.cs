using System.ComponentModel;
using System.Runtime.CompilerServices;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using R3;

using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.State;
namespace MiKiNuo.Mvi.Application.MVI.ViewModel;

/// <summary>
/// 表示 MVI ViewModel 基类。
/// </summary>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TIntent">意图类型。</typeparam>
/// <typeparam name="TEffect">副作用类型。</typeparam>
public abstract class MviViewModelBase<TState, TIntent, TEffect> : INotifyPropertyChanged, IDisposable
    where TState : IMviState
    where TIntent : IMviIntent
    where TEffect : IMviEffect
{
    private readonly IDisposable _stateSubscription;
    private bool _isDisposed;

    /// <summary>
    /// 初始化 MVI ViewModel 基类。
    /// </summary>
    /// <param name="store">状态存储。</param>
    protected MviViewModelBase(IMviStore<TState, TIntent, TEffect> store)
    {
        ArgumentNullException.ThrowIfNull(store);

        Store = store;
        _stateSubscription = Store.States.Subscribe(this, static (state, viewModel) => viewModel.ApplyState(state));
        ApplyState(Store.CurrentState);
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 获取状态存储。
    /// </summary>
    protected IMviStore<TState, TIntent, TEffect> Store { get; }

    /// <summary>
    /// 异步派发意图。
    /// </summary>
    /// <param name="intent">意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步派发过程的任务。</returns>
    protected ValueTask DispatchAsync(TIntent intent, CancellationToken cancellationToken = default)
    {
        return Store.DispatchAsync(intent, cancellationToken);
    }

    /// <summary>
    /// 设置属性值并通知 UI。
    /// </summary>
    /// <typeparam name="TValue">属性值类型。</typeparam>
    /// <param name="field">字段引用。</param>
    /// <param name="value">新值。</param>
    /// <param name="propertyName">属性名称。</param>
    /// <returns>如果值发生变化则返回 true。</returns>
    protected bool SetProperty<TValue>(
        ref TValue field,
        TValue value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<TValue>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// 释放命令或子类资源。
    /// </summary>
    protected virtual void DisposeManagedResources()
    {
    }

    /// <summary>
    /// 应用状态。
    /// </summary>
    /// <param name="state">新状态。</param>
    protected void ApplyState(TState state)
    {
        ApplyStateCore(state);
    }

    /// <summary>
    /// 子类实现状态到 ViewModel 属性的映射。
    /// </summary>
    /// <param name="state">新状态。</param>
    protected abstract void ApplyStateCore(TState state);

    /// <summary>
    /// 触发属性变更通知。
    /// </summary>
    /// <param name="propertyName">属性名称。</param>
    protected void OnPropertyChanged(string? propertyName)
    {
        PostToUiThread(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _stateSubscription.Dispose();
        DisposeManagedResources();
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    private static void PostToUiThread(Action action)
    {
        MviUiNotificationDispatcher.Post(action);
    }
}
