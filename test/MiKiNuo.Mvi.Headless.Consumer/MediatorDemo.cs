using System.Collections.Immutable;

namespace MiKiNuo.Mvi.Headless.Consumer;

internal static class MediatorDemo
{
    internal static async Task RunAsync()
    {
        Mediator scope = new();
        DetailsFeature first = new("left");
        using IDisposable firstRegistration = scope.Register(first.Load);
        RequestResult<DetailsReply> unique = await scope.SendAsync<LoadDetailsRequest, DetailsReply>(new(7));
        Require(unique.Kind == RequestResultKind.Responded
            && unique.OperationResult?.Kind == OperationResultKind.Completed
            && first.Snapshot.State.ObjectId == 7, "唯一提供方必须完成目标自己的状态提交。");

        DetailsFeature second = new("right");
        using IDisposable secondRegistration = scope.Register(second.Load);
        RequestResult<DetailsReply> ambiguous = await scope.SendAsync<LoadDetailsRequest, DetailsReply>(new(7));
        Require(ambiguous.Kind == RequestResultKind.AmbiguousTarget && second.Snapshot.Version == 0,
            "同类型多开必须由宿主选择，歧义不能隐式调用第一个目标。");

        QueryFeature leftQuery = new(scope, first.Load);
        QueryFeature rightQuery = new(scope, second.Load);
        leftQuery.SetObjectId(7);
        rightQuery.SetObjectId(7);
        await leftQuery.QueryAsync();
        Require(second.Snapshot.Version == 0, "请求左侧端口不能修改右侧实例。");
        await rightQuery.QueryAsync();
        Require(first.Snapshot.State.ObjectId == 7 && second.Snapshot.State.ObjectId == 7
            && first.Snapshot.State.Text == "left: object 7" && second.Snapshot.State.Text == "right: object 7",
            "同一业务对象多开仍必须保留独立状态。");
        Require(leftQuery.Snapshot.State.Replies[0] == "left: object 7"
            && rightQuery.Snapshot.State.Replies[0] == "right: object 7"
            && !leftQuery.Snapshot.OperationStates["QueryAsync"].IsRunning,
            "发送者必须通过自己的操作反馈入口提交响应。");

        QueryFeature coordinator = new(scope, first.Load, second.Load);
        coordinator.SetObjectId(7);
        OperationResult<ImmutableArray<RequestResult<DetailsReply>>> coordinated = await coordinator.QueryAsync();
        Require(coordinated.Kind == OperationResultKind.Completed && coordinated.Value.Length == 2
            && coordinator.Snapshot.State.Replies.Length == 2, "多目标业务必须明确逐端口协调。");

        leftQuery.SetObjectId(404);
        OperationResult<ImmutableArray<RequestResult<DetailsReply>>> businessFailure = await leftQuery.QueryAsync();
        Require(businessFailure.Value[0].OperationResult is { Kind: OperationResultKind.Completed, Value.Found: false },
            "未找到业务对象仍是带业务失败值的正常完成。");
        first.SetEnabled(false);
        OperationResult<ImmutableArray<RequestResult<DetailsReply>>> rejected = await leftQuery.QueryAsync();
        Require(rejected.Value[0].OperationResult is { Kind: OperationResultKind.Rejected, Reason: "ValidationFailed" },
            "契约端口不能绕过目标业务验证。");

        Mediator otherScope = new();
        RequestResult<DetailsReply> outside = await otherScope.SendAsync(new LoadDetailsRequest(7), second.Load);
        Require(outside.Kind == RequestResultKind.TargetUnavailable, "独立范围不会自动看见外部端口。");
        using IDisposable exported = otherScope.Register(second.Load);
        RequestResult<DetailsReply> wired = await otherScope.SendAsync(new LoadDetailsRequest(7), second.Load);
        Require(wired.OperationResult?.Kind == OperationResultKind.Completed, "跨范围由宿主明确接线。");
        second.Load.Deactivate();
        RequestResult<DetailsReply> unavailable = await otherScope.SendAsync(new LoadDetailsRequest(7), second.Load);
        Require(unavailable.Kind == RequestResultKind.TargetUnavailable, "停用端口必须拒绝新请求。");

        Console.WriteLine("Headless mediator loop PASS: reusable query/details Features, same-object multi-open, explicit ports, scope wiring and own-operation feedback; no subscriptions.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

/// <summary>请求一个业务对象的详情，由宿主端口确定接收实例。</summary>
/// <param name="ObjectId">待查询的业务对象标识，与功能实例身份分别管理。</param>
public sealed record LoadDetailsRequest(int ObjectId);

/// <summary>表达详情查询的业务结果，不替代操作执行结论。</summary>
/// <param name="Found">业务对象是否存在。</param>
/// <param name="Text">供查询功能展示的业务摘要。</param>
public sealed record DetailsReply(bool Found, string Text);

/// <summary>保存每个详情实例独立拥有的不可变状态。</summary>
public sealed record DetailsState
{
    /// <summary>获取宿主或本功能允许执行查询的业务条件。</summary>
    [Input]
    public bool Enabled { get; init; } = true;

    /// <summary>获取最近完成查询的业务对象标识。</summary>
    public int ObjectId { get; init; }

    /// <summary>获取由本实例操作反馈提交的详情文字。</summary>
    public string Text { get; init; } = string.Empty;
}

/// <summary>可独立复用和多开的详情功能，仅暴露业务契约端口。</summary>
public sealed partial class DetailsFeature : Feature<DetailsState>
{
    private readonly string displayName;

    /// <summary>创建拥有独立状态和操作身份的详情实例。</summary>
    /// <param name="displayName">用于区分演示中多开详情的业务显示名称。</param>
    public DetailsFeature(string displayName) : base(new())
    {
        this.displayName = displayName;
        Load = CreateLoadAsyncPort();
    }

    /// <summary>获取供宿主接线的强类型详情请求端口。</summary>
    public RequestPort<LoadDetailsRequest, DetailsReply> Load { get; }

    private static bool CanLoad(DetailsState state, LoadDetailsRequest request) => state.Enabled && request.ObjectId > 0;

    [RequestHandler(Validate = nameof(CanLoad))]
    private async ValueTask<DetailsReply> LoadAsync(Operation<DetailsState> operation, LoadDetailsRequest request)
    {
        DetailsReply reply = request.ObjectId == 404 ? new(false, "not found") : new(true, $"{displayName}: object {request.ObjectId}");
        await operation.UpdateAsync(static (state, payload) => state with { ObjectId = payload.ObjectId, Text = payload.Text },
            (request.ObjectId, reply.Text));
        return reply;
    }
}

/// <summary>保存查询实例的输入及经自身反馈入口提交的响应摘要。</summary>
public sealed record QueryState
{
    /// <summary>获取当前允许编辑的业务对象标识。</summary>
    [Input]
    public int ObjectId { get; init; }

    /// <summary>获取明确目标已返回的不可变业务摘要。</summary>
    public ImmutableArray<string> Replies { get; init; } = [];
}

/// <summary>只依赖业务契约和宿主目标端口的可复用查询与协调功能。</summary>
/// <param name="mediator">宿主建立的通信范围。</param>
/// <param name="targets">宿主明确选择的一个或多个业务契约端口。</param>
public sealed partial class QueryFeature(Mediator mediator, params RequestPort<LoadDetailsRequest, DetailsReply>[] targets)
    : Feature<QueryState>(new())
{
    [Operation]
    private async ValueTask<ImmutableArray<RequestResult<DetailsReply>>> QueryAsync(Operation<QueryState> operation)
    {
        ImmutableArray<RequestResult<DetailsReply>>.Builder replies = ImmutableArray.CreateBuilder<RequestResult<DetailsReply>>();
        foreach (RequestPort<LoadDetailsRequest, DetailsReply> target in targets)
        {
            RequestResult<DetailsReply> response = await mediator.SendAsync(new LoadDetailsRequest(operation.Snapshot.ObjectId), target,
                operation.CancellationToken);
            replies.Add(response);
            string text = response.OperationResult?.Value?.Text
                ?? response.OperationResult?.Reason ?? response.Kind.ToString();
            await operation.UpdateAsync(static (state, value) => state with { Replies = state.Replies.Add(value) }, text);
        }

        return replies.ToImmutable();
    }
}
