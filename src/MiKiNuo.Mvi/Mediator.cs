namespace MiKiNuo.Mvi;

/// <summary>表示请求的路由或等待结论，与目标操作执行结果分别表达。</summary>
public enum RequestResultKind
{
    /// <summary>目标已返回原始操作结果，包括完成、拒绝、取消和故障。</summary>
    Responded,
    /// <summary>范围内没有提供该请求与返回值契约的目标。</summary>
    MissingTarget,
    /// <summary>范围内有多个契约目标，需要宿主明确选择端口。</summary>
    AmbiguousTarget,
    /// <summary>指定端口未在当前范围接线，或目标端口已经停止接纳。</summary>
    TargetUnavailable,
    /// <summary>调用方已取消接纳前的请求或接纳后的等待，不证明目标执行停止。</summary>
    WaitCanceled,
}

/// <summary>保存请求的等待结论和未经改写的目标操作结果。</summary>
/// <typeparam name="TResult">业务契约的返回值类型。</typeparam>
public sealed class RequestResult<TResult>
{
    internal RequestResult(RequestResultKind kind, OperationResult<TResult>? operationResult = null)
    {
        Kind = kind;
        OperationResult = operationResult;
    }

    /// <summary>获取本次路由或等待的结论。</summary>
    public RequestResultKind Kind { get; }

    /// <summary>获取目标返回的原始执行结果，仅在已响应时存在；业务失败值仍可正常完成。</summary>
    public OperationResult<TResult>? OperationResult { get; }
}

/// <summary>提供隐藏具体功能和状态类型的独立实例请求入口。</summary>
/// <typeparam name="TRequest">业务契约的不可变请求类型。</typeparam>
/// <typeparam name="TResult">业务契约的返回值类型。</typeparam>
public sealed class RequestPort<TRequest, TResult> where TRequest : notnull
{
    private readonly object gate = new();
    private readonly Func<TRequest, Task<OperationResult<TResult>>> start;
    private bool accepting = true;

    internal RequestPort(Func<TRequest, Task<OperationResult<TResult>>> start) => this.start = start;

    /// <summary>永久停止本端口后续接纳；与接纳原子排序，已接纳的目标执行及其状态反馈继续完成。</summary>
    public void Deactivate()
    {
        lock (gate)
        {
            accepting = false;
        }
    }

    internal (RequestResultKind Kind, Task<OperationResult<TResult>>? Execution) Admit(TRequest request,
        CancellationToken waitCancellationToken)
    {
        lock (gate)
        {
            if (waitCancellationToken.IsCancellationRequested)
            {
                return (RequestResultKind.WaitCanceled, null);
            }

            if (!accepting)
            {
                return (RequestResultKind.TargetUnavailable, null);
            }
        }

        // 接纳决定已经完成；后续停用不能撤销该请求，启动及平台回调在门外执行。
        return (RequestResultKind.Responded, start(request));
    }
}

/// <summary>在宿主显式建立的契约范围内选择独立实例并等待其处理完成。</summary>
public sealed class Mediator
{
    private readonly object gate = new();
    private readonly Dictionary<(Type Request, Type Result), List<Registration>> registrations = [];

    /// <summary>将端口接入本范围；已登记端口组成候选集，同一端口重复接线返回同一回执，跨范围由宿主分别接线。</summary>
    /// <typeparam name="TRequest">业务契约的不可变请求类型。</typeparam>
    /// <typeparam name="TResult">业务契约的返回值类型。</typeparam>
    /// <param name="port">由目标功能创建的契约端口。</param>
    /// <returns>供宿主解除本范围路由的回执；解除阻止后续目标选择，不取消已经选定的请求。</returns>
    public IDisposable Register<TRequest, TResult>(RequestPort<TRequest, TResult> port) where TRequest : notnull
    {
        ArgumentNullException.ThrowIfNull(port);
        (Type Request, Type Result) contract = (typeof(TRequest), typeof(TResult));
        lock (gate)
        {
            if (!registrations.TryGetValue(contract, out List<Registration>? candidates))
            {
                candidates = [];
                registrations.Add(contract, candidates);
            }

            Registration? existing = candidates.Find(candidate => ReferenceEquals(candidate.Port, port));
            if (existing is not null)
            {
                return existing;
            }

            Registration registration = new(this, contract, port);
            candidates.Add(registration);
            return registration;
        }
    }

