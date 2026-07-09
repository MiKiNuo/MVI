using global::Godot;
using MiKiNuo.Mvi.Presentation.ViewRegistry;

namespace MiKiNuo.Mvi.Platforms.Godot.Composition;

/// <summary>
/// 表示将 <see cref="IGodotMviViewRegistry"/> 适配为平台无关 <see cref="IMviViewRegistry"/> 的桥接器。
/// <para>
/// 源生成器 <c>MviCompositeSlotBindingGenerator</c> 走 <see cref="IMviViewRegistry.CreateView"/>（按 ViewModel 实例）
/// 创建子 View；Godot 原生 <see cref="IGodotMviViewRegistry"/> 走字符串键（如 <c>"MissionBoardView"</c>）。
/// 本适配器把 ViewModel 类型名解析为 View 类名（默认约定：去掉 <c>"Model"</c> 后缀）后委托给
/// <see cref="IGodotMviViewRegistry.TryCreate"/>，让源生成器生成的槽位绑定代码可以
/// <c>registry.CreateView(viewModel)</c> 直接拿到 <see cref="Control"/> 实例。
/// </para>
/// <para>
/// 约定优于配置：MVI ViewModel 与 View 默认按 <c>{Name}ViewModel</c> / <c>{Name}View</c> 命名一一对应。
/// </para>
/// </summary>
public class GodotMviViewRegistryAdapter : IMviViewRegistry
{
    /// <summary>
    /// 由 ViewModel 类型名解析为 Godot ViewRegistry 注册键时，去掉的默认后缀：<c>"Model"</c>。
    /// </summary>
    protected const string ViewModelSuffix = "Model";

    private readonly IGodotMviViewRegistry _inner;

    /// <summary>
    /// 初始化 Godot MVI ViewRegistry 适配器。
    /// </summary>
    /// <param name="inner">底层 Godot ViewRegistry（已注册所有 View）。</param>
    public GodotMviViewRegistryAdapter(IGodotMviViewRegistry inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    /// <summary>
    /// 按 ViewModel 创建平台视图对象。
    /// </summary>
    /// <param name="viewModel">视图模型。</param>
    /// <returns>平台视图对象。</returns>
    public object CreateView(object viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        string key = ResolveViewKey(viewModel.GetType());
        if (_inner.TryCreate(key, out Control? view) && view is not null)
        {
            return view;
        }

        throw new MviViewNotFoundException(viewModel.GetType());
    }

    /// <summary>
    /// 由 ViewModel 类型解析 Godot ViewRegistry 注册键。
    /// <para>
    /// 默认实现：去掉类型名末尾的 <see cref="ViewModelSuffix"/> 后缀
    /// （如 <c>MissionBoardViewModel</c> → <c>MissionBoardView</c>）。
    /// </para>
    /// </summary>
    /// <param name="viewModelType">ViewModel 运行时类型。</param>
    /// <returns>Godot ViewRegistry 注册键。</returns>
    protected virtual string ResolveViewKey(Type viewModelType)
    {
        ArgumentNullException.ThrowIfNull(viewModelType);

        string name = viewModelType.Name;
        if (name.EndsWith(ViewModelSuffix, StringComparison.Ordinal) && name.Length > ViewModelSuffix.Length)
        {
            return name.Substring(0, name.Length - ViewModelSuffix.Length);
        }

        return name;
    }
}
