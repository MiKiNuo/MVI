namespace MiKiNuo.Mvi.Application.MVI.Mediator;

using MiKiNuo.Mvi.Domain.MVI.Mediator;

/// <summary>管理组合范围内显式接线的实例通信。</summary>
public sealed class MviCompositionScope : IDisposable
{
    /// <summary>保护接线与生命周期的同步锁。</summary>
    private readonly object _gate = new();
    /// <summary>串行保护短小的同步接纳与注销，不在此执行异步业务链。</summary>
    private readonly object _deliveryGate = new();
    /// <summary>实例生命周期。</summary>
    private readonly Dictionary<Guid, CancellationTokenSource> _members = new();
    /// <summary>只注册一次后只读的契约处理器。</summary>
    private readonly Dictionary<(Guid Target, Type Request), MviMediator> _routes = new();
    /// <summary>来源请求的目标绑定。</summary>
    private readonly Dictionary<(Guid Source, Type Request), Guid> _bindings = new();
    /// <summary>已使用的实例标识，关闭后禁止复用。</summary>
    private readonly HashSet<Guid> _used = new();
    /// <summary>按注册顺序维护的显式订阅。</summary>
    private readonly List<Subscription> _subscriptions = [];
    /// <summary>范围是否关闭。</summary>
    private bool _disposed;

