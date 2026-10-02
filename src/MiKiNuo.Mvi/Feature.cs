using System.Collections.Immutable;

namespace MiKiNuo.Mvi;

/// <summary>为一个独立功能实例提供统一状态输入入口。</summary>
/// <typeparam name="TState">由生成器验证的不可变业务状态类型。</typeparam>
public abstract class Feature<TState> : Feature where TState : notnull
{
    private readonly FeatureStore<TState> store;

    /// <summary>使用初始业务状态创建独立实例。</summary>
    /// <param name="initialState">实例的初始不可变状态。</param>
    /// <param name="postCapacity">本实例共享收件箱的正数容量，包含正在处理及等待处理的未终结投递。</param>
    protected Feature(TState initialState, int postCapacity = 16)
    {
        ArgumentNullException.ThrowIfNull(initialState);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(postCapacity);
        store = new FeatureStore<TState>(initialState, postCapacity);
        ExecutionFeatures.Add(store, this);
        FeatureFactory.Capture(this);
    }

    /// <summary>获取一次一致提交的完整快照。</summary>
    public RuntimeSnapshot<TState> Snapshot => store.Snapshot;

    /// <summary>获取实例是否已逻辑关闭。</summary>
    public override bool IsClosed => store.IsClosed;

    internal override void OwnScope(IAsyncDisposable scope) => store.OwnScope(scope);
    internal override void OwnConstructionScope(IAsyncDisposable scope, Task constructionExited)
        => store.OwnScope(scope, constructionExited);
    internal override object ExecutionOwner => store;
    internal override (CloseResult Result, Action Finish) CommitClose(Task<Exception?> childrenReleased)
        => store.CommitClose(childrenReleased);

    internal void AttachProjection(FeatureProjection<TState> projection) => store.AttachProjection(projection);

    internal void DetachProjection(FeatureProjection<TState> projection) => store.DetachProjection(projection);

    /// <summary>将生成的强类型输入交给实例的状态提交入口。</summary>
    /// <typeparam name="TValue">输入值类型。</typeparam>
    /// <param name="value">本次输入值。</param>
    /// <param name="reduce">根据当前状态计算下一状态的纯转换。</param>
    protected void DispatchInput<TValue>(TValue value, Func<TState, TValue, TState> reduce)
    {
        ArgumentNullException.ThrowIfNull(reduce);
        store.Dispatch(new InputIntent<TState, TValue>(value, reduce));
    }

    /// <summary>将生成的操作调用交给实例统一接纳入口。</summary>
    /// <typeparam name="TResult">业务方法的返回值类型。</typeparam>
    /// <param name="name">声明的业务操作名称；同名入口必须共用相同并发配置。</param>
    /// <param name="validate">启动时的纯验证方法。</param>
    /// <param name="execute">在提交门外执行的业务方法。</param>
    /// <param name="cancellationToken">本次执行的协作取消令牌。</param>
    /// <param name="concurrency">同名操作的并发接纳方式。</param>
    /// <param name="capacity">Queue 的等待容量，不包含运行项。</param>
    /// <param name="maxConcurrency">Parallel 的正数并行上限，其他策略使用零。</param>
    /// <returns>业务方法退出且有关状态提交后的结构化结果。</returns>
    protected Task<OperationResult<TResult>> DispatchOperation<TResult>(string name, Func<TState, bool>? validate,
        Func<Operation<TState>, ValueTask<TResult>> execute, CancellationToken cancellationToken,
        OperationConcurrency concurrency = OperationConcurrency.Reject, int capacity = 0, int maxConcurrency = 0)
        => store.Start(name, validate, execute, cancellationToken, concurrency, capacity, maxConcurrency);

    /// <summary>创建使用默认重复拒绝策略的强类型请求端口。</summary>
    /// <typeparam name="TRequest">由业务契约定义的不可变请求类型。</typeparam>
    /// <typeparam name="TResult">目标操作的业务返回值类型。</typeparam>
    /// <param name="name">与程序调用共用的操作名称。</param>
    /// <param name="validate">在启动原子区间根据当前状态和本次请求执行的纯验证。</param>
    /// <param name="execute">在提交门外处理请求并通过操作上下文反馈状态的业务方法。</param>
    /// <param name="cancellationPolicy">接纳后的执行取消归属；默认 TargetOwned 忽略发送方执行令牌，Propagate 接受显式执行令牌。</param>
    /// <returns>隐藏本实例具体类型和状态类型的独立契约端口。</returns>
    protected RequestPort<TRequest, TResult> CreateRequestPort<TRequest, TResult>(string name,
        Func<TState, TRequest, bool>? validate, Func<Operation<TState>, TRequest, ValueTask<TResult>> execute,
        RequestCancellationPolicy cancellationPolicy = RequestCancellationPolicy.TargetOwned)
        where TRequest : notnull
        => CreateRequestPort(name, validate, execute, OperationConcurrency.Reject, cancellationPolicy: cancellationPolicy);

