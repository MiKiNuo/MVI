using MiKiNuo.Mvi.Application.DI;

namespace MiKiNuo.Mvi.Platforms.Godot.Binding;

/// <summary>
/// 表示可以绑定和解绑 ViewModel 的 Godot MVI View。
/// </summary>
/// <typeparam name="TViewModel">ViewModel 类型。</typeparam>
public interface IMviGodotBindable<TViewModel>
    where TViewModel : class
{
    /// <summary>
    /// 获取当前绑定的 ViewModel。
    /// </summary>
    public TViewModel? ViewModel { get; }

    /// <summary>
    /// 绑定 ViewModel 与子组件解析容器。
    /// <para>
    /// 1-arg 重载 <b>不</b>触发 <c>OnBindSlots</c> 槽位绑定钩子（缺解析器）；
    /// 拥有 [MviSlot] 字段的 View 必须由父 View / 组合根通过本 2-arg 重载传入解析器。
    /// </para>
    /// </summary>
    /// <param name="viewModel">需要绑定的 ViewModel。</param>
    /// <param name="resolver">用于解析子 ViewModel 与子 View 的 <see cref="IMviResolver"/>。</param>
    public void Bind(TViewModel viewModel, IMviResolver resolver);

    /// <summary>
    /// 解绑当前 ViewModel。
    /// </summary>
    public void Unbind();
}
