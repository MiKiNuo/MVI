namespace MiKiNuo.Mvi.Domain.DI;

/// <summary>
/// 表示一个 MVI 组合的装配声明。
/// </summary>
/// <remarks>
/// 标注在 partial 声明类上并列出成员 Feature 的 Reducer 类型，
/// 源生成器据此 emit 组合构建器：创建组合范围与各成员实例，
/// 并按路由处理器与通知接纳器声明完成请求绑定与订阅接线。
/// 声明类本身成为生成的组合句柄（partial 合并），成员实例以属性暴露。
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MviCompositionAttribute : Attribute
{
    /// <summary>
    /// 初始化组合装配声明。
    /// </summary>
    /// <param name="memberReducers">成员 Feature 的 Reducer 类型集合，须已标注 [MviFeature]。</param>
    public MviCompositionAttribute(params Type[] memberReducers)
    {
        ArgumentNullException.ThrowIfNull(memberReducers);
        MemberReducers = memberReducers;
    }

    /// <summary>
    /// 获取成员 Feature 的 Reducer 类型集合。
    /// </summary>
    public IReadOnlyList<Type> MemberReducers { get; }
}
