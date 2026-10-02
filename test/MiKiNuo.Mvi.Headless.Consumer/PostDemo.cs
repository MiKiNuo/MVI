using System.Collections.Immutable;

namespace MiKiNuo.Mvi.Headless.Consumer;

internal static class PostDemo
{
    internal static async Task RunAsync()
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        PostDemoFeature feature = new(async value =>
        {
            if (value == 1) { entered.SetResult(); await release.Task; }
            return value;
        });
        Mediator mediator = new();
        using IDisposable registration = mediator.Register(feature.Port);
        PostReceipt<int> first = mediator.Post(new PostedValue(1), feature.Port);
        try
        {
            Require(first.Kind == PostResultKind.Accepted, "首项应先取得接纳回执。");
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Require(!first.Completion!.IsCompleted, "接纳不能表示业务已经完成。");
            PostReceipt<int> invalid = mediator.Post(new PostedValue(2), feature.Port);
            Require(invalid.Kind == PostResultKind.Accepted, "投递接纳不能预先执行业务验证。");
            Require(!mediator.TryPost(new PostedValue(3), feature.Port, out PostReceipt<int> full)
                && full.Kind == PostResultKind.InboxFull && full.Completion is null, "共享容量应保留明确满载原因。");
            feature.SetEnabled(false);
            release.SetResult();
            OperationResult<int> completed = await first.Completion.WaitAsync(TimeSpan.FromSeconds(15));
            OperationResult<int> rejected = await invalid.Completion!.WaitAsync(TimeSpan.FromSeconds(15));
            Require(completed.OperationId == first.Id && completed.Kind == OperationResultKind.Completed
                && rejected.OperationId == invalid.Id && rejected.Reason == "ValidationFailed", "处理时验证与结果应关联原回执。");
            CloseResult close = feature.Close();
            Require(mediator.Post(new PostedValue(4), feature.Port).Kind == PostResultKind.TargetUnavailable,
                "逻辑关闭后不能继续投递。");
            Require((await close.Ticket.Released.WaitAsync(TimeSpan.FromSeconds(15))).Succeeded, "收件箱退出后应完成真实释放。");
            Console.WriteLine("Headless post loop PASS: admission/completion separation, bounded inbox, processing-time validation, correlated results and close.");
        }
        finally
        {
            release.TrySetResult();
            await feature.Close().Ticket.Released.WaitAsync(TimeSpan.FromSeconds(15));
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

/// <summary>定向投递一个业务数值。</summary>
/// <param name="Value">待处理的不可变数值。</param>
public sealed record PostedValue(int Value);

/// <summary>保存独立投递示例的业务条件和已处理数值。</summary>
public sealed record PostDemoState
{
    /// <summary>获取当前是否允许处理消息。</summary>
    [Input]
    public bool Enabled { get; init; } = true;
    /// <summary>获取已经提交的不可变业务数值。</summary>
    public ImmutableList<int> Values { get; init; } = [];
}

/// <summary>通过现有契约端口接纳定向消息的有界实例。</summary>
public sealed partial class PostDemoFeature : Feature<PostDemoState>
{
    /// <summary>创建独立投递示例。</summary>
    /// <param name="service">用于可控外部处理的服务。</param>
    public PostDemoFeature(Func<int, ValueTask<int>> service) : base(new(), postCapacity: 2)
    {
        Port = CreateRequestPort<PostedValue, int>("Process", static (state, _) => state.Enabled, async (operation, message) =>
        {
            int result = await service(message.Value);
            await operation.UpdateAsync(static (state, value) => state with { Values = state.Values.Add(value) }, result);
            return result;
        });
    }

    /// <summary>获取宿主明确接线的业务端口。</summary>
    public RequestPort<PostedValue, int> Port { get; }
}