    /// <summary>创建独占的实例端点。</summary>
    /// <param name="instanceId">不复用的实例标识。</param>
    /// <returns>实例端点。</returns>
    public MviMediatorEndpoint CreateEndpoint(Guid instanceId)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (!_used.Add(instanceId)) throw new InvalidOperationException("实例标识不能重复使用。");
            _members.Add(instanceId, new CancellationTokenSource());
            return new MviMediatorEndpoint(this, instanceId);
        }
    }

    /// <summary>注册目标实例的请求处理器。</summary>
    /// <typeparam name="TRequest">请求类型。</typeparam>
    /// <typeparam name="TResponse">响应类型。</typeparam>
    /// <param name="target">目标实例。</param>
    /// <param name="handler">业务响应处理器。</param>
    public void Register<TRequest, TResponse>(Guid target, Func<TRequest, CancellationToken, ValueTask<TResponse>> handler)
        where TRequest : notnull, IMviRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(handler);
        lock (_gate)
        {
            GetMember(target);
            if (!_routes.TryAdd((target, typeof(TRequest)), new MviMediator().Register(handler)))
                throw new InvalidOperationException("目标请求处理器重复注册。");
        }
    }

    /// <summary>绑定来源请求到明确目标。</summary>
    /// <typeparam name="TRequest">请求类型。</typeparam>
    /// <param name="source">来源实例。</param>
    /// <param name="target">目标实例。</param>
    public void Bind<TRequest>(Guid source, Guid target)
    {
        lock (_gate)
        {
            GetMember(source);
            GetMember(target);
            _bindings.Add((source, typeof(TRequest)), target);
        }
    }

    /// <summary>订阅通知；回调只能同步接纳本地意图，拒绝接纳时须抛出异常。</summary>
    /// <typeparam name="TNotification">通知类型。</typeparam>
    /// <param name="target">目标实例。</param>
    /// <param name="accept">同步接纳回调，不得等待业务副作用。</param>
    /// <returns>用于退订的句柄。</returns>
    public IDisposable Subscribe<TNotification>(Guid target, Action<TNotification> accept)
        where TNotification : IMviNotification
    {
        ArgumentNullException.ThrowIfNull(accept);
        lock (_gate)
        {
            GetMember(target);
            Subscription subscription = new(this, target, typeof(TNotification), notification => accept((TNotification)notification));
            _subscriptions.Add(subscription);
            return subscription;
        }
    }

    /// <summary>向快照订阅逐项投递，回调在接线锁外执行。</summary>
    /// <typeparam name="TNotification">通知类型。</typeparam>
    /// <param name="source">来源实例。</param>
    /// <param name="notification">通知契约。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>接纳报告。</returns>
    internal ValueTask<MviNotificationReport> PublishAsync<TNotification>(Guid source, TNotification notification, CancellationToken cancellationToken)
        where TNotification : IMviNotification
    {
        ArgumentNullException.ThrowIfNull(notification);
        Subscription[] snapshot;
        lock (_gate)
        {
            GetMember(source);
            snapshot = _subscriptions.Where(subscription => subscription.NotificationType == typeof(TNotification) && subscription.Target != source).ToArray();
        }
        List<MviNotificationDelivery> deliveries = new(snapshot.Length);
        foreach (Subscription subscription in snapshot)
        {
            lock (_deliveryGate)
            {
            Action<IMviNotification>? accept;
            lock (_gate)
            {
                if (cancellationToken.IsCancellationRequested || !_members.ContainsKey(source))
                {
                    deliveries.Add(new(subscription.Target, MviNotificationDeliveryStatus.NotAttempted));
                    continue;
                }
                if (!_members.ContainsKey(subscription.Target))
                {
                    deliveries.Add(new(subscription.Target, MviNotificationDeliveryStatus.Closed));
                    continue;
                }
                accept = subscription.Accept;
                if (accept is null)
                {
                    deliveries.Add(new(subscription.Target, MviNotificationDeliveryStatus.NotAttempted));
                    continue;
                }
            }
            try
            {
                accept(notification);
                deliveries.Add(new(subscription.Target, MviNotificationDeliveryStatus.Accepted));
            }
            catch (Exception exception)
            {
                deliveries.Add(new(subscription.Target, MviNotificationDeliveryStatus.Failed, exception));
            }
            }
        }
        return ValueTask.FromResult(new MviNotificationReport(deliveries.AsReadOnly()));
    }

    /// <summary>撤销订阅并释放回调引用。</summary>
    /// <param name="subscription">订阅句柄。</param>
    private void Unsubscribe(Subscription subscription)
    {
        lock (_deliveryGate)
        lock (_gate)
        {
            _subscriptions.Remove(subscription);
            subscription.Accept = null;
        }
    }

    /// <summary>取得仍存活的实例处理器。</summary>
    /// <param name="id">实例标识。</param>
    /// <returns>实例生命周期。</returns>
    private CancellationTokenSource GetMember(Guid id)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_members.TryGetValue(id, out CancellationTokenSource? member)) throw new ObjectDisposedException("实例端点", "实例不存在或已关闭。");
        return member;
    }

    /// <summary>发送已绑定的请求。</summary>
    /// <typeparam name="TResponse">响应类型。</typeparam>
    /// <param name="source">来源实例。</param>
    /// <param name="request">请求契约。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>业务响应。</returns>
    internal async ValueTask<TResponse> SendAsync<TResponse>(Guid source, IMviRequest<TResponse> request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        MviMediator route;
        CancellationTokenSource lifetime;
        lock (_gate)
        {
            CancellationTokenSource origin = GetMember(source);
            if (!_bindings.TryGetValue((source, request.GetType()), out Guid target)) throw new MviMediatorRouteNotFoundException(request.GetType(), typeof(TResponse));
            CancellationTokenSource destination = GetMember(target);
            if (!_routes.TryGetValue((target, request.GetType()), out MviMediator? found)) throw new MviMediatorRouteNotFoundException(request.GetType(), typeof(TResponse));
            route = found;
            lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, origin.Token, destination.Token);
        }
        using (lifetime)
        {
            lifetime.Token.ThrowIfCancellationRequested();
            return await route.SendAsync(request, lifetime.Token).AsTask().WaitAsync(lifetime.Token).ConfigureAwait(false);
        }
    }

    /// <summary>移除实例和相关接线。</summary>
    /// <param name="id">实例标识。</param>
    internal void Remove(Guid id)
    {
        CancellationTokenSource? lifetime = null;
        try
        {
            lock (_deliveryGate)
            lock (_gate)
            {
                if (!_members.Remove(id, out lifetime)) return;
                foreach ((Guid Source, Type Request) key in _bindings.Where(pair => pair.Key.Source == id || pair.Value == id).Select(pair => pair.Key).ToArray()) _bindings.Remove(key);
                foreach ((Guid Target, Type Request) key in _routes.Keys.Where(key => key.Target == id).ToArray()) _routes.Remove(key);
                foreach (Subscription subscription in _subscriptions.Where(subscription => subscription.Target == id).ToArray())
                    Unsubscribe(subscription);
            }
            lifetime.Cancel();
        }
        finally { lifetime?.Dispose(); }
    }

    /// <summary>释放范围内全部端点与接线。</summary>
    public void Dispose()
    {
        CancellationTokenSource[] lifetimes;
        lock (_deliveryGate)
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            lifetimes = _members.Values.ToArray();
            _members.Clear();
            _bindings.Clear();
            _routes.Clear();
            foreach (Subscription subscription in _subscriptions) subscription.Accept = null;
            _subscriptions.Clear();
        }
        CloseMembers(lifetimes);
    }

    /// <summary>取消并释放全部生命周期，回调失败不阻止其他实例关闭。</summary>
    /// <param name="lifetimes">待关闭的生命周期。</param>
    private static void CloseMembers(CancellationTokenSource[] lifetimes)
    {
        List<Exception> errors = [];
        foreach (CancellationTokenSource lifetime in lifetimes)
        {
            try { lifetime.Cancel(); }
            catch (AggregateException exception) { errors.Add(exception); }
            finally { lifetime.Dispose(); }
        }
        if (errors.Count != 0) throw new AggregateException("实例取消回调失败。", errors);
    }

    /// <summary>范围拥有的订阅句柄。</summary>
    private sealed class Subscription : IDisposable
    {
        /// <summary>所属范围。</summary>
        private readonly MviCompositionScope _scope;
        /// <summary>初始化订阅。</summary>
        /// <param name="scope">所属范围。</param>
        /// <param name="target">目标实例。</param>
        /// <param name="notificationType">通知类型。</param>
        /// <param name="accept">接纳回调。</param>
        public Subscription(MviCompositionScope scope, Guid target, Type notificationType, Action<IMviNotification> accept)
        {
            _scope = scope;
            Target = target;
            NotificationType = notificationType;
            Accept = accept;
        }
        /// <summary>获取目标实例。</summary>
        public Guid Target { get; }
        /// <summary>获取通知类型。</summary>
        public Type NotificationType { get; }
        /// <summary>获取或设置仍有效的接纳回调。</summary>
        public Action<IMviNotification>? Accept { get; set; }
        /// <summary>幂等退订。</summary>
        public void Dispose() => _scope.Unsubscribe(this);
    }
}