    /// <summary>创建显式共用同名操作并发配置的强类型请求端口。</summary>
    /// <typeparam name="TRequest">由业务契约定义的不可变请求类型。</typeparam>
    /// <typeparam name="TResult">目标操作的业务返回值类型。</typeparam>
    /// <param name="name">与程序调用共用的操作名称。</param>
    /// <param name="validate">在启动原子区间根据当前状态和本次请求执行的纯验证。</param>
    /// <param name="execute">在提交门外处理请求并通过操作上下文反馈状态的业务方法。</param>
    /// <param name="concurrency">与同名生成操作共用的显式接纳策略。</param>
    /// <param name="capacity">与同名 Queue 操作共用的正数等待容量，其他策略使用零。</param>
    /// <param name="maxConcurrency">与同名 Parallel 操作共用的正数并行上限，其他策略使用零。</param>
    /// <param name="cancellationPolicy">接纳后的执行取消归属；默认 TargetOwned 忽略发送方执行令牌，Propagate 接受显式执行令牌。</param>
    /// <returns>隐藏本实例具体类型和状态类型的独立契约端口。</returns>
    protected RequestPort<TRequest, TResult> CreateRequestPort<TRequest, TResult>(string name,
        Func<TState, TRequest, bool>? validate, Func<Operation<TState>, TRequest, ValueTask<TResult>> execute,
        OperationConcurrency concurrency, int capacity = 0, int maxConcurrency = 0,
        RequestCancellationPolicy cancellationPolicy = RequestCancellationPolicy.TargetOwned)
        where TRequest : notnull
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(execute);
        if (!Enum.IsDefined(cancellationPolicy))
        {
            throw new ArgumentOutOfRangeException(nameof(cancellationPolicy));
        }

        RequestPort<TRequest, TResult> port = new((request, executionCancellationToken) => DispatchOperation(name,
            state => validate?.Invoke(state, request) ?? true, operation => execute(operation, request),
            cancellationPolicy == RequestCancellationPolicy.Propagate ? executionCancellationToken : CancellationToken.None,
            concurrency, capacity, maxConcurrency),
            (request, id) => store.Post(name, id, state => validate?.Invoke(state, request) ?? true,
                operation => execute(operation, request), concurrency, capacity, maxConcurrency));
        port.Owner = this;
        store.RegisterPort(port.Deactivate);
        return port;
    }
}

internal readonly struct InputIntent<TState, TValue>(TValue value, Func<TState, TValue, TState> reduce)
{
    internal TState Reduce(TState state) => reduce(state, value);
}

/// <summary>描述启动请求及在入口采样的取消条件。</summary>
internal readonly struct OperationStartIntent<TState, TResult>(string name, Guid id, Func<TState, bool>? validate,
    Func<Operation<TState>, ValueTask<TResult>> execute, CancellationToken cancellationToken,
    OperationConcurrency concurrency, int capacity, int maxConcurrency, bool queueActive = false, bool fromQueue = false,
    bool configurationMatches = true) where TState : notnull
{
    internal string Name { get; } = name;
    internal Guid Id { get; } = id;
    internal Func<TState, bool>? Validate { get; } = validate;
    internal Func<Operation<TState>, ValueTask<TResult>> Execute { get; } = execute;
    internal CancellationToken CancellationToken { get; } = cancellationToken;
    internal bool CancellationRequested { get; } = cancellationToken.IsCancellationRequested;
    internal OperationConcurrency Concurrency { get; } = concurrency;
    internal int Capacity { get; } = capacity;
    internal int MaxConcurrency { get; } = maxConcurrency;
    internal bool QueueActive { get; } = queueActive;
    internal bool FromQueue { get; } = fromQueue;
    internal bool ConfigurationMatches { get; } = configurationMatches;
}

/// <summary>描述提交后执行的业务方法与通过验证的开始输入。</summary>
internal readonly struct OperationEffect<TState, TResult>(OperationStartIntent<TState, TResult> request, TState input)
    where TState : notnull
{
    internal OperationStartIntent<TState, TResult> Request { get; } = request;
    internal TState Input { get; } = input;
}

/// <summary>描述实际执行退出后的结果输入，不携带可变执行上下文。</summary>
internal readonly struct OperationCompletedIntent<TResult>(string name, Guid id, TResult? value, bool canceled, Exception? failure)
{
    internal string Name { get; } = name;
    internal Guid Id { get; } = id;
    internal TResult? Value { get; } = value;
    internal bool Canceled { get; } = canceled;
    internal Exception? Failure { get; } = failure;
}

internal readonly struct QueuedOperationCanceledIntent(string name, Guid id)
{
    internal string Name { get; } = name;
    internal Guid Id { get; } = id;
}

internal enum OperationDecision
{
    ExecuteEffect,
    ReturnResult,
    Enqueue,
}

/// <summary>保存纯转换计算的下一快照、调用决定与门外副作用描述。</summary>
internal readonly struct OperationReduction<TState, TResult>(RuntimeSnapshot<TState> snapshot,
    OperationResult<TResult>? result = null, OperationEffect<TState, TResult>? effect = null, bool queued = false) where TState : notnull
{
    internal RuntimeSnapshot<TState> Snapshot { get; } = snapshot;
    internal OperationResult<TResult>? Result { get; } = result;
    internal OperationEffect<TState, TResult>? Effect { get; } = effect;
    internal OperationDecision Decision => queued ? OperationDecision.Enqueue
        : Effect.HasValue ? OperationDecision.ExecuteEffect : OperationDecision.ReturnResult;
}

