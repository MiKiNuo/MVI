namespace MiKiNuo.Mvi;

/// <summary>为一个独立功能实例提供统一状态输入入口。</summary>
/// <typeparam name="TState">由生成器验证的不可变业务状态类型。</typeparam>
public abstract class Feature<TState> where TState : notnull
{
    private readonly FeatureStore<TState> store;

    /// <summary>使用初始业务状态创建独立实例。</summary>
    /// <param name="initialState">实例的初始不可变状态。</param>
    protected Feature(TState initialState)
    {
        ArgumentNullException.ThrowIfNull(initialState);
        store = new FeatureStore<TState>(initialState);
    }

    /// <summary>获取一次一致提交的完整快照。</summary>
    public RuntimeSnapshot<TState> Snapshot => store.Snapshot;

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
    /// <param name="name">声明的业务操作名称。</param>
    /// <param name="validate">启动时的纯验证方法。</param>
    /// <param name="execute">在提交门外执行的业务方法。</param>
    /// <param name="cancellationToken">本次执行的协作取消令牌。</param>
    /// <param name="concurrency">同名操作的并发准入规则。</param>
    /// <returns>业务方法退出且有关状态提交后的结构化结果。</returns>
    protected Task<OperationResult<TResult>> DispatchOperation<TResult>(string name, Func<TState, bool>? validate,
        Func<Operation<TState>, ValueTask<TResult>> execute, CancellationToken cancellationToken,
        OperationConcurrency concurrency = OperationConcurrency.Reject)
        => store.Start(name, validate, execute, cancellationToken, concurrency);
}

internal readonly struct InputIntent<TState, TValue>(TValue value, Func<TState, TValue, TState> reduce)
{
    internal TState Reduce(TState state) => reduce(state, value);
}

