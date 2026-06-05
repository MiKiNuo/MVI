using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Presentation.Events;

namespace MiKiNuo.Mvi.Platforms.Avalonia.Events;

/// <summary>
/// 表示 Avalonia ViewEvent 到命令的附加属性入口。
/// 4 个 Rebind* 方法共享同一 <see cref="Rebind{TControl}(TControl, AttachedProperty{IDisposable?}, IMviCommand?, MviViewEventBindingOptions, Func{MviViewEventCommandBinding, IDisposable})"/>
/// 模板：释放旧订阅、命令为空时短路、构造 <see cref="MviViewEventCommandBinding"/>、
/// 调用具体事件类型的 wire 委托完成事件订阅并返回 <see cref="IDisposable"/> 订阅、
/// 最后通过 <see cref="RegisterSubscription{TControl}(TControl, AttachedProperty{IDisposable?}, IDisposable)"/> 注册订阅。
/// 各 Rebind 方法只关心：取哪个命令/防抖、订阅哪个事件、如何从事件参数构造 payload、如何反订阅。
/// </summary>
/// <remarks>
/// 附加属性类型选择 <see cref="IMviCommand"/> 而非 <c>System.Windows.Input.ICommand</c>：
/// Avalonia 数据绑定系统对自定义类型透明，绑定 <c>{Binding FooCommand}</c> 时
/// 只要源属性实现 <see cref="IMviCommand"/> 即可工作；XAML 不会强制要求 <c>ICommand</c>。
/// 平台控件内置的 <c>Button.Command</c> 属性需要 <c>ICommand</c> 时，
/// 调用方应使用 <c>MiKiNuo.Mvi.Presentation.Command.MviCommandBridge.ToICommand</c> 适配。
/// </remarks>
public sealed class ViewEvents
{
    /// <summary>
    /// 定义显式动作命令附加属性。
    /// </summary>
    public static readonly AttachedProperty<IMviCommand?> ActionCommandProperty =
        AvaloniaProperty.RegisterAttached<ViewEvents, Button, IMviCommand?>("ActionCommand");

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
    public static readonly AttachedProperty<IMviCommand?> TextChangedCommandProperty =
        AvaloniaProperty.RegisterAttached<ViewEvents, TextBox, IMviCommand?>("TextChangedCommand");

    /// <summary>
    /// 定义文本变化防抖附加属性。
    /// </summary>
    public static readonly AttachedProperty<TimeSpan?> TextChangedDebounceProperty =
        AvaloniaProperty.RegisterAttached<ViewEvents, TextBox, TimeSpan?>("TextChangedDebounce");

    /// <summary>
    /// 定义选择变化命令附加属性。
    /// </summary>
    public static readonly AttachedProperty<IMviCommand?> SelectionChangedCommandProperty =
        AvaloniaProperty.RegisterAttached<ViewEvents, SelectingItemsControl, IMviCommand?>("SelectionChangedCommand");

    /// <summary>
    /// 定义指针按下命令附加属性。
    /// </summary>
    public static readonly AttachedProperty<IMviCommand?> PointerPressedCommandProperty =
        AvaloniaProperty.RegisterAttached<ViewEvents, InputElement, IMviCommand?>("PointerPressedCommand");

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
    public static IMviCommand? GetActionCommand(Button button)
    {
        ArgumentNullException.ThrowIfNull(button);
        return button.GetValue(ActionCommandProperty);
    }

    /// <summary>
    /// 设置显式动作命令。
    /// </summary>
    /// <param name="button">按钮。</param>
    /// <param name="value">显式动作命令。</param>
    public static void SetActionCommand(Button button, IMviCommand? value)
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
    public static IMviCommand? GetTextChangedCommand(TextBox textBox)
    {
        ArgumentNullException.ThrowIfNull(textBox);
        return textBox.GetValue(TextChangedCommandProperty);
    }

    /// <summary>
    /// 设置文本变化命令。
    /// </summary>
    /// <param name="textBox">文本框。</param>
    /// <param name="value">文本变化命令。</param>
    public static void SetTextChangedCommand(TextBox textBox, IMviCommand? value)
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
    public static IMviCommand? GetSelectionChangedCommand(SelectingItemsControl control)
    {
        ArgumentNullException.ThrowIfNull(control);
        return control.GetValue(SelectionChangedCommandProperty);
    }