internal sealed class FeatureStore<TState> where TState : notnull
{
    private readonly object gate = new();
    private RuntimeSnapshot<TState> snapshot;
    private bool reducing;
    private FeatureProjection<TState>? projection;
    private readonly Dictionary<string, Operation<TState>> executions = new(StringComparer.Ordinal);
    private readonly HashSet<Operation<TState>> activeExecutions = [];
    private readonly Queue<PostEnvelope> inbox = new();
    private readonly int postCapacity;
    private int pendingPosts;
    private bool consumingPosts;
    private readonly List<Action> ports = [];
    private CloseResult? closeResult;
    private TaskCompletionSource? drained;
    private IAsyncDisposable? ownedScope;
    private Task constructionExited = Task.CompletedTask;
    private readonly Dictionary<string, OperationQueue> queues = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (OperationConcurrency Concurrency, int Capacity, int MaxConcurrency)> configurations = new(StringComparer.Ordinal);

    private sealed class OperationQueue
    {
        internal LinkedList<QueuedOperation> Waiting { get; } = new();
        internal bool Active { get; set; }
    }

    private sealed class QueuedOperation(string name, Guid id, Func<Action> start, Action canceled)
    {
        internal string Name { get; } = name;
        internal Guid Id { get; } = id;
        internal Func<Action> Start { get; } = start;
        internal Action Canceled { get; } = canceled;
        internal LinkedListNode<QueuedOperation>? Node { get; set; }
        internal CancellationTokenRegistration Registration { get; set; }
    }

    private sealed class PostEnvelope(Func<Task> process, Action cancel)
    {
        internal Func<Task> Process { get; } = process;
        internal Action Cancel { get; } = cancel;
    }

    internal FeatureStore(TState state, int postCapacity)
    {
        snapshot = new RuntimeSnapshot<TState>(state, 0);
        this.postCapacity = postCapacity;
    }

    internal (PostReceipt<TResult> Receipt, Action? Schedule) Post<TResult>(string name, Guid id, Func<TState, bool> validate,
        Func<Operation<TState>, ValueTask<TResult>> execute, OperationConcurrency concurrency, int capacity, int maxConcurrency)
    {
        TaskCompletionSource<OperationResult<TResult>> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        bool startConsumer;
        lock (gate)
        {
            if (closeResult is not null) return (new PostReceipt<TResult>(id, PostResultKind.TargetUnavailable), null);
            if (pendingPosts == postCapacity) return (new PostReceipt<TResult>(id, PostResultKind.InboxFull), null);
            inbox.Enqueue(new PostEnvelope(ProcessAsync,
                () => completion.SetResult(new OperationResult<TResult>(name, id, OperationResultKind.Canceled, reason: "Closed"))));
            pendingPosts++;
            startConsumer = !consumingPosts;
            consumingPosts = true;
        }

        return (new PostReceipt<TResult>(id, PostResultKind.Accepted, completion.Task), startConsumer ? Schedule : null);

        void Schedule()
        {
            try { _ = Task.Run(ConsumePostsAsync); }
            catch (Exception) { _ = ConsumePostsAsync(); }
        }

        async Task ProcessAsync()
        {
            OperationResult<TResult> result;
            try
            {
                result = await Start(name, validate, execute, CancellationToken.None, concurrency, capacity, maxConcurrency, id)
                    .ConfigureAwait(false);
            }
            catch (Exception failure)
            {
                result = new OperationResult<TResult>(name, id, OperationResultKind.Faulted, exception: failure);
            }

            lock (gate) pendingPosts--;
            completion.SetResult(result);
        }
    }

    private async Task ConsumePostsAsync()
    {
        OperationExecutionContext.Current.Value = this;
        while (true)
        {
            PostEnvelope envelope;
            lock (gate)
            {
                if (!inbox.TryDequeue(out PostEnvelope? next))
                {
                    consumingPosts = false;
                    SignalDrained();
                    return;
                }

                envelope = next;
            }

            await envelope.Process().ConfigureAwait(false);
        }
    }

    private void SignalDrained()
    {
        if (closeResult is not null && activeExecutions.Count == 0 && !consumingPosts) drained!.TrySetResult();
    }

    internal RuntimeSnapshot<TState> Snapshot => Volatile.Read(ref snapshot);

    internal bool IsClosed => Volatile.Read(ref closeResult) is not null;

    internal void OwnScope(IAsyncDisposable scope, Task? constructing = null)
    {
        lock (gate)
        {
            if (closeResult is not null || ownedScope is not null)
            {
                throw new InvalidOperationException("实例已经关闭或已拥有服务范围，不能重新绑定范围。");
            }

            ownedScope = scope;
            constructionExited = constructing ?? Task.CompletedTask;
        }
    }

    internal void RegisterPort(Action deactivate)
    {
        bool closed;
        lock (gate)
        {
            closed = closeResult is not null;
            if (!closed) ports.Add(deactivate);
        }

        if (closed) deactivate();
    }

