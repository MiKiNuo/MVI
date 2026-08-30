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
/// 表示根据 [MviEffect] 特性自动生成副作用分派逻辑的源生成器。
/// <para>
/// 扫描继承 MviEffectDispatcherBase&lt;TIntent, TEffect&gt; 的 partial 类，
/// 收集标记 [MviEffect(typeof(EffectSubtype))] 的方法，
/// 自动 emit DispatchCoreAsync override 的 switch 分派代码。
/// 与 MviReducerDispatchGenerator 对称，实现全框架零手写 switch。
/// </para>
/// </summary>
[Generator]
public sealed class MviEffectDispatchGenerator : IIncrementalGenerator
{
    /// <summary>
    /// 初始化源生成器注册编译回调。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<INamedTypeSymbol?> candidates = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
                static (syntaxContext, cancellationToken) => GetCandidate(syntaxContext, cancellationToken))
            .Where(static candidate => candidate is not null);

        context.RegisterSourceOutput(candidates, Execute);
    }

    private static INamedTypeSymbol? GetCandidate(
        GeneratorSyntaxContext context,
        System.Threading.CancellationToken cancellationToken)
    {
        INamedTypeSymbol? dispatcherBaseSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName(
            "MiKiNuo.Mvi.Application.MVI.Effect.MviEffectDispatcherBase`2");
        if (dispatcherBaseSymbol is null
            || context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken) is not INamedTypeSymbol dispatcherSymbol
            || GeneratorSyntaxHelpers.FindGenericBaseInChain(dispatcherSymbol, dispatcherBaseSymbol) is null)
        {
            return null;
        }

        return dispatcherSymbol;
    }

    private static void Execute(SourceProductionContext context, INamedTypeSymbol? dispatcherSymbol)
    {
        if (dispatcherSymbol is null)
        {
            return;
        }

        INamedTypeSymbol? baseGeneric = FindDispatcherBase(dispatcherSymbol);
        if (baseGeneric is null)
        {
            return;
        }

        INamedTypeSymbol intentType = (INamedTypeSymbol)baseGeneric.TypeArguments[0];
        INamedTypeSymbol effectType = (INamedTypeSymbol)baseGeneric.TypeArguments[1];

        if (!IsPartial(dispatcherSymbol))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rules.DispatcherNotPartialRule,
                dispatcherSymbol.Locations.FirstOrDefault(),
                dispatcherSymbol.Name));
            return;
        }

        List<EffectHandlerModel> handlers = CollectHandlers(dispatcherSymbol, effectType, context);
        ReportMissingHandlers(effectType, handlers, dispatcherSymbol, context);

        string source = Emission.Emit(dispatcherSymbol, intentType, effectType, handlers);
        context.AddSource(
            $"{dispatcherSymbol.Name}.MviEffect.g.cs",
            SourceText.From(source, Encoding.UTF8));
    }

    private static INamedTypeSymbol? FindDispatcherBase(INamedTypeSymbol symbol)
    {
        INamedTypeSymbol? current = symbol.BaseType;
        while (current is not null)
        {
            if (current.OriginalDefinition.ToDisplayString() ==
                "MiKiNuo.Mvi.Application.MVI.Effect.MviEffectDispatcherBase<TIntent, TEffect>")
            {
                return current;
            }

            current = current.BaseType;
        }

        return null;
    }

    private static bool IsPartial(INamedTypeSymbol dispatcherSymbol)
    {
        return dispatcherSymbol.Locations.Any(static loc => loc.IsInSource && IsPartialDeclaration(loc));
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

    private static List<EffectHandlerModel> CollectHandlers(
        INamedTypeSymbol dispatcherSymbol,
        INamedTypeSymbol effectType,
        SourceProductionContext context)
    {
        List<EffectHandlerModel> handlers = new();
        Dictionary<INamedTypeSymbol, List<string>> effectToMethods = new(
            SymbolEqualityComparer.Default);

        foreach (IMethodSymbol method in dispatcherSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            AttributeData? effectAttr = FindMviEffectAttribute(method);
            if (effectAttr is null)
            {
                continue;
            }

            if (effectAttr.ConstructorArguments.Length == 0
                || effectAttr.ConstructorArguments[0].Value is not INamedTypeSymbol effectSubtype)
            {
                continue;
            }

            if (!ValidateMethodSignature(method, effectSubtype))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rules.HandlerSignatureInvalidRule,
                    method.Locations.FirstOrDefault(),
                    method.Name));
                continue;
            }

            if (!effectToMethods.TryGetValue(effectSubtype, out List<string>? methodNames))
            {
                methodNames = new List<string>();
                effectToMethods[effectSubtype] = methodNames;
            }

            methodNames.Add(method.Name);
            handlers.Add(new EffectHandlerModel(method.Name, effectSubtype));
        }

        foreach (KeyValuePair<INamedTypeSymbol, List<string>> pair in effectToMethods)
        {
            if (pair.Value.Count <= 1)
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rules.HandlerDuplicateRule,
                dispatcherSymbol.Locations.FirstOrDefault(),
                pair.Key.Name,
                string.Join(", ", pair.Value)));
        }

        return handlers;
    }

    private static AttributeData? FindMviEffectAttribute(IMethodSymbol method)
    {
        foreach (AttributeData attr in method.GetAttributes())
        {
            if (attr.AttributeClass?.Name == "MviEffectAttribute"
                || attr.AttributeClass?.Name == "MviEffect")
            {
                return attr;
            }
        }

        return null;
    }

    private static bool ValidateMethodSignature(
        IMethodSymbol method,
        INamedTypeSymbol effectSubtype)
    {
        if (method.Parameters.Length != 2)
        {
            return false;
        }

        if (!method.Parameters[0].Type.Equals(effectSubtype, SymbolEqualityComparer.Default))
        {
            return false;
        }

        if (method.Parameters[1].Type.Name != "CancellationToken")
        {
            return false;
        }

        return method.ReturnType.Name == "ValueTask";
    }

    private static void ReportMissingHandlers(
        INamedTypeSymbol effectType,
        List<EffectHandlerModel> handlers,
        INamedTypeSymbol dispatcherSymbol,
        SourceProductionContext context)
    {
        HashSet<INamedTypeSymbol> handledEffects = new(
            handlers.Select(static h => h.EffectSubtype),
            SymbolEqualityComparer.Default);

        foreach (INamedTypeSymbol member in effectType.GetTypeMembers())
        {
            if (member.TypeKind != TypeKind.Class
                || !member.IsSealed
                || member.IsAbstract
                || member.DeclaredAccessibility != Accessibility.Public)
            {
                continue;
            }

            if (!handledEffects.Contains(member))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rules.HandlerMissingRule,
                    dispatcherSymbol.Locations.FirstOrDefault(),
                    member.Name));
            }
        }
    }

    /// <summary>
    /// 表示副作用分派生成器的诊断规则集合。
    /// </summary>
    internal static class Rules
    {
        /// <summary>副作用分发器类必须标记 partial 修饰符。</summary>
        public static readonly DiagnosticDescriptor DispatcherNotPartialRule = new(
            id: DiagnosticIdCatalog.MviEffectDispatcherNotPartial,
            title: "副作用分发器类必须标记 partial 修饰符",
            messageFormat: "副作用分发器类“{0}”必须标记 partial 修饰符，否则源生成器无法 emit DispatchCoreAsync 方法。",
            category: "MviEffect",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>副作用子类型缺少对应的处理方法。</summary>
        public static readonly DiagnosticDescriptor HandlerMissingRule = new(
            id: DiagnosticIdCatalog.MviEffectHandlerMissing,
            title: "副作用子类型缺少对应的处理方法",
            messageFormat: "副作用子类型“{0}”没有对应的 [MviEffect] 方法，分发时将走默认分支不执行任何操作。",
            category: "MviEffect",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>多个处理方法标记同一副作用子类型。</summary>
        public static readonly DiagnosticDescriptor HandlerDuplicateRule = new(
            id: DiagnosticIdCatalog.MviEffectHandlerDuplicate,
            title: "多个处理方法标记同一副作用子类型",
            messageFormat: "副作用子类型“{0}”被多个方法标记：{1}。每个副作用子类型只能有一个处理方法。",
            category: "MviEffect",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>副作用处理方法签名不符合约定。</summary>
        public static readonly DiagnosticDescriptor HandlerSignatureInvalidRule = new(
            id: DiagnosticIdCatalog.MviEffectHandlerSignatureInvalid,
            title: "副作用处理方法签名不符合约定",
            messageFormat: "方法“{0}”的签名不符合约定。必须是 (TEffect.Xxx effect, CancellationToken cancellationToken) => ValueTask。",
            category: "MviEffect",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }

    /// <summary>
    /// 表示代码发射阶段：根据描述生成 DispatchCoreAsync override 方法。
    /// </summary>
    internal static class Emission
    {
        /// <summary>
        /// 发射副作用分派代码。
        /// </summary>
        /// <param name="dispatcherSymbol">分发器类型符号。</param>
        /// <param name="intentType">意图类型。</param>
        /// <param name="effectType">副作用类型。</param>
        /// <param name="handlers">处理方法集合。</param>
        /// <returns>生成的源代码。</returns>
        public static string Emit(
            INamedTypeSymbol dispatcherSymbol,
            INamedTypeSymbol intentType,
            INamedTypeSymbol effectType,
            IReadOnlyList<EffectHandlerModel> handlers)
        {
            string? namespaceName = GeneratorSyntaxHelpers.GetNamespaceForEmit(dispatcherSymbol);
            string className = dispatcherSymbol.Name;
            string effectTypeName = GeneratorSyntaxHelpers.FormatFullyQualified(effectType);

            StringBuilder builder = new();
            builder.AppendLine("// <auto-generated/>");
            builder.AppendLine("#pragma warning disable");
            builder.AppendLine();
            builder.AppendLine("using System;");
            builder.AppendLine("using System.Threading;");
            builder.AppendLine("using System.Threading.Tasks;");
            builder.AppendLine($"using {effectType.ContainingNamespace.ToDisplayString()};");
            builder.AppendLine();

            if (namespaceName is not null)
            {
                builder.AppendLine($"namespace {namespaceName};");
                builder.AppendLine();
            }

            string accessibility = dispatcherSymbol.DeclaredAccessibility == Accessibility.Public
                ? "public"
                : "internal";
            string sealedModifier = dispatcherSymbol.IsSealed ? " sealed" : string.Empty;
            builder.AppendLine($"{accessibility}{sealedModifier} partial class {className}");
            builder.AppendLine("{");
            builder.AppendLine($"    protected override ValueTask DispatchCoreAsync(");
            builder.AppendLine($"        {effectTypeName} effect,");
            builder.AppendLine("        CancellationToken cancellationToken)");
            builder.AppendLine("    {");
            builder.AppendLine("        return effect switch");
            builder.AppendLine("        {");

            foreach (EffectHandlerModel handler in handlers)
            {
                string effectSubtypeName = GeneratorSyntaxHelpers.FormatFullyQualified(handler.EffectSubtype);
                string variableName = GeneratorSyntaxHelpers.ToCamelCase(handler.EffectSubtype.Name);
                builder.AppendLine(
                    $"            {effectSubtypeName} {variableName} => {handler.MethodName}({variableName}, cancellationToken),");
            }

            builder.AppendLine("            _ => ValueTask.CompletedTask,");
            builder.AppendLine("        };");
            builder.AppendLine("    }");
            builder.AppendLine("}");

            return builder.ToString();
        }
    }
}

/// <summary>
/// 表示副作用处理方法映射信息。
/// </summary>
internal sealed class EffectHandlerModel
{
    /// <summary>
    /// 初始化处理方法模型。
    /// </summary>
    /// <param name="methodName">方法名称。</param>
    /// <param name="effectSubtype">副作用子类型。</param>
    public EffectHandlerModel(string methodName, INamedTypeSymbol effectSubtype)
    {
        MethodName = methodName;
        EffectSubtype = effectSubtype;
    }

    /// <summary>方法名称。</summary>
    public string MethodName { get; }

    /// <summary>副作用子类型。</summary>
    public INamedTypeSymbol EffectSubtype { get; }
}
