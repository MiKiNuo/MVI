namespace MiKiNuo.Mvi;

/// <summary>管理显式子实例所有权及脱离活动集合后的真实释放归属。</summary>
public sealed class FeatureOwnership
{
    // ponytail: 结构变更共用低频锁；业务 Store 独立，结构吞吐成为瓶颈时再按根范围分锁。
    internal static object Gate { get; } = new();
    [ThreadStatic]
    private static int reductionDepth;
    internal static bool IsReducing => reductionDepth != 0;
    internal static void EnterReduction() => reductionDepth++;
    internal static void ExitReduction() => reductionDepth--;
    private readonly Feature parent;
    private readonly Dictionary<Feature, FeatureMember> active = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<Feature, CloseTicket> retired = new(ReferenceEqualityComparer.Instance);
    private readonly List<Exception> releaseFailures = [];
    private long revision;

    internal FeatureOwnership(Feature parent) => this.parent = parent;

    /// <summary>获取显式结构变更版本；新增及活动退出均推进。</summary>
    public long Revision { get { VerifyOutsideReduction(); lock (Gate) return revision; } }

    /// <summary>获取当前活动成员的独立快照。</summary>
    public IReadOnlyList<FeatureMember> Active { get { VerifyOutsideReduction(); lock (Gate) return active.Values.ToArray(); } }

    /// <summary>获取已逻辑退出但仍未完成真实释放的子实例票据快照。</summary>
    public IReadOnlyList<CloseTicket> Retired { get { VerifyOutsideReduction(); lock (Gate) return retired.Values.ToArray(); } }

    private static void VerifyOutsideReduction()
    {
        if (IsReducing) throw new InvalidOperationException("纯状态转换不能读取实例所有权结构。");
    }