    internal (CloseResult Result, Action Finish) CommitClose(Task<Exception?> childrenReleased)
    {
        CloseResult result;
        Action[] deactivate;
        FeatureProjection<TState>? view;
        List<QueuedOperation> waiting = [];
        PostEnvelope[] posted;
        List<(Operation<TState> Operation, TaskCompletionSource Completed)> cancellation = [];
        List<Exception> failures = [];
        Task exit;
        lock (gate)
        {
            if (reducing) throw new InvalidOperationException("纯状态转换不能关闭所属功能实例。");
            if (closeResult is not null) return (closeResult, static () => { });
            result = new CloseResult(new CloseTicket(this));
            closeResult = result;
            drained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            SignalDrained();
            exit = drained.Task;
            deactivate = ports.ToArray();
            ports.Clear();
            view = projection;
            projection = null;
            posted = inbox.ToArray();
            inbox.Clear();
            pendingPosts -= posted.Length;
            foreach (OperationQueue queue in queues.Values)
            {
                foreach (QueuedOperation pending in queue.Waiting)
                {
                    waiting.Add(pending);
                    Commit(Reduce(snapshot, new QueuedOperationCanceledIntent(pending.Name, pending.Id)));
                }

                queue.Waiting.Clear();
            }

            foreach (Operation<TState> operation in activeExecutions)
            {
                if (!operation.Accepting || operation.CancellationFinished) continue;
                TaskCompletionSource completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
                operation.Work.Add(completed.Task);
                cancellation.Add((operation, completed));
            }
        }

        return (result, Finish);

        void Finish()
        {
            foreach (Action action in deactivate)
            {
                try
                {
                    action();
                }
                catch (Exception failure)
                {
                    failures.Add(failure);
                }
            }

            try
            {
                view?.Dispose();
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }

            foreach (QueuedOperation pending in waiting)
            {
                pending.Registration.Unregister();
                pending.Canceled();
            }

            foreach (PostEnvelope envelope in posted) envelope.Cancel();
            _ = Task.Run(ReleaseAsync);
        }

        async Task ReleaseAsync()
        {
            OperationExecutionContext.Current.Value = null;

            foreach ((Operation<TState> operation, TaskCompletionSource completed) in cancellation)
            {
                _ = CancelSupersededAsync(operation, completed);
            }

            await exit.ConfigureAwait(false);
            await constructionExited.ConfigureAwait(false);
            Exception? childFailure = await childrenReleased.ConfigureAwait(false);
            if (childFailure is not null) failures.Add(childFailure);
            try
            {
                if (ownedScope is not null) await ownedScope.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }

            result.Ticket.Complete(failures.Count == 0 ? null : failures.Count == 1 ? failures[0] : new AggregateException(failures));
        }
    }

    private void CompleteExecution(Operation<TState> operation)
    {
        lock (gate)
        {
            activeExecutions.Remove(operation);
            SignalDrained();
        }
    }

    internal void AttachProjection(FeatureProjection<TState> connection)
    {
        lock (gate)
        {
            if (closeResult is not null) throw new FeatureClosedException();
            if (projection is not null)
            {
                throw new InvalidOperationException("一个功能实例只能连接一个活动的本地 View 投影；释放旧投影后可重新连接。");
            }

            connection.SetInitialSnapshot(snapshot);
            projection = connection;
        }
    }

    internal void DetachProjection(FeatureProjection<TState> connection)
    {
        lock (gate)
        {
            if (ReferenceEquals(projection, connection))
            {
                projection = null;
            }
        }
    }

    internal void Dispatch<TValue>(InputIntent<TState, TValue> intent)
    {
        FeatureProjection<TState>? display;
        lock (gate)
        {
            if (closeResult is not null) throw new FeatureClosedException();
            if (reducing)
            {
                throw new InvalidOperationException("纯状态转换不能重入同一功能实例。");
            }

            reducing = true;
            FeatureOwnership.EnterReduction();
            try
            {
                RuntimeSnapshot<TState> next = Reduce(snapshot, intent);
                display = Commit(next);
            }
            finally
            {
                reducing = false;
                FeatureOwnership.ExitReduction();
            }
        }

        display?.RequestDisplay();
    }

    private static RuntimeSnapshot<TState> Reduce<TValue>(RuntimeSnapshot<TState> current, InputIntent<TState, TValue> intent)
    {
        TState state = intent.Reduce(current.State);
        ArgumentNullException.ThrowIfNull(state);
        return new RuntimeSnapshot<TState>(state, checked(current.Version + 1), current.OperationStates);
    }

