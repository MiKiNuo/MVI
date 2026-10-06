namespace MiKiNuo.Mvi.Abstractions.MVI.Reducer;
/// <summary>标记纯状态转换方法；输入必须为状态与该状态的 Mutation。</summary>
/// <param name="mutationType">处理的变化类型。</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class MviReduceAttribute(Type mutationType) : Attribute
{
    /// <summary>获取变化类型。</summary>
    public Type MutationType { get; } = mutationType;
}
