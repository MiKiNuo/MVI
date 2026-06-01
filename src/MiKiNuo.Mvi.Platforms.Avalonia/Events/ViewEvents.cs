using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using MiKiNuo.Mvi.Presentation.Events;

namespace MiKiNuo.Mvi.Platforms.Avalonia.Events;

/// <summary>
/// 表示 Avalonia ViewEvent 到命令的附加属性入口。
/// </summary>
public sealed class ViewEvents
{
    /// <summary>
    /// 定义显式动作命令附加属性。
    /// </summary>
    public static readonly AttachedProperty<ICommand?> ActionCommandProperty =
        AvaloniaProperty.RegisterAttached<ViewEvents, Button, ICommand?>("ActionCommand");

    /// <summary>
    /// 定义显式动作名称附加属性。
    /// </summary>
    public static readonly AttachedProperty<string?> ActionNameProperty =
        AvaloniaProperty.RegisterAttached<ViewEvents, Button, string?>("ActionName");

    /// <summary>
    /// 定义显式动作防抖附加属性。
    /// </summary>
    public static readonly AttachedProperty<TimeSpan?> ActionDebounceProperty =
        AvaloniaProperty.RegisterAttached<ViewEvents, Button, TimeSpan?>("ActionDebounce");

    /// <summary>
    /// 定义文本变化命令附加属性。
    /// </summary>
    public static readonly AttachedProperty<ICommand?> TextChangedCommandProperty =
        AvaloniaProperty.RegisterAttached<ViewEvents, TextBox, ICommand?>("TextChangedCommand");

    /// <summary>
    /// 定义文本变化防抖附加属性。
    /// </summary>
    public static readonly AttachedProperty<TimeSpan?> TextChangedDebounceProperty =
        AvaloniaProperty.RegisterAttached<ViewEvents, TextBox, TimeSpan?>("TextChangedDebounce");

    /// <summary>
    /// 定义选择变化命令附加属性。
    /// </summary>
    public static readonly AttachedProperty<ICommand?> SelectionChangedCommandProperty =
        AvaloniaProperty.RegisterAttached<ViewEvents, SelectingItemsControl, ICommand?>("SelectionChangedCommand");

    /// <summary>
    /// 定义指针按下命令附加属性。
    /// </summary>
    public static readonly AttachedProperty<ICommand?> PointerPressedCommandProperty =
        AvaloniaProperty.RegisterAttached<ViewEvents, InputElement, ICommand?>("PointerPressedCommand");

    private static readonly AttachedProperty<IDisposable?> ActionSubscriptionProperty =
        AvaloniaProperty.RegisterAttached<ViewEvents, Button, IDisposable?>("ActionSubscription");

    private static readonly AttachedProperty<IDisposable?> TextChangedSubscriptionProperty =
        AvaloniaProperty.RegisterAttached<ViewEvents, TextBox, IDisposable?>("TextChangedSubscription");

    private static readonly AttachedProperty<IDisposable?> SelectionChangedSubscriptionProperty =
        AvaloniaProperty.RegisterAttached<ViewEvents, SelectingItemsControl, IDisposable?>("SelectionChangedSubscription");

    private static readonly AttachedProperty<IDisposable?> PointerPressedSubscriptionProperty =
        AvaloniaProperty.RegisterAttached<ViewEvents, InputElement, IDisposable?>("PointerPressedSubscription");

    static ViewEvents()
    {
        ActionCommandProperty.Changed.AddClassHandler<Button>(static (button, _) => RebindAction(button));
        ActionDebounceProperty.Changed.AddClassHandler<Button>(static (button, _) => RebindAction(button));
        TextChangedCommandProperty.Changed.AddClassHandler<TextBox>(static (textBox, _) => RebindTextChanged(textBox));
        TextChangedDebounceProperty.Changed.AddClassHandler<TextBox>(static (textBox, _) => RebindTextChanged(textBox));
        SelectionChangedCommandProperty.Changed.AddClassHandler<SelectingItemsControl>(static (control, _) => RebindSelectionChanged(control));
        PointerPressedCommandProperty.Changed.AddClassHandler<InputElement>(static (element, _) => RebindPointerPressed(element));
    }

    private ViewEvents()
    {
    }

    /// <summary>
    /// 获取显式动作命令。
    /// </summary>
    /// <param name="button">按钮。</param>
    /// <returns>显式动作命令。</returns>
    public static ICommand? GetActionCommand(Button button)
    {
        ArgumentNullException.ThrowIfNull(button);
        return button.GetValue(ActionCommandProperty);
    }

