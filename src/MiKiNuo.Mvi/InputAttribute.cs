namespace MiKiNuo.Mvi;

/// <summary>标记由所属功能的强类型输入入口编辑的状态属性。</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class InputAttribute : Attribute;
