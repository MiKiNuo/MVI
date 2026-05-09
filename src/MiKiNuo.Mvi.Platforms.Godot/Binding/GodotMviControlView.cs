using System.ComponentModel;
using System.Windows.Input;
using global::Godot;

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

        void Pressed()
        {
            object? parameter = CreateParameter();
            bool canExecute = command.CanExecute(parameter);
            GD.Print($"[Godot MVI Button] {button.Name} pressed, CanExecute={canExecute}");
            if (canExecute)
            {
                command.Execute(parameter);
            }
            else
            {
                GD.PushWarning($"Godot MVI Button {button.Name} pressed but command cannot execute.");
            }
        }

        void CanExecuteChanged(object? sender, EventArgs args)
        {
            RefreshCanExecute();
        }

        button.Pressed += Pressed;
        command.CanExecuteChanged += CanExecuteChanged;
        RefreshCanExecute();
        GD.Print($"[Godot MVI Button] {button.Name} bound, InitialCanExecute={!button.Disabled}");

        bindings.Add(() =>
        {
            button.Pressed -= Pressed;
            command.CanExecuteChanged -= CanExecuteChanged;
        });
    }
}
