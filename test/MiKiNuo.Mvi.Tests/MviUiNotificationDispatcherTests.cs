using MiKiNuo.Mvi.Application.MVI.Threading;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 UI 通知调度器测试。
/// </summary>
[NotInParallel]
public sealed class MviUiNotificationDispatcherTests
{
    /// <summary>
    /// 验证 UI 通知调度器配置可以通过作用域释放恢复上一层配置。
    /// </summary>
    [Test]
    public async Task Configure_Should_ReturnScopeThatRestoresPreviousDispatcherAsync()
    {
        List<string> calls = [];

        using IDisposable outer = MviUiNotificationDispatcher.Configure(action =>
        {
            calls.Add("outer");
            action();
        });

        MviUiNotificationDispatcher.Post(() => calls.Add("first"));

        using (MviUiNotificationDispatcher.Configure(action =>
        {
            calls.Add("inner");
            action();
        }))
        {
            MviUiNotificationDispatcher.Post(() => calls.Add("second"));
        }

        MviUiNotificationDispatcher.Post(() => calls.Add("third"));
        outer.Dispose();
        MviUiNotificationDispatcher.Post(() => calls.Add("fourth"));

        await Assert.That(calls).IsEquivalentTo([
            "outer",
            "first",
            "inner",
            "second",
            "outer",
            "third",
            "fourth"
        ]);
    }
}
