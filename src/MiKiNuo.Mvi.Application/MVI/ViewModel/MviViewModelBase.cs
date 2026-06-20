using System.ComponentModel;
using System.Runtime.CompilerServices;
using MiKiNuo.Mvi.Application.MVI.EventBinding;
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
public abstract class MviViewModelBase<TState, TIntent, TEffect> : MviComponent, INotifyPropertyChanged
    where TState : IMviState
    where TIntent : IMviIntent
    where TEffect : IMviEffect
{
    private readonly IDisposable _stateSubscription;
    private readonly IMviUiDispatcher _uiDispatcher;
    private bool _isDisposed;

    /// <summary>
    /// 初始化 MVI ViewModel 基类。
    /// </summary>
    /// <param name="store">状态存储。</param>
    /// <param name="uiDispatcher">UI 调度器（可选，缺省时使用 <see cref="MviInlineUiDispatcher.Instance"/>）。</param>
    protected MviViewModelBase(IMviStore<TState, TIntent, TEffect> store, IMviUiDispatcher? uiDispatcher = null)
    {
        ArgumentNullException.ThrowIfNull(store);

        Store = store;
        _uiDispatcher = uiDispatcher ?? MviInlineUiDispatcher.Instance;
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
    /// 获取 ViewModel 用于 PropertyChanged/Command 通知的 UI 调度器。
    /// 源生成器在创建 <c>MviCommand</c>/<c>MviAsyncCommand</c> 时复用此调度器。
    /// </summary>
    public IMviUiDispatcher UiDispatcher => _uiDispatcher;

    /// <summary>
    /// 异步派发意图。
    /// </summary>
    /// <param name="intent">意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步派发过程的任务。</returns>
    protected async ValueTask DispatchAsync(TIntent intent, CancellationToken cancellationToken = default)
    {
        await Store.DispatchAsync(intent, cancellationToken);
    }

    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2012:Avoid unnecessary zero-bit allocation",
        Justification = "fire-and-forget 派发 Intent，Store 内部已处理异步完成。")]
    protected override void Dispatch(IMviIntent intent)
    {
        if (intent is TIntent typedIntent)
        {
            _ = DispatchAsync(typedIntent);
        }
        else
        {
            throw new ArgumentException(
                $"意图类型不匹配：期望 {typeof(TIntent).FullName}，实际 {intent?.GetType().FullName ?? "null"}。",
                nameof(intent));
        }
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
    /// <remarks>
    /// 源生成器会为带命令的子类重写此方法以释放命令资源。
    /// 若子类需要释放其它资源，请重写 <see cref="OnDispose"/>，避免与生成器冲突。
    /// </remarks>
    protected virtual void DisposeManagedResources()
    {
    }

    /// <summary>
    /// ViewModel 释放的最终扩展点，由 <see cref="Dispose"/> 在 <see cref="DisposeManagedResources"/> 之后调用。
    /// </summary>
    /// <remarks>
    /// 适用于"额外的订阅/资源"需要在 ViewModel 生命周期结束时释放，但与源生成器管理的命令资源无直接依赖关系的场景。
    /// </remarks>
    protected virtual void OnDispose()
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
        _uiDispatcher.Post(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        _stateSubscription.Dispose();
        DisposeManagedResources();
        OnDispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
