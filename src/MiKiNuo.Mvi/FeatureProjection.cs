using System.ComponentModel;

namespace MiKiNuo.Mvi;

/// <summary>选择本地 View 等待展示的快照处理方式。</summary>
public enum ProjectionMode
{
    /// <summary>合并等待中的展示，只显示最新提交；业务输入仍逐次提交。</summary>
    Coalesce,
    /// <summary>按提交顺序展示每个中间快照。</summary>
    EveryCommit
}

/// <summary>支持生成的强类型本地绑定；一个功能实例同时只允许一个活动 View 投影。</summary>
/// <typeparam name="TState">功能的不可变状态类型。</typeparam>
public abstract class FeatureProjection<TState> : INotifyPropertyChanged, IDisposable where TState : notnull
{
    private readonly object gate = new();
    private readonly Feature<TState> feature;
    private readonly Action<Action> schedule;
    private readonly ProjectionMode mode;
    // ponytail: EveryCommit 保留全部待展示快照；持续产出快于 UI 时需明确选择有界展示策略。
    private readonly Queue<RuntimeSnapshot<TState>> pending = new();
    private readonly HashSet<string> inputFeedback = new(StringComparer.Ordinal);
    private RuntimeSnapshot<TState> snapshot;
    private bool disposed;
    private bool scheduled;
    private long scheduleId;
    private event Action? CommandsChanged;
    private event Action? CommandsDisposed;

    /// <summary>准备本地投影；派生构造函数完成字段初始化后再建立连接。</summary>
    /// <param name="feature">该 View 所属的功能实例。</param>
    /// <param name="schedule">将回调安排到所属 UI 线程的原生调度入口。</param>
    /// <param name="mode">等待展示的合并方式。</param>
    protected FeatureProjection(Feature<TState> feature, Action<Action> schedule, ProjectionMode mode)
    {
        ArgumentNullException.ThrowIfNull(feature);
        ArgumentNullException.ThrowIfNull(schedule);
        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        this.feature = feature;
        this.schedule = schedule;
        this.mode = mode;
        snapshot = feature.Snapshot;
    }

    /// <summary>获取当前已展示的提交快照；绑定属性应在所属 UI 线程读取。</summary>
    public RuntimeSnapshot<TState> Snapshot => Volatile.Read(ref snapshot);

    /// <summary>通过平台原生绑定报告有关属性的变化。</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>建立该实例唯一的活动本地 View 连接。</summary>
    protected void InitializeProjection() => feature.AttachProjection(this);

    /// <summary>创建复用生成操作入口的本地 UI 命令。</summary>
    /// <typeparam name="TResult">操作的业务返回值类型。</typeparam>
    /// <param name="name">操作名称。</param>
    /// <param name="validate">已展示状态的纯验证反馈。</param>
    /// <param name="execute">生成的统一操作入口。</param>
    /// <param name="concurrency">该操作声明的同名接纳策略。</param>
    /// <param name="capacity">Queue 的正数等待容量。</param>
    /// <param name="maxConcurrency">Parallel 的正数并行上限。</param>
    /// <returns>在该投影生命周期内活动的原生命令。</returns>
    protected OperationCommand<TResult> CreateOperationCommand<TResult>(string name, Func<TState, bool>? validate,
        Func<Task<OperationResult<TResult>>> execute, OperationConcurrency concurrency = OperationConcurrency.Reject,
        int capacity = 0, int maxConcurrency = 0)
    {
        EnsureActive();
        OperationCommand<TResult> command = new(() =>
        {
            RuntimeSnapshot<TState> current = Snapshot;
            try
            {
                current.OperationStates.TryGetValue(name, out OperationState? operation);
                bool available = concurrency switch
                {
                    OperationConcurrency.Reject => operation?.IsRunning != true,
                    OperationConcurrency.Latest => true,
                    OperationConcurrency.Queue => capacity > 0 && (operation?.QueuedCount ?? 0) < capacity,
                    OperationConcurrency.Parallel => maxConcurrency > 0 && (operation?.RunningCount ?? 0) < maxConcurrency,
                    _ => false,
                };
                return available && (validate?.Invoke(current.State) ?? true);
            }
            catch
            {
                return false;
            }
        }, () => { EnsureActive(); return execute(); });
        CommandsChanged += command.NotifyChanged;
        CommandsDisposed += command.Detach;
        return command;
    }

