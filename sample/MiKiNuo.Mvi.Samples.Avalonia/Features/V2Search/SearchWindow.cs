using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using MiKiNuo.Mvi.Platforms.Avalonia;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.V2Search;

/// <summary>通过原生输入和只读绑定展示连续搜索及乱序返回。</summary>
public sealed class SearchWindow : Window
{
    private readonly SearchFeature feature;
    private readonly SearchFeature.Projection projection;
    private readonly DemoSearchService demoService = new();
    private readonly TextBox queryInput = new() { PlaceholderText = "搜索；A 较慢，B 较快，fail/fault 演示错误" };
    private readonly TextBox notesInput = new() { PlaceholderText = "搜索期间仍可编辑的备注" };
    private readonly TextBlock results = new() { MinHeight = 80 };
    private readonly TextBlock busy = new();
    private readonly TextBlock error = new();
    private readonly ProgressBar progress = new() { Minimum = 0, Maximum = 100, Height = 12 };
    private readonly TextBlock demonstration = new() { TextWrapping = global::Avalonia.Media.TextWrapping.Wrap };
    private readonly Button demoButton = new() { Content = "演示 A → B（A 最后返回）" };
    private readonly IDisposable queryConnection;
    private readonly IDisposable notesConnection;
    private string lastSubmittedQuery = string.Empty;

    /// <summary>创建独立的搜索功能及其唯一活动投影。</summary>
    /// <param name="search">可替代的外部服务；为空时使用故意忽略取消的演示服务。</param>
    public SearchWindow(Func<string, Func<int, ValueTask>, CancellationToken, ValueTask<SearchReply>>? search = null)
    {
        feature = new SearchFeature(search ?? demoService.SearchAsync);
        projection = AvaloniaProjection.Create<SearchFeature.Projection>(feature.CreateProjection);
        DataContext = projection;
        Title = "MVI v2 — Latest 搜索";
        Width = 640;
        Height = 520;
        queryConnection = AvaloniaProjection.BindInput(projection, queryInput, TextBox.TextProperty, static view => view.Query);
        notesConnection = AvaloniaProjection.BindInput(projection, notesInput, TextBox.TextProperty, static view => view.Notes);
        // BindInput 先提交真实输入；旧投影写回的文本与当前业务输入不一致时不能启动新 IO。
        queryInput.PropertyChanged += QueryChanged;
        results.Bind(TextBlock.TextProperty, new Binding(nameof(projection.ResultsText)) { Mode = BindingMode.OneWay });
        progress.Bind(ProgressBar.ValueProperty, new Binding(nameof(projection.Progress)) { Mode = BindingMode.OneWay });
        busy.Bind(TextBlock.TextProperty, new Binding(nameof(projection.Snapshot))
        {
            Mode = BindingMode.OneWay,
            Converter = new FuncValueConverter<RuntimeSnapshot<SearchState>, string>(snapshot =>
                snapshot?.OperationStates.GetValueOrDefault(nameof(feature.SearchAsync))?.IsRunning == true ? "搜索中…" : "搜索已结束"),
        });
        error.Bind(TextBlock.TextProperty, new Binding(nameof(projection.Snapshot))
        {
            Mode = BindingMode.OneWay,
            Converter = new FuncValueConverter<RuntimeSnapshot<SearchState>, string?>(snapshot =>
            {
                OperationState? operation = snapshot?.OperationStates.GetValueOrDefault(nameof(feature.SearchAsync));
                return snapshot?.State.ErrorMessage ?? operation?.Exception?.Message
                    ?? (operation?.Reason == "ValidationFailed" ? "请输入查询条件。" : null);
            }),
        });
        Button searchButton = new() { Content = "重新搜索" };
        searchButton.Click += (_, _) => StartSearch();
        demoButton.IsEnabled = search is null;
        demoButton.Click += (_, _) => DemoExecution = DemonstrateAsync();
        Content = new StackPanel
        {
            Margin = new Thickness(32),
            Spacing = 16,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Children =
            {
                new TextBlock { Text = "输入即搜索；迟到反馈不会覆盖最新执行", FontSize = 20 },
                queryInput,
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, Children = { searchButton, demoButton } },
                busy,
                progress,
                error,
                results,
                notesInput,
                demonstration,
            },
        };
        Closed += (_, _) =>
        {
            queryInput.PropertyChanged -= QueryChanged;
            queryConnection.Dispose();
            notesConnection.Dispose();
            projection.Dispose();
        };
    }

    internal SearchFeature Feature => feature;
    internal SearchFeature.Projection Projection => projection;
    internal TextBox QueryInput => queryInput;
    internal TextBox NotesInput => notesInput;
    internal TextBlock Results => results;
    internal TextBlock Busy => busy;
    internal TextBlock Error => error;
    internal ProgressBar Progress => progress;
    internal Button DemoButton => demoButton;
    internal TextBlock Demonstration => demonstration;
    internal Task<OperationResult<SearchReply>>? LastExecution { get; private set; }
    internal Task? DemoExecution { get; private set; }

    private async Task DemonstrateAsync()
    {
        demoButton.IsEnabled = false;
        try
        {
            demonstration.Text = "A 等待外部服务；B 将先完成。";
            demoService.AStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Task<OperationResult<SearchReply>>? previous = LastExecution;
            queryInput.SetCurrentValue(TextBox.TextProperty, "A");
            if (ReferenceEquals(previous, LastExecution))
            {
                StartSearch();
            }

            Task<OperationResult<SearchReply>> first = LastExecution!;
            await demoService.AStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
            queryInput.SetCurrentValue(TextBox.TextProperty, "B");
            OperationResult<SearchReply> second = await LastExecution!;
            OperationResult<SearchReply> old = await first;
            demonstration.Text = $"B：{second.Kind}；A 的外部工作最后退出：{old.Kind}。结果和加载状态仍属于 B。";
        }
        catch (Exception exception)
        {
            demonstration.Text = exception.Message;
        }
        finally
        {
            demoService.AStarted = null;
            demoButton.IsEnabled = true;
        }
    }

    private void QueryChanged(object? sender, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.Property == TextBox.TextProperty && queryInput.Text == feature.Snapshot.State.Query
            && queryInput.Text != lastSubmittedQuery)
        {
            StartSearch();
        }
    }

    private void StartSearch()
    {
        lastSubmittedQuery = feature.Snapshot.State.Query;
        LastExecution = feature.SearchAsync();
    }
}