    /// <summary>
    /// 设置选择变化命令。
    /// </summary>
    /// <param name="control">选择控件。</param>
    /// <param name="value">选择变化命令。</param>
    public static void SetSelectionChangedCommand(SelectingItemsControl control, IMviCommand? value)
    {
        ArgumentNullException.ThrowIfNull(control);
        control.SetValue(SelectionChangedCommandProperty, value);
    }

    /// <summary>
    /// 获取指针按下命令。
    /// </summary>
    /// <param name="element">输入元素。</param>
    /// <returns>指针按下命令。</returns>
    public static IMviCommand? GetPointerPressedCommand(InputElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.GetValue(PointerPressedCommandProperty);
    }

    /// <summary>
    /// 设置指针按下命令。
    /// </summary>
    /// <param name="element">输入元素。</param>
    /// <param name="value">指针按下命令。</param>
    public static void SetPointerPressedCommand(InputElement element, IMviCommand? value)
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
        Rebind(
            button,
            ActionSubscriptionProperty,
            GetActionCommand(button),
            new MviViewEventBindingOptions(GetActionDebounce(button)),
            binding =>
            {
                EventHandler<RoutedEventArgs> handler = (_, args) =>
                {
                    binding.Handle(new MviActionEventPayload(
                        button.Name,
                        GetActionName(button) ?? "Click",
                        args));
                };

                button.Click += handler;
                return new EventSubscription
                {
                    Binding = binding,
                    Unsubscribe = () => button.Click -= handler,
                };
            });
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "订阅对象保存到 Avalonia 附加属性，由下次重绑或控件生命周期释放。")]
    private static void RebindTextChanged(TextBox textBox)
    {
        TextChangedEventSubscription subscription = new(textBox.Text ?? string.Empty);
        Rebind(
            textBox,
            TextChangedSubscriptionProperty,
            GetTextChangedCommand(textBox),
            new MviViewEventBindingOptions(GetTextChangedDebounce(textBox)),
            binding =>
            {
                subscription.Binding = binding;
                EventHandler<TextChangedEventArgs> handler = (_, args) =>
                {
                    string text = textBox.Text ?? string.Empty;
                    string previousText = subscription.PreviousText;
                    subscription.PreviousText = text;
                    binding.Handle(new MviTextChangedEventPayload(text, previousText, true, args));
                };

                textBox.TextChanged += handler;
                subscription.Unsubscribe = () => textBox.TextChanged -= handler;
                return subscription;
            });
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "订阅对象保存到 Avalonia 附加属性，由下次重绑或控件生命周期释放。")]
    private static void RebindSelectionChanged(SelectingItemsControl control)
    {
        SelectionChangedEventSubscription subscription = new(control.SelectedItem);
        Rebind(
            control,
            SelectionChangedSubscriptionProperty,
            GetSelectionChangedCommand(control),
            MviViewEventBindingOptions.None,
            binding =>
            {
                subscription.Binding = binding;
                EventHandler<SelectionChangedEventArgs> handler = (_, args) =>
                {
                    object? previousSelectedValue = args.RemovedItems.Count > 0
                        ? args.RemovedItems[0]
                        : subscription.PreviousSelectedValue;
                    object? selectedValue = control.SelectedItem;
                    subscription.PreviousSelectedValue = selectedValue;
                    binding.Handle(new MviSelectionChangedEventPayload(
                        selectedValue,
                        control.SelectedIndex,
                        previousSelectedValue,
                        args));
                };

                control.SelectionChanged += handler;
                subscription.Unsubscribe = () => control.SelectionChanged -= handler;
                return subscription;
            });
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "订阅对象保存到 Avalonia 附加属性，由下次重绑或控件生命周期释放。")]
    private static void RebindPointerPressed(InputElement element)
    {
        Rebind(
            element,
            PointerPressedSubscriptionProperty,
            GetPointerPressedCommand(element),
            MviViewEventBindingOptions.None,
            binding =>
            {
                EventHandler<PointerPressedEventArgs> handler = (_, args) =>
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
                };

                element.PointerPressed += handler;
                return new EventSubscription
                {
                    Binding = binding,
                    Unsubscribe = () => element.PointerPressed -= handler,
                };
            });
    }

    /// <summary>
    /// 通用重绑模板：释放旧订阅、命令为空时短路、创建绑定并交给具体事件类型的 wire 委托完成事件订阅和反订阅。
    /// </summary>
    /// <typeparam name="TControl">Avalonia 控件类型。</typeparam>
    /// <param name="control">目标控件。</param>
    /// <param name="subscriptionProperty">存储当前订阅的附加属性。</param>
    /// <param name="command">附加属性上读取到的命令；为空时直接返回。</param>
    /// <param name="options">绑定选项（含防抖时间）。</param>
    /// <param name="wire">完成"绑定事件 → 构造 payload → 返回订阅实例"三件事的委托。</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "订阅对象保存到 Avalonia 附加属性，由下次重绑或控件生命周期释放。")]
    private static void Rebind<TControl>(
        TControl control,
        AttachedProperty<IDisposable?> subscriptionProperty,
        IMviCommand? command,
        MviViewEventBindingOptions options,
        Func<MviViewEventCommandBinding, IDisposable> wire)
        where TControl : AvaloniaObject
    {
        DisposeSubscription(control, subscriptionProperty);
        if (command is null)
        {
            return;
        }

        MviViewEventCommandBinding binding = new(command, options);
        IDisposable subscription = wire(binding);
        RegisterSubscription(control, subscriptionProperty, subscription);
    }

    private static void RegisterSubscription<TControl>(
        TControl control,
        AttachedProperty<IDisposable?> subscriptionProperty,
        IDisposable subscription)
        where TControl : AvaloniaObject
    {
        if (subscription is EventSubscription eventSubscription && control is Control visualControl)
        {
            void DetachedHandler(object? sender, VisualTreeAttachmentEventArgs args)
            {
                DisposeSubscription(control, subscriptionProperty);
            }

            visualControl.DetachedFromVisualTree += DetachedHandler;
            eventSubscription.SetDetachedCleanup(() => visualControl.DetachedFromVisualTree -= DetachedHandler);
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
        return pointerUpdateKind switch
        {
            PointerUpdateKind.LeftButtonPressed or PointerUpdateKind.LeftButtonReleased => MviPointerButton.Left,
            PointerUpdateKind.RightButtonPressed or PointerUpdateKind.RightButtonReleased => MviPointerButton.Right,
            PointerUpdateKind.MiddleButtonPressed or PointerUpdateKind.MiddleButtonReleased => MviPointerButton.Middle,
            PointerUpdateKind.XButton1Pressed or PointerUpdateKind.XButton1Released => MviPointerButton.XButton1,
            PointerUpdateKind.XButton2Pressed or PointerUpdateKind.XButton2Released => MviPointerButton.XButton2,
            _ => MviPointerButton.None,
        };
    }

    private static MviInputModifiers MapModifiers(KeyModifiers modifiers)
    {
        MviInputModifiers result = MviInputModifiers.None;

        if (modifiers.HasFlag(KeyModifiers.Shift))
        {
            result |= MviInputModifiers.Shift;
        }

        if (modifiers.HasFlag(KeyModifiers.Control))
        {
            result |= MviInputModifiers.Control;
        }

        if (modifiers.HasFlag(KeyModifiers.Alt))
        {
            result |= MviInputModifiers.Alt;
        }

        if (modifiers.HasFlag(KeyModifiers.Meta))
        {
            result |= MviInputModifiers.Meta;
        }

        return result;
    }

    /// <summary>
    /// 单个附加属性上的命令订阅封装。
    /// <see cref="Binding"/> 与 <see cref="Unsubscribe"/> 由 wire 委托在订阅时填充，
    /// <see cref="SetDetachedCleanup(Action)"/> 由 <see cref="RegisterSubscription{TControl}"/> 在
    /// 控件存在可视树时填充。Dispose 顺序：先解绑事件、再解绑可视树 detach 钩子、最后释放 binding。
    /// </summary>
    private class EventSubscription : IDisposable
    {
        private Action? _detachCleanup;
        private bool _isDisposed;

        public MviViewEventCommandBinding? Binding { get; set; }

        public Action? Unsubscribe { get; set; }

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

            _isDisposed = true;
            Unsubscribe?.Invoke();
            _detachCleanup?.Invoke();
            Binding?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// 文本变化订阅：除了通用订阅职责外，需要在事件触发时记住"上一次文本"用于 diff payload。
    /// </summary>
    private sealed class TextChangedEventSubscription : EventSubscription
    {
        public TextChangedEventSubscription(string previousText)
        {
            PreviousText = previousText;
        }

        public string PreviousText { get; set; }
    }

    /// <summary>
    /// 选择变化订阅：除了通用订阅职责外，需要在事件触发时记住"上一次选中项"用于 diff payload。
    /// </summary>
    private sealed class SelectionChangedEventSubscription : EventSubscription
    {
        public SelectionChangedEventSubscription(object? previousSelectedValue)
        {
            PreviousSelectedValue = previousSelectedValue;
        }

        public object? PreviousSelectedValue { get; set; }
    }
}