    /// <summary>
    /// 设置显式动作命令。
    /// </summary>
    /// <param name="button">按钮。</param>
    /// <param name="value">显式动作命令。</param>
    public static void SetActionCommand(Button button, ICommand? value)
    {
        ArgumentNullException.ThrowIfNull(button);
        button.SetValue(ActionCommandProperty, value);
    }

    /// <summary>
    /// 获取显式动作名称。
    /// </summary>
    /// <param name="button">按钮。</param>
    /// <returns>显式动作名称。</returns>
    public static string? GetActionName(Button button)
    {
        ArgumentNullException.ThrowIfNull(button);
        return button.GetValue(ActionNameProperty);
    }

    /// <summary>
    /// 设置显式动作名称。
    /// </summary>
    /// <param name="button">按钮。</param>
    /// <param name="value">显式动作名称。</param>
    public static void SetActionName(Button button, string? value)
    {
        ArgumentNullException.ThrowIfNull(button);
        button.SetValue(ActionNameProperty, value);
    }

    /// <summary>
    /// 获取显式动作防抖时间。
    /// </summary>
    /// <param name="button">按钮。</param>
    /// <returns>显式动作防抖时间。</returns>
    public static TimeSpan? GetActionDebounce(Button button)
    {
        ArgumentNullException.ThrowIfNull(button);
        return button.GetValue(ActionDebounceProperty);
    }

    /// <summary>
    /// 设置显式动作防抖时间。
    /// </summary>
    /// <param name="button">按钮。</param>
    /// <param name="value">显式动作防抖时间。</param>
    public static void SetActionDebounce(Button button, TimeSpan? value)
    {
        ArgumentNullException.ThrowIfNull(button);
        button.SetValue(ActionDebounceProperty, value);
    }

    /// <summary>
    /// 获取文本变化命令。
    /// </summary>
    /// <param name="textBox">文本框。</param>
    /// <returns>文本变化命令。</returns>
    public static ICommand? GetTextChangedCommand(TextBox textBox)
    {
        ArgumentNullException.ThrowIfNull(textBox);
        return textBox.GetValue(TextChangedCommandProperty);
    }

    /// <summary>
    /// 设置文本变化命令。
    /// </summary>
    /// <param name="textBox">文本框。</param>
    /// <param name="value">文本变化命令。</param>
    public static void SetTextChangedCommand(TextBox textBox, ICommand? value)
    {
        ArgumentNullException.ThrowIfNull(textBox);
        textBox.SetValue(TextChangedCommandProperty, value);
    }

    /// <summary>
    /// 获取文本变化防抖时间。
    /// </summary>
    /// <param name="textBox">文本框。</param>
    /// <returns>文本变化防抖时间。</returns>
    public static TimeSpan? GetTextChangedDebounce(TextBox textBox)
    {
        ArgumentNullException.ThrowIfNull(textBox);
        return textBox.GetValue(TextChangedDebounceProperty);
    }

    /// <summary>
    /// 设置文本变化防抖时间。
    /// </summary>
    /// <param name="textBox">文本框。</param>
    /// <param name="value">文本变化防抖时间。</param>
    public static void SetTextChangedDebounce(TextBox textBox, TimeSpan? value)
    {
        ArgumentNullException.ThrowIfNull(textBox);
        textBox.SetValue(TextChangedDebounceProperty, value);
    }

    /// <summary>
    /// 获取选择变化命令。
    /// </summary>
    /// <param name="control">选择控件。</param>
    /// <returns>选择变化命令。</returns>
    public static ICommand? GetSelectionChangedCommand(SelectingItemsControl control)
    {
        ArgumentNullException.ThrowIfNull(control);
        return control.GetValue(SelectionChangedCommandProperty);
    }

    /// <summary>
    /// 设置选择变化命令。
    /// </summary>
    /// <param name="control">选择控件。</param>
    /// <param name="value">选择变化命令。</param>
    public static void SetSelectionChangedCommand(SelectingItemsControl control, ICommand? value)
    {
        ArgumentNullException.ThrowIfNull(control);
        control.SetValue(SelectionChangedCommandProperty, value);
    }

    /// <summary>
    /// 获取指针按下命令。
    /// </summary>
    /// <param name="element">输入元素。</param>
    /// <returns>指针按下命令。</returns>
    public static ICommand? GetPointerPressedCommand(InputElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.GetValue(PointerPressedCommandProperty);
    }

