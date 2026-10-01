using Avalonia.Controls;
using Avalonia.Threading;
using MiKiNuo.Mvi.Platforms.Avalonia;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.V2Input;

internal static class InputFormVerification
{
    internal static async Task<string> RunAsync(InputFormWindow window)
    {
        Require(window.TryGetPlatformHandle() is not null && window.TryGetPlatformHandle()!.Handle != IntPtr.Zero, "真实平台窗口句柄");
        Require(Dispatcher.UIThread.CheckAccess(), "验收运行于 UI 线程");
        int uiThread = Environment.CurrentManagedThreadId;
        List<long> displayed = [];
        List<string> changed = [];
        bool correctThread = true;
        window.Projection.PropertyChanged += (_, args) =>
        {
            correctThread &= Dispatcher.UIThread.CheckAccess() && Environment.CurrentManagedThreadId == uiThread;
            changed.Add(args.PropertyName!);
            if (args.PropertyName == nameof(window.Projection.Snapshot))
            {
                displayed.Add(window.Projection.Snapshot.Version);
            }
        };

        long before = window.Feature.Snapshot.Version;
        window.NameInput.SetCurrentValue(TextBox.TextProperty, "  Ada  ");
        Require(window.Feature.Snapshot.State.Name == "Ada", "TextBox 原生输入连接调用附加纯规则");
        Require(window.Feature.Snapshot.Version == before + 1, "原生输入仅提交一次");
        Require(window.Projection.Name == "", "输入后的显示仍为旧提交，直到 UI 调度");
        await FlushAsync();
        Require(window.NameInput.Text == "Ada" && window.Greeting.Text == "你好，Ada", "名称及派生展示来自已提交快照");

        before = window.Feature.Snapshot.Version;
        changed.Clear();
        window.NameInput.SetCurrentValue(TextBox.TextProperty, "  Ada  ");
        Require(window.Feature.Snapshot.Version == before + 1, "同值归一化输入仅提交一次");
        await FlushAsync();
        Require(window.NameInput.Text == "Ada", "同值归一化后原生输入控件回到已提交值");
        Require(!changed.Contains(nameof(window.Projection.Greeting)), "同值归一化不刷新无关派生展示");

        before = window.Feature.Snapshot.Version;
        window.NameInput.SetCurrentValue(TextBox.TextProperty, "B");
        window.NameInput.SetCurrentValue(TextBox.TextProperty, "Ada");
        Require(window.Feature.Snapshot.Version == before + 2, "展示等待期间 A/B/A 的两次真实输入均提交");
        Require(window.Feature.Snapshot.State.Name == "Ada", "等于旧显示的最后一次输入不能被反馈过滤吞掉");
        await FlushAsync();
        Require(window.NameInput.Text == "Ada", "合并展示保留最后一次真实输入");

        bool crossFieldInput = false;
        window.Projection.PropertyChanged += (_, args) =>
        {
            if (!crossFieldInput && args.PropertyName == nameof(window.Projection.Name))
            {
                crossFieldInput = true;
                window.AmountInput.SetCurrentValue(TextBox.TextProperty, "-");
            }
        };
        before = window.Feature.Snapshot.Version;
        window.NameInput.SetCurrentValue(TextBox.TextProperty, "cross-field");
        await FlushAsync();
        Require(window.Feature.Snapshot.Version == before + 2 && window.Feature.Snapshot.State.AmountText == "-", "Name 展示通知中的 Amount 真实输入必须提交");
        Require(window.AmountInput.Text == "-", "跨字段通知输入最终按提交快照展示");

        bool sameFieldInput = false;
        window.Projection.PropertyChanged += (_, args) =>
        {
            if (!sameFieldInput && args.PropertyName == nameof(window.Projection.Name))
            {
                sameFieldInput = true;
                window.NameInput.SetCurrentValue(TextBox.TextProperty, "callback-new-value");
            }
        };
        before = window.Feature.Snapshot.Version;
        window.NameInput.SetCurrentValue(TextBox.TextProperty, "same-field");
        await FlushAsync();
        Require(window.Feature.Snapshot.Version == before + 2 && window.Feature.Snapshot.State.Name == "callback-new-value", "Name 展示通知中的不同新输入必须提交");
        Require(window.NameInput.Text == "callback-new-value", "同字段通知输入最终按提交快照展示");

        bool sameFieldAba = false;
        window.Projection.PropertyChanged += (_, args) =>
        {
            if (!sameFieldAba && args.PropertyName == nameof(window.Projection.Name))
            {
                sameFieldAba = true;
                window.NameInput.SetCurrentValue(TextBox.TextProperty, "callback-B");
                window.NameInput.SetCurrentValue(TextBox.TextProperty, "callback-A");
            }
        };
        before = window.Feature.Snapshot.Version;
        window.NameInput.SetCurrentValue(TextBox.TextProperty, "callback-A");
        await FlushAsync();
        Require(window.Feature.Snapshot.Version == before + 3 && window.Feature.Snapshot.State.Name == "callback-A", "同字段通知内 A/B/A 的全部真实输入必须提交");
        Require(window.NameInput.Text == "callback-A", "同字段通知内最后一次 A 输入最终展示");

        try
        {
            using IDisposable invalid = AvaloniaProjection.BindInput(window.Projection, new TextBox(), TextBox.TextProperty, static view => view.Greeting);
            throw new InvalidOperationException("只读投影不能成为输入回写入口。");
        }
        catch (ArgumentException)
        {
            // 原生输入连接必须拒绝只读字段。
        }

        TextBox disconnected = new();
        using IDisposable oldInput = AvaloniaProjection.BindInput(window.Projection, disconnected, TextBox.TextProperty, static view => view.Name);
        oldInput.Dispose();
        before = window.Feature.Snapshot.Version;
        disconnected.SetCurrentValue(TextBox.TextProperty, "old-view-input");
        Require(window.Feature.Snapshot.Version == before, "释放原生输入连接后旧控件不再回写");

        window.Feature.SetAmountText("");
        await FlushAsync();
        window.AmountInput.SetCurrentValue(TextBox.TextProperty, "-");
        Require(window.Feature.Snapshot.State.AmountText == "-", "模型保存中间输入");
        await FlushAsync();
        Require(window.AmountDisplay.Text == "金额尚未完成", "只读中间输入展示");
        window.AmountInput.SetCurrentValue(TextBox.TextProperty, "12.5");
        await FlushAsync();
        Require(window.AmountDisplay.Text == "12.50", "普通回写和只读金额展示");

        changed.Clear();
        window.Feature.SetOther(1);
        await FlushAsync();
        Require(!changed.Contains(nameof(window.Projection.Name)) && !changed.Contains(nameof(window.Projection.Greeting)), "无关字段不通知名称展示");
        changed.Clear();
        window.Feature.SetOther(1);
        await FlushAsync();
        Require(!changed.Contains(nameof(window.Projection.Other)), "同值字段不重复通知");

        displayed.Clear();
        before = window.Feature.Snapshot.Version;
        Task inputs = Task.Run(() =>
        {
            for (int index = 0; index < 500; index++)
            {
                window.Feature.SetName("background-" + index);
            }
        });
        Require(inputs.Wait(TimeSpan.FromSeconds(10)), "后台输入不等待 UI 绘制或提交门外回调");
        Require(window.Feature.Snapshot.Version == before + 500, "展示合并不丢失任何业务输入");
        await FlushAsync();
        Require(displayed.Count == 1 && displayed[0] == before + 500, "默认合并已等待的 500 次展示");
        Require(window.NameInput.Text == "background-499" && window.Greeting.Text == "你好，background-499", "后台输入经 UI 原生绑定展示");

        InputFormFeature every = new();
        using InputFormFeature.Projection everyProjection = AvaloniaProjection.Create<InputFormFeature.Projection>(every.CreateProjection, ProjectionMode.EveryCommit);
        TextBlock everyLabel = new() { DataContext = everyProjection };
        ((StackPanel)window.Content!).Children.Add(everyLabel);
        using IDisposable binding = everyLabel.Bind(TextBlock.TextProperty, new global::Avalonia.Data.Binding(nameof(everyProjection.Name)));
        List<string> intermediate = [];
        List<long> versions = [];
        everyProjection.PropertyChanged += (_, args) =>
        {
            correctThread &= Dispatcher.UIThread.CheckAccess() && Environment.CurrentManagedThreadId == uiThread;
            if (args.PropertyName == nameof(everyProjection.Snapshot))
            {
                intermediate.Add(everyLabel.Text!);
                versions.Add(everyProjection.Snapshot.Version);
            }
        };
        Task everyInputs = Task.Run(() => { every.SetName("A"); every.SetName("B"); every.SetName("C"); });
        Require(everyInputs.Wait(TimeSpan.FromSeconds(10)), "逐次模式后台输入完成");
        await FlushAsync();
        Require(string.Join(",", intermediate) == "A,B,C" && string.Join(",", versions) == "1,2,3", "原生绑定逐次展示全部中间快照");
        Require(correctThread, "所有字段通知与绑定 getter 在 UI 线程");
        Require(displayed.SequenceEqual(displayed.Order()), "展示版本单调");
        return "PASS v2-input: real Windows Avalonia Window; native input properties/readonly OneWay bindings; unchanged normalization; pending A/B/A commits; cross-field/same-field notification inputs; notification A/B/A commits; readonly input rejection; disposed input disconnected; pure input rule; intermediate input; field filtering; UI thread; 500 commits/1 coalesced display; EveryCommit A,B,C versions 1,2,3.";
    }

    private static async Task FlushAsync() => await Dispatcher.UIThread.InvokeAsync(static () => { }, DispatcherPriority.Background);

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
