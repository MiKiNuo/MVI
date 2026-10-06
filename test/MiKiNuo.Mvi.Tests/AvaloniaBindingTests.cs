using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using MiKiNuo.Mvi.Binding.Command;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;
using MiKiNuo.Mvi.Platforms.Avalonia.Events;
using MiKiNuo.Mvi.Samples.Avalonia.Composition;
using TUnit.Assertions;
using TUnit.Core;
namespace MiKiNuo.Mvi.Tests;
/// <summary>使用真实 Headless 控件和路由事件验证绑定生命周期。</summary>
public sealed class AvaloniaBindingTests
{
    /// <summary>实际 Enter 事件、换绑、卸载、重挂载均只使用当前命令。</summary>
    [Test] public async Task EnterCommandUsesCurrentBindingAsync()
    {
        await using HeadlessUnitTestSession session = HeadlessUnitTestSession.StartNew(typeof(HeadlessTestApp));
        bool result = await session.Dispatch(() =>
        {
            TextBox box = new(); Window window = new() { Content = box }; window.Show();
            int first = 0, second = 0;
            using MviAsyncCommand a = new(static () => true, (_, _) => { first++; return ValueTask.CompletedTask; });
            using MviAsyncCommand b = new(static () => true, (_, _) => { second++; return ValueTask.CompletedTask; });
            try
            {
                MviEvents.SetEnterCommand(box, a); Enter(box);
                MviEvents.SetEnterCommand(box, b); MviEvents.SetEnterCommand(box, b); Enter(box);
                box.AcceptsReturn = true; Enter(box); box.AcceptsReturn = false;
                box.IsEnabled = false; Enter(box); box.IsEnabled = true;
                window.Content = null; Enter(box); window.Content = box; Enter(box);
                MviEvents.SetEnterCommand(box, null); Enter(box);
                return first == 1 && second == 2;
            }
            finally { window.Close(); }
        }, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(15));
        await Assert.That(result).IsTrue();
    }
    /// <summary>安装失败时进入未绑定状态，保留原始和清理异常。</summary>
    [Test] public async Task FailedViewBindingCleansEverythingAsync()
    {
        await using HeadlessUnitTestSession session = HeadlessUnitTestSession.StartNew(typeof(HeadlessTestApp));
        bool result = await session.Dispatch(() =>
        {
            UserControl view = new(); Window window = new() { Content = view }; window.Show();
            int cleanup = 0;
            using AvaloniaViewBinding<object> binding = new(view, (_, resources, _) =>
            {
                resources.Add(() => cleanup++);
                resources.Add(() => throw new InvalidOperationException("cleanup"));
                throw new InvalidOperationException("install");
            });
            try
            {
                try { binding.Bind(new object(), new GeneratedMviContainer()); return false; }
                catch (AggregateException error)
                { return cleanup == 1 && error.Flatten().InnerExceptions.Count == 2 && view.DataContext is null && binding.ViewModel is null; }
            }
            finally { window.Close(); }
        }, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(15));
        await Assert.That(result).IsTrue();
    }
    private static void Enter(TextBox box) => box.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
}
/// <summary>Headless 专属空应用，不启动真实联网示例窗口。</summary>
public sealed class HeadlessTestApp : Application { }
