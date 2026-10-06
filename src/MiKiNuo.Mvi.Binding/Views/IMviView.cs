using MiKiNuo.Mvi.Runtime.DI;
namespace MiKiNuo.Mvi.Binding.Views;
/// <summary>不要求继承框架控件基类的跨平台 View 契约；业务对象由外部作用域拥有。</summary>
/// <typeparam name="TViewModel">借用的绑定模型。</typeparam>
public interface IMviView<in TViewModel> where TViewModel : class
{
    /// <summary>绑定模型并提供子组件解析器。</summary>
    /// <param name="viewModel">借用的模型。</param>
    /// <param name="resolver">借用的解析器。</param>
    public void Bind(TViewModel viewModel, IMviResolver resolver);
    /// <summary>仅解绑，不销毁模型或 Store。</summary>
    public void Unbind();
}