    /// <summary>
    /// 设置指针按下命令。
    /// </summary>
    /// <param name="element">输入元素。</param>
    /// <param name="value">指针按下命令。</param>
    public static void SetPointerPressedCommand(InputElement element, ICommand? value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(PointerPressedCommandProperty, value);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "订阅对象保存到 Avalonia 附加属性，由下次重绑或控件生命周期释放。")]
    private static void RebindAction(Button button)
    {
        DisposeSubscription(button, ActionSubscriptionProperty);
        ICommand? command = GetActionCommand(button);
        if (command is null)
        {
            return;
        }

        MviViewEventCommandBinding binding = new(
            command,
            new MviViewEventBindingOptions(GetActionDebounce(button)));

        void Handler(object? sender, RoutedEventArgs args)
        {
            binding.Handle(new MviActionEventPayload(
                button.Name,
                GetActionName(button) ?? "Click",
                args));
        }

        button.Click += Handler;
        RegisterSubscription(button, ActionSubscriptionProperty, new EventSubscription(binding, () => button.Click -= Handler));
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "订阅对象保存到 Avalonia 附加属性，由下次重绑或控件生命周期释放。")]
    private static void RebindTextChanged(TextBox textBox)
    {
        DisposeSubscription(textBox, TextChangedSubscriptionProperty);
        ICommand? command = GetTextChangedCommand(textBox);
        if (command is null)
        {
            return;
        }

        TextChangedEventSubscription subscription = new(
            new MviViewEventCommandBinding(
                command,
                new MviViewEventBindingOptions(GetTextChangedDebounce(textBox))),
            textBox.Text ?? string.Empty);

        void Handler(object? sender, TextChangedEventArgs args)
        {
            string text = textBox.Text ?? string.Empty;
            string previousText = subscription.PreviousText;
            subscription.PreviousText = text;

            subscription.Binding.Handle(new MviTextChangedEventPayload(
                text,
                previousText,
                true,
                args));
        }

        textBox.TextChanged += Handler;
        subscription.SetUnsubscribe(() => textBox.TextChanged -= Handler);
        RegisterSubscription(textBox, TextChangedSubscriptionProperty, subscription);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "订阅对象保存到 Avalonia 附加属性，由下次重绑或控件生命周期释放。")]
    private static void RebindSelectionChanged(SelectingItemsControl control)
    {
        DisposeSubscription(control, SelectionChangedSubscriptionProperty);
        ICommand? command = GetSelectionChangedCommand(control);
        if (command is null)
        {
            return;
        }

        SelectionChangedEventSubscription subscription = new(
            new MviViewEventCommandBinding(command, MviViewEventBindingOptions.None),
            control.SelectedItem);

        void Handler(object? sender, SelectionChangedEventArgs args)
        {
            object? previousSelectedValue = args.RemovedItems.Count > 0
                ? args.RemovedItems[0]
                : subscription.PreviousSelectedValue;
            object? selectedValue = control.SelectedItem;
            subscription.PreviousSelectedValue = selectedValue;

            subscription.Binding.Handle(new MviSelectionChangedEventPayload(
                selectedValue,
                control.SelectedIndex,
                previousSelectedValue,
                args));
        }

        control.SelectionChanged += Handler;
        subscription.SetUnsubscribe(() => control.SelectionChanged -= Handler);
        RegisterSubscription(control, SelectionChangedSubscriptionProperty, subscription);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "订阅对象保存到 Avalonia 附加属性，由下次重绑或控件生命周期释放。")]
    private static void RebindPointerPressed(InputElement element)
    {
        DisposeSubscription(element, PointerPressedSubscriptionProperty);
        ICommand? command = GetPointerPressedCommand(element);
        if (command is null)
        {
            return;
        }

        MviViewEventCommandBinding binding = new(command, MviViewEventBindingOptions.None);

        void Handler(object? sender, PointerPressedEventArgs args)
        {
            Point position = args.GetPosition(element);
            PointerPoint point = args.GetCurrentPoint(element);
            binding.Handle(new MviPointerEventPayload(
                position.X,
                position.Y,
                MapPointerButton(point.Properties.PointerUpdateKind),
                args.ClickCount,
                true,
                MapModifiers(args.KeyModifiers),
                args));
        }

        element.PointerPressed += Handler;
        RegisterSubscription(element, PointerPressedSubscriptionProperty, new EventSubscription(binding, () => element.PointerPressed -= Handler));
    }

    private static void RegisterSubscription<TControl>(
        TControl control,
        AttachedProperty<IDisposable?> subscriptionProperty,
        EventSubscription subscription)
        where TControl : AvaloniaObject
    {
        if (control is Control visualControl)
        {
            void DetachedHandler(object? sender, VisualTreeAttachmentEventArgs args)
            {
                DisposeSubscription(control, subscriptionProperty);
            }

            visualControl.DetachedFromVisualTree += DetachedHandler;
            subscription.SetDetachedCleanup(() => visualControl.DetachedFromVisualTree -= DetachedHandler);
        }

        control.SetValue(subscriptionProperty, subscription);
    }

    private static void DisposeSubscription<TControl>(
        TControl control,
        AttachedProperty<IDisposable?> subscriptionProperty)
        where TControl : AvaloniaObject
    {
        IDisposable? subscription = control.GetValue(subscriptionProperty);
        subscription?.Dispose();
        control.SetValue(subscriptionProperty, null);
    }

    private static MviPointerButton MapPointerButton(PointerUpdateKind pointerUpdateKind)
    {
        string pointerUpdateKindName = pointerUpdateKind.ToString();

        if (pointerUpdateKindName.Contains("LeftButton", StringComparison.Ordinal))
        {
            return MviPointerButton.Left;
        }

        if (pointerUpdateKindName.Contains("RightButton", StringComparison.Ordinal))
        {
            return MviPointerButton.Right;
        }

        if (pointerUpdateKindName.Contains("MiddleButton", StringComparison.Ordinal))
        {
            return MviPointerButton.Middle;
        }

        if (pointerUpdateKindName.Contains("XButton1", StringComparison.Ordinal))
        {
            return MviPointerButton.XButton1;
        }

        if (pointerUpdateKindName.Contains("XButton2", StringComparison.Ordinal))
        {
            return MviPointerButton.XButton2;
        }

        return MviPointerButton.None;
    }

    private static MviInputModifiers MapModifiers(KeyModifiers modifiers)
    {
        string modifierText = modifiers.ToString();
        MviInputModifiers result = MviInputModifiers.None;

        if (modifierText.Contains("Shift", StringComparison.Ordinal))
        {
            result |= MviInputModifiers.Shift;
        }

        if (modifierText.Contains("Control", StringComparison.Ordinal))
        {
            result |= MviInputModifiers.Control;
        }

        if (modifierText.Contains("Alt", StringComparison.Ordinal))
        {
            result |= MviInputModifiers.Alt;
        }

        if (modifierText.Contains("Meta", StringComparison.Ordinal))
        {
            result |= MviInputModifiers.Meta;
        }

        return result;
    }

    private class EventSubscription : IDisposable
    {
        private readonly Action _unsubscribe;
        private Action _detachCleanup = static () => { };
        private bool _isDisposed;

        public EventSubscription(MviViewEventCommandBinding binding, Action unsubscribe)
        {
            Binding = binding;
            _unsubscribe = unsubscribe;
        }

        public MviViewEventCommandBinding Binding { get; }

        public void SetDetachedCleanup(Action detachCleanup)
        {
            ArgumentNullException.ThrowIfNull(detachCleanup);
            _detachCleanup = detachCleanup;
        }

        public virtual void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _unsubscribe();
            _detachCleanup();
            Binding.Dispose();
            _isDisposed = true;
            GC.SuppressFinalize(this);
        }
    }

    private sealed class TextChangedEventSubscription : EventSubscription
    {
        public TextChangedEventSubscription(MviViewEventCommandBinding binding, string previousText)
            : base(binding, static () => { })
        {
            PreviousText = previousText;
        }

        public string PreviousText { get; set; }

        public void SetUnsubscribe(Action unsubscribe)
        {
            Unsubscribe = unsubscribe;
        }

        private Action Unsubscribe { get; set; } = static () => { };

        public override void Dispose()
        {
            Unsubscribe();
            base.Dispose();
        }
    }

    private sealed class SelectionChangedEventSubscription : EventSubscription
    {
        public SelectionChangedEventSubscription(MviViewEventCommandBinding binding, object? previousSelectedValue)
            : base(binding, static () => { })
        {
            PreviousSelectedValue = previousSelectedValue;
        }

        public object? PreviousSelectedValue { get; set; }

        public void SetUnsubscribe(Action unsubscribe)
        {
            Unsubscribe = unsubscribe;
        }

        private Action Unsubscribe { get; set; } = static () => { };

        public override void Dispose()
        {
            Unsubscribe();
            base.Dispose();
        }
    }
}
