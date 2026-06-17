using System.Reflection;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 <c>MviAvaloniaView&lt;T&gt;.OnBindSlots</c> 虚方法钩子的契约测试。
/// </summary>
public sealed class MviAvaloniaViewOnBindSlotsHookTests
{
    /// <summary>
    /// 验证 <c>MviAvaloniaView&lt;T&gt;</c> 声明 <c>OnBindSlots</c> 虚方法，
    /// 用于源生成器在子类 <c>override</c> 实现槽位绑定。
    /// </summary>
    [Test]
    public async Task MviAvaloniaView_Should_DeclareOnBindSlotsVirtualMethodAsync()
    {
        Type viewBase = typeof(MiKiNuo.Mvi.Platforms.Avalonia.Views.MviAvaloniaView<>);
        MethodInfo? method = viewBase.GetMethod(
            "OnBindSlots",
            BindingFlags.NonPublic | BindingFlags.Instance);

        await Assert.That(method).IsNotNull();
        await Assert.That(method!.IsVirtual).IsTrue();
        await Assert.That(method.IsFinal).IsFalse();
    }

    /// <summary>
    /// 验证 <c>OnBindSlots</c> 形参签名包含 <c>viewModel</c>、<c>bindings</c> 与 <c>resolver</c> 三个参数。
    /// </summary>
    [Test]
    public async Task OnBindSlots_Should_HaveViewModelBindingsAndResolverParametersAsync()
    {
        Type viewBase = typeof(MiKiNuo.Mvi.Platforms.Avalonia.Views.MviAvaloniaView<>);
        MethodInfo method = viewBase.GetMethod(
            "OnBindSlots",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        ParameterInfo[] parameters = method.GetParameters();

        await Assert.That(parameters.Length).IsEqualTo(3);
        await Assert.That(parameters[0].ParameterType.Name).IsEqualTo("TViewModel");
        await Assert.That(parameters[1].ParameterType).IsEqualTo(typeof(MiKiNuo.Mvi.Presentation.Disposables.MviDisposableBag));
        await Assert.That(parameters[2].ParameterType).IsEqualTo(typeof(MiKiNuo.Mvi.Application.DI.IMviResolver));
    }
}
