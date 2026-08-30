namespace MiKiNuo.Mvi.Domain.DI;

/// <summary>
/// 表示一个 MVI Feature 的装配声明。
/// </summary>
/// <remarks>
/// 标注在 Feature 的 Reducer 类上（该类须继承 MviReducerBase&lt;TState, TIntent, TEffect&gt;），
/// 源生成器据此推导 State/Intent/Effect 三件套，
/// 并按类型签名发现匹配的 EffectDispatcher 与 ViewModel，
/// 将完整对象图（Store / Reducer / EffectDispatcher / ViewModel）装配进生成的 DI 容器。
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MviFeatureAttribute : Attribute
{
}
