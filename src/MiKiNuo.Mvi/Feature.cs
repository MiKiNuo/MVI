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

    /// <summary>将生成的强类型输入交给实例的状态提交入口。</summary>
    /// <typeparam name="TValue">输入值类型。</typeparam>
    /// <param name="value">本次输入值。</param>
    /// <param name="reduce">根据当前状态计算下一状态的纯转换。</param>
    protected void DispatchInput<TValue>(TValue value, Func<TState, TValue, TState> reduce)
    {
        ArgumentNullException.ThrowIfNull(reduce);
        store.Dispatch(new InputIntent<TState, TValue>(value, reduce));
    }
}

internal readonly struct InputIntent<TState, TValue>(TValue value, Func<TState, TValue, TState> reduce)
{
    internal TState Reduce(TState state) => reduce(state, value);
}

internal sealed class FeatureStore<TState> where TState : notnull
{
    private readonly object gate = new();
    private RuntimeSnapshot<TState> snapshot;
    private bool reducing;

    internal FeatureStore(TState state) => snapshot = new RuntimeSnapshot<TState>(state, 0);

    internal RuntimeSnapshot<TState> Snapshot => Volatile.Read(ref snapshot);

    internal void Dispatch<TValue>(InputIntent<TState, TValue> intent)
    {
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
                Volatile.Write(ref snapshot, next);
            }
            finally
            {
                reducing = false;
            }
        }
    }

    private static RuntimeSnapshot<TState> Reduce<TValue>(RuntimeSnapshot<TState> current, InputIntent<TState, TValue> intent)
    {
        TState state = intent.Reduce(current.State);
        ArgumentNullException.ThrowIfNull(state);
        return new RuntimeSnapshot<TState>(state, checked(current.Version + 1));
    }
}