    internal Task<OperationResult<TResult>> Start<TResult>(string name, Func<TState, bool>? validate,
        Func<Operation<TState>, ValueTask<TResult>> execute, CancellationToken cancellationToken,
        OperationConcurrency concurrency, int capacity, int maxConcurrency, Guid? admittedId = null)
    {

        OperationReduction<TState, TResult> reduction;
        FeatureProjection<TState>? display;
        Operation<TState>? operation = null;
        Operation<TState>? previous = null;
        TaskCompletionSource? cancellationCompleted = null;
        OperationQueue? queue = null;
        TaskCompletionSource<OperationResult<TResult>>? completion = null;
        QueuedOperation? pending = null;
        Guid id = admittedId ?? Guid.NewGuid();
        (OperationConcurrency Concurrency, int Capacity, int MaxConcurrency) configuration = (concurrency, capacity, maxConcurrency);
        lock (gate)
        {
            if (closeResult is not null)
            {
                return Task.FromResult(new OperationResult<TResult>(name, id, OperationResultKind.Rejected, reason: "Closed"));
            }

            if (reducing)
            {
                throw new InvalidOperationException("纯状态转换不能重入同一功能实例。");
            }

            reducing = true;
            FeatureOwnership.EnterReduction();
            try
            {
                if (concurrency == OperationConcurrency.Queue && !queues.TryGetValue(name, out queue))
                {
                    queue = new OperationQueue();
                    queues.Add(name, queue);
                }

                bool configurationMatches = !configurations.TryGetValue(name,
                    out (OperationConcurrency Concurrency, int Capacity, int MaxConcurrency) existing) || existing == configuration;
                OperationStartIntent<TState, TResult> intent = new(name, id, validate, execute, cancellationToken,
                    concurrency, capacity, maxConcurrency, queue?.Active == true, configurationMatches: configurationMatches);
                reduction = Reduce(snapshot, intent);
                if (reduction.Decision == OperationDecision.ExecuteEffect)
                {
                    // 已接纳的同名入口固定策略与界限，避免等待项缺少推进归属。
                    configurations.TryAdd(name, configuration);
                    CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    operation = new Operation<TState>(this, name, intent.Id, reduction.Effect!.Value.Input,
                        source?.Token ?? cancellationToken, source);
                    if (concurrency == OperationConcurrency.Latest && executions.TryGetValue(name, out previous)
                        && !previous.CancellationFinished)
                    {
                        // 在门内登记屏障，防止旧执行先退出并释放门外取消请求仍需使用的资源。
                        cancellationCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                        previous.Work.Add(cancellationCompleted.Task);
                    }

                    executions[name] = operation;
                    activeExecutions.Add(operation);
                }

                display = Commit(reduction.Snapshot);
                if (reduction.Decision == OperationDecision.Enqueue)
                {
                    completion = new TaskCompletionSource<OperationResult<TResult>>(TaskCreationOptions.RunContinuationsAsynchronously);
                    TaskCompletionSource<OperationResult<TResult>> queuedCompletion = completion;
                    OperationQueue admittedQueue = queue!;
                    pending = new QueuedOperation(name, id, () => ActivateQueued(intent, admittedQueue, queuedCompletion),
                        () => queuedCompletion.SetResult(new OperationResult<TResult>(name, id, OperationResultKind.Canceled)));
                    pending.Node = admittedQueue.Waiting.AddLast(pending);
                }
                else if (queue is not null && reduction.Decision == OperationDecision.ExecuteEffect)
                {
                    queue.Active = true;
                }
            }
            finally
            {
                reducing = false;
                FeatureOwnership.ExitReduction();
            }
        }

        if (previous is not null && cancellationCompleted is not null)
        {
            _ = CancelSupersededAsync(previous, cancellationCompleted!);
        }

        if (pending is not null && cancellationToken.CanBeCanceled)
        {
            RegisterQueuedCancellation(pending, cancellationToken);
        }

        Task<OperationResult<TResult>> execution = reduction.Decision switch
        {
            OperationDecision.Enqueue => completion!.Task,
            OperationDecision.ReturnResult => Task.FromResult(reduction.Result!),
            _ => queue is null ? RunEffect(operation!, execute) : RunQueueEffect(operation!, execute, queue),
        };
        RequestOperationDisplay(display, id);
        return execution;
    }

    private void RegisterQueuedCancellation(QueuedOperation pending, CancellationToken cancellationToken)
    {
        // Cancel 可由纯规则调用，回调仅安排工作，避免重入提交或等待持门线程。
        CancellationTokenRegistration registration = cancellationToken.UnsafeRegister(static state =>
        {
            Action cancel = (Action)state!;
            ThreadPool.QueueUserWorkItem(static callback => callback(), cancel, preferLocal: false);
        }, (Action)(() => CancelQueued(pending)));
        bool removed;
        lock (gate)
        {
            pending.Registration = registration;
            removed = pending.Node?.List is null;
        }

        if (removed)
        {
            registration.Unregister();
        }
    }

    private void CancelQueued(QueuedOperation pending)
    {
        FeatureProjection<TState>? display;
        CancellationTokenRegistration registration;
        lock (gate)
        {
            if (pending.Node?.List is not LinkedList<QueuedOperation> waiting)
            {
                return;
            }

            waiting.Remove(pending.Node);
            display = Commit(Reduce(snapshot, new QueuedOperationCanceledIntent(pending.Name, pending.Id)));
            registration = pending.Registration;
        }

        registration.Unregister();
        pending.Canceled();
        RequestOperationDisplay(display, pending.Id);
    }

    private static RuntimeSnapshot<TState> Reduce(RuntimeSnapshot<TState> current, QueuedOperationCanceledIntent intent)
    {
        OperationState state = current.OperationStates[intent.Name];
        return WithOperationState(current, intent.Name,
            new OperationState(state.RunningIds, intent.Id, OperationResultKind.Canceled, queuedCount: state.QueuedCount - 1));
    }

