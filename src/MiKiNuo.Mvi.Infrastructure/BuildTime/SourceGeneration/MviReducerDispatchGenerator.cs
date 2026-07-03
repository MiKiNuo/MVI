using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using MiKiNuo.Mvi.Infrastructure.BuildTime.Diagnostics;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;

/// <summary>
/// 表示根据 [MviReduce] 特性自动生成规约器分发逻辑的源生成器。
/// <para>
/// 扫描继承 MviReducerBase&lt;TState, TIntent, TEffect&gt; 的 partial 类，
/// 收集标记 [MviReduce(typeof(IntentSubtype))] 的方法，
/// 自动 emit Reduce override 方法的 switch 分发代码。
/// </para>
/// </summary>
[Generator]
public sealed class MviReducerDispatchGenerator : IIncrementalGenerator
{
    /// <summary>
    /// 初始化源生成器注册编译回调。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, Execute);
    }

    private static void Execute(SourceProductionContext context, Compilation compilation)
    {
        INamedTypeSymbol? reducerBaseSymbol = compilation.GetTypeByMetadataName(
            "MiKiNuo.Mvi.Application.MVI.Reducer.MviReducerBase`3");

        if (reducerBaseSymbol is null)
        {
            return;
        }

        foreach (INamedTypeSymbol reducerSymbol in GeneratorSyntaxHelpers.EnumerateClassSymbols(
            compilation,
            context.CancellationToken))
        {
            ReducerDescriptor? descriptor = Analysis.Collect(
                reducerSymbol,
                reducerBaseSymbol,
                compilation,
                context);

            if (descriptor is null)
            {
                continue;
            }

            string source = Emission.Emit(descriptor);
            context.AddSource(
                $"{descriptor.ReducerSymbol.Name}.MviReduce.g.cs",
                SourceText.From(source, Encoding.UTF8));
        }
    }

    /// <summary>
    /// 表示分析阶段：提取规约器方法映射、验证签名、报告诊断。
    /// </summary>
    internal static class Analysis
    {
        private static readonly DiagnosticDescriptor ReducerNotPartialRule = new(
            id: DiagnosticIdCatalog.MviReducerNotPartial,
            title: "规约器类必须标记 partial 修饰符",
            messageFormat: "规约器类“{0}”必须标记 partial 修饰符，否则源生成器无法 emit Reduce 方法。",
            category: "MviReducer",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor HandlerMissingRule = new(
            id: DiagnosticIdCatalog.MviReduceHandlerMissing,
            title: "意图子类型缺少对应的规约方法",
            messageFormat: "意图子类型“{0}”没有对应的 [MviReduce] 方法，将走默认分支返回原状态。",
            category: "MviReducer",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor HandlerDuplicateRule = new(
            id: DiagnosticIdCatalog.MviReduceHandlerDuplicate,
            title: "多个规约方法标记同一意图子类型",
            messageFormat: "意图子类型“{0}”被多个方法标记：{1}。每个意图子类型只能有一个规约方法。",
            category: "MviReducer",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor HandlerSignatureInvalidRule = new(
            id: DiagnosticIdCatalog.MviReduceHandlerSignatureInvalid,
            title: "规约方法签名不符合约定",
            messageFormat: "方法“{0}”的签名不符合约定。必须是 (TState state, TIntent.Xxx intent, IMviBusinessResult? result) => MviReduceResult<TState, TEffect>。",
            category: "MviReducer",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor GuardInvalidRule = new(
            id: DiagnosticIdCatalog.MviReduceGuardInvalid,
            title: "守卫谓词方法不存在或签名不匹配",
            messageFormat: "方法“{0}”的 Guard 谓词“{1}”不存在或签名不是 (TState state) => bool。",
            category: "MviReducer",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
    /// 收集规约器描述信息。
    /// </summary>
    /// <param name="reducerSymbol">规约器类型符号。</param>
    /// <param name="reducerBaseSymbol">规约器基类符号。</param>
    /// <param name="compilation">编译对象。</param>
    /// <param name="context">源生成上下文。</param>
    /// <returns>规约器描述，不匹配则返回 null。</returns>
    public static ReducerDescriptor? Collect(
        INamedTypeSymbol reducerSymbol,
        INamedTypeSymbol reducerBaseSymbol,
        Compilation compilation,
        SourceProductionContext context)
        {
            INamedTypeSymbol? baseGeneric = GeneratorSyntaxHelpers.FindGenericBaseInChain(reducerSymbol, reducerBaseSymbol);
            if (baseGeneric is null)
            {
                return null;
            }

            INamedTypeSymbol stateType = (INamedTypeSymbol)baseGeneric.TypeArguments[0];
            INamedTypeSymbol intentType = (INamedTypeSymbol)baseGeneric.TypeArguments[1];
            INamedTypeSymbol effectType = (INamedTypeSymbol)baseGeneric.TypeArguments[2];

            if (!IsPartial(reducerSymbol))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ReducerNotPartialRule,
                    reducerSymbol.Locations.FirstOrDefault(),
                    reducerSymbol.Name));
                return null;
            }

            List<ReduceHandlerModel> handlers = CollectHandlers(
                reducerSymbol,
                stateType,
                intentType,
                effectType,
                compilation,
                context);

            ReportMissingHandlers(intentType, handlers, reducerSymbol, context);

            return new ReducerDescriptor(
                reducerSymbol,
                stateType,
                intentType,
                effectType,
                handlers);
        }

        private static bool IsPartial(INamedTypeSymbol reducerSymbol)
        {
            return reducerSymbol.DeclaredAccessibility == Accessibility.Public
                && reducerSymbol.IsSealed
                && reducerSymbol.Locations.Any(static loc => loc.IsInSource && IsPartialDeclaration(loc));
        }

        private static bool IsPartialDeclaration(Location location)
        {
            SyntaxTree? tree = location.SourceTree;
            if (tree is null)
            {
                return false;
            }

            SyntaxNode root = tree.GetRoot();
            ClassDeclarationSyntax? declaration = root
                .FindNode(location.SourceSpan)
                .FirstAncestorOrSelf<ClassDeclarationSyntax>();

            return declaration is not null
                && declaration.Modifiers.Any(SyntaxKind.PartialKeyword);
        }

        private static List<ReduceHandlerModel> CollectHandlers(
            INamedTypeSymbol reducerSymbol,
            INamedTypeSymbol stateType,
            INamedTypeSymbol intentType,
            INamedTypeSymbol effectType,
            Compilation compilation,
            SourceProductionContext context)
        {
            List<ReduceHandlerModel> handlers = new();
            Dictionary<INamedTypeSymbol, List<string>> intentToMethods = new(
                SymbolEqualityComparer.Default);

            foreach (IMethodSymbol method in reducerSymbol.GetMembers().OfType<IMethodSymbol>())
            {
                AttributeData? reduceAttr = FindMviReduceAttribute(method);
                if (reduceAttr is null)
                {
                    continue;
                }

                if (!TryExtractIntentType(reduceAttr, out INamedTypeSymbol? intentSubtype))
                {
                    continue;
                }

                string? guardName = TryExtractGuard(reduceAttr);

                if (!ValidateMethodSignature(method, stateType, intentSubtype, effectType, compilation))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        HandlerSignatureInvalidRule,
                        method.Locations.FirstOrDefault(),
                        method.Name));
                    continue;
                }

                if (guardName is not null
                    && !ValidateGuardMethod(reducerSymbol, stateType, guardName))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        GuardInvalidRule,
                        method.Locations.FirstOrDefault(),
                        method.Name,
                        guardName));
                    continue;
                }

                if (!intentToMethods.TryGetValue(intentSubtype!, out List<string>? methodNames))
                {
                    methodNames = new List<string>();
                    intentToMethods[intentSubtype!] = methodNames;
                }

                methodNames.Add(method.Name);

                handlers.Add(new ReduceHandlerModel(
                    method.Name,
                    intentSubtype!,
                    guardName));
            }

            ReportDuplicateHandlers(intentToMethods, reducerSymbol, context);

            return handlers;
        }

        private static AttributeData? FindMviReduceAttribute(IMethodSymbol method)
        {
            foreach (AttributeData attr in method.GetAttributes())
            {
                if (attr.AttributeClass?.Name == "MviReduceAttribute"
                    || attr.AttributeClass?.Name == "MviReduce")
                {
                    return attr;
                }
            }

            return null;
        }

        private static bool TryExtractIntentType(AttributeData attr, out INamedTypeSymbol? intentType)
        {
            intentType = null;
            if (attr.ConstructorArguments.Length == 0)
            {
                return false;
            }

            if (attr.ConstructorArguments[0].Value is INamedTypeSymbol type)
            {
                intentType = type;
                return true;
            }

            return false;
        }

        private static string? TryExtractGuard(AttributeData attr)
        {
            foreach (KeyValuePair<string, TypedConstant> named in attr.NamedArguments)
            {
                if (named.Key == "Guard" && named.Value.Value is string guardName)
                {
                    return guardName;
                }
            }

            return null;
        }

        private static bool ValidateMethodSignature(
            IMethodSymbol method,
            INamedTypeSymbol stateType,
            INamedTypeSymbol? intentSubtype,
            INamedTypeSymbol effectType,
            Compilation compilation)
        {
            if (intentSubtype is null)
            {
                return false;
            }

            if (method.Parameters.Length != 3)
            {
                return false;
            }

            if (!method.Parameters[0].Type.Equals(stateType, SymbolEqualityComparer.Default))
            {
                return false;
            }

            if (!method.Parameters[1].Type.Equals(intentSubtype, SymbolEqualityComparer.Default))
            {
                return false;
            }

            INamedTypeSymbol? businessResultType = compilation.GetTypeByMetadataName(
                "MiKiNuo.Mvi.Domain.MVI.Business.IMviBusinessResult");
            if (businessResultType is null)
            {
                return false;
            }

            ITypeSymbol thirdParamType = method.Parameters[2].Type;
            INamedTypeSymbol? thirdParamNamed = (thirdParamType as INamedTypeSymbol)?.OriginalDefinition;
            if (thirdParamNamed is null
                || !thirdParamNamed.Equals(businessResultType, SymbolEqualityComparer.Default))
            {
                return false;
            }

            INamedTypeSymbol? expectedReturn = compilation.GetTypeByMetadataName(
                "MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult`2");
            if (expectedReturn is null)
            {
                return false;
            }

            INamedTypeSymbol constructedReturn = expectedReturn.Construct(stateType, effectType);
            if (method.ReturnType is not INamedTypeSymbol actualReturn
                || !actualReturn.Equals(constructedReturn, SymbolEqualityComparer.Default))
            {
                return false;
            }

            return true;
        }

        private static bool ValidateGuardMethod(
            INamedTypeSymbol reducerSymbol,
            INamedTypeSymbol stateType,
            string guardName)
        {
            foreach (IMethodSymbol method in reducerSymbol.GetMembers().OfType<IMethodSymbol>())
            {
                if (method.Name != guardName)
                {
                    continue;
                }

                if (method.Parameters.Length != 1)
                {
                    continue;
                }

                if (!method.Parameters[0].Type.Equals(stateType, SymbolEqualityComparer.Default))
                {
                    continue;
                }

                if (method.ReturnType.SpecialType != SpecialType.System_Boolean)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static void ReportMissingHandlers(
            INamedTypeSymbol intentType,
            List<ReduceHandlerModel> handlers,
            INamedTypeSymbol reducerSymbol,
            SourceProductionContext context)
        {
            HashSet<INamedTypeSymbol> handledIntents = new(
                handlers.Select(static h => h.IntentSubtype),
                SymbolEqualityComparer.Default);

            foreach (INamedTypeSymbol member in intentType.GetTypeMembers())
            {
                if (!IsConcreteIntentSubtype(member))
                {
                    continue;
                }

                if (!handledIntents.Contains(member))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        HandlerMissingRule,
                        reducerSymbol.Locations.FirstOrDefault(),
                        member.Name));
                }
            }
        }

        private static bool IsConcreteIntentSubtype(INamedTypeSymbol type)
        {
            return type.TypeKind == TypeKind.Class
                && type.IsSealed
                && !type.IsAbstract
                && type.DeclaredAccessibility == Accessibility.Public;
        }

        private static void ReportDuplicateHandlers(
            Dictionary<INamedTypeSymbol, List<string>> intentToMethods,
            INamedTypeSymbol reducerSymbol,
            SourceProductionContext context)
        {
            foreach (KeyValuePair<INamedTypeSymbol, List<string>> pair in intentToMethods)
            {
                if (pair.Value.Count <= 1)
                {
                    continue;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    HandlerDuplicateRule,
                    reducerSymbol.Locations.FirstOrDefault(),
                    pair.Key.Name,
                    string.Join(", ", pair.Value)));
            }
        }
    }

    /// <summary>
    /// 表示代码发射阶段：根据描述生成 Reduce override 方法。
    /// </summary>
    internal static class Emission
    {
        /// <summary>
        /// 发射规约器分发代码。
        /// </summary>
        /// <param name="descriptor">规约器描述。</param>
        /// <returns>生成的源代码。</returns>
        public static string Emit(ReducerDescriptor descriptor)
        {
            string? namespaceName = GeneratorSyntaxHelpers.GetNamespaceForEmit(descriptor.ReducerSymbol);
            string className = descriptor.ReducerSymbol.Name;
            string stateTypeName = GeneratorSyntaxHelpers.FormatFullyQualified(descriptor.StateType);
            string intentTypeName = GeneratorSyntaxHelpers.FormatFullyQualified(descriptor.IntentType);
            string effectTypeName = GeneratorSyntaxHelpers.FormatFullyQualified(descriptor.EffectType);

            StringBuilder builder = new();
            builder.AppendLine("// <auto-generated/>");
            builder.AppendLine("#pragma warning disable");
            builder.AppendLine();
            builder.AppendLine("using System;");
            builder.AppendLine($"using {descriptor.StateType.ContainingNamespace.ToDisplayString()};");
            builder.AppendLine($"using {descriptor.IntentType.ContainingNamespace.ToDisplayString()};");
            builder.AppendLine($"using {descriptor.EffectType.ContainingNamespace.ToDisplayString()};");
            builder.AppendLine("using MiKiNuo.Mvi.Domain.MVI.Business;");
            builder.AppendLine("using MiKiNuo.Mvi.Domain.MVI.Reducer;");
            builder.AppendLine();

            if (namespaceName is not null)
            {
                builder.AppendLine($"namespace {namespaceName};");
                builder.AppendLine();
            }

            builder.AppendLine($"public sealed partial class {className}");
            builder.AppendLine("{");

            builder.AppendLine($"    public override MviReduceResult<{stateTypeName}, {effectTypeName}> Reduce(");
            builder.AppendLine($"        {stateTypeName} state,");
            builder.AppendLine($"        {intentTypeName} intent,");
            builder.AppendLine("        IMviBusinessResult? result = null)");
            builder.AppendLine("    {");
            builder.AppendLine("        ArgumentNullException.ThrowIfNull(state);");
            builder.AppendLine("        ArgumentNullException.ThrowIfNull(intent);");
            builder.AppendLine();
            builder.AppendLine("        return intent switch");
            builder.AppendLine("        {");

            for (int i = 0; i < descriptor.Handlers.Count; i++)
            {
                ReduceHandlerModel handler = descriptor.Handlers[i];
                string intentSubtypeName = GeneratorSyntaxHelpers.FormatFullyQualified(handler.IntentSubtype);
                string variableName = GeneratorSyntaxHelpers.ToCamelCase(handler.IntentSubtype.Name);

                string guardClause = handler.GuardName is null
                    ? string.Empty
                    : $" when {handler.GuardName}(state)";

                builder.Append($"            {intentSubtypeName} {variableName}{guardClause} => {handler.MethodName}(state, {variableName}, result)");

                if (i < descriptor.Handlers.Count - 1)
                {
                    builder.AppendLine(",");
                }
                else
                {
                    builder.AppendLine(",");
                }
            }

            builder.AppendLine($"            _ => MviReduceResult.State<{stateTypeName}, {effectTypeName}>(state),");
            builder.AppendLine("        };");
            builder.AppendLine("    }");
            builder.AppendLine("}");

            return builder.ToString();
        }
    }
}