    /// <summary>请求范围内唯一的契约目标，等待其业务方法、所属工作及有关状态提交完成。</summary>
    /// <typeparam name="TRequest">业务契约的不可变请求类型。</typeparam>
    /// <typeparam name="TResult">业务契约的返回值类型。</typeparam>
    /// <param name="request">本次不可变业务请求。</param>
    /// <param name="waitCancellationToken">只控制请求接纳前取消和调用方等待，不传播到已接纳的目标执行。</param>
    /// <returns>路由或等待结论，以及目标返回的原始操作结果。</returns>
    public Task<RequestResult<TResult>> SendAsync<TRequest, TResult>(TRequest request,
        CancellationToken waitCancellationToken = default) where TRequest : notnull
        => SendCoreAsync<TRequest, TResult>(request, null, waitCancellationToken);

    /// <summary>请求宿主明确选择且已在本范围接线的端口，等待目标处理和有关状态提交完成。</summary>
    /// <typeparam name="TRequest">业务契约的不可变请求类型。</typeparam>
    /// <typeparam name="TResult">业务契约的返回值类型。</typeparam>
    /// <param name="request">本次不可变业务请求。</param>
    /// <param name="target">宿主为发送者选择的强类型目标端口。</param>
    /// <param name="waitCancellationToken">只控制请求接纳前取消和调用方等待，不传播到已接纳的目标执行。</param>
    /// <returns>路由或等待结论，以及目标返回的原始操作结果。</returns>
    public Task<RequestResult<TResult>> SendAsync<TRequest, TResult>(TRequest request, RequestPort<TRequest, TResult> target,
        CancellationToken waitCancellationToken = default) where TRequest : notnull
    {
        ArgumentNullException.ThrowIfNull(target);
        return SendCoreAsync(request, target, waitCancellationToken);
    }

    private async Task<RequestResult<TResult>> SendCoreAsync<TRequest, TResult>(TRequest request,
        RequestPort<TRequest, TResult>? target, CancellationToken waitCancellationToken) where TRequest : notnull
    {
        ArgumentNullException.ThrowIfNull(request);
        if (waitCancellationToken.IsCancellationRequested)
        {
            return new RequestResult<TResult>(RequestResultKind.WaitCanceled);
        }

        RequestResultKind route = SelectTarget(ref target);
        if (route != RequestResultKind.Responded)
        {
            return new RequestResult<TResult>(route);
        }

        (RequestResultKind kind, Task<OperationResult<TResult>>? execution) = target!.Admit(request, waitCancellationToken);
        if (execution is null)
        {
            return new RequestResult<TResult>(kind);
        }

        try
        {
            OperationResult<TResult> response = await execution.WaitAsync(waitCancellationToken).ConfigureAwait(false);
            return new RequestResult<TResult>(RequestResultKind.Responded, response);
        }
        catch (OperationCanceledException) when (waitCancellationToken.IsCancellationRequested)
        {
            return new RequestResult<TResult>(RequestResultKind.WaitCanceled);
        }
    }

    private RequestResultKind SelectTarget<TRequest, TResult>(ref RequestPort<TRequest, TResult>? target) where TRequest : notnull
    {
        lock (gate)
        {
            if (!registrations.TryGetValue((typeof(TRequest), typeof(TResult)), out List<Registration>? candidates))
            {
                return target is null ? RequestResultKind.MissingTarget : RequestResultKind.TargetUnavailable;
            }

            if (target is not null)
            {
                RequestPort<TRequest, TResult> selected = target;
                return candidates.Exists(candidate => ReferenceEquals(candidate.Port, selected))
                    ? RequestResultKind.Responded : RequestResultKind.TargetUnavailable;
            }

            if (candidates.Count != 1)
            {
                return RequestResultKind.AmbiguousTarget;
            }

            target = (RequestPort<TRequest, TResult>)candidates[0].Port;
            return RequestResultKind.Responded;
        }
    }

    private void Remove(Registration registration)
    {
        lock (gate)
        {
            if (registrations.TryGetValue(registration.Contract, out List<Registration>? candidates)
                && candidates.Remove(registration) && candidates.Count == 0)
            {
                registrations.Remove(registration.Contract);
            }
        }
    }

    private sealed class Registration(Mediator owner, (Type Request, Type Result) contract, object port) : IDisposable
    {
        internal (Type Request, Type Result) Contract { get; } = contract;

        internal object Port { get; } = port;

        public void Dispose() => owner.Remove(this);
    }
}
