using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using MiKiNuo.Mvi.Platforms.Avalonia;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.V2Input;

/// <summary>使用生成的本地投影和原生绑定展示 v2 输入表单。</summary>
public sealed class InputFormWindow : Window
{
    private readonly InputFormFeature feature;
    private readonly InputFormFeature.Projection projection;
    private readonly TextBox nameInput = new() { PlaceholderText = "显示名称" };
    private readonly TextBox amountInput = new() { PlaceholderText = "金额（允许输入 - 等中间值）" };
    private readonly TextBlock greeting = new();
    private readonly TextBlock amountDisplay = new();
    private readonly IDisposable nameConnection;
    private readonly IDisposable amountConnection;

    /// <summary>创建一个独立功能及其唯一活动的本地 View 投影。</summary>
    public InputFormWindow()
    {
        feature = new InputFormFeature();
        projection = AvaloniaProjection.Create<InputFormFeature.Projection>(feature.CreateProjection);
        DataContext = projection;
        Title = "MVI v2 — 输入与已提交快照";
        Width = 520;
        Height = 360;
        nameConnection = AvaloniaProjection.BindInput(projection, nameInput, TextBox.TextProperty, static view => view.Name);
        amountConnection = AvaloniaProjection.BindInput(projection, amountInput, TextBox.TextProperty, static view => view.AmountText);
        greeting.Bind(TextBlock.TextProperty, new Binding(nameof(projection.Greeting)) { Mode = BindingMode.OneWay });
        amountDisplay.Bind(TextBlock.TextProperty, new Binding(nameof(projection.AmountDisplay)) { Mode = BindingMode.OneWay });
        Content = new StackPanel
        {
            Margin = new Thickness(32),
            Spacing = 16,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Children =
            {
                new TextBlock { Text = "一次声明输入，由生成投影连接原生绑定", FontSize = 20 },
                nameInput,
                greeting,
                amountInput,
                amountDisplay
            }
        };
        Closed += (_, _) =>
        {
            nameConnection.Dispose();
            amountConnection.Dispose();
            projection.Dispose();
        };
    }

    internal InputFormFeature Feature => feature;
    internal InputFormFeature.Projection Projection => projection;
    internal TextBox NameInput => nameInput;
    internal TextBox AmountInput => amountInput;
    internal TextBlock Greeting => greeting;
    internal TextBlock AmountDisplay => amountDisplay;
}
