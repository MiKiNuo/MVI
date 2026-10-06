using System.Threading.Channels;
using MiKiNuo.Mvi.Abstractions.MVI.Intent;
using MiKiNuo.Mvi.Abstractions.MVI.Mutation;
using MiKiNuo.Mvi.Abstractions.MVI.State;
using MiKiNuo.Mvi.Runtime.MVI.Intent;
using MiKiNuo.Mvi.Runtime.MVI.Middleware;
using MiKiNuo.Mvi.Runtime.MVI.Reducer;
using R3;
namespace MiKiNuo.Mvi.Runtime.MVI.Store;
/// <summary>
/// 单一 Handler/Mutation 内核：意图经中间件进入 Handler，只有 Reduce 能提交状态。
/// 锁只覆盖守卫、纯 Reducer 与提交，不跨 await，不在锁内调用状态订阅者。
/// </summary>
/// <typeparam name="TState">不可变状态。</typeparam>
/// <typeparam name="TIntent">本组件意图。</typeparam>
public sealed class MviStore<TState, TIntent> : IMviStore<TState, TIntent>, IDisposable, IAsyncDisposable
    where TState : IMviState where TIntent : IMviIntent
{
    private readonly object _gate = new();
    private readonly object _errorGate = new();
    private readonly IMviReducer<TState> _reducer;
    private readonly MviMiddlewarePipeline<TState, TIntent> _pipeline;
    private readonly ReactiveProperty<TState> _states;
    private readonly Subject<Exception> _errors = new();
    private readonly Queue<TState> _publications = new();
    private readonly Channel<TIntent> _notifications;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly TaskCompletionSource _drained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Task _notificationWorker;
    private readonly int _maxConcurrentIntents;
    private TState _current;
    private int _active;
    private bool _stopping;
    private bool _publishing;
    private Task? _cancellation;
    private Task? _disposeTask;

    /// <summary>创建独占 Handler 的组件 Store；不会拥有注入的 Handler、Reducer 或中间件。</summary>
    /// <param name="initialState">初始不可变状态。</param>
    /// <param name="handler">唯一业务处理器。</param>
    /// <param name="reducer">纯状态转换器。</param>
    /// <param name="middlewares">按顺序执行的中间件。</param>
    /// <param name="notificationCapacity">等待通知数量上限。</param>
    /// <param name="maxConcurrentIntents">实际在途 Handler 数量上限，含网络等待。</param>
    public MviStore(TState initialState, IIntentHandler<TIntent, TState> handler, IMviReducer<TState> reducer,
        IReadOnlyList<IMviMiddleware<TState, TIntent>>? middlewares = null,
        int notificationCapacity = 64, int maxConcurrentIntents = 64)
    {
        ArgumentNullException.ThrowIfNull(initialState);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(reducer);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(notificationCapacity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConcurrentIntents);
        _current = initialState;
        _reducer = reducer;
        _maxConcurrentIntents = maxConcurrentIntents;
        _pipeline = new(middlewares, handler);
        _states = new(initialState);
        _notifications = Channel.CreateBounded<TIntent>(new BoundedChannelOptions(notificationCapacity)
        { SingleReader = true, FullMode = BoundedChannelFullMode.Wait, AllowSynchronousContinuations = false });
        try
        {
            if (handler is IMviIntentSinkAttachable<TIntent> attachable) attachable.Attach(this);
        }
        catch
        {
            _states.Dispose(); _errors.Dispose(); _lifetime.Dispose();
            throw;
        }
        _notificationWorker = Task.Run(ProcessNotificationsAsync);
    }
    /// <summary>获取最新已提交状态，不等待 UI 投影。</summary>
    public TState CurrentState { get { lock (_gate) return _current; } }
    /// <summary>获取按提交顺序发布的状态流。</summary>
    public Observable<TState> States => _states;
    /// <summary>获取后台错误流；内容不自动包含 Intent、State 或凭据。</summary>
    public Observable<Exception> Errors => _errors;
    /// <summary>派发并等待该 Handler 显式等待的完整业务链；容量不足时明确拒绝。</summary>
    /// <param name="intent">已捕获的输入快照。</param>
    /// <param name="cancellationToken">协作式取消标记。</param>
    /// <returns>本次操作的完成任务。</returns>
    public ValueTask DispatchAsync(TIntent intent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_stopping, this);
            if (_active >= _maxConcurrentIntents) throw new MviCapacityExceededException();
            _active++;
        }
        return ExecuteAsync(intent, cancellationToken);
    }
    private async ValueTask ExecuteAsync(TIntent intent, CancellationToken caller)
    {
        Context? context = null;
        try
        {
            using CancellationTokenSource? linked = caller.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(caller, _lifetime.Token) : null;
            CancellationToken token = linked?.Token ?? _lifetime.Token;
            token.ThrowIfCancellationRequested();
            context = new(this);
            await _pipeline.InvokeAsync(intent, context, token).ConfigureAwait(false);
        }
        finally
        {
            lock (_gate)
            {
                // 与 Reduce 使用同一把锁，封住处理结束后偷用上下文的竞态。
                if (context is not null) context.Closed = true;
                if (--_active == 0 && _stopping) _drained.TrySetResult();
            }
        }
    }
    /// <summary>同步接纳通知；仅保证进入队列，后续失败通过 Errors 报告。</summary>
    /// <param name="intent">通知映射后的本地意图。</param>
    /// <returns>是否进入有界等待队列。</returns>
    public bool TryPost(TIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        lock (_gate) return !_stopping && _notifications.Writer.TryWrite(intent);
    }
    private async Task ProcessNotificationsAsync()
    {
        await foreach (TIntent intent in _notifications.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            lock (_gate) { if (_stopping) break; }
            try { await DispatchAsync(intent, _lifetime.Token).ConfigureAwait(false); }
            catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { break; }
            catch (ObjectDisposedException) when (_lifetime.IsCancellationRequested) { break; }
            catch (Exception exception) { ReportError(exception); }
        }
    }
    private bool Commit(Context context, Func<TState, bool>? guard, IMviMutation<TState> mutation)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        bool publish;
        lock (_gate)
        {
            if (guard is not null && (_stopping || context.Closed)) return false;
            ObjectDisposedException.ThrowIf(_stopping, this);
            if (context.Closed) throw new InvalidOperationException("Intent 已结束，不能继续使用它的上下文。");
            if (guard is not null && !guard(_current)) return false;
            TState next = _reducer.Reduce(_current, mutation);
            ArgumentNullException.ThrowIfNull(next);
            if (EqualityComparer<TState>.Default.Equals(_current, next)) return true;
            _current = next;
            _publications.Enqueue(next);
            publish = !_publishing;
            _publishing = true;
        }
        if (publish) PublishPending();
        return true;
    }
    private void PublishPending()
    {
        while (true)
        {
            TState next;
            lock (_gate)
            {
                if (_publications.Count == 0) { _publishing = false; return; }
                next = _publications.Dequeue();
            }
            // 订阅者可以派发新 Intent；队列保证重入时仍按提交顺序发布。
            try { _states.Value = next; }
            catch (Exception exception) { ReportError(exception); }
        }
    }
    private void ReportError(Exception exception)
    {
        lock (_errorGate)
        {
            try { _errors.OnNext(exception); }
            catch { /* 错误观察者故障不能破坏 Store 排空与资源释放。 */ }
        }
    }
    /// <summary>停止准入并请求取消；状态已提交的部分不会回滚。</summary>
    public void Stop()
    {
        lock (_gate)
        {
            if (_stopping) return;
            _stopping = true;
            _notifications.Writer.TryComplete();
            _cancellation = _lifetime.CancelAsync();
            if (_active == 0) _drained.TrySetResult();
        }
    }
    /// <summary>启动异步回收；需要确认资源已释放的所有者必须等待 DisposeAsync。</summary>
    public void Dispose()
    {
        Stop();
        lock (_gate) _disposeTask ??= Task.Run(ReleaseAsync);
        GC.SuppressFinalize(this);
    }
    /// <summary>等待所有已准入 Handler 退出后释放流；不伪装成强制终止外部任务。</summary>
    /// <returns>真正释放完成的任务。</returns>
    public ValueTask DisposeAsync()
    {
        Dispose();
        lock (_gate) return new(_disposeTask!);
    }
    private async Task ReleaseAsync()
    {
        try { await _cancellation!.ConfigureAwait(false); }
        catch (Exception exception) { ReportError(exception); }
        await _notificationWorker.ConfigureAwait(false);
        await _drained.Task.ConfigureAwait(false);
        while (_notifications.Reader.TryRead(out _)) { }
        _states.Dispose(); _errors.Dispose(); _lifetime.Dispose();
    }
    private sealed class Context(MviStore<TState, TIntent> owner) : IIntentContext<TState>
    {
        internal bool Closed;
        public TState State { get { lock (owner._gate) { if (Closed) throw new InvalidOperationException("Intent 已结束。"); return owner._current; } } }
        public void Reduce(IMviMutation<TState> mutation) => owner.Commit(this, null, mutation);
        public bool TryReduce(Func<TState, bool> guard, IMviMutation<TState> mutation)
        {
            ArgumentNullException.ThrowIfNull(guard);
            return owner.Commit(this, guard, mutation);
        }
    }
}
