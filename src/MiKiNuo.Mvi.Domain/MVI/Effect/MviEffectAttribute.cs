namespace MiKiNuo.Mvi.Domain.MVI.Effect;

/// <summary>
/// 表示副作用处理方法与副作用子类型的声明式映射。
/// </summary>
/// <remarks>
/// 标注在 MviEffectDispatcherBase 子类的处理方法上，
/// 由源生成器 emit DispatchCoreAsync 的分派代码，
/// 签名约定为 (TEffect.Xxx effect, CancellationToken cancellationToken) => ValueTask。
/// </remarks>
/// <param name="effectType">副作用子类型。</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class MviEffectAttribute(Type effectType) : Attribute
{
    /// <summary>
    /// 获取副作用子类型。
    /// </summary>
    public Type EffectType { get; } = effectType;
}
