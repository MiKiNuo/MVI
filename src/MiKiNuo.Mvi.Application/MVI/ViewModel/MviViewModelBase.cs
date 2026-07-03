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
/// <remarks>
/// 所有权矩阵（源生成器 vs 子类实现）：
/// <list type="bullet">
/// <item>有 [MviBind] + 有 [MviCommand]：
///   全部钩子由生成器 emit。</item>
/// <item>有 [MviBind] + 无 [MviCommand]：
///   ApplyStateCore 由生成器 emit，
///   OnConstructed/DisposeGeneratedCommands
///   为基类空体。</item>
/// <item>无 [MviBind] + 有 [MviCommand]：
///   ApplyStateCore 由子类手写，
///   OnConstructed/DisposeGeneratedCommands
///   由生成器 emit。</item>
/// <item>无 [MviBind] + 无 [MviCommand]：
///   ApplyStateCore 由子类手写，
///   其余为基类空体。</item>
/// </list>
/// <para>
/// 混合模式（[MviCommand] + 手写命令）：
/// 手写命令在构造函数初始化，
/// 在 <see cref="OnDispose"/> 释放。
/// </para>
/// <para>
/// 基类 <see cref="OnDispose"/> 无副作用，
/// 调用 base.OnDispose() 可选。
/// </para>
/// </remarks>
public abstract class MviViewModelBase<TState, TIntent, TEffect> : MviComponent, INotifyPropertyChanged
    where TState : IMviState
    where TIntent : IMviIntent
    where TEffect : IMviEffect
{
    private readonly IDisposable _stateSubscription;
    private readonly List<IDisposable> _siblingSubscriptions = new();
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
        OnConstructed();
    }

    /// <summary>
    /// 构造钩子，由源生成器重写以初始化命令。
    /// </summary>
    /// <remarks>
    /// 基类构造函数末尾调用此方法；源生成器在含 [MviCommand] 的子类中 emit 重写以自动初始化命令，子类无需手调。
    /// </remarks>
    protected virtual void OnConstructed()
    {
    }

    /// <summary>
    /// 属性变更事件。
    /// </summary>
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

    /// <summary>
    /// 派发意图到状态存储。
    /// </summary>
    /// <param name="intent">意图。</param>
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
    /// 释放源生成器创建的命令资源。
    /// </summary>
    /// <remarks>
    /// 此方法专留给源生成器重写以释放命令资源；子类请重写 <see cref="OnDispose"/> 释放额外资源。
    /// </remarks>
    protected virtual void DisposeGeneratedCommands()
    {
    }

    /// <summary>
    /// 绑定兄弟 Store 的状态到当前 ViewModel。
    /// </summary>
    /// <typeparam name="TSiblingState">兄弟状态类型。</typeparam>
    /// <typeparam name="TSiblingIntent">兄弟意图类型。</typeparam>
    /// <typeparam name="TSiblingEffect">兄弟副作用类型。</typeparam>
    /// <param name="siblingStore">兄弟 Store。</param>
    /// <param name="applySiblingState">状态应用回调。</param>
    /// <returns>订阅句柄（自动由基类管理释放）。</returns>
    protected IDisposable BindSiblingState<TSiblingState, TSiblingIntent, TSiblingEffect>(
        IMviStore<TSiblingState, TSiblingIntent, TSiblingEffect> siblingStore,
        Action<TSiblingState> applySiblingState)
        where TSiblingState : IMviState
        where TSiblingIntent : IMviIntent
        where TSiblingEffect : IMviEffect
    {
        ArgumentNullException.ThrowIfNull(siblingStore);
        ArgumentNullException.ThrowIfNull(applySiblingState);

        IDisposable subscription = siblingStore.States
            .Subscribe(applySiblingState);
        _siblingSubscriptions.Add(subscription);
        return subscription;
    }

    /// <summary>
    /// ViewModel 释放的最终扩展点，由 <see cref="Dispose"/> 在 <see cref="DisposeGeneratedCommands"/> 之后调用。
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

    /// <summary>
    /// 释放 ViewModel 资源。
    /// </summary>
    public override void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        _stateSubscription.Dispose();
        DisposeGeneratedCommands();

        // 释放所有 sibling Store 订阅
        foreach (IDisposable subscription in _siblingSubscriptions)
        {
            subscription.Dispose();
        }
        _siblingSubscriptions.Clear();

        OnDispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
