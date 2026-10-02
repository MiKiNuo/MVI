using System.Collections.Immutable;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.V2Workspace;

/// <summary>请求独立编辑器载入一个业务对象。</summary>
/// <param name="ObjectId">业务对象标识，不充当实例地址。</param>
/// <param name="Text">本次不可变业务文本。</param>
public sealed record WorkspaceLoad(int ObjectId, string Text);

/// <summary>保存每个编辑器独立的不可变业务状态。</summary>
public sealed record WorkspaceEditorState
{
    /// <summary>获取允许编辑的本地文本。</summary>
    [Input]
    public string Text { get; init; } = string.Empty;
    /// <summary>获取业务对象标识。</summary>
    public int ObjectId { get; init; } = 7;
}

/// <summary>保存协调者已收到的独立实例响应。</summary>
public sealed record WorkspaceState
{
    /// <summary>获取明确多目标响应的不可变记录。</summary>
    public ImmutableArray<string> Replies { get; init; } = [];
}

/// <summary>通过契约明确协调多个独立编辑器，不直接调用子业务。</summary>
public sealed partial class WorkspaceFeature : Feature<WorkspaceState>
{
    private readonly Mediator mediator;

    /// <summary>创建宿主所有者及其显式通信范围。</summary>
    /// <param name="mediator">宿主建立的中介者范围。</param>
    public WorkspaceFeature(Mediator mediator) : base(new()) => this.mediator = mediator;

    /// <summary>明确逐目标请求并在自身操作中提交响应。</summary>
    /// <param name="targets">宿主选择的强类型实例端口。</param>
    /// <returns>协调操作的结构化执行结果。</returns>
    public Task<OperationResult<ImmutableArray<string>>> CoordinateAsync(params RequestPort<WorkspaceLoad, string>[] targets)
        => DispatchOperation("Coordinate", null, async operation =>
        {
            ImmutableArray<string>.Builder replies = ImmutableArray.CreateBuilder<string>();
            foreach (RequestPort<WorkspaceLoad, string> target in targets)
            {
                RequestResult<string> result = await mediator.SendAsync(new WorkspaceLoad(7, "coordinated"), target,
                    operation.CancellationToken);
                replies.Add(result.OperationResult?.Value ?? result.Kind.ToString());
            }

            ImmutableArray<string> values = replies.ToImmutable();
            await operation.UpdateAsync(static (state, value) => state with { Replies = value }, values);
            return values;
        }, CancellationToken.None);
}

/// <summary>可在工作区或独立宿主中运行的编辑器，仅提供业务契约。</summary>
public sealed partial class WorkspaceEditorFeature : Feature<WorkspaceEditorState>
{
    /// <summary>创建具有独立状态、运行活动和可替换服务的编辑器。</summary>
    /// <param name="service">构造时解析的外部业务服务。</param>
    public WorkspaceEditorFeature(WorkspaceService service) : base(new())
    {
        Load = CreateRequestPort<WorkspaceLoad, string>("Load", null, async (operation, message) =>
        {
            string result = await service.LoadAsync(message.Text, operation.CancellationToken);
            await operation.UpdateAsync(static (state, value) => state with { ObjectId = value.ObjectId, Text = value.Text },
                (message.ObjectId, Text: result));
            return result;
        });
    }

    /// <summary>获取宿主可明确接线的业务端口。</summary>
    public RequestPort<WorkspaceLoad, string> Load { get; }
}

/// <summary>为工作区提供可替换、可验证范围归属的业务服务。</summary>
public sealed class WorkspaceService : IAsyncDisposable
{
    private readonly Func<string, CancellationToken, Task<string>> run;
    private int disposed;

    /// <summary>创建用于独立编辑器的外部服务。</summary>
    /// <param name="run">可替换的外部调用，默认返回原文本。</param>
    public WorkspaceService(Func<string, CancellationToken, Task<string>>? run = null)
        => this.run = run ?? (static (text, _) => Task.FromResult(text));

    /// <summary>获取资源是否已经真实释放。</summary>
    public bool IsDisposed => Volatile.Read(ref disposed) != 0;

    /// <summary>执行外部业务，并在返回尾部核对服务仍可用。</summary>
    /// <param name="text">本次不可变业务文本。</param>
    /// <param name="token">所属执行的协作取消令牌。</param>
    /// <returns>业务处理结果。</returns>
    public async Task<string> LoadAsync(string text, CancellationToken token)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        string result = await run(text, token).ConfigureAwait(false);
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        return result;
    }

    /// <summary>记录范围完成真实释放。</summary>
    /// <returns>资源释放任务。</returns>
    public ValueTask DisposeAsync() { Interlocked.Exchange(ref disposed, 1); return ValueTask.CompletedTask; }
}