    /// <summary>拒绝已释放 View 的输入回写。</summary>
    protected void EnsureActive()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (feature.IsClosed) throw new FeatureClosedException();
        }
    }

    /// <summary>通过生成入口提交 View 输入，并在展示时反馈该字段的已提交值。</summary>
    /// <typeparam name="TFeature">所属功能声明类型。</typeparam>
    /// <typeparam name="TValue">输入属性类型。</typeparam>
    /// <param name="name">生成的输入属性名称。</param>
    /// <param name="value">本次 View 编辑值。</param>
    /// <param name="source">所属功能实例。</param>
    /// <param name="dispatch">所属功能生成的输入入口。</param>
    protected void SetProjectionInput<TFeature, TValue>(string name, TValue value, TFeature source, Action<TFeature, TValue> dispatch)
        where TFeature : Feature<TState>
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(dispatch);
        EnsureActive();
        lock (gate)
        {
            inputFeedback.Add(name);
        }

        dispatch(source, value);
    }

    /// <summary>取出待反馈的本地输入字段，不为后台同值提交产生通知。</summary>
    /// <param name="name">生成的输入属性名称。</param>
    /// <returns>该字段是否有等待展示的 View 输入反馈。</returns>
    protected bool TakeInputFeedback(string name)
    {
        lock (gate)
        {
            return inputFeedback.Remove(name);
        }
    }

    /// <summary>在所属 UI 线程比较状态并通知有关属性。</summary>
    /// <param name="previous">上次展示的状态。</param>
    /// <param name="current">本次展示的状态。</param>
    protected abstract void OnSnapshotChanged(TState previous, TState current);

    /// <summary>通知一个已改变的绑定属性。</summary>
    /// <param name="name">绑定属性名称。</param>
    protected void NotifyPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <summary>断开本地连接，使等待中的旧 View 回调失效；允许重新创建投影。</summary>
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            pending.Clear();
            inputFeedback.Clear();
            PropertyChanged = null;
            CommandsDisposed?.Invoke();
            CommandsDisposed = null;
            CommandsChanged = null;
        }

        feature.DetachProjection(this);
        GC.SuppressFinalize(this);
    }

    internal void SetInitialSnapshot(RuntimeSnapshot<TState> initial) => snapshot = initial;

    internal void Enqueue(RuntimeSnapshot<TState> next)
    {
        lock (gate)
        {
            if (disposed)
            {
                return;
            }

            if (mode == ProjectionMode.Coalesce)
            {
                pending.Clear();
            }

            pending.Enqueue(next);
        }
    }

    internal void RequestDisplay()
    {
        using OperationExecutionContext.FreshScope fresh = OperationExecutionContext.Fresh();
        long id;
        lock (gate)
        {
            if (disposed || scheduled || pending.Count == 0)
            {
                return;
            }

            scheduled = true;
            id = ++scheduleId;
        }

        try
        {
            schedule(() => Display(id));
        }
        catch
        {
            lock (gate)
            {
                if (scheduleId == id)
                {
                    scheduled = false;
                }
            }

            throw;
        }
    }

    private void Display(long id)
    {
        using OperationExecutionContext.FreshScope fresh = OperationExecutionContext.Fresh();
        try
        {
            while (true)
            {
                RuntimeSnapshot<TState> next;
                lock (gate)
                {
                    if (disposed || feature.IsClosed || pending.Count == 0 || scheduleId != id)
                    {
                        return;
                    }

                    next = pending.Dequeue();
                }

                RuntimeSnapshot<TState> previous = Snapshot;
                Volatile.Write(ref snapshot, next);
                OnSnapshotChanged(previous.State, next.State);
                NotifyPropertyChanged(nameof(Snapshot));
                CommandsChanged?.Invoke();
                if (mode == ProjectionMode.Coalesce)
                {
                    return;
                }
            }
        }
        finally
        {
            lock (gate)
            {
                if (scheduleId == id)
                {
                    scheduled = false;
                }
            }

            RequestDisplay();
        }
    }
}