    private Action ActivateQueued<TResult>(OperationStartIntent<TState, TResult> request, OperationQueue queue,
        TaskCompletionSource<OperationResult<TResult>> completion)
    {
        if (closeResult is not null)
        {
            return () => completion.SetResult(new OperationResult<TResult>(request.Name, request.Id, OperationResultKind.Canceled,
                reason: "Closed"));
        }

        // 只在实际启动时重新采样取消、校验与业务输入；调用者持有提交门。
        OperationStartIntent<TState, TResult> intent = new(request.Name, request.Id, request.Validate, request.Execute,
            request.CancellationToken, request.Concurrency, request.Capacity, request.MaxConcurrency, fromQueue: true);
        OperationReduction<TState, TResult> reduction = Reduce(snapshot, intent);
        Operation<TState>? operation = null;
        if (reduction.Decision == OperationDecision.ExecuteEffect)
        {
            CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(request.CancellationToken);
            operation = new Operation<TState>(this, request.Name, request.Id, reduction.Effect!.Value.Input, source.Token, source);
            executions[request.Name] = operation;
            activeExecutions.Add(operation);
        }

        FeatureProjection<TState>? display = Commit(reduction.Snapshot);
        return () =>
        {
            RequestOperationDisplay(display, request.Id);
            if (reduction.Decision == OperationDecision.ReturnResult)
            {
                completion.SetResult(reduction.Result!);
                // 连续拒绝或故障不能递归整个有界队列。
                ThreadPool.QueueUserWorkItem(_ => AdvanceQueue(queue));
            }
            else
            {
                _ = CompleteQueuedAsync();
            }

            async Task CompleteQueuedAsync()
            {
                OperationResult<TResult> result = await RunQueueEffect(operation!, request.Execute, queue).ConfigureAwait(false);
                completion.SetResult(result);
            }
        };
    }

    private async Task<OperationResult<TResult>> RunQueueEffect<TResult>(Operation<TState> operation,
        Func<Operation<TState>, ValueTask<TResult>> execute, OperationQueue queue)
    {
        OperationResult<TResult> result = await RunEffect(operation, execute).ConfigureAwait(false);
        ThreadPool.QueueUserWorkItem(_ => AdvanceQueue(queue));
        return result;
    }

    private void AdvanceQueue(OperationQueue queue)
    {
        Action start;
        CancellationTokenRegistration registration;
        lock (gate)
        {
            if (closeResult is not null || queue.Waiting.First is not LinkedListNode<QueuedOperation> next)
            {
                queue.Active = false;
                return;
            }

            queue.Waiting.RemoveFirst();
            registration = next.Value.Registration;
            reducing = true;
            FeatureOwnership.EnterReduction();
            try
            {
                start = next.Value.Start();
            }
            finally
            {
                reducing = false;
                FeatureOwnership.ExitReduction();
            }
        }

        registration.Unregister();
        start();
    }

    private static OperationReduction<TState, TResult> Reduce<TResult>(RuntimeSnapshot<TState> current,
        OperationStartIntent<TState, TResult> intent)
    {
        OperationState? operationState = current.OperationStates.GetValueOrDefault(intent.Name);
        ImmutableList<Guid> runningIds = operationState?.RunningIds ?? ImmutableList<Guid>.Empty;
        int queuedCount = (operationState?.QueuedCount ?? 0) - (intent.FromQueue ? 1 : 0);
        if (!Enum.IsDefined(intent.Concurrency)
            || (intent.Concurrency == OperationConcurrency.Queue ? intent.Capacity <= 0 : intent.Capacity != 0)
            || (intent.Concurrency == OperationConcurrency.Parallel ? intent.MaxConcurrency <= 0 : intent.MaxConcurrency != 0))
        {
            RuntimeSnapshot<TState> next = WithOperationState(current, intent.Name,
                new OperationState(runningIds, intent.Id, OperationResultKind.Rejected, "InvalidConcurrency", queuedCount: queuedCount));
            return new OperationReduction<TState, TResult>(next,
                new OperationResult<TResult>(intent.Name, intent.Id, OperationResultKind.Rejected, reason: "InvalidConcurrency"));
        }

        if (!intent.ConfigurationMatches)
        {
            RuntimeSnapshot<TState> next = WithOperationState(current, intent.Name,
                new OperationState(runningIds, intent.Id, OperationResultKind.Rejected, "OperationConfigurationMismatch", queuedCount: queuedCount));
            return new OperationReduction<TState, TResult>(next,
                new OperationResult<TResult>(intent.Name, intent.Id, OperationResultKind.Rejected, reason: "OperationConfigurationMismatch"));
        }

        if (intent.CancellationRequested)
        {
            RuntimeSnapshot<TState> next = WithOperationState(current, intent.Name,
                new OperationState(runningIds, intent.Id, OperationResultKind.Canceled, queuedCount: queuedCount));
            return new OperationReduction<TState, TResult>(next,
                new OperationResult<TResult>(intent.Name, intent.Id, OperationResultKind.Canceled));
        }

        if (!intent.FromQueue && (intent.Concurrency == OperationConcurrency.Parallel
            ? runningIds.Count >= intent.MaxConcurrency
            : intent.Concurrency != OperationConcurrency.Latest && (!runningIds.IsEmpty || intent.QueueActive)))
        {
            if (intent.Concurrency == OperationConcurrency.Queue && queuedCount < intent.Capacity)
            {
                RuntimeSnapshot<TState> queued = WithOperationState(current, intent.Name,
                    new OperationState(runningIds, intent.Id, null, queuedCount: queuedCount + 1));
                return new OperationReduction<TState, TResult>(queued, queued: true);
            }

            string reason = intent.Concurrency == OperationConcurrency.Queue ? "QueueFull"
                : intent.Concurrency == OperationConcurrency.Parallel ? "ConcurrencyLimitReached" : "AlreadyRunning";
            RuntimeSnapshot<TState> next = WithOperationState(current, intent.Name,
                new OperationState(runningIds, intent.Id, OperationResultKind.Rejected, reason, queuedCount: queuedCount));
            return new OperationReduction<TState, TResult>(next,
                new OperationResult<TResult>(intent.Name, intent.Id, OperationResultKind.Rejected, reason: reason));
        }

        bool valid;
        try
        {
            valid = intent.Validate?.Invoke(current.State) ?? true;
        }
        catch (Exception exception)
        {
            RuntimeSnapshot<TState> next = intent.FromQueue ? WithOperationState(current, intent.Name,
                new OperationState(runningIds, intent.Id, OperationResultKind.Faulted, "ValidationFault", exception, queuedCount)) : current;
            return new OperationReduction<TState, TResult>(next,
                new OperationResult<TResult>(intent.Name, intent.Id, OperationResultKind.Faulted,
                    reason: "ValidationFault", exception: exception));
        }

        if (!valid)
        {
            RuntimeSnapshot<TState> next = WithOperationState(current, intent.Name,
                new OperationState(runningIds, intent.Id, OperationResultKind.Rejected, "ValidationFailed", queuedCount: queuedCount));
            return new OperationReduction<TState, TResult>(next,
                new OperationResult<TResult>(intent.Name, intent.Id, OperationResultKind.Rejected, reason: "ValidationFailed"));
        }

        RuntimeSnapshot<TState> started = WithOperationState(current, intent.Name,
            new OperationState(intent.Concurrency == OperationConcurrency.Latest ? ImmutableList.Create(intent.Id) : runningIds.Add(intent.Id),
                intent.Id, null, queuedCount: queuedCount));
        return new OperationReduction<TState, TResult>(started, effect: new OperationEffect<TState, TResult>(intent, current.State));
    }

