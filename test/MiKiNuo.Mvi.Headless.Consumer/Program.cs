using System.Collections.Immutable;

namespace MiKiNuo.Mvi.Headless.Consumer;

internal static class Program
{
    private static void Main()
    {
        EditorFeature first = new();
        EditorFeature second = new();
        first.SetName("draft");
        first.SetDelta(2);
        first.SetDelta(3);
        RuntimeSnapshot<EditorState> committed = first.Snapshot;
        first.SetName(string.Empty);
        Require(first.Snapshot.State.Total == 5 && first.Snapshot.State.Name == string.Empty, "输入规则必须仅处理有关输入。");
        Require(first.Snapshot.Version == 4 && first.Snapshot.OperationStates.IsEmpty, "快照必须包含一致版本与操作状态。");
        Require(committed.Version == 3 && committed.State.Name == "draft", "旧快照必须保留原状态。");
        Require(second.Snapshot.State.Name == string.Empty && second.Snapshot.State.Total == 0 && second.Snapshot.Version == 0, "实例必须独立。");
        Require(typeof(EditorFeature).GetMethod("SetTotal") is null, "只读属性不能生成可写入口。");
        Console.WriteLine("Headless state loop PASS: two isolated instances, typed inputs, custom rule, immutable coherent snapshots.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

/// <summary>保存独立编辑器的不可变业务状态。</summary>
public sealed record EditorState
{
    /// <summary>获取允许中间值的编辑名称。</summary>
    [Input]
    public string Name { get; init; } = string.Empty;

    /// <summary>获取最近一次累加输入。</summary>
    [Input]
    public int Delta { get; init; }

    /// <summary>获取只能由纯规则更新的累计值。</summary>
    public int Total { get; init; }

    /// <summary>获取不可变附加数据。</summary>
    public ImmutableArray<string> Labels { get; init; } = [];
}

/// <summary>通过生成入口执行纯状态转换的无界面编辑器。</summary>
public sealed partial class EditorFeature() : Feature<EditorState>(new())
{
    [OnInput(nameof(EditorState.Delta))]
    private static EditorState Accumulate(EditorState state, int value)
        => state with { Delta = value, Total = state.Total + value };
}