    /// <summary>接管一个尚未关闭且无所有者的独立实例。</summary>
    /// <param name="child">完整独立子实例，与其业务对象或视觉挂载分别管理。</param>
    /// <returns>用于明确接线和移除的活动成员句柄。</returns>
    public FeatureMember Add(Feature child)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (IsReducing) throw new InvalidOperationException("纯状态转换不能改变实例所有权。");
        lock (Gate)
        {
            if (parent.IsClosed || child.IsClosed) throw new FeatureClosedException();
            if (child.Membership is not null) throw new InvalidOperationException("子实例已经拥有逻辑所有者。");
            for (Feature? ancestor = parent; ancestor is not null; ancestor = ancestor.Membership?.Parent)
            {
                if (ReferenceEquals(ancestor, child)) throw new InvalidOperationException("实例所有权不能形成环。");
            }

            FeatureMember member = new(parent, child);
            active.Add(child, member);
            child.Membership = member;
            revision++;
            return member;
        }
    }

    /// <summary>提交所属子实例关闭并移出活动集合，仍跟踪其真实释放。</summary>
    /// <param name="child">本范围曾接管的子实例。</param>
    /// <returns>子实例唯一的关闭结果与释放票据。</returns>
    public CloseResult Remove(Feature child)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (IsReducing) throw new InvalidOperationException("纯状态转换不能改变实例所有权。");
        lock (Gate)
        {
            if (!ReferenceEquals(child.Membership?.Parent, parent)) throw new InvalidOperationException("实例不属于当前所有权范围。");
        }

        return child.Close();
    }

    internal static CloseResult Close(Feature feature)
    {
        List<Action> finish = [];
        CloseResult result;
        lock (Gate)
        {
            result = Commit(feature, finish);
        }

        foreach (Action action in finish) action();
        return result;
    }

    internal static async Task<CloseRequestResult> RequestCloseAsync(Feature root, CancellationToken cancellationToken)
    {
        VerifyOutsideReduction();
        while (true)
        {
            CloseRequestResult? interrupted = ReadInterruption(root, cancellationToken);
            if (interrupted is not null) return interrupted;
            CloseCondition[] conditions;
            lock (Gate)
            {
                if (root.IsClosed) return new CloseRequestResult(CloseRequestKind.Closed, root.CommitClose(Task.FromResult<Exception?>(null)).Result);
                Feature[] features = ActiveTree(root).OrderBy(feature => feature.InstanceId).ToArray();
                foreach (Feature feature in features) Monitor.Enter(feature.ModelGate);
                try
                {
                    conditions = features.Select(feature =>
                    {
                        (long version, object state) = feature.ReadCloseCondition();
                        return new CloseCondition(feature, feature.Children.revision, version, state);
                    }).ToArray();
                }
                finally
                {
                    foreach (Feature feature in features.Reverse()) Monitor.Exit(feature.ModelGate);
                }
            }

            foreach (CloseCondition condition in conditions)
            {
                OperationResult<bool> confirmation;
                try
                {
                    confirmation = await condition.Feature.ConfirmCondition(condition.State, cancellationToken)
                        .WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return ReadInterruption(root, cancellationToken)!;
                }

                if (confirmation.Kind == OperationResultKind.Rejected && confirmation.Reason == "Closed") break;
                if (confirmation.Kind == OperationResultKind.Canceled)
                {
                    CloseRequestResult? canceled = ReadInterruption(root, cancellationToken);
                    if (canceled is not null) return canceled;
                    break; // 确认成员自身退出，重新准备当前剩余树，不冒充调用者取消。
                }
                if (confirmation.Kind == OperationResultKind.Faulted) return new CloseRequestResult(CloseRequestKind.Faulted, exception: confirmation.Exception);
                if (!confirmation.Value) return new CloseRequestResult(CloseRequestKind.Rejected);
            }

            List<Action> finish = [];
            CloseResult? result = null;
            lock (Gate)
            {
                if (root.IsClosed) return new CloseRequestResult(CloseRequestKind.Closed, root.CommitClose(Task.FromResult<Exception?>(null)).Result);
                Feature[] current = ActiveTree(root).OrderBy(feature => feature.InstanceId).ToArray();
                foreach (Feature feature in current) Monitor.Enter(feature.ModelGate);
                try
                {
                    if (cancellationToken.IsCancellationRequested) return new CloseRequestResult(CloseRequestKind.WaitCanceled);
                    bool valid = current.Length == conditions.Length && conditions.Zip(current).All(pair =>
                        ReferenceEquals(pair.First.Feature, pair.Second) && !pair.Second.IsClosed
                        && pair.First.Revision == pair.Second.Children.revision
                        && pair.First.StateVersion == pair.Second.ReadCloseCondition().Version);
                    if (valid) result = Commit(root, finish);
                }
                finally
                {
                    foreach (Feature feature in current.Reverse()) Monitor.Exit(feature.ModelGate);
                }
            }

            if (result is null) continue;
            foreach (Action action in finish) action();
            return new CloseRequestResult(CloseRequestKind.Closed, result);
        }
    }

    private static IEnumerable<Feature> ActiveTree(Feature root)
    {
        yield return root;
        foreach (FeatureMember member in root.Children.active.Values)
        {
            foreach (Feature child in ActiveTree(member.Child)) yield return child;
        }
    }

    private static CloseRequestResult? ReadInterruption(Feature root, CancellationToken cancellationToken)
    {
        lock (Gate)
        {
            if (root.IsClosed) return new CloseRequestResult(CloseRequestKind.Closed, root.CommitClose(Task.FromResult<Exception?>(null)).Result);
            return cancellationToken.IsCancellationRequested ? new CloseRequestResult(CloseRequestKind.WaitCanceled) : null;
        }
    }

    private static CloseResult Commit(Feature feature, List<Action> finish)
    {
        FeatureOwnership children = feature.Children;
        if (feature.IsClosed) return feature.CommitClose(Task.FromResult<Exception?>(null)).Result;
        TaskCompletionSource<Exception?> dependency = new(TaskCreationOptions.RunContinuationsAsynchronously);
        (CloseResult result, Action close) = feature.CommitClose(dependency.Task);
        foreach (FeatureMember member in children.active.Values.ToArray()) Commit(member.Child, finish);
        Task<ReleaseResult>[] releases = children.retired.Values.Select(ticket => ticket.Completion).ToArray();
        Exception[] previousFailures = children.releaseFailures.ToArray();
        finish.Add(() => _ = CompleteDependenciesAsync());
        if (feature.Membership is FeatureMember membership)
        {
            FeatureOwnership owner = membership.Parent.Children;
            if (owner.active.Remove(feature))
            {
                owner.revision++;
                owner.retired.Add(feature, result.Ticket);
                IDisposable[] routes = membership.TakeRoutes();
                finish.Add(() =>
                {
                    foreach (IDisposable route in routes) route.Dispose();
                    _ = owner.ObserveReleaseAsync(feature, result.Ticket);
                });
            }
        }

        finish.Add(close);
        return result;

        async Task CompleteDependenciesAsync()
        {
            List<Exception> failures = [.. previousFailures];
            ReleaseResult[] results = await Task.WhenAll(releases).ConfigureAwait(false);
            foreach (ReleaseResult released in results) if (released.Exception is not null) failures.Add(released.Exception);
            dependency.SetResult(failures.Count == 0 ? null : failures.Count == 1 ? failures[0] : new AggregateException(failures));
        }
    }

    private async Task ObserveReleaseAsync(Feature child, CloseTicket ticket)
    {
        ReleaseResult result = await ticket.Completion.ConfigureAwait(false);
        lock (Gate)
        {
            retired.Remove(child);
            if (result.Exception is not null) releaseFailures.Add(result.Exception);
        }
    }

    internal static bool DependsOnExecution(object owner, object? execution)
    {
        if (ReferenceEquals(owner, execution)) return true;
        if (execution is null) return false;
        VerifyOutsideReduction();
        lock (Gate)
        {
            if (!Feature.ExecutionFeatures.TryGetValue(execution, out Feature? executing)) return false;
            for (Feature? feature = executing; feature is not null; feature = feature.Membership?.Parent)
            {
                if (ReferenceEquals(feature.ExecutionOwner, owner)) return true;
            }

            return false;
        }
    }
}

