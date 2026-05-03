namespace MiKiNuo.Mvi.Domain.MVI.Reducer;

/// <summary>
/// 表示 MVI Feature 规约器特性。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MviFeatureReducerAttribute : Attribute
{
    /// <summary>
    /// 初始化 MVI Feature 规约器特性。
    /// </summary>
    /// <param name="stateType">状态类型。</param>
    /// <param name="intentType">意图类型。</param>
    /// <param name="effectType">副作用类型。</param>
    public MviFeatureReducerAttribute(Type stateType, Type intentType, Type effectType)
    {
        StateType = stateType;
        IntentType = intentType;
        EffectType = effectType;
    }

    /// <summary>
    /// 获取状态类型。
    /// </summary>
    public Type StateType { get; }

    /// <summary>
    /// 获取意图类型。
    /// </summary>
    public Type IntentType { get; }

    /// <summary>
    /// 获取副作用类型。
    /// </summary>
    public Type EffectType { get; }
}