    private async Task CancelSupersededAsync(Operation<TState> operation, TaskCompletionSource completed)
    {
        try
        {
            await operation.CancellationSource!.CancelAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            RecordFailure(operation, exception);
        }
        finally
        {
            completed.SetResult();
        }
    }

    private Task<OperationResult<TResult>> RunEffect<TResult>(Operation<TState> operation, Func<Operation<TState>, ValueTask<TResult>> execute)
    {
        try
        {
            return Task.Run(() => ExecuteAsync(operation, execute));
        }
        catch (Exception exception)
        {
            RecordFailure(operation, exception);
            return ExecuteAsync(operation, _ => ValueTask.FromException<TResult>(exception));
        }
    }

    internal void Dispatch<TPayload>(FeedbackIntent<TState, TPayload> intent)
    {
        FeatureProjection<TState>? display;
        lock (gate)
        {
            if (reducing)
            {
                throw new InvalidOperationException("纯状态转换不能重入同一功能实例。");
            }

            EnsureActive(intent.Operation);
            reducing = true;
            FeatureOwnership.EnterReduction();
            try
            {
                RuntimeSnapshot<TState> next = Reduce(snapshot, intent);
                EnsureActive(intent.Operation);
                display = Commit(next);
            }
            finally
            {
                reducing = false;
                FeatureOwnership.ExitReduction();
            }
        }

        RequestOperationDisplay(display, intent.Operation.Id);
    }

    private static RuntimeSnapshot<TState> Reduce<TPayload>(RuntimeSnapshot<TState> current, FeedbackIntent<TState, TPayload> intent)
    {
        TState state = intent.Reduce(current.State);
        ArgumentNullException.ThrowIfNull(state);
        return new RuntimeSnapshot<TState>(state, checked(current.Version + 1), current.OperationStates);
    }

    private void EnsureActive(Operation<TState> operation)
    {
        EnsureOwned(operation);
        if (closeResult is not null) throw new FeatureClosedException();
        if (snapshot.OperationStates.GetValueOrDefault(operation.Name)?.RunningIds.Contains(operation.Id) != true)
        {
            throw new OperationSupersededException();
        }

        operation.CancellationToken.ThrowIfCancellationRequested();
        if (operation.Failure is not null)
        {
            throw new InvalidOperationException("操作已发生故障，不能继续提交反馈。", operation.Failure);
        }
    }

    private void EnsureOwned(Operation<TState> operation)
    {
        if (!operation.Accepting)
        {
            throw new InvalidOperationException("操作上下文已经结束，不能继续提交反馈。");
        }
    }

    internal void Track(Operation<TState> operation, Task task)
    {
        lock (gate)
        {
            if (reducing)
            {
                throw new InvalidOperationException("纯状态转换不能登记操作子工作。");
            }

            EnsureOwned(operation);
            operation.Work.Add(task);
        }
    }

    internal void RecordFailure(Operation<TState> operation, Exception exception)
    {
        lock (gate)
        {
            // 令牌关联关系不可查询；执行已取消时，关联令牌产生的取消异常也属于协作取消。
            if (exception is not OperationSupersededException && exception is not FeatureClosedException
                && (exception is not OperationCanceledException || !operation.CancellationToken.IsCancellationRequested))
            {
                operation.Failure ??= exception;
            }
        }
    }

    private FeatureProjection<TState>? Commit(RuntimeSnapshot<TState> next)
    {
        if (ReferenceEquals(snapshot, next))
        {
            return null;
        }

        Volatile.Write(ref snapshot, next);
        FeatureProjection<TState>? display = projection;
        display?.Enqueue(next);
        return display;
    }

