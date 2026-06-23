using System.ComponentModel;
using global::Godot;
using MiKiNuo.Mvi.Application.DI;
using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Presentation.Disposables;

namespace MiKiNuo.Mvi.Platforms.Godot.Binding;

/// <summary>
/// 表示 Godot Control 版本的 MVI View 基类。
/// </summary>
/// <typeparam name="TViewModel">ViewModel 类型。</typeparam>
public abstract partial class GodotMviControlView<TViewModel> : Control, IMviGodotBindable<TViewModel>
    where TViewModel : class
{
    private MviDisposableBag? _bindingBag;
    private IMviResolver? _resolver;

    /// <summary>
    /// 获取当前绑定的 ViewModel。
    /// </summary>
    public TViewModel? ViewModel { get; private set; }

    /// <summary>
    /// 获取最近一次 <c>Bind(viewModel, resolver)</c> 传入的 <see cref="IMviResolver"/>；未通过 2-arg 重载绑定时为 <c>null</c>。
    /// <para>
    /// 父 View 的 <c>OnBind</c> 在构造子 View 的槽位绑定时需读取本字段并向下传递；
    /// 子 View 的 <c>OnBindSlots</c> 则由源生成器在编译期 emit 时直接拿到 <c>resolver</c> 形参，
    /// 不必读取本字段。
    /// </para>
    /// </summary>
    protected IMviResolver? Resolver => _resolver;

    /// <summary>
    /// 绑定 ViewModel 与子组件解析容器。
    /// </summary>
    /// <param name="viewModel">需要绑定的 ViewModel。</param>
    /// <param name="resolver">用于解析子 ViewModel 与子 View 的 <see cref="IMviResolver"/>。</param>
    public void Bind(TViewModel viewModel, IMviResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(resolver);

        Unbind();
        ViewModel = viewModel;
        _resolver = resolver;
        MviDisposableBag? bindingBag = new();
        try
        {
            OnBind(viewModel, bindingBag);
            OnBindSlots(viewModel, bindingBag, resolver);
            _bindingBag = bindingBag;
            bindingBag = null;
        }
        finally
        {
            bindingBag?.Dispose();
        }
    }

    /// <summary>
    /// 解绑当前 ViewModel。
    /// </summary>
    public void Unbind()
    {
        _bindingBag?.Dispose();
        _bindingBag = null;
        _resolver = null;
        ViewModel = null;
        OnUnbind();
    }

    /// <summary>
    /// 节点离开场景树时解绑。
    /// </summary>
#pragma warning disable CODE0002 // Godot 原生生命周期方法名称固定为 _ExitTree。
    public override void _ExitTree()
#pragma warning restore CODE0002
    {
        Unbind();
        base._ExitTree();
    }

    /// <summary>
    /// 执行具体 View 的绑定逻辑。
    /// <para>
    /// 子类 <c>override</c> 本方法，通过 <c>ToEventSource().EventName</c> 获取
    /// <see cref="Application.MVI.EventBinding.IEventSource{TEvent}"/>，
    /// 调用 <see cref="Presentation.Binding.EventBindingExtensions.BindTo{TEvent}"/> 把事件映射为 Intent 并注册到 <paramref name="bindings"/>。
    /// 基类提供空实现；不依赖事件绑定的 View 得到零成本默认行为。
    /// </para>
    /// </summary>
    /// <param name="viewModel">当前 ViewModel。</param>
    /// <param name="bindings">绑定生命周期集合。</param>
    protected virtual void OnBind(TViewModel viewModel, MviDisposableBag bindings)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(bindings);
    }

    /// <summary>
    /// 组合模式槽位绑定钩子；由源生成器在子类 <c>override</c> 实现，
    /// 子类可在 <see cref="OnBind"/> 末尾手动调用本方法以激活源生成器 emit 的槽位绑定逻辑。
    /// <para>
    /// 基类提供空实现；不依赖源生成器的 View（如无 [MviSlot] 字段）会得到一个零成本的默认行为。
    /// </para>
    /// </summary>
    /// <param name="viewModel">当前 ViewModel。</param>
    /// <param name="bindings">绑定生命周期集合。</param>
    /// <param name="resolver">由 <c>Bind</c> 传入的 <see cref="IMviResolver"/>；源生成器用它解析子 ViewModel 与子 View。</param>
    protected virtual void OnBindSlots(TViewModel viewModel, MviDisposableBag bindings, IMviResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(resolver);
    }

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
        MviDisposableBag bindings)
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
                update();
            }
        }

        source.PropertyChanged += Handler;
        update();
        bindings.Add(() => source.PropertyChanged -= Handler);
    }

    /// <summary>
    /// 把 Godot Button 绑定到 MVI 命令。
    /// </summary>
    /// <param name="button">按钮。</param>
    /// <param name="command">MVI 命令。</param>
    /// <param name="bindings">绑定生命周期集合。</param>
    /// <param name="parameterFactory">命令参数工厂。</param>
    protected static void BindButton(
        Button button,
        IMviCommand command,
        MviDisposableBag bindings,
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

        bindings.Add(() =>
        {
            command.CanExecuteChanged -= CanExecuteChanged;
        });
    }

    /// <summary>
    /// 绑定无参数 Godot 事件到 MVI 命令。
    /// </summary>
    /// <param name="subscribe">事件订阅动作。</param>
    /// <param name="unsubscribe">事件取消订阅动作。</param>
    /// <param name="command">目标 MVI 命令。</param>
    /// <param name="bindings">绑定生命周期集合。</param>
    /// <param name="payloadFactory">命令载荷工厂。</param>
    /// <param name="sourceName">事件来源名称。</param>
    protected static void BindEvent(
        Action<Action> subscribe,
        Action<Action> unsubscribe,
        IMviCommand command,
        MviDisposableBag bindings,
        Func<object?>? payloadFactory = null,
        string? sourceName = null)
    {
        ArgumentNullException.ThrowIfNull(subscribe);
        ArgumentNullException.ThrowIfNull(unsubscribe);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(bindings);

        void Handler()
        {
            object? payload = payloadFactory?.Invoke();
            if (command.CanExecute(payload))
            {
                command.Execute(payload);
            }
        }

        subscribe(Handler);
        bindings.Add(() => unsubscribe(Handler));
    }

    /// <summary>
    /// 绑定带一个事件参数的 Godot 事件到 MVI 命令。
    /// </summary>
    /// <typeparam name="TEventArgs">事件参数类型。</typeparam>
    /// <param name="subscribe">事件订阅动作。</param>
    /// <param name="unsubscribe">事件取消订阅动作。</param>
    /// <param name="command">目标 MVI 命令。</param>
    /// <param name="bindings">绑定生命周期集合。</param>
    /// <param name="payloadFactory">命令载荷工厂。</param>
    /// <param name="sourceName">事件来源名称。</param>
    protected static void BindEvent<TEventArgs>(
        Action<Action<TEventArgs>> subscribe,
        Action<Action<TEventArgs>> unsubscribe,
        IMviCommand command,
        MviDisposableBag bindings,
        Func<TEventArgs, object?> payloadFactory,
        string? sourceName = null)
    {
        ArgumentNullException.ThrowIfNull(subscribe);
        ArgumentNullException.ThrowIfNull(unsubscribe);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(payloadFactory);

        void Handler(TEventArgs args)
        {
            object? payload = payloadFactory(args);
            if (command.CanExecute(payload))
            {
                command.Execute(payload);
            }
        }

        subscribe(Handler);
        bindings.Add(() => unsubscribe(Handler));
    }

    /// <summary>
    /// 绑定带一个事件参数且使用自定义委托类型的 Godot 事件到 MVI 命令。
    /// </summary>
    /// <typeparam name="TEventArgs">事件参数类型。</typeparam>
    /// <typeparam name="TDelegate">Godot 事件委托类型。</typeparam>
    /// <param name="subscribe">事件订阅动作。</param>
    /// <param name="unsubscribe">事件取消订阅动作。</param>
    /// <param name="createHandler">委托适配工厂。</param>
    /// <param name="command">目标 MVI 命令。</param>
    /// <param name="bindings">绑定生命周期集合。</param>
    /// <param name="payloadFactory">命令载荷工厂。</param>
    /// <param name="sourceName">事件来源名称。</param>
    protected static void BindEvent<TEventArgs, TDelegate>(
        Action<TDelegate> subscribe,
        Action<TDelegate> unsubscribe,
        Func<Action<TEventArgs>, TDelegate> createHandler,
        IMviCommand command,
        MviDisposableBag bindings,
        Func<TEventArgs, object?> payloadFactory,
        string? sourceName = null)
        where TDelegate : Delegate
    {
        ArgumentNullException.ThrowIfNull(subscribe);
        ArgumentNullException.ThrowIfNull(unsubscribe);
        ArgumentNullException.ThrowIfNull(createHandler);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(payloadFactory);

        void Handler(TEventArgs args)
        {
            object? payload = payloadFactory(args);
            if (command.CanExecute(payload))
            {
                command.Execute(payload);
            }
        }

        TDelegate eventHandler = createHandler(Handler);
        subscribe(eventHandler);
        bindings.Add(() => unsubscribe(eventHandler));
    }
}
