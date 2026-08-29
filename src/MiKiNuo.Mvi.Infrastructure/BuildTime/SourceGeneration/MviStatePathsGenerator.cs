using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using MiKiNuo.Mvi.Infrastructure.BuildTime.Diagnostics;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;

/// <summary>
/// 表示为所有实现 IMviState 的状态类型生成强类型 StatePath 的源生成器。
/// <para>
/// 对每个状态类型 emit 一个 &lt;StateName&gt;StatePaths 静态类，
/// 叶子属性生成为 StatePath 字段，可展开的嵌套 record 生成为嵌套静态类，
/// 形成 DashboardStatePaths.Machine.Speed 式的访问形态。
/// </para>
/// </summary>
[Generator]
public sealed class MviStatePathsGenerator : IIncrementalGenerator
{
    /// <summary>
    /// 初始化源生成器注册编译回调。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<INamedTypeSymbol> stateTypes = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is TypeDeclarationSyntax { BaseList: not null },
                static (syntaxContext, cancellationToken) => GetStateType(syntaxContext, cancellationToken))
            .Where(static stateType => stateType is not null)
            .Select(static (stateType, _) => stateType!);

        context.RegisterSourceOutput(stateTypes, Execute);
    }

    private static INamedTypeSymbol? GetStateType(
        GeneratorSyntaxContext context,
        System.Threading.CancellationToken cancellationToken)
    {
        INamedTypeSymbol? stateMarker = context.SemanticModel.Compilation.GetTypeByMetadataName(
            "MiKiNuo.Mvi.Domain.MVI.State.IMviState");
        if (stateMarker is null)
        {
            return null;
        }

        if (context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken) is not INamedTypeSymbol typeSymbol
            || !StatePathGraph.IsNamespaceAccessible(typeSymbol)
            || !typeSymbol.AllInterfaces.Any(i => i.Equals(stateMarker, SymbolEqualityComparer.Default)))
        {
            return null;
        }

        return typeSymbol;
    }

    private static void Execute(SourceProductionContext context, INamedTypeSymbol typeSymbol)
    {
        if (IsGeneric(typeSymbol))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Analysis.GenericStateSkippedRule,
                typeSymbol.Locations.FirstOrDefault(),
                typeSymbol.ToDisplayString()));
            return;
        }

        List<StatePathNode> roots = StatePathGraph.Expand(
            typeSymbol,
            cycleType => context.ReportDiagnostic(Diagnostic.Create(
                Analysis.StateGraphCycleRule,
                typeSymbol.Locations.FirstOrDefault(),
                typeSymbol.Name,
                cycleType.Name)));

        string source = Emission.Emit(typeSymbol, roots);
        context.AddSource(
            GetHintName(typeSymbol),
            SourceText.From(source, Encoding.UTF8));
    }

    private static bool IsGeneric(INamedTypeSymbol symbol)
    {
        for (INamedTypeSymbol? current = symbol; current is not null; current = current.ContainingType)
        {
            if (current.IsGenericType)
            {
                return true;
            }
        }

        return false;
    }

    private static string GetHintName(INamedTypeSymbol symbol)
    {
        string fullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        StringBuilder builder = new(fullName.Length + 20);
        foreach (char character in fullName)
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        return builder.Append(".StatePaths.g.cs").ToString();
    }

    /// <summary>
    /// 表示状态路径生成器的诊断规则。
    /// </summary>
    internal static class Analysis
    {
        /// <summary>状态属性图存在循环引用。</summary>
        public static readonly DiagnosticDescriptor StateGraphCycleRule = new(
            id: DiagnosticIdCatalog.MviStatePathGraphCycle,
            title: "状态属性图存在循环引用",
            messageFormat: "状态类型“{0}”的属性图存在循环引用，涉及类型“{1}”，已跳过该分支的路径生成。",
            category: "MviStatePath",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>泛型状态类型跳过路径生成。</summary>
        public static readonly DiagnosticDescriptor GenericStateSkippedRule = new(
            id: DiagnosticIdCatalog.MviStatePathGenericStateSkipped,
            title: "泛型状态类型跳过 StatePath 生成",
            messageFormat: "状态类型“{0}”为泛型类型或其包含类型为泛型，跳过 StatePath 生成。",
            category: "MviStatePath",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
    }

    /// <summary>
    /// 表示代码发射阶段：根据路径节点树生成 StatePath 静态类。
    /// </summary>
    internal static class Emission
    {
        /// <summary>
        /// 发射状态路径类源码。
        /// </summary>
        /// <param name="stateSymbol">状态类型符号。</param>
        /// <param name="roots">根级路径节点。</param>
        /// <returns>生成的源代码。</returns>
        public static string Emit(INamedTypeSymbol stateSymbol, IReadOnlyList<StatePathNode> roots)
        {
            string? namespaceName = GeneratorSyntaxHelpers.GetNamespaceForEmit(stateSymbol);
            string className = GetGeneratedBaseName(stateSymbol) + "Paths";
            string stateTypeName = GeneratorSyntaxHelpers.FormatFullyQualified(stateSymbol);
            string accessibility = IsPublic(stateSymbol) ? "public" : "internal";

            StringBuilder builder = new();
            builder.AppendLine("// <auto-generated/>");
            builder.AppendLine("#nullable enable");
            builder.AppendLine("#pragma warning disable");
            builder.AppendLine();

            if (namespaceName is not null)
            {
                builder.Append("namespace ").Append(namespaceName).AppendLine(";");
                builder.AppendLine();
            }

            builder.AppendLine("/// <summary>");
            builder.Append("/// 表示 ").Append(stateSymbol.Name).AppendLine(" 的强类型状态路径，由源生成器产出。");
            builder.AppendLine("/// </summary>");
            builder.Append(accessibility).Append(" static class ").AppendLine(className);
            builder.AppendLine("{");

            foreach (StatePathNode node in roots)
            {
                EmitNode(builder, node, stateTypeName, "    ");
            }

            builder.AppendLine("}");
            return builder.ToString();
        }

        /// <summary>
        /// 计算状态类型对应的生成类名前缀，嵌套类型以下划线连接。
        /// 例如 DashboardState 对应 DashboardState，生成的路径类为 DashboardStatePaths。
        /// </summary>
        /// <param name="stateSymbol">状态类型符号。</param>
        /// <returns>生成类名前缀。</returns>
        public static string GetGeneratedBaseName(INamedTypeSymbol stateSymbol)
        {
            Stack<string> names = new();
            for (INamedTypeSymbol? current = stateSymbol; current is not null; current = current.ContainingType)
            {
                names.Push(current.Name);
            }

            return string.Join("_", names);
        }

        private static void EmitNode(
            StringBuilder builder,
            StatePathNode node,
            string stateTypeName,
            string indent)
        {
            if (!node.IsBranch)
            {
                EmitPathField(builder, node, stateTypeName, indent, StatePathGraph.EscapeIdentifier(node.Name));
                return;
            }

            builder.Append(indent).AppendLine("/// <summary>");
            builder.Append(indent).Append("/// 表示路径 ").Append(node.DisplayPath).AppendLine(" 下的子路径。");
            builder.Append(indent).AppendLine("/// </summary>");
            builder.Append(indent).Append("public static class ").AppendLine(StatePathGraph.EscapeIdentifier(node.Name));
            builder.Append(indent).AppendLine("{");

            string selfName = node.Children.Any(static child => child.Name == StatePathGraph.SelfPathName)
                ? StatePathGraph.SelfPathFallbackName
                : StatePathGraph.SelfPathName;
            EmitPathField(builder, node, stateTypeName, indent + "    ", selfName);

            foreach (StatePathNode child in node.Children)
            {
                EmitNode(builder, child, stateTypeName, indent + "    ");
            }

            builder.Append(indent).AppendLine("}");
        }

        private static void EmitPathField(
            StringBuilder builder,
            StatePathNode node,
            string stateTypeName,
            string indent,
            string memberName)
        {
            string valueTypeName = GeneratorSyntaxHelpers.FormatFullyQualified(node.ValueType);
            string statePathType = "global::MiKiNuo.Mvi.Domain.MVI.State.StatePath<"
                + stateTypeName + ", " + valueTypeName + ">";

            builder.Append(indent).AppendLine("/// <summary>");
            builder.Append(indent).Append("/// 表示路径 ").Append(node.DisplayPath).AppendLine("。");
            builder.Append(indent).AppendLine("/// </summary>");
            builder.Append(indent).Append("public static readonly ").Append(statePathType).Append(' ')
                .Append(memberName).AppendLine(" =");
            builder.Append(indent).Append("    ").Append(statePathType).AppendLine(".Create(");
            builder.Append(indent).Append("        \"")
                .Append(GeneratorSyntaxHelpers.EscapeStringLiteral(node.DisplayPath)).AppendLine("\",");
            builder.Append(indent).Append("        static state => ").Append(node.AccessPath).AppendLine(");");
            builder.AppendLine();
        }

        private static bool IsPublic(INamedTypeSymbol symbol)
        {
            for (INamedTypeSymbol? current = symbol; current is not null; current = current.ContainingType)
            {
                if (current.DeclaredAccessibility != Accessibility.Public)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
