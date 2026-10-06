using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
namespace MiKiNuo.Mvi.Platforms.Avalonia.Events;
/// <summary>常用原生事件的声明式绑定；不捕获旧 DataContext，不建立第二条业务路径。</summary>
public sealed class MviEvents : AvaloniaObject
{
    /// <summary>回车复用的命令。</summary>
    public static readonly AttachedProperty<ICommand?> EnterCommandProperty =
        AvaloniaProperty.RegisterAttached<MviEvents, TextBox, ICommand?>("EnterCommand");
    static MviEvents() => EnterCommandProperty.Changed.AddClassHandler<TextBox>(static (box, _) =>
    {
        box.KeyDown -= KeyDown;
        if (GetEnterCommand(box) is not null) box.KeyDown += KeyDown;
    });
    /// <summary>取得文本框的回车命令。</summary>
    /// <param name="box">文本框。</param>
    /// <returns>命令。</returns>
    public static ICommand? GetEnterCommand(TextBox box)
    {
        ArgumentNullException.ThrowIfNull(box);
        return box.GetValue(EnterCommandProperty);
    }
    /// <summary>设置或解除回车命令。</summary>
    /// <param name="box">文本框。</param>
    /// <param name="command">命令。</param>
    public static void SetEnterCommand(TextBox box, ICommand? command)
    {
        ArgumentNullException.ThrowIfNull(box);
        box.SetValue(EnterCommandProperty, command);
    }
    private static void KeyDown(object? sender, KeyEventArgs args)
    {
        if (sender is not TextBox box || args.Handled || args.Key != Key.Enter || args.KeyModifiers != KeyModifiers.None
            || box.AcceptsReturn || !box.IsEffectivelyVisible || !box.IsEffectivelyEnabled || !box.IsAttachedToVisualTree()) return;
        ICommand? command = GetEnterCommand(box);
        if (command?.CanExecute(null) != true) return;
        args.Handled = true;
        command.Execute(null);
    }
}
