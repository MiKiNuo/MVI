namespace MiKiNuo.Mvi.Samples.Godot;

/// <summary>指定目标编辑器要加载的业务文本。</summary>
/// <param name="Text">不可变请求文本。</param>
public sealed record EditorLoad(string Text);

/// <summary>表示业务结果，业务拒绝仍可属于正常执行完成。</summary>
/// <param name="Success">业务是否成功。</param>
/// <param name="Text">可供界面展示的业务响应。</param>
public sealed record EditorReply(bool Success, string Text);

/// <summary>保存独立编辑器的不可变状态。</summary>
public sealed record EditorState
{
    /// <summary>获取允许在 IO 期间继续编辑的输入。</summary>
    [Input]
    public string Text { get; init; } = "editor";
    /// <summary>获取业务对象标识，同一对象可具有多个独立实例。</summary>
    public int ObjectId { get; init; } = 7;
    /// <summary>获取已提交的业务响应。</summary>
    public string Result { get; init; } = "Ready";
    /// <summary>获取可预期的业务失败原因。</summary>
    public string? BusinessError { get; init; }
}

/// <summary>保存组合所有者自身的独立业务状态。</summary>
public sealed record CompositionState;

/// <summary>通过 Core 管理子实例和退休实例的组合所有者。</summary>
public sealed partial class CompositionFeature() : Feature<CompositionState>(new());

/// <summary>可独立挂载并只通过强类型契约通信的异步编辑器。</summary>
public sealed partial class EditorFeature : Feature<EditorState>
{
    /// <summary>创建具有独立服务范围的业务实例。</summary>
    /// <param name="service">构造时解析的 scoped 业务服务。</param>
    public EditorFeature(EditorService service) : base(new())
    {
        Service = service;
        LoadPort = CreateHandleLoadPort();
    }

    /// <summary>获取本实例使用的业务服务。</summary>
    public EditorService Service { get; }
    /// <summary>获取由宿主明确接线的业务端口。</summary>
    public RequestPort<EditorLoad, EditorReply> LoadPort { get; }

    [Operation(Validate = nameof(CanLoad))]
    private ValueTask<EditorReply> Load(Operation<EditorState> operation) => RunAsync(operation, operation.Snapshot.Text);

    [RequestHandler(Validate = nameof(CanLoadMessage))]
    private ValueTask<EditorReply> HandleLoad(Operation<EditorState> operation, EditorLoad request) => RunAsync(operation, request.Text);

    private async ValueTask<EditorReply> RunAsync(Operation<EditorState> operation, string text)
    {
        EditorReply reply = await Service.LoadAsync(text, operation.CancellationToken).ConfigureAwait(false);
        await operation.UpdateAsync(static (state, value) => state with
        {
            Result = value.Text,
            BusinessError = value.Success ? null : value.Text,
        }, reply);
        return reply;
    }

    private static bool CanLoad(EditorState state) => state.Text.Length != 0;
    private static bool CanLoadMessage(EditorState state, EditorLoad request) => request.Text.Length != 0;
}

/// <summary>提供可替换的慢 IO，并检测真实尾部执行前是否错误释放了范围。</summary>
public sealed class EditorService : IAsyncDisposable
{
    private int disposeCount;

    /// <summary>获取或设置实际业务 IO，默认包含可协作取消的延迟。</summary>
    public Func<string, CancellationToken, Task<EditorReply>> Run { get; set; } = DefaultAsync;
    /// <summary>获取实际释放次数。</summary>
    public int DisposeCount => Volatile.Read(ref disposeCount);
    /// <summary>获取最近一次 IO 是否在资源仍有效时执行完尾部。</summary>
    public bool TailUsed { get; private set; }

    /// <summary>执行外部 IO，并在真正返回时再次核对资源仍可用。</summary>
    /// <param name="text">本次开始输入或请求载荷。</param>
    /// <param name="token">所属操作的取消令牌。</param>
    /// <returns>业务结果。</returns>
    public async Task<EditorReply> LoadAsync(string text, CancellationToken token)
    {
        ObjectDisposedException.ThrowIf(DisposeCount != 0, this);
        EditorReply reply = await Run(text, token).ConfigureAwait(false);
        ObjectDisposedException.ThrowIf(DisposeCount != 0, this);
        TailUsed = true;
        return reply;
    }

    /// <summary>记录标准 DI 范围的真实资源释放。</summary>
    /// <returns>资源释放任务。</returns>
    public ValueTask DisposeAsync() { Interlocked.Increment(ref disposeCount); return ValueTask.CompletedTask; }

    private static async Task<EditorReply> DefaultAsync(string text, CancellationToken token)
    {
        await Task.Delay(350, token).ConfigureAwait(false);
        if (text == "fault") throw new InvalidOperationException("Simulated service fault");
        return text == "business-fail" ? new(false, "Business request declined") : new(true, "Loaded: " + text);
    }
}
