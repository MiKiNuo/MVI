using System.Collections.Immutable;

namespace MiKiNuo.Mvi;

/// <summary>保存一次提交的业务状态、操作状态与版本。</summary>
/// <typeparam name="TState">不可变业务状态类型。</typeparam>
public sealed class RuntimeSnapshot<TState> where TState : notnull
{
    internal RuntimeSnapshot(TState state, long version, ImmutableDictionary<string, OperationState>? operationStates = null)
    {
        State = state;
        Version = version;
        OperationStates = operationStates ?? ImmutableDictionary<string, OperationState>.Empty;
    }

    /// <summary>获取本次提交的不可变业务状态。</summary>
    public TState State { get; }

    /// <summary>获取与业务状态在同一次提交中发布的操作运行状态与调用反馈。</summary>
    public ImmutableDictionary<string, OperationState> OperationStates { get; }

    /// <summary>获取本实例单调递增的提交版本。</summary>
    public long Version { get; }
}
