namespace MiKiNuo.Mvi.Domain.MVI.State;

/// <summary>
/// 表示将一个位置参数 record 声明为指定状态的切片，
/// 由源生成器生成对应的 <see cref="StatePath{TState, TValue}"/> 入口。
/// </summary>
/// <remarks>
/// 切片构造参数按“参数名匹配状态属性图叶子名称 + 类型一致”的规则解析，
/// 找不到唯一匹配时生成器会在编译期报告诊断错误。
/// </remarks>
/// <param name="stateType">切片来源的状态类型。</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MviStateSliceAttribute(Type stateType) : Attribute
{
    /// <summary>
    /// 获取切片来源的状态类型。
    /// </summary>
    public Type StateType { get; } = stateType;
}
