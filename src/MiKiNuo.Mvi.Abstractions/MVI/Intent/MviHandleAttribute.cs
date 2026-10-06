namespace MiKiNuo.Mvi.Abstractions.MVI.Intent;
/// <summary>将一个类型化业务方法注册到生成的意图分派入口。</summary>
/// <param name="intentType">方法处理的具体意图。</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class MviHandleAttribute(Type intentType) : Attribute
{
    /// <summary>获取处理的具体意图类型。</summary>
    public Type IntentType { get; } = intentType;
}
