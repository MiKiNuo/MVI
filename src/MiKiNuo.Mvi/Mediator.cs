namespace MiKiNuo.Mvi;

/// <summary>声明契约端口是否接受调用方显式提供的目标执行取消令牌。</summary>
public enum RequestCancellationPolicy
{
    /// <summary>接纳后由目标管理执行，忽略发送方提供的执行取消令牌。</summary>
    TargetOwned,
    /// <summary>将发送方显式提供的执行取消令牌用于目标协作取消，不自动传播等待令牌。</summary>
    Propagate,
}

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
    private readonly Func<TRequest, CancellationToken, Task<OperationResult<TResult>>> start;
    private readonly Func<TRequest, Guid, (PostReceipt<TResult> Receipt, Action? Schedule)> post;
    private bool accepting = true;

    internal RequestPort(Func<TRequest, CancellationToken, Task<OperationResult<TResult>>> start,
        Func<TRequest, Guid, (PostReceipt<TResult> Receipt, Action? Schedule)> post)
    {
        this.start = start;
        this.post = post;
    }

    /// <summary>永久停止本端口后续接纳；与接纳原子排序，已接纳的目标执行及其状态反馈继续完成。</summary>
    public void Deactivate()
    {
        lock (gate)
        {
            accepting = false;
        }
    }

    internal (RequestResultKind Kind, Task<OperationResult<TResult>>? Execution) Admit(TRequest request,
        CancellationToken waitCancellationToken, CancellationToken executionCancellationToken)
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
        return (RequestResultKind.Responded, start(request, executionCancellationToken));
    }

    internal PostReceipt<TResult> Post(TRequest message, Guid id)
    {
        PostReceipt<TResult> receipt;
        Action? schedule;
        lock (gate)
        {
            if (!accepting) return new PostReceipt<TResult>(id, PostResultKind.TargetUnavailable);
            // 仅把实例的纯入箱决定与停用原子排序；业务及消费者调度都在端口门外。
            (receipt, schedule) = post(message, id);
        }

        schedule?.Invoke();
        return receipt;
    }
}

/// <summary>在宿主显式建立的契约范围内选择独立实例并等待其处理完成。</summary>
public sealed class Mediator
{
    private readonly object gate = new();
    private readonly Dictionary<(Type Request, Type Result), List<Registration>> registrations = [];

    /// <summary>向宿主明确选定的已接线端口投递，仅取得目标实例收件箱的接纳回执。</summary>
    /// <typeparam name="TMessage">不可变业务消息类型。</typeparam>
    /// <typeparam name="TResult">后续处理的业务返回值类型。</typeparam>
    /// <param name="message">本次定向业务消息。</param>
    /// <param name="target">宿主明确选定的强类型端口。</param>
    /// <returns>容量或目标不可用的拒绝原因，或关联后续处理结果的接纳回执。</returns>
    public PostReceipt<TResult> Post<TMessage, TResult>(TMessage message, RequestPort<TMessage, TResult> target) where TMessage : notnull
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(target);
        Guid id = Guid.NewGuid();
        RequestPort<TMessage, TResult>? selected = target;
        return SelectTarget(ref selected) == RequestResultKind.Responded ? target.Post(message, id)
            : new PostReceipt<TResult>(id, PostResultKind.TargetUnavailable);
    }

    /// <summary>尝试定向投递，并通过完整回执保留接纳或拒绝原因。</summary>
    /// <typeparam name="TMessage">不可变业务消息类型。</typeparam>
    /// <typeparam name="TResult">后续处理的业务返回值类型。</typeparam>
    /// <param name="message">本次定向业务消息。</param>
    /// <param name="target">宿主明确选定的强类型端口。</param>
    /// <param name="receipt">包含关联身份、拒绝原因和已接纳消息完成任务的回执。</param>
    /// <returns>消息是否已进入目标实例收件箱。</returns>
    public bool TryPost<TMessage, TResult>(TMessage message, RequestPort<TMessage, TResult> target, out PostReceipt<TResult> receipt)
        where TMessage : notnull
    {
        receipt = Post(message, target);
        return receipt.Kind == PostResultKind.Accepted;
    }

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
    /// <param name="executionCancellationToken">仅在目标端口声明 Propagate 时用于执行协作取消；TargetOwned 忽略该令牌。与等待令牌分别控制；两者使用同一令牌也必须显式传入。</param>
    /// <returns>路由或等待结论，以及目标返回的原始操作结果。</returns>
    public Task<RequestResult<TResult>> SendAsync<TRequest, TResult>(TRequest request,
        CancellationToken waitCancellationToken = default, CancellationToken executionCancellationToken = default) where TRequest : notnull
        => SendCoreAsync<TRequest, TResult>(request, null, waitCancellationToken, executionCancellationToken);

    /// <summary>请求宿主明确选择且已在本范围接线的端口，等待目标处理和有关状态提交完成。</summary>
    /// <typeparam name="TRequest">业务契约的不可变请求类型。</typeparam>
    /// <typeparam name="TResult">业务契约的返回值类型。</typeparam>
    /// <param name="request">本次不可变业务请求。</param>
    /// <param name="target">宿主为发送者选择的强类型目标端口。</param>
    /// <param name="waitCancellationToken">只控制请求接纳前取消和调用方等待，不传播到已接纳的目标执行。</param>
    /// <param name="executionCancellationToken">仅在目标端口声明 Propagate 时用于执行协作取消；TargetOwned 忽略该令牌。与等待令牌分别控制；两者使用同一令牌也必须显式传入。</param>
    /// <returns>路由或等待结论，以及目标返回的原始操作结果。</returns>
    public Task<RequestResult<TResult>> SendAsync<TRequest, TResult>(TRequest request, RequestPort<TRequest, TResult> target,
        CancellationToken waitCancellationToken = default, CancellationToken executionCancellationToken = default) where TRequest : notnull
    {
        ArgumentNullException.ThrowIfNull(target);
        return SendCoreAsync(request, target, waitCancellationToken, executionCancellationToken);
    }

    private async Task<RequestResult<TResult>> SendCoreAsync<TRequest, TResult>(TRequest request,
        RequestPort<TRequest, TResult>? target, CancellationToken waitCancellationToken,
        CancellationToken executionCancellationToken) where TRequest : notnull
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

        (RequestResultKind kind, Task<OperationResult<TResult>>? execution) = target!.Admit(request, waitCancellationToken,
            executionCancellationToken);
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
