using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;

/// <summary>
/// 表示 <see cref="MviViewModelGenerator"/> 的分析阶段：
/// 从 <see cref="INamedTypeSymbol"/> 中提取 Bind/Command 特性信息、解析构造函数、报告诊断。
/// 与代码发射解耦。
/// </summary>
internal static class MviViewModelAnalysis
{
    private static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .WithMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
            | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    private static readonly DiagnosticDescriptor AmbiguousPayloadConstructorRule = new(
        id: "MVI0001",
        title: "命令 Intent 存在多个 payload 构造函数",
        messageFormat: "命令“{0}”绑定的 Intent“{1}”存在多个一参构造函数，请在 MviCommandAttribute.PayloadType 中指定载荷类型。",
        category: "MviBinding",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingPayloadConstructorRule = new(
        id: "MVI0002",
        title: "命令 Intent 缺少指定 payload 构造函数",
        messageFormat: "命令“{0}”绑定的 Intent“{1}”没有匹配 PayloadType“{2}”的一参构造函数。",
        category: "MviBinding",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingIntentConstructorRule = new(
        id: "MVI0003",
        title: "命令 Intent 缺少可用构造函数",
        messageFormat: "命令“{0}”绑定的 Intent“{1}”需要公开无参构造函数或唯一的一参 payload 构造函数。",
        category: "MviBinding",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// 收集 ViewModel 类型的完整描述。
    /// </summary>
    /// <param name="viewModelSymbol">候选 ViewModel 类型符号。</param>
    /// <param name="viewModelBaseSymbol"><c>MviViewModelBase&lt;,,&gt;</c> 泛型定义符号。</param>
    /// <param name="context">源生成上下文。</param>
    /// <returns>若非 MviViewModelBase 子类或无 Bind/Command 特性则返回 null。</returns>
    public static MviViewModelModels.ViewModelDescriptor? Collect(
        INamedTypeSymbol viewModelSymbol,
        INamedTypeSymbol viewModelBaseSymbol,
        SourceProductionContext context)
    {
        if (!TryGetMviBase(viewModelSymbol, viewModelBaseSymbol, out INamedTypeSymbol? mviBase))
        {
            return null;
        }

        IReadOnlyList<MviViewModelModels.BindPropertyModel> bindProperties = GetBindProperties(viewModelSymbol);
        IReadOnlyList<MviViewModelModels.CommandPropertyModel> commandProperties = GetCommandProperties(viewModelSymbol, context);

        if (bindProperties.Count == 0 && commandProperties.Count == 0)
        {
            return null;
        }

        return new MviViewModelModels.ViewModelDescriptor(viewModelSymbol, mviBase!, bindProperties, commandProperties);
    }

    /// <summary>
    /// 判断给定类型是否继承自 <paramref name="viewModelBaseSymbol"/> 泛型基类。
    /// </summary>
    public static bool TryGetMviBase(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol viewModelBaseSymbol,
        out INamedTypeSymbol? mviBase)
    {
        INamedTypeSymbol? current = typeSymbol.BaseType;
        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, viewModelBaseSymbol))
            {
                mviBase = current;
                return true;
            }

            current = current.BaseType;
        }

        mviBase = null;
        return false;
    }

    private static IReadOnlyList<MviViewModelModels.BindPropertyModel> GetBindProperties(INamedTypeSymbol viewModelSymbol)
    {
        List<MviViewModelModels.BindPropertyModel> result = new();

        foreach (IPropertySymbol property in viewModelSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            AttributeData? attribute = property.GetAttributes().FirstOrDefault(static item => item.AttributeClass?.Name == "MviBindAttribute");
            if (attribute is null)
            {
                continue;
            }

            string stateProperty = attribute.ConstructorArguments.Length > 0
                ? attribute.ConstructorArguments[0].Value?.ToString() ?? property.Name
                : property.Name;
            bool isTwoWay = false;
            string? intentTypeName = null;

            foreach (KeyValuePair<string, TypedConstant> namedArgument in attribute.NamedArguments)
            {
                if (namedArgument.Key == "BindingMode")
                {
                    isTwoWay = namedArgument.Value.Value?.ToString() == "1";
                }
                else if (namedArgument.Key == "IntentType" && namedArgument.Value.Value is INamedTypeSymbol intentType)
                {
                    intentTypeName = intentType.ToDisplayString(TypeFormat);
                }
            }

            result.Add(new MviViewModelModels.BindPropertyModel(
                property.Name,
                property.Type.ToDisplayString(TypeFormat),
                stateProperty,
                intentTypeName,
                isTwoWay,
                GetSetterAccessibility(property),
                "_" + ToCamelCase(property.Name)));
        }

        return result;
    }

