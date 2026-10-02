using System.Collections.Immutable;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.V2Search;

/// <summary>表达搜索服务的业务返回值，业务失败仍可正常完成执行。</summary>
/// <param name="Results">不可变搜索结果。</param>
/// <param name="ErrorMessage">可预期的业务错误。</param>
public sealed record SearchReply(ImmutableArray<string> Results, string? ErrorMessage = null);

/// <summary>使用 Latest 身份隔离忽略取消的迟到搜索。</summary>
/// <param name="search">外部服务；进度回调与结果都经同一受控反馈入口。</param>
public sealed partial class SearchFeature(Func<string, Func<int, ValueTask>, CancellationToken, ValueTask<SearchReply>> search)
    : Feature<SearchState>(new())
{
    [Operation(Validate = nameof(CanSearch), Concurrency = OperationConcurrency.Latest)]
    private async ValueTask<SearchReply> SearchAsync(Operation<SearchState> operation)
    {
        await operation.UpdateAsync(static (state, _) => state with { Progress = 0, ErrorMessage = null }, 0);
        SearchReply reply = await search(operation.Snapshot.Query,
            progress => operation.UpdateAsync(static (state, value) => state with { Progress = value }, progress),
            operation.CancellationToken);
        await operation.UpdateAsync(static (state, result) => state with
        {
            Results = result.Results,
            ErrorMessage = result.ErrorMessage,
            Progress = 100,
        }, reply);
        return reply;
    }

    private static bool CanSearch(SearchState state) => !string.IsNullOrWhiteSpace(state.Query);
}

internal sealed class DemoSearchService
{
    internal TaskCompletionSource? AStarted { get; set; }

    internal async ValueTask<SearchReply> SearchAsync(string query, Func<int, ValueTask> progress, CancellationToken cancellationToken)
    {
        // 演示服务刻意忽略取消；外部 IO 的真实退出不等于反馈身份仍然有效。
        await progress(10);
        Task waiting = Task.Delay(query == "A" ? 1800 : 350, CancellationToken.None);
        if (query == "A")
        {
            AStarted?.TrySetResult();
        }

        await waiting;
        await progress(75);
        if (query == "fault")
        {
            throw new InvalidOperationException("搜索服务发生演示故障。");
        }

        return query == "fail" ? new SearchReply([], "搜索服务返回了业务失败。")
            : new SearchReply([$"{query} · 最新结果 1", $"{query} · 最新结果 2"]);
    }
}