    private void RequestOperationDisplay(FeatureProjection<TState>? display, Guid operationId)
    {
        try
        {
            display?.RequestDisplay();
        }
        catch (Exception exception)
        {
            try
            {
                System.Diagnostics.Trace.TraceError("MVI operation display failed: ExceptionType={0}; SnapshotVersion={1}; OperationId={2}",
                    exception.GetType().FullName, Snapshot.Version, operationId);
            }
            catch (Exception)
            {
                // 宿主诊断回调失败也不得改变已经提交的操作结果。
            }
        }
    }

    private static RuntimeSnapshot<TState> WithOperationState(RuntimeSnapshot<TState> current, string name, OperationState state)
        => new(current.State, checked(current.Version + 1), current.OperationStates.SetItem(name, state));

    private async Task<OperationResult<TResult>> ExecuteAsync<TResult>(Operation<TState> operation,
        Func<Operation<TState>, ValueTask<TResult>> execute)
    {
        object? previous = OperationExecutionContext.Current.Value;
        OperationExecutionContext.Current.Value = this;
        try
        {
            return await ExecuteCoreAsync(operation, execute).ConfigureAwait(false);
        }
        finally
        {
            OperationExecutionContext.Current.Value = previous;
            CompleteExecution(operation);
        }
    }

    private async Task<OperationResult<TResult>> ExecuteCoreAsync<TResult>(Operation<TState> operation,
        Func<Operation<TState>, ValueTask<TResult>> execute)
    {
        TResult? value = default;
        try
        {
            operation.CancellationToken.ThrowIfCancellationRequested();
            value = await execute(operation).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            RecordFailure(operation, exception);
        }

        int observed = 0;
        while (true)
        {
            Task[] work;
            OperationResult<TResult>? completed = null;
            FeatureProjection<TState>? display = null;
            bool finishCancellation = false;
            lock (gate)
            {
                if (observed == operation.Work.Count)
                {
                    if (!operation.CancellationFinished)
                    {
                        // 先断开并等待已进入的取消回调；它们仍可登记子工作，下一轮重新排空。
                        operation.CancellationFinished = true;
                        finishCancellation = true;
                    }
                    else
                    {
                        completed = Finish(operation, value, out display);
                    }

                    work = [];
                }
                else
                {
                    work = operation.Work.Skip(observed).ToArray();
                    observed = operation.Work.Count;
                }
            }

            if (finishCancellation)
            {
                if (operation.CancellationToken.IsCancellationRequested)
                {
                    // 取消续体可能内联在回调线程；切换后注销才能真实等回调尾部退出。
                    await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
                }

                operation.CancellationSource!.Dispose();
                continue;
            }

            if (completed is not null)
            {
                RequestOperationDisplay(display, completed.OperationId);
                return completed;
            }

            try
            {
                await Task.WhenAll(work).ConfigureAwait(false);
            }
            catch (Exception)
            {
                foreach (Task task in work)
                {
                    if (task.Exception is AggregateException faults)
                    {
                        foreach (Exception exception in faults.Flatten().InnerExceptions)
                        {
                            RecordFailure(operation, exception);
                        }

                        continue;
                    }

                    try
                    {
                        task.GetAwaiter().GetResult();
                    }
                    catch (Exception exception)
                    {
                        RecordFailure(operation, exception);
                    }
                }
            }
        }
    }

    private OperationResult<TResult> Finish<TResult>(Operation<TState> operation, TResult? value, out FeatureProjection<TState>? display)
    {
        OperationCompletedIntent<TResult> intent = new(operation.Name, operation.Id, value,
            operation.CancellationToken.IsCancellationRequested, operation.Failure);
        OperationReduction<TState, TResult> reduction = Reduce(snapshot, intent);
        operation.Accepting = false;
        operation.Work.Clear();
        if (executions.GetValueOrDefault(operation.Name)?.Id == operation.Id)
        {
            executions.Remove(operation.Name);
        }

        display = Commit(reduction.Snapshot);
        return reduction.Result!;
    }

    private static OperationReduction<TState, TResult> Reduce<TResult>(RuntimeSnapshot<TState> current, OperationCompletedIntent<TResult> intent)
    {
        OperationState? operationState = current.OperationStates.GetValueOrDefault(intent.Name);
        if (operationState?.RunningIds.Contains(intent.Id) != true)
        {
            return new OperationReduction<TState, TResult>(current,
                new OperationResult<TResult>(intent.Name, intent.Id, OperationResultKind.Superseded, reason: "Superseded",
                    exception: intent.Failure));
        }

        OperationResultKind kind = intent.Failure is not null ? OperationResultKind.Faulted
            : intent.Canceled ? OperationResultKind.Canceled : OperationResultKind.Completed;
        RuntimeSnapshot<TState> next = WithOperationState(current, intent.Name,
            new OperationState(operationState.RunningIds.Remove(intent.Id), intent.Id, kind, exception: intent.Failure,
                queuedCount: operationState.QueuedCount));
        return new OperationReduction<TState, TResult>(next, new OperationResult<TResult>(intent.Name, intent.Id, kind,
            kind == OperationResultKind.Completed ? intent.Value : default, exception: intent.Failure));
    }
}
