using System.Collections.Immutable;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.V2Search;

/// <summary>保存搜索输入、不可变结果与业务反馈。</summary>
public sealed record SearchState
{
    /// <summary>获取可编辑的查询条件。</summary>
    [Input]
    public string Query { get; init; } = string.Empty;

    /// <summary>获取搜索期间仍可编辑的无关备注。</summary>
    [Input]
    public string Notes { get; init; } = string.Empty;

    /// <summary>获取最近有效查询返回的不可变结果。</summary>
    public ImmutableArray<string> Results { get; init; } = [];

    /// <summary>获取最近有效执行报告的进度。</summary>
    public int Progress { get; init; }

    /// <summary>获取最近有效执行的业务错误。</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>获取结果列表的本地展示文本。</summary>
    public string ResultsText => string.Join(Environment.NewLine, Results);
}