/// <summary>描述启动请求及在入口采样的取消条件。</summary>
internal readonly struct OperationStartIntent<TState, TResult>(string name, Guid id, Func<TState, bool>? validate,
    Func<Operation<TState>, ValueTask<TResult>> execute, CancellationToken cancellationToken, OperationConcurrency concurrency) where TState : notnull
{
    internal string Name { get; } = name;
    internal Guid Id { get; } = id;
    internal Func<TState, bool>? Validate { get; } = validate;
    internal Func<Operation<TState>, ValueTask<TResult>> Execute { get; } = execute;
    internal CancellationToken CancellationToken { get; } = cancellationToken;
    internal bool CancellationRequested { get; } = cancellationToken.IsCancellationRequested;
    internal OperationConcurrency Concurrency { get; } = concurrency;
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

internal enum OperationDecision
{
    ExecuteEffect,
    ReturnResult,
}

/// <summary>保存纯转换计算的下一快照、调用决定与门外副作用描述。</summary>
internal readonly struct OperationReduction<TState, TResult>(RuntimeSnapshot<TState> snapshot,
    OperationResult<TResult>? result = null, OperationEffect<TState, TResult>? effect = null) where TState : notnull
{
    internal RuntimeSnapshot<TState> Snapshot { get; } = snapshot;
    internal OperationResult<TResult>? Result { get; } = result;
    internal OperationEffect<TState, TResult>? Effect { get; } = effect;
    internal OperationDecision Decision => Effect.HasValue ? OperationDecision.ExecuteEffect : OperationDecision.ReturnResult;
}

internal sealed class FeatureStore<TState> where TState : notnull
{
    private readonly object gate = new();
    private RuntimeSnapshot<TState> snapshot;
    private bool reducing;
    private FeatureProjection<TState>? projection;
    private readonly Dictionary<string, Operation<TState>> executions = new(StringComparer.Ordinal);

    internal FeatureStore(TState state) => snapshot = new RuntimeSnapshot<TState>(state, 0);

    internal RuntimeSnapshot<TState> Snapshot => Volatile.Read(ref snapshot);

    internal void AttachProjection(FeatureProjection<TState> connection)
    {
        lock (gate)
        {
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
            if (reducing)
            {
                throw new InvalidOperationException("纯状态转换不能重入同一功能实例。");
            }

            reducing = true;
            try
            {
                RuntimeSnapshot<TState> next = Reduce(snapshot, intent);
                display = Commit(next);
            }
            finally
            {
                reducing = false;
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
        Func<Operation<TState>, ValueTask<TResult>> execute, CancellationToken cancellationToken, OperationConcurrency concurrency)
    {
        if (!Enum.IsDefined(concurrency))
        {
            throw new ArgumentOutOfRangeException(nameof(concurrency));
        }

        OperationReduction<TState, TResult> reduction;
        FeatureProjection<TState>? display;
        Operation<TState>? operation = null;
        Operation<TState>? previous = null;
        TaskCompletionSource? cancellationCompleted = null;
        lock (gate)
        {
            if (reducing)
            {
                throw new InvalidOperationException("纯状态转换不能重入同一功能实例。");
            }

            reducing = true;
            try
            {
                OperationStartIntent<TState, TResult> intent = new(name, Guid.NewGuid(), validate, execute, cancellationToken, concurrency);
                reduction = Reduce(snapshot, intent);
                if (reduction.Decision == OperationDecision.ExecuteEffect)
                {
                    CancellationTokenSource? source = concurrency == OperationConcurrency.Latest
                        ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken) : null;
                    operation = new Operation<TState>(this, name, intent.Id, reduction.Effect!.Value.Input,
                        source?.Token ?? cancellationToken, source);
                    if (executions.TryGetValue(name, out previous))
                    {
                        // 在门内登记屏障，防止旧执行先退出并释放门外取消请求仍需使用的资源。
                        cancellationCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                        previous.Work.Add(cancellationCompleted.Task);
                    }

                    executions[name] = operation;
                }

                display = Commit(reduction.Snapshot);
            }
            finally
            {
                reducing = false;
            }
        }

        if (previous is not null)
        {
            _ = CancelSupersededAsync(previous, cancellationCompleted!);
        }

        Task<OperationResult<TResult>> execution = reduction.Decision == OperationDecision.ReturnResult
            ? Task.FromResult(reduction.Result!) : RunEffect(operation!, reduction.Effect!.Value.Request.Execute);
        RequestOperationDisplay(display, reduction.Result?.OperationId ?? reduction.Effect!.Value.Request.Id);
        return execution;
    }

    private static OperationReduction<TState, TResult> Reduce<TResult>(RuntimeSnapshot<TState> current,
        OperationStartIntent<TState, TResult> intent)
    {
        OperationState? operationState = current.OperationStates.GetValueOrDefault(intent.Name);
        if (intent.CancellationRequested)
        {
            RuntimeSnapshot<TState> next = WithOperationState(current, intent.Name,
                new OperationState(operationState?.RunningId, intent.Id, OperationResultKind.Canceled));
            return new OperationReduction<TState, TResult>(next,
                new OperationResult<TResult>(intent.Name, intent.Id, OperationResultKind.Canceled));
        }

        if (operationState?.IsRunning == true && intent.Concurrency == OperationConcurrency.Reject)
        {
            RuntimeSnapshot<TState> next = WithOperationState(current, intent.Name,
                new OperationState(operationState.RunningId, intent.Id, OperationResultKind.Rejected, "AlreadyRunning"));
            return new OperationReduction<TState, TResult>(next,
                new OperationResult<TResult>(intent.Name, intent.Id, OperationResultKind.Rejected, reason: "AlreadyRunning"));
        }

        bool valid;
        try
        {
            valid = intent.Validate?.Invoke(current.State) ?? true;
        }
        catch (Exception exception)
        {
            return new OperationReduction<TState, TResult>(current,
                new OperationResult<TResult>(intent.Name, intent.Id, OperationResultKind.Faulted,
                    reason: "ValidationFault", exception: exception));
        }

        if (!valid)
        {
            RuntimeSnapshot<TState> next = WithOperationState(current, intent.Name,
                new OperationState(operationState?.RunningId, intent.Id, OperationResultKind.Rejected, "ValidationFailed"));
            return new OperationReduction<TState, TResult>(next,
                new OperationResult<TResult>(intent.Name, intent.Id, OperationResultKind.Rejected, reason: "ValidationFailed"));
        }

        RuntimeSnapshot<TState> started = WithOperationState(current, intent.Name, new OperationState(intent.Id, intent.Id, null));
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
            OperationResult<TResult> result;
            FeatureProjection<TState>? display;
            lock (gate)
            {
                result = Finish<TResult>(operation, default, out display);
            }

            operation.CancellationSource?.Dispose();
            RequestOperationDisplay(display, result.OperationId);
            return Task.FromResult(result);
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
            try
            {
                RuntimeSnapshot<TState> next = Reduce(snapshot, intent);
                EnsureActive(intent.Operation);
                display = Commit(next);
            }
            finally
            {
                reducing = false;
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
        if (snapshot.OperationStates.GetValueOrDefault(operation.Name)?.RunningId != operation.Id)
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
            if (exception is not OperationSupersededException
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
            lock (gate)
            {
                if (observed == operation.Work.Count)
                {
                    completed = Finish(operation, value, out display);
                    work = [];
                }
                else
                {
                    work = operation.Work.Skip(observed).ToArray();
                    observed = operation.Work.Count;
                }
            }

            if (completed is not null)
            {
                operation.CancellationSource?.Dispose();
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
        if (current.OperationStates.GetValueOrDefault(intent.Name)?.RunningId != intent.Id)
        {
            return new OperationReduction<TState, TResult>(current,
                new OperationResult<TResult>(intent.Name, intent.Id, OperationResultKind.Superseded, reason: "Superseded",
                    exception: intent.Failure));
        }

        OperationResultKind kind = intent.Failure is not null ? OperationResultKind.Faulted
            : intent.Canceled ? OperationResultKind.Canceled : OperationResultKind.Completed;
        RuntimeSnapshot<TState> next = WithOperationState(current, intent.Name, new OperationState(null, intent.Id, kind, exception: intent.Failure));
        return new OperationReduction<TState, TResult>(next, new OperationResult<TResult>(intent.Name, intent.Id, kind,
            kind == OperationResultKind.Completed ? intent.Value : default, exception: intent.Failure));
    }
}
