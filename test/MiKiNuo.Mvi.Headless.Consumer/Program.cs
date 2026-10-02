using System.Collections.Immutable;

namespace MiKiNuo.Mvi.Headless.Consumer;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        if (args.Contains("--parallel", StringComparer.Ordinal))
        {
            await RunParallelAsync();
            return;
        }

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

        TaskCompletionSource<string> serviceEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> releaseService = new(TaskCreationOptions.RunContinuationsAsynchronously);
        EditorFeature submitting = new(async (input, cancellationToken) =>
        {
            serviceEntered.SetResult(input.Name);
            return await releaseService.Task.WaitAsync(cancellationToken);
        });
        OperationResult<int> rejected = await submitting.SubmitAsync();
        Require(rejected.Kind == OperationResultKind.Rejected
            && submitting.Snapshot.OperationStates["SubmitAsync"].Reason == "ValidationFailed", "无效输入必须在快照中可见且不能调用服务。");
        submitting.SetName("validated draft");
        Task<OperationResult<int>> execution = submitting.SubmitAsync();
        Require(await serviceEntered.Task.WaitAsync(TimeSpan.FromSeconds(15)) == "validated draft", "服务必须收到通过验证的开始输入。");
        Require(submitting.Snapshot.OperationStates["SubmitAsync"].IsRunning, "运行状态必须来自一致快照。");
        submitting.SetName("edited during IO");
        releaseService.SetResult(11);
        OperationResult<int> result = await execution.WaitAsync(TimeSpan.FromSeconds(15));
        Require(result.Kind == OperationResultKind.Completed && result.HasValue && result.Value == 11, "公开入口必须返回强类型完成结果。");
        Require(submitting.Snapshot.State.Total == 11 && submitting.Snapshot.State.Name == "edited during IO"
            && !submitting.Snapshot.OperationStates["SubmitAsync"].IsRunning, "完成必须已提交反馈、保留并发编辑并结束运行状态。");
        Console.WriteLine("Headless operation loop PASS: validation, starting input, concurrent editing, typed feedback and completion.");
        await MediatorDemo.RunAsync();
    }

    private static async Task RunParallelAsync()
    {
        TaskCompletionSource<string> firstEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<string> secondEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> firstResponse = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> secondResponse = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ParallelEditorFeature feature = new(async (input, token) =>
        {
            TaskCompletionSource<string> entered = input.Name == "first" ? firstEntered : secondEntered;
            TaskCompletionSource<int> response = input.Name == "first" ? firstResponse : secondResponse;
            entered.SetResult(input.Name);
            return await response.Task.WaitAsync(token);
        });
        feature.SetName("first");
        Task<OperationResult<int>> first = feature.SubmitAsync();
        Require(await firstEntered.Task.WaitAsync(TimeSpan.FromSeconds(15)) == "first", "第一项必须使用自己的开始输入。");
        feature.SetName("second");
        Task<OperationResult<int>> second = feature.SubmitAsync();
        Require(await secondEntered.Task.WaitAsync(TimeSpan.FromSeconds(15)) == "second", "第二项必须使用自己的开始输入。");
        RuntimeSnapshot<EditorState> bothRunning = feature.Snapshot;
        Require(bothRunning.OperationStates["SubmitAsync"].RunningCount == 2, "并行接纳必须保留两个身份。");
        OperationResult<int> excess = await feature.SubmitAsync();
        Require(excess.Kind == OperationResultKind.Rejected && excess.Reason == "ConcurrencyLimitReached", "达到上限必须明确拒绝。");
        feature.SetName("edited during parallel IO");
        secondResponse.SetResult(7);
        OperationResult<int> secondResult = await second.WaitAsync(TimeSpan.FromSeconds(15));
        Require(secondResult.Kind == OperationResultKind.Completed && secondResult.Value == 7
            && feature.Snapshot.State.Total == 7 && feature.Snapshot.OperationStates["SubmitAsync"].RunningCount == 1,
            "后启动先完成时必须保留其他执行与当前状态。");
        Console.WriteLine("Parallel partial completion PASS: limit=2, excess=Rejected/ConcurrencyLimitReached, second=Completed/7, running=1.");
        firstResponse.SetResult(3);
        OperationResult<int> firstResult = await first.WaitAsync(TimeSpan.FromSeconds(15));
        Require(firstResult.Kind == OperationResultKind.Completed && firstResult.Value == 3
            && firstResult.OperationId != secondResult.OperationId, "每次执行必须有独立身份与业务结果。");
        Require(feature.Snapshot.State.Total == 10 && feature.Snapshot.State.Name == "edited during parallel IO"
            && !feature.Snapshot.OperationStates["SubmitAsync"].IsRunning
            && bothRunning.State.Total == 0 && bothRunning.OperationStates["SubmitAsync"].RunningCount == 2,
            "并行反馈必须基于当前状态累计，保留编辑与旧快照。");
        Console.WriteLine("Headless bounded parallel PASS: independent inputs/results, out-of-order completion, total=10, running=0, immutable snapshots.");
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

/// <summary>通过生成入口执行纯状态转换和后台业务操作的无界面编辑器。</summary>
/// <param name="service">接受开始输入与取消令牌的可替换外部服务。</param>
public sealed partial class EditorFeature(Func<EditorState, CancellationToken, ValueTask<int>>? service = null) : Feature<EditorState>(new())
{
    [OnInput(nameof(EditorState.Delta))]
    private static EditorState Accumulate(EditorState state, int value)
        => state with { Delta = value, Total = state.Total + value };

    [Operation(Validate = nameof(CanSubmit))]
    private async ValueTask<int> SubmitAsync(Operation<EditorState> operation)
    {
        int total = service is null ? operation.Snapshot.Total : await service(operation.Snapshot, operation.CancellationToken);
        await operation.UpdateAsync(ApplyTotal, total);
        return total;
    }

    private static bool CanSubmit(EditorState state) => !string.IsNullOrWhiteSpace(state.Name);

    private static EditorState ApplyTotal(EditorState state, int total) => state with { Total = total };
}

/// <summary>通过声明的有限上限执行独立并行业务，并统一累计反馈。</summary>
/// <param name="service">使用本次开始输入与取消令牌的外部服务。</param>
public sealed partial class ParallelEditorFeature(Func<EditorState, CancellationToken, ValueTask<int>> service) : Feature<EditorState>(new())
{
    [Operation(Validate = nameof(CanSubmit), Concurrency = OperationConcurrency.Parallel, MaxConcurrency = 2)]
    private async ValueTask<int> SubmitAsync(Operation<EditorState> operation)
    {
        int result = await service(operation.Snapshot, operation.CancellationToken);
        await operation.UpdateAsync(static (state, value) => state with { Total = state.Total + value }, result);
        return result;
    }

    private static bool CanSubmit(EditorState state) => !string.IsNullOrWhiteSpace(state.Name);
}
