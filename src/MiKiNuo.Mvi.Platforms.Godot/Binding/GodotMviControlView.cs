using System.ComponentModel;
using System.Windows.Input;
using global::Godot;
using MiKiNuo.Mvi.Presentation.Events;

namespace MiKiNuo.Mvi.Platforms.Godot.Binding;

/// <summary>
/// 表示 Godot Control 版本的 MVI View 基类。
/// </summary>
/// <typeparam name="TViewModel">ViewModel 类型。</typeparam>
public abstract partial class GodotMviControlView<TViewModel> : Control, IMviGodotBindable<TViewModel>
    where TViewModel : class
{
    private GodotMviDisposableBag? _bindingBag;

    /// <inheritdoc />
    public TViewModel? ViewModel { get; private set; }

    /// <inheritdoc />
    public void Bind(TViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        Unbind();
        ViewModel = viewModel;
        GodotMviDisposableBag? bindingBag = new();
        try
        {
            OnBind(viewModel, bindingBag);
            _bindingBag = bindingBag;
            bindingBag = null;
        }
        finally
        {
            bindingBag?.Dispose();
        }
    }

    /// <inheritdoc />
    public void Unbind()
    {
        _bindingBag?.Dispose();
        _bindingBag = null;
        ViewModel = null;
        OnUnbind();
    }

    /// <inheritdoc />
#pragma warning disable CODE0002 // Godot 原生生命周期方法名称固定为 _ExitTree。
    public override void _ExitTree()
#pragma warning restore CODE0002
    {
        Unbind();
        base._ExitTree();
    }

    /// <summary>
    /// 执行具体 View 的绑定逻辑。
    /// </summary>
    /// <param name="viewModel">当前 ViewModel。</param>
    /// <param name="bindings">绑定生命周期集合。</param>
    protected abstract void OnBind(TViewModel viewModel, GodotMviDisposableBag bindings);

    /// <summary>
    /// ViewModel 解绑后执行的扩展点。
    /// </summary>
    protected virtual void OnUnbind()
    {
    }

    /// <summary>
    /// 绑定 ViewModel 属性变化通知。
    /// </summary>
    /// <param name="source">属性变化来源。</param>
    /// <param name="propertyName">属性名称。</param>
    /// <param name="update">更新动作。</param>
    /// <param name="bindings">绑定生命周期集合。</param>
    protected static void BindPropertyChanged(
        INotifyPropertyChanged source,
        string propertyName,
        Action update,
        GodotMviDisposableBag bindings)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        ArgumentNullException.ThrowIfNull(update);
        ArgumentNullException.ThrowIfNull(bindings);

        void Handler(object? sender, PropertyChangedEventArgs args)
        {
            if (string.IsNullOrEmpty(args.PropertyName) ||
                string.Equals(args.PropertyName, propertyName, StringComparison.Ordinal))
            {
                string sourceTypeName = source.GetType().Name;
                GD.Print($"[Godot MVI Binding] {sourceTypeName}.{propertyName} changed");
                update();
            }
        }

        source.PropertyChanged += Handler;
        update();
        bindings.Add(() => source.PropertyChanged -= Handler);
    }

    /// <summary>
    /// 把 Godot Button 绑定到 ICommand。
    /// </summary>
    /// <param name="button">按钮。</param>
    /// <param name="command">命令。</param>
    /// <param name="bindings">绑定生命周期集合。</param>
    /// <param name="parameterFactory">命令参数工厂。</param>
    protected static void BindButton(
        Button button,
        ICommand command,
        GodotMviDisposableBag bindings,
        Func<object?>? parameterFactory = null)
    {
        ArgumentNullException.ThrowIfNull(button);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(bindings);

        object? CreateParameter()
        {
            return parameterFactory?.Invoke();
        }

        void RefreshCanExecute()
        {
            button.Disabled = !command.CanExecute(CreateParameter());
        }

        void CanExecuteChanged(object? sender, EventArgs args)
        {
            RefreshCanExecute();
        }

        BindEvent(
            handler => button.Pressed += handler,
            handler => button.Pressed -= handler,
            command,
            bindings,
            CreateParameter,
            button.Name);

        command.CanExecuteChanged += CanExecuteChanged;
        RefreshCanExecute();
        GD.Print($"[Godot MVI Button] {button.Name} bound, InitialCanExecute={!button.Disabled}");

        bindings.Add(() =>
        {
            command.CanExecuteChanged -= CanExecuteChanged;
        });
    }

    /// <summary>
    /// 绑定无参数 Godot 事件到命令。
    /// </summary>
    /// <param name="subscribe">事件订阅动作。</param>
    /// <param name="unsubscribe">事件取消订阅动作。</param>
    /// <param name="command">目标命令。</param>
    /// <param name="bindings">绑定生命周期集合。</param>
    /// <param name="payloadFactory">命令载荷工厂。</param>
    /// <param name="sourceName">事件来源名称。</param>
    /// <param name="options">事件绑定选项。</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "事件命令绑定注册到 GodotMviDisposableBag，由视图生命周期统一释放。")]
    protected static void BindEvent(
        Action<Action> subscribe,
        Action<Action> unsubscribe,
        ICommand command,
        GodotMviDisposableBag bindings,
        Func<object?>? payloadFactory = null,
        string? sourceName = null,
        MviViewEventBindingOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(subscribe);
        ArgumentNullException.ThrowIfNull(unsubscribe);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(bindings);

        MviViewEventCommandBinding binding = new(command, options ?? MviViewEventBindingOptions.None);

        void Handler()
        {
            binding.Handle(payloadFactory?.Invoke());
        }

        subscribe(Handler);
        bindings.Add(() =>
        {
            unsubscribe(Handler);
            binding.Dispose();
        });
    }

    /// <summary>
    /// 绑定带一个事件参数的 Godot 事件到命令。
    /// </summary>
    /// <typeparam name="TEventArgs">事件参数类型。</typeparam>
    /// <param name="subscribe">事件订阅动作。</param>
    /// <param name="unsubscribe">事件取消订阅动作。</param>
    /// <param name="command">目标命令。</param>
    /// <param name="bindings">绑定生命周期集合。</param>
    /// <param name="payloadFactory">命令载荷工厂。</param>
    /// <param name="sourceName">事件来源名称。</param>
    /// <param name="options">事件绑定选项。</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "事件命令绑定注册到 GodotMviDisposableBag，由视图生命周期统一释放。")]
    protected static void BindEvent<TEventArgs>(
        Action<Action<TEventArgs>> subscribe,
        Action<Action<TEventArgs>> unsubscribe,
        ICommand command,
        GodotMviDisposableBag bindings,
        Func<TEventArgs, object?> payloadFactory,
        string? sourceName = null,
        MviViewEventBindingOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(subscribe);
        ArgumentNullException.ThrowIfNull(unsubscribe);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(payloadFactory);

        MviViewEventCommandBinding binding = new(command, options ?? MviViewEventBindingOptions.None);

        void Handler(TEventArgs args)
        {
            binding.Handle(payloadFactory(args));
        }

        subscribe(Handler);
        bindings.Add(() =>
        {
            unsubscribe(Handler);
            binding.Dispose();
        });
    }

    /// <summary>
    /// 绑定带一个事件参数且使用自定义委托类型的 Godot 事件到命令。
    /// </summary>
    /// <typeparam name="TEventArgs">事件参数类型。</typeparam>
    /// <typeparam name="TDelegate">Godot 事件委托类型。</typeparam>
    /// <param name="subscribe">事件订阅动作。</param>
    /// <param name="unsubscribe">事件取消订阅动作。</param>
    /// <param name="createHandler">委托适配工厂。</param>
    /// <param name="command">目标命令。</param>
    /// <param name="bindings">绑定生命周期集合。</param>
    /// <param name="payloadFactory">命令载荷工厂。</param>
    /// <param name="sourceName">事件来源名称。</param>
    /// <param name="options">事件绑定选项。</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "事件命令绑定注册到 GodotMviDisposableBag，由视图生命周期统一释放。")]
    protected static void BindEvent<TEventArgs, TDelegate>(
        Action<TDelegate> subscribe,
        Action<TDelegate> unsubscribe,
        Func<Action<TEventArgs>, TDelegate> createHandler,
        ICommand command,
        GodotMviDisposableBag bindings,
        Func<TEventArgs, object?> payloadFactory,
        string? sourceName = null,
        MviViewEventBindingOptions? options = null)
        where TDelegate : Delegate
    {
        ArgumentNullException.ThrowIfNull(subscribe);
        ArgumentNullException.ThrowIfNull(unsubscribe);
        ArgumentNullException.ThrowIfNull(createHandler);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(payloadFactory);

        MviViewEventCommandBinding binding = new(command, options ?? MviViewEventBindingOptions.None);

        void Handler(TEventArgs args)
        {
            binding.Handle(payloadFactory(args));
        }

        TDelegate eventHandler = createHandler(Handler);
        subscribe(eventHandler);
        bindings.Add(() =>
        {
            unsubscribe(eventHandler);
            binding.Dispose();
        });
    }
}
