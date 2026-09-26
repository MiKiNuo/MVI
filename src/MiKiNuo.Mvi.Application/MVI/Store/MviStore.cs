using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Middleware;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.State;
using R3;
using System.Threading.Channels;

namespace MiKiNuo.Mvi.Application.MVI.Store;

/// <summary>
/// 表示经典 MVI 状态存储。
/// </summary>
/// <remarks>
/// 数据流：Intent → Middleware → Reduce → State 发布 → EffectDispatcher（锁外顺序派发）。
/// EffectDispatcher 执行副作用后可回流新 Intent，回流 Intent 作为普通派发重新进入管线，
/// 中间件全程可见。Reducer 是唯一决策点：是否发起异步调用由它产出的 Effect 表达。
/// </remarks>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TIntent">意图类型。</typeparam>
/// <typeparam name="TEffect">副作用类型。</typeparam>
public sealed class MviStore<TState, TIntent, TEffect>
    : IMviStore<TState, TIntent, TEffect>, IMviIntentSink<TIntent>, IAsyncDisposable
    where TState : IMviState
    where TIntent : IMviIntent
    where TEffect : IMviEffect
{
    /// <summary>保存并发布当前状态。</summary>
    private readonly ReactiveProperty<TState> _state;
    /// <summary>执行本地纯规约。</summary>
    private readonly IMviReducer<TState, TIntent, TEffect> _reducer;
    /// <summary>执行规约产生的副作用。</summary>
    private readonly IMviEffectDispatcher<TEffect> _effectDispatcher;
    /// <summary>串联意图处理管线。</summary>
    private readonly MviMiddlewarePipeline<TState, TIntent, TEffect> _pipeline;
    /// <summary>串行保护规约及状态发布。</summary>
    private readonly SemaphoreSlim _dispatchGate;
    /// <summary>保护准入和关闭状态。</summary>
    private readonly object _lifetimeGate = new();
    /// <summary>取消存储所属操作。</summary>
    private readonly CancellationTokenSource _lifetime = new();
    /// <summary>记录所有已准入操作退出。</summary>
    private readonly TaskCompletionSource _drained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    /// <summary>记录在途操作数。</summary>
    private int _activeDispatches;
    /// <summary>标记准入已经关闭。</summary>
    private bool _isDisposed;
    /// <summary>保存唯一的资源回收任务。</summary>
    private Task? _disposeTask;
    /// <summary>保存停止准入时发出的取消回调任务。</summary>
    private Task? _cancellationTask;
    /// <summary>保存等待规约的有界通知。</summary>
    private readonly Channel<TIntent> _notifications;
    /// <summary>处理通知准入后的规约调度。</summary>
    private readonly Task _notificationWorker;
    /// <summary>发布后台处理异常。</summary>
    private readonly Subject<Exception> _errors = new();
    /// <summary>串行发布后台异常。</summary>
    private readonly object _errorGate = new();

    /// <summary>
    /// 初始化 MVI 状态存储。
    /// </summary>
    /// <param name="initialState">初始状态。</param>
    /// <param name="reducer">规约器。</param>
    /// <param name="effectDispatcher">副作用分发器。</param>
    /// <param name="middlewares">中间件集合。</param>
    /// <param name="notificationCapacity">等待处理的通知容量，必须大于零。</param>
    public MviStore(
        TState initialState,
        IMviReducer<TState, TIntent, TEffect> reducer,
        IMviEffectDispatcher<TEffect> effectDispatcher,
        IReadOnlyList<IMviMiddleware<TState, TIntent, TEffect>>? middlewares = null,
        int notificationCapacity = 64)
    {
        ArgumentNullException.ThrowIfNull(initialState);
        ArgumentNullException.ThrowIfNull(reducer);
        ArgumentNullException.ThrowIfNull(effectDispatcher);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(notificationCapacity);

        _state = new ReactiveProperty<TState>(initialState);
        _reducer = reducer;
        _effectDispatcher = effectDispatcher;
        _pipeline = new MviMiddlewarePipeline<TState, TIntent, TEffect>(middlewares, ExecuteReduceCore);
        _dispatchGate = new SemaphoreSlim(1, 1);

        if (effectDispatcher is IMviIntentSinkAttachable<TIntent> attachable)
        {
            try
            {
                attachable.Attach(this);
            }
            catch
            {
                _state.Dispose();
                _dispatchGate.Dispose();
                _lifetime.Dispose();
                _errors.Dispose();
                throw;
            }
        }
        _notifications = Channel.CreateBounded<TIntent>(new BoundedChannelOptions(notificationCapacity)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait,
            AllowSynchronousContinuations = false,
        });
        _notificationWorker = Task.Run(ProcessNotificationsAsync);
    }

    /// <summary>
    /// 获取当前状态。
    /// </summary>
    public TState CurrentState => _state.Value;

    /// <summary>
    /// 获取状态变化流。
    /// </summary>
    public Observable<TState> States => _state;

    /// <summary>获取通知后台处理和关闭取消回调的异常流。</summary>
    public Observable<Exception> Errors => _errors;

    /// <summary>尝试接纳通知意图，队列已满或存储关闭时明确拒绝。</summary>
    /// <param name="intent">通知适配后的本地意图。</param>
    /// <returns>成功进入有界队列时为真，不表示业务处理完成。</returns>
    public bool TryPost(TIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        lock (_lifetimeGate)
        {
            return !_isDisposed && _notifications.Writer.TryWrite(intent);
        }
    }

    /// <summary>
    /// 派发意图：锁内完成 Middleware → Reduce → State 原子发布，锁外顺序派发副作用。
    /// </summary>
    /// <remarks>
    /// 派发门（SemaphoreSlim）只保护同步的规约管线与状态更新；
    /// 副作用在门释放后统一派发，因此 EffectDispatcher 可以安全地向
    /// 同一 Store 回流后续 Intent（重入安全），不会形成死锁，
    /// 慢副作用也不会阻塞同 Store 的其他 Intent。
    /// </remarks>
    /// <param name="intent">意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步派发过程的任务。</returns>
    public async ValueTask DispatchAsync(TIntent intent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);
        lock (_lifetimeGate)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            _activeDispatches++;
        }
        await ExecuteDispatchAsync(intent, cancellationToken, null).ConfigureAwait(false);
    }

    /// <summary>执行已准入派发，并跟踪包括副作用在内的完整生命周期。</summary>
    /// <param name="intent">本地意图。</param>
    /// <param name="cancellationToken">调用方取消标记。</param>
    /// <param name="reduced">通知规约完成信号，普通派发不提供。</param>
    /// <returns>完整派发任务。</returns>
    private async Task ExecuteDispatchAsync(
        TIntent intent, CancellationToken cancellationToken, TaskCompletionSource? reduced)
    {
        try
        {
            using CancellationTokenSource? linked = cancellationToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token)
                : null;
            CancellationToken dispatchToken = linked?.Token ?? _lifetime.Token;
            MviReduceResult<TState, TEffect> result;
            await _dispatchGate.WaitAsync(dispatchToken).ConfigureAwait(false);
            try
            {
                dispatchToken.ThrowIfCancellationRequested();
                MviMiddlewareContext<TState, TIntent, TEffect> context = new(CurrentState, intent);
                result = await _pipeline.InvokeAsync(context, dispatchToken).ConfigureAwait(false);
                lock (_lifetimeGate)
                {
                    ObjectDisposedException.ThrowIf(_isDisposed, this);
                    _state.Value = result.State;
                }
            }
            finally
            {
                _ = _dispatchGate.Release();
            }
            reduced?.TrySetResult();
            await DispatchEffectsAsync(result.Effects, dispatchToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (reduced is not null && _lifetime.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (reduced is not null)
        {
            ReportError(exception);
        }
        finally
        {
            reduced?.TrySetResult();
            lock (_lifetimeGate)
            {
                if (--_activeDispatches == 0 && _isDisposed)
                {
                    _drained.TrySetResult();
                }
            }
        }
    }

    /// <summary>
    /// 停止准入并取消所属操作，在途操作退出后回收资源。
    /// </summary>
    public void Dispose()
    {
        lock (_lifetimeGate)
        {
            Stop();
            _disposeTask ??= Task.Run(() => ReleaseAsync(_cancellationTask!));
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>关闭准入与取消操作，最终资源回收仍由 Dispose 发起；供实例所有者声明为停止动作。</summary>
    public void Stop()
    {
        lock (_lifetimeGate)
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            _notifications.Writer.TryComplete();
            if (_activeDispatches == 0)
            {
                _drained.TrySetResult();
            }
            _cancellationTask = _lifetime.CancelAsync();
        }
    }

    /// <summary>关闭准入并等待所有在途操作与资源回收完成。</summary>
    /// <returns>安全回收完成的任务。</returns>
    public ValueTask DisposeAsync()
    {
        Dispose();
        lock (_lifetimeGate)
        {
            return new ValueTask(_disposeTask!);
        }
    }

    /// <summary>取消操作并在所有使用者退出后释放同步资源。</summary>
    /// <param name="cancellation">已经发出取消请求后的回调完成任务。</param>
    /// <returns>资源回收任务。</returns>
    private async Task ReleaseAsync(Task cancellation)
    {
        try
        {
            await cancellation.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
        finally
        {
            await _notificationWorker.ConfigureAwait(false);
            await _drained.Task.ConfigureAwait(false);
            while (_notifications.Reader.TryRead(out _))
            {
            }
            _lifetime.Dispose();
            _state.Dispose();
            _dispatchGate.Dispose();
            _errors.Dispose();
        }
    }

    /// <summary>按队列顺序启动规约，不等待副作用链，允许递归通知继续处理。</summary>
    /// <returns>通知消费任务。</returns>
    private async Task ProcessNotificationsAsync()
    {
        await foreach (TIntent intent in _notifications.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            TaskCompletionSource reduced = new(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_lifetimeGate)
            {
                if (_isDisposed)
                {
                    break;
                }
                _activeDispatches++;
            }
            _ = Task.Run(() => ExecuteDispatchAsync(intent, default, reduced));
            await reduced.Task.ConfigureAwait(false);
        }
    }

    /// <summary>串行向订阅者发布后台处理故障。</summary>
    /// <param name="exception">处理故障。</param>
    private void ReportError(Exception exception)
    {
        lock (_errorGate)
        {
            _errors.OnNext(exception);
        }
    }

    /// <summary>
    /// 执行同步规约，是中间件管线的终端步骤。
    /// </summary>
    /// <param name="context">中间件上下文。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>规约结果。</returns>
    private ValueTask<MviReduceResult<TState, TEffect>> ExecuteReduceCore(
        MviMiddlewareContext<TState, TIntent, TEffect> context,
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(_reducer.Reduce(context.State, context.Intent));
    }

    /// <summary>
    /// 在锁外按序派发副作用集合到分发器。
    /// </summary>
    /// <param name="effects">副作用集合。</param>
    /// <param name="cancellationToken">取消标记。</param>
    private async ValueTask DispatchEffectsAsync(
        IReadOnlyList<TEffect> effects,
        CancellationToken cancellationToken)
    {
        foreach (TEffect effect in effects)
        {
            lock (_lifetimeGate)
            {
                ObjectDisposedException.ThrowIf(_isDisposed, this);
            }
            cancellationToken.ThrowIfCancellationRequested();
            await _effectDispatcher.DispatchAsync(effect, cancellationToken).ConfigureAwait(false);
        }
    }
}
