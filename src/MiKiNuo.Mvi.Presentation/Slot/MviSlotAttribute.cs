namespace MiKiNuo.Mvi.Presentation.Slot;

/// <summary>
/// 表示将父 View 中的槽位（slot）字段标记为组合模式槽位的特性。
/// <para>
/// 源生成器扫描父 View partial class 中标有此特性的字段，
/// emit 槽位绑定逻辑：调用 <see cref="Factory"/> 指定的父 ViewModel 方法（或 <c>IMviResolver.Resolve&lt;T&gt;</c>）
/// 解析子 ViewModel、走 <c>IMviViewRegistry</c> 创建子 View、写入槽位控件 <c>Content</c>；
/// 若 <see cref="Observes"/> 非空，还会在父 ViewModel 上订阅对应属性的 <c>PropertyChanged</c> 事件实现自动重渲。
/// </para>
/// <para>
/// 与父 View 基类 <c>MviAvaloniaView&lt;T&gt;</c> / <c>GodotMviControlView&lt;T&gt;</c> 的
/// <c>OnBindSlots</c> 虚方法共同组成"声明式 + 编译器安全"的槽位绑定管道：
/// 特性声明元数据，源生成器 emit <c>override</c> 实现。
/// </para>
/// </summary>
/// <remarks>
/// 仅允许标在字段上，每个字段只允许一个，不允许继承——避免重复绑定。
/// </remarks>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class MviSlotAttribute : Attribute
{
    /// <summary>
    /// 创建 <see cref="MviSlotAttribute"/> 实例（不观察任何属性，槽位一次性绑定）。
    /// </summary>
    /// <param name="childViewType">此槽位要展示的子 View 类型（源生成器据此读取子 ViewModel 类型）。</param>
    /// <exception cref="ArgumentNullException"><paramref name="childViewType"/> 为 <c>null</c>。</exception>
    public MviSlotAttribute(Type childViewType)
        : this(childViewType, null, Array.Empty<string>())
    {
    }

    /// <summary>
    /// 创建 <see cref="MviSlotAttribute"/> 实例，并指定父 ViewModel 中需要观察的属性名集合。
    /// </summary>
    /// <param name="childViewType">此槽位要展示的子 View 类型。</param>
    /// <param name="observes">父 ViewModel 上触发此槽位重新解析的属性名集合；
    /// 传入的数组会被去重并剔除 <c>null</c>/空字符串。</param>
    /// <exception cref="ArgumentNullException"><paramref name="childViewType"/> 为 <c>null</c>。</exception>
    public MviSlotAttribute(Type childViewType, params string[] observes)
        : this(childViewType, null, observes)
    {
    }

    /// <summary>
    /// 创建 <see cref="MviSlotAttribute"/> 实例，指定父 ViewModel 上解析子 ViewModel 的方法名。
    /// <para>
    /// 源生成器 emit 的槽位绑定逻辑会调用 <c>viewModel.{<paramref name="factory"/>}()</c> 获取子 ViewModel。
    /// 该方法必须返回 <c>object?</c>（<c>null</c> 表示空槽），由父 ViewModel 自行决定如何从其依赖与状态构造子 VM。
    /// </para>
    /// </summary>
    /// <param name="childViewType">此槽位要展示的子 View 类型（源生成器据此读取子 ViewModel 类型）。</param>
    /// <param name="factory">父 ViewModel 上用于解析子 ViewModel 的方法名（无参，返回 <c>object?</c>）。</param>
    /// <param name="observes">父 ViewModel 上触发此槽位重新解析的属性名集合。</param>
    /// <exception cref="ArgumentNullException"><paramref name="childViewType"/> 为 <c>null</c>。</exception>
    public MviSlotAttribute(Type childViewType, string? factory, params string[] observes)
    {
        ArgumentNullException.ThrowIfNull(childViewType);

        ChildViewType = childViewType;
        Factory = string.IsNullOrWhiteSpace(factory) ? null : factory;
        Observes = NormalizeObserves(observes);
    }

    /// <summary>
    /// 获取此槽位要展示的子 View 类型。
    /// </summary>
    public Type ChildViewType { get; }

    /// <summary>
    /// 获取父 ViewModel 上用于解析子 ViewModel 的方法名（无参，返回 <c>object?</c>）。
    /// 为 <c>null</c> 时源生成器回退到 <c>IMviResolver.Resolve&lt;T&gt;</c>()，仅适用于容器已注册的子 ViewModel。
    /// </summary>
    public string? Factory { get; }

    /// <summary>
    /// 获取父 ViewModel 中触发此槽位重新解析的属性名集合。
    /// 已去重、剔除 <c>null</c>/空字符串、按声明顺序保留。
    /// </summary>
    public IReadOnlyList<string> Observes { get; }

    /// <summary>
    /// 规范化观察属性名：去重、剔除 <c>null</c>/空字符串，按声明顺序保留。
    /// </summary>
    private static IReadOnlyList<string> NormalizeObserves(string[]? observes)
    {
        if (observes is null || observes.Length == 0)
        {
            return Array.Empty<string>();
        }

        List<string> result = new(observes.Length);
        foreach (string name in observes)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (!result.Contains(name, StringComparer.Ordinal))
            {
                result.Add(name);
            }
        }

        return result;
    }
}