    private static IReadOnlyList<MviViewModelModels.CommandPropertyModel> GetCommandProperties(
        INamedTypeSymbol viewModelSymbol,
        SourceProductionContext context)
    {
        List<MviViewModelModels.CommandPropertyModel> result = new();

        foreach (IPropertySymbol property in viewModelSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            AttributeData? attribute = property.GetAttributes().FirstOrDefault(static item => item.AttributeClass?.Name == "MviCommandAttribute");
            if (attribute is null || attribute.ConstructorArguments.Length == 0 || attribute.ConstructorArguments[0].Value is not INamedTypeSymbol intentType)
            {
                continue;
            }

            string? canExecuteProperty = null;
            bool isAsync = false;
            ITypeSymbol? explicitPayloadType = null;
            foreach (KeyValuePair<string, TypedConstant> namedArgument in attribute.NamedArguments)
            {
                if (namedArgument.Key == "CanExecuteProperty")
                {
                    canExecuteProperty = namedArgument.Value.Value?.ToString();
                }
                else if (namedArgument.Key == "IsAsync")
                {
                    isAsync = namedArgument.Value.Value is true;
                }
                else if (namedArgument.Key == "PayloadType" && namedArgument.Value.Value is ITypeSymbol payloadType)
                {
                    explicitPayloadType = payloadType;
                }
            }

            MviViewModelModels.ConstructorBindingModel constructorBinding = ResolveConstructorBinding(
                property,
                intentType,
                explicitPayloadType,
                context);

            result.Add(new MviViewModelModels.CommandPropertyModel(
                property.Name,
                property.Type.ToDisplayString(TypeFormat),
                intentType.ToDisplayString(TypeFormat),
                constructorBinding.PayloadTypeName,
                constructorBinding.HasParameterlessConstructor,
                canExecuteProperty,
                isAsync,
                GetSetterAccessibility(property),
                "_" + ToCamelCase(property.Name)));
        }

        return result;
    }

    private static MviViewModelModels.ConstructorBindingModel ResolveConstructorBinding(
        IPropertySymbol commandProperty,
        INamedTypeSymbol intentType,
        ITypeSymbol? explicitPayloadType,
        SourceProductionContext context)
    {
        List<IMethodSymbol> publicConstructors = intentType.InstanceConstructors
            .Where(static constructor => constructor.DeclaredAccessibility == Accessibility.Public)
            .ToList();
        bool hasParameterlessConstructor = publicConstructors.Any(static constructor => constructor.Parameters.Length == 0);
        List<IMethodSymbol> payloadConstructors = publicConstructors
            .Where(static constructor => constructor.Parameters.Length == 1)
            .ToList();
        ITypeSymbol? payloadType = null;

        if (explicitPayloadType is not null)
        {
            IMethodSymbol? matchingConstructor = payloadConstructors.FirstOrDefault(
                constructor => SymbolEqualityComparer.Default.Equals(constructor.Parameters[0].Type, explicitPayloadType));

            if (matchingConstructor is null)
            {
                Report(
                    context,
                    MissingPayloadConstructorRule,
                    commandProperty,
                    commandProperty.Name,
                    intentType.ToDisplayString(TypeFormat),
                    explicitPayloadType.ToDisplayString(TypeFormat));
            }
            else
            {
                payloadType = explicitPayloadType;
            }
        }
        else if (payloadConstructors.Count == 1)
        {
            payloadType = payloadConstructors[0].Parameters[0].Type;
        }
        else if (payloadConstructors.Count > 1)
        {
            Report(
                context,
                AmbiguousPayloadConstructorRule,
                commandProperty,
                commandProperty.Name,
                intentType.ToDisplayString(TypeFormat));
        }

        if (!hasParameterlessConstructor && payloadType is null)
        {
            Report(
                context,
                MissingIntentConstructorRule,
                commandProperty,
                commandProperty.Name,
                intentType.ToDisplayString(TypeFormat));
        }

        return new MviViewModelModels.ConstructorBindingModel(
            hasParameterlessConstructor,
            payloadType?.ToDisplayString(TypeFormat));
    }

    private static void Report(
        SourceProductionContext context,
        DiagnosticDescriptor descriptor,
        IPropertySymbol property,
        params object[] messageArgs)
    {
        Location location = property.Locations.Length > 0 ? property.Locations[0] : Location.None;
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));
    }

    private static string GetSetterAccessibility(IPropertySymbol property)
    {
        Accessibility accessibility = property.SetMethod?.DeclaredAccessibility ?? Accessibility.Private;
        return accessibility == Accessibility.Private ? "private set" : "set";
    }

    private static string ToCamelCase(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value.Length == 1
            ? value.ToLowerInvariant()
            : char.ToLowerInvariant(value[0]) + value.Substring(1);
    }
}
