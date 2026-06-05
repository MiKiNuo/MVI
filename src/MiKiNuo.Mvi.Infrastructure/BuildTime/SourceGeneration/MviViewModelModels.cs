using System.Collections.Generic;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;

/// <summary>
/// 表示 <see cref="MviViewModelGenerator"/> 在分析阶段收集到的数据模型集合。
/// 与发射逻辑解耦，便于单元测试和未来扩展。
/// </summary>
internal static class MviViewModelModels
{
    /// <summary>
    /// 表示 <see cref="MviViewModelGenerator"/> 关注的 ViewModel 类型描述。
    /// </summary>
    public sealed class ViewModelDescriptor
    {
        /// <summary>
        /// 初始化 ViewModel 描述。
        /// </summary>
        public ViewModelDescriptor(
            Microsoft.CodeAnalysis.INamedTypeSymbol viewModelSymbol,
            Microsoft.CodeAnalysis.INamedTypeSymbol mviBase,
            IReadOnlyList<BindPropertyModel> bindProperties,
            IReadOnlyList<CommandPropertyModel> commandProperties)
        {
            ViewModelSymbol = viewModelSymbol;
            MviBase = mviBase;
            BindProperties = bindProperties;
            CommandProperties = commandProperties;
        }

        /// <summary>ViewModel 类型符号。</summary>
        public Microsoft.CodeAnalysis.INamedTypeSymbol ViewModelSymbol { get; }

        /// <summary>泛型基类 <c>MviViewModelBase&lt;TState, TIntent, TEffect&gt;</c> 的已绑定符号。</summary>
        public Microsoft.CodeAnalysis.INamedTypeSymbol MviBase { get; }

        /// <summary>分析得到的 [MviBind] 属性集合。</summary>
        public IReadOnlyList<BindPropertyModel> BindProperties { get; }

        /// <summary>分析得到的 [MviCommand] 属性集合。</summary>
        public IReadOnlyList<CommandPropertyModel> CommandProperties { get; }
    }

    /// <summary>
    /// 表示 <c>[MviBind]</c> 属性的解析结果。
    /// </summary>
    public sealed class BindPropertyModel
    {
        /// <summary>
        /// 初始化绑定属性模型。
        /// </summary>
        public BindPropertyModel(
            string name,
            string typeName,
            string statePropertyName,
            string? intentTypeName,
            bool isTwoWay,
            string setterAccessibility,
            string fieldName)
        {
            Name = name;
            TypeName = typeName;
            StatePropertyName = statePropertyName;
            IntentTypeName = intentTypeName;
            IsTwoWay = isTwoWay;
            SetterAccessibility = setterAccessibility;
            FieldName = fieldName;
        }

        /// <summary>属性名称。</summary>
        public string Name { get; }

        /// <summary>属性类型（完整限定名）。</summary>
        public string TypeName { get; }

        /// <summary>对应 State 上的属性名。</summary>
        public string StatePropertyName { get; }

        /// <summary>TwoWay 绑定时派发的 Intent 类型（仅在 <see cref="IsTwoWay"/> 为 true 时可能非空）。</summary>
        public string? IntentTypeName { get; }

        /// <summary>是否为双向绑定。</summary>
        public bool IsTwoWay { get; }

        /// <summary>setter 访问性文本（"<c>private set</c>" 或 "<c>set</c>"）。</summary>
        public string SetterAccessibility { get; }

        /// <summary>生成代码中使用的后备字段名（带下划线前缀）。</summary>
        public string FieldName { get; }
    }

    /// <summary>
    /// 表示 <c>[MviCommand]</c> 属性的解析结果。
    /// </summary>
    public sealed class CommandPropertyModel
    {
        /// <summary>
        /// 初始化命令属性模型。
        /// </summary>
        public CommandPropertyModel(
            string name,
            string typeName,
            string intentTypeName,
            string? payloadTypeName,
            bool hasParameterlessConstructor,
            string? canExecuteProperty,
            bool isAsync,
            string setterAccessibility,
            string fieldName)
        {
            Name = name;
            TypeName = typeName;
            IntentTypeName = intentTypeName;
            PayloadTypeName = payloadTypeName;
            HasParameterlessConstructor = hasParameterlessConstructor;
            CanExecuteProperty = canExecuteProperty;
            IsAsync = isAsync;
            SetterAccessibility = setterAccessibility;
            FieldName = fieldName;
        }

        /// <summary>命令属性名称。</summary>
        public string Name { get; }

        /// <summary>命令类型（完整限定名）。</summary>
        public string TypeName { get; }

        /// <summary>Intent 类型（完整限定名）。</summary>
        public string IntentTypeName { get; }

        /// <summary>显式指定或推导出的 payload 类型（完整限定名）；无参构造时可空。</summary>
        public string? PayloadTypeName { get; }

        /// <summary>Intent 是否存在公开无参构造函数。</summary>
        public bool HasParameterlessConstructor { get; }

        /// <summary>State 中驱动 CanExecute 的属性名；为空时默认返回 true。</summary>
        public string? CanExecuteProperty { get; }

        /// <summary>是否为异步命令。</summary>
        public bool IsAsync { get; }

        /// <summary>setter 访问性文本（"<c>private set</c>" 或 "<c>set</c>"）。</summary>
        public string SetterAccessibility { get; }

        /// <summary>生成代码中使用的后备字段名（带下划线前缀）。</summary>
        public string FieldName { get; }
    }

    /// <summary>
    /// 表示 Intent 构造函数解析结果。
    /// </summary>
    public sealed class ConstructorBindingModel
    {
        /// <summary>
        /// 初始化构造函数绑定模型。
        /// </summary>
        public ConstructorBindingModel(bool hasParameterlessConstructor, string? payloadTypeName)
        {
            HasParameterlessConstructor = hasParameterlessConstructor;
            PayloadTypeName = payloadTypeName;
        }

        /// <summary>Intent 是否存在公开无参构造函数。</summary>
        public bool HasParameterlessConstructor { get; }

        /// <summary>payload 类型（完整限定名）；若选择无参构造则为 null。</summary>
        public string? PayloadTypeName { get; }
    }
}