/// <summary>持有所属子实例的身份与宿主明确建立的路由回执。</summary>
public sealed class FeatureMember
{
    private readonly List<IDisposable> routes = [];
    internal FeatureMember(Feature parent, Feature child) { Parent = parent; Child = child; }

    /// <summary>获取该成员的逻辑所有者。</summary>
    public Feature Parent { get; }
    /// <summary>获取具有独立状态与生命周期的子实例。</summary>
    public Feature Child { get; }

    /// <summary>将本子实例拥有的端口接入一个明确通信范围，并在成员退出时解除。</summary>
    /// <typeparam name="TRequest">不可变契约请求类型。</typeparam>
    /// <typeparam name="TResult">业务返回值类型。</typeparam>
    /// <param name="mediator">宿主明确建立的通信范围。</param>
    /// <param name="port">由该子实例创建的业务端口。</param>
    public void Wire<TRequest, TResult>(Mediator mediator, RequestPort<TRequest, TResult> port) where TRequest : notnull
    {
        ArgumentNullException.ThrowIfNull(mediator);
        ArgumentNullException.ThrowIfNull(port);
        if (FeatureOwnership.IsReducing) throw new InvalidOperationException("纯状态转换不能建立跨实例接线。");
        lock (FeatureOwnership.Gate)
        {
            if (Parent.IsClosed || Child.IsClosed) throw new FeatureClosedException();
            if (!ReferenceEquals(port.Owner, Child)) throw new InvalidOperationException("接线端口不属于该成员实例。");
            routes.Add(mediator.Register(port));
        }
    }

    /// <summary>从所属范围移除实例并取得其唯一关闭结果。</summary>
    /// <returns>逻辑关闭结果与真实释放票据。</returns>
    public CloseResult Remove() => Parent.Children.Remove(Child);

    internal IDisposable[] TakeRoutes() { IDisposable[] result = routes.ToArray(); routes.Clear(); return result; }
}
