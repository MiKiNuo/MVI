using MiKiNuo.Mvi.Abstractions.MVI.Intent;
using MiKiNuo.Mvi.Abstractions.MVI.Mutation;
using MiKiNuo.Mvi.Abstractions.MVI.State;
using MiKiNuo.Mvi.Runtime.MVI.Intent;
using MiKiNuo.Mvi.Runtime.MVI.Reducer;
using MiKiNuo.Mvi.Runtime.MVI.Store;
using MiKiNuo.Mvi.Runtime.MVI.Middleware;
using R3;
using TUnit.Assertions;
using TUnit.Core;
namespace MiKiNuo.Mvi.Tests;
/// <summary>真实生成的 StatePath 在新内核上保持原有语义。</summary>
public sealed class SelectStateTests
{
    /// <summary>单路径输出初始值并去除未变化的状态。</summary>
    [Test] public async Task SinglePathDistinctValuesAsync()
    {
        await using var store = TestCheck.Store(); List<int> values = [];
        using var subscription = store.SelectState(CounterStatePaths.Count).Subscribe(values.Add);
        await store.DispatchAsync(new CounterIntent.Increment());
        await store.DispatchAsync(new CounterIntent.Rename("x"));
        await Assert.That(string.Join(",", values)).IsEqualTo("0,1");
    }
    /// <summary>多路径从同一个已提交快照计算。</summary>
    [Test] public async Task MultiplePathsShareSnapshotAsync()
    {
        await using var store = TestCheck.Store(); List<string> values = [];
        using var subscription = store.SelectState(CounterStatePaths.Count, CounterStatePaths.Label, static (count, name) => count + ":" + name).Subscribe(values.Add);
        await store.DispatchAsync(new CounterIntent.Increment());
        await store.DispatchAsync(new CounterIntent.Rename("x"));
        await Assert.That(string.Join(",", values)).IsEqualTo("0:,1:,1:x");
    }
}