/// <summary>
/// 表示规约器描述信息。
/// </summary>
internal sealed class ReducerDescriptor
{
    /// <summary>
    /// 初始化规约器描述。
    /// </summary>
    /// <param name="reducerSymbol">规约器类型符号。</param>
    /// <param name="stateType">状态类型。</param>
    /// <param name="intentType">意图类型。</param>
    /// <param name="effectType">副作用类型。</param>
    /// <param name="handlers">规约方法集合。</param>
    public ReducerDescriptor(
        INamedTypeSymbol reducerSymbol,
        INamedTypeSymbol stateType,
        INamedTypeSymbol intentType,
        INamedTypeSymbol effectType,
        IReadOnlyList<ReduceHandlerModel> handlers)
    {
        ReducerSymbol = reducerSymbol;
        StateType = stateType;
        IntentType = intentType;
        EffectType = effectType;
        Handlers = handlers;
    }

    /// <summary>规约器类型符号。</summary>
    public INamedTypeSymbol ReducerSymbol { get; }

    /// <summary>状态类型。</summary>
    public INamedTypeSymbol StateType { get; }

    /// <summary>意图类型。</summary>
    public INamedTypeSymbol IntentType { get; }

    /// <summary>副作用类型。</summary>
    public INamedTypeSymbol EffectType { get; }

    /// <summary>规约方法集合。</summary>
    public IReadOnlyList<ReduceHandlerModel> Handlers { get; }
}

/// <summary>
/// 表示规约方法映射信息。
/// </summary>
internal sealed class ReduceHandlerModel
{
    /// <summary>
    /// 初始化规约方法模型。
    /// </summary>
    /// <param name="methodName">方法名称。</param>
    /// <param name="intentSubtype">意图子类型。</param>
    /// <param name="guardName">守卫谓词方法名。</param>
    public ReduceHandlerModel(
        string methodName,
        INamedTypeSymbol intentSubtype,
        string? guardName)
    {
        MethodName = methodName;
        IntentSubtype = intentSubtype;
        GuardName = guardName;
    }

    /// <summary>方法名称。</summary>
    public string MethodName { get; }

    /// <summary>意图子类型。</summary>
    public INamedTypeSymbol IntentSubtype { get; }

    /// <summary>守卫谓词方法名。</summary>
    public string? GuardName { get; }
}
