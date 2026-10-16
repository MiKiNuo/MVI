using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using MiKiNuo.Mvi.Binding.Disposables;
using MiKiNuo.Mvi.Platforms.Godot.Binding;
using MiKiNuo.Mvi.Runtime.DI;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 <c>GodotMviControlView&lt;T&gt;.OnBindSlots</c> 虚方法钩子的契约测试。
/// </summary>
public sealed class GodotMviControlViewOnBindSlotsHookTests
{
    /// <summary>
    /// 验证 <c>GodotMviControlView&lt;T&gt;</c> 声明 <c>OnBindSlots</c> 虚方法，
    /// 用于源生成器在子类 <c>override</c> 实现槽位绑定。
    /// </summary>
    [Test]
    public async Task GodotMviControlView_Should_DeclareOnBindSlotsVirtualMethodAsync()
    {
        Type viewBase = typeof(MiKiNuo.Mvi.Platforms.Godot.Binding.GodotMviControlView<>);
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
        Type viewBase = typeof(MiKiNuo.Mvi.Platforms.Godot.Binding.GodotMviControlView<>);
        MethodInfo method = viewBase.GetMethod(
            "OnBindSlots",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        ParameterInfo[] parameters = method.GetParameters();

        await Assert.That(parameters.Length).IsEqualTo(3);
        await Assert.That(parameters[0].ParameterType.Name).IsEqualTo("TViewModel");
        await Assert.That(parameters[1].ParameterType).IsEqualTo(typeof(MiKiNuo.Mvi.Binding.Disposables.MviDisposableBag));
        await Assert.That(parameters[2].ParameterType).IsEqualTo(typeof(MiKiNuo.Mvi.Runtime.DI.IMviResolver));
    }

    /// <summary>首次属性刷新失败后，绑定入口退订且允许重新绑定。</summary>
    [Test]
    public async Task Bind_Should_UnsubscribeWhenInitialPropertyRefreshFailsAsync()
    {
        // 仅执行托管绑定路径，绕过需要原生引擎的 Control 构造与终结。
        PropertyBindingView view = (PropertyBindingView)RuntimeHelpers.GetUninitializedObject(typeof(PropertyBindingView));
#pragma warning disable CA1816 // 未构造原生对象，不能运行 GodotObject 终结器。
        GC.SuppressFinalize(view);
#pragma warning restore CA1816
        PropertySource source = new();
        InvalidOperationException failure = new("首次刷新失败");
        int updates = 0;
        view.Update = () => { updates++; throw failure; };

        Exception? observed = null;
        try { view.Bind(source, new UnusedResolver()); }
        catch (Exception exception) { observed = exception; }

        await Assert.That(ReferenceEquals(observed, failure)).IsTrue();
        await Assert.That(view.ViewModel).IsNull();
        // 清理后触发源事件，不能再调用已经失败的刷新动作。
        source.Notify();
        await Assert.That(updates).IsEqualTo(1);

        view.Update = () => updates++;
        view.Bind(source, new UnusedResolver());
        source.Notify();
        await Assert.That(updates).IsEqualTo(3);
        view.Unbind();
        source.Notify();
        await Assert.That(updates).IsEqualTo(3);
    }

    private sealed class PropertySource : INotifyPropertyChanged
    {
        /// <summary>属性变化通知。</summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>通知值发生变化。</summary>
        public void Notify() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));
    }

    private sealed partial class PropertyBindingView : GodotMviControlView<PropertySource>
    {
        /// <summary>首次及后续刷新动作。</summary>
        public Action Update { get; set; } = null!;

        /// <summary>绑定属性变化通知。</summary>
        protected override void OnBind(PropertySource viewModel, MviDisposableBag bindings)
            => BindPropertyChanged(viewModel, "Value", Update, bindings);
    }

    private sealed class UnusedResolver : IMviResolver
    {
        /// <summary>本测试不解析服务。</summary>
        public TService Resolve<TService>() where TService : notnull => throw new NotSupportedException();

        /// <summary>本测试不创建服务。</summary>
        public TService CreateWith<TService>(params object[] args) where TService : notnull => throw new NotSupportedException();

        /// <summary>本测试不解析服务。</summary>
        public object Resolve(Type serviceType) => throw new NotSupportedException();

        /// <summary>本测试不创建作用域。</summary>
        public IMviScope CreateScope() => throw new NotSupportedException();
    }
}
