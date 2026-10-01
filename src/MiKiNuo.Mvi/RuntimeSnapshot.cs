using System.Collections.Immutable;

namespace MiKiNuo.Mvi;

/// <summary>保存一次提交的业务状态、操作状态与版本。</summary>
/// <typeparam name="TState">不可变业务状态类型。</typeparam>
public sealed class RuntimeSnapshot<TState> where TState : notnull
{
    internal RuntimeSnapshot(TState state, long version)
    {
        State = state;
        Version = version;
    }

    /// <summary>获取本次提交的不可变业务状态。</summary>
    public TState State { get; }

    /// <summary>获取操作状态；纯状态内核尚无操作，此集合始终为空。</summary>
    public ImmutableDictionary<string, string> OperationStates { get; } = ImmutableDictionary<string, string>.Empty;

    /// <summary>获取本实例单调递增的提交版本。</summary>
    public long Version { get; }
}
