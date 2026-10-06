using MiKiNuo.Mvi.Generators.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
namespace MiKiNuo.Mvi.Generators.SourceGeneration;
internal static class MviMethodDispatch
{
    private static readonly DiagnosticDescriptor Invalid = new(DiagnosticIdCatalog.MviPipelineDeclarationInvalid, "处理方法声明无效", "{0}", "MviPipeline", DiagnosticSeverity.Error, true);
    internal static void Register(IncrementalGeneratorInitializationContext context, bool intent)
    {
        var candidates = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is ClassDeclarationSyntax c && c.BaseList is not null,
            (ctx, token) => Analyze(ctx, token, intent)).Where(static result => result is not null);
        context.RegisterSourceOutput(candidates, static (output, result) =>
        {
            foreach (Diagnostic diagnostic in result!.Diagnostics) output.ReportDiagnostic(diagnostic);
            if (result.Source is not null) output.AddSource(result.Hint, SourceText.From(result.Source, Encoding.UTF8));
        });
    }
    private static Result? Analyze(GeneratorSyntaxContext context, System.Threading.CancellationToken token, bool intent)
    {
        if (context.SemanticModel.GetDeclaredSymbol(context.Node, token) is not INamedTypeSymbol type) return null;
        string baseName = intent ? "MiKiNuo.Mvi.Runtime.MVI.Intent.MviIntentHandler`2" : "MiKiNuo.Mvi.Runtime.MVI.Reducer.MviReducerBase`1";
        INamedTypeSymbol? definition = context.SemanticModel.Compilation.GetTypeByMetadataName(baseName);
        INamedTypeSymbol? parent = type.BaseType;
        while (parent is not null && !SymbolEqualityComparer.Default.Equals(parent.OriginalDefinition, definition)) parent = parent.BaseType;
        if (parent is null) return null;
        SyntaxReference? owner = type.DeclaringSyntaxReferences.FirstOrDefault(reference => reference.GetSyntax(token) is ClassDeclarationSyntax c && c.BaseList is not null);
        if (owner is not null && (owner.SyntaxTree != context.Node.SyntaxTree || owner.Span != context.Node.Span)) return null;
        string entry = intent ? "HandleAsync" : "Reduce";
        if (type.GetMembers(entry).OfType<IMethodSymbol>().Any(m => m.IsOverride)) return null;
        var errors = new List<Diagnostic>();
        void Error(ISymbol at, string text) => errors.Add(Diagnostic.Create(Invalid, at.Locations.FirstOrDefault(), text));
        if (type.ContainingType is not null || type.Arity != 0 || type.IsAbstract || !type.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax(token) is ClassDeclarationSyntax c && c.Modifiers.Any(SyntaxKind.PartialKeyword)))
            Error(type, "生成式 Handler/Reducer 必须为顶层非泛型、非抽象 partial class。");
        string S(ITypeSymbol t) => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        ITypeSymbol state = parent.TypeArguments[0];
        string stateName = S(state);
        ITypeSymbol input = intent ? parent.TypeArguments[1] : context.SemanticModel.Compilation.GetTypeByMetadataName("MiKiNuo.Mvi.Abstractions.MVI.Mutation.IMviMutation`1")!.Construct(state);
        var methods = new List<(IMethodSymbol Method, INamedTypeSymbol Input)>();
        string attrName = intent ? "MiKiNuo.Mvi.Abstractions.MVI.Intent.MviHandleAttribute" : "MiKiNuo.Mvi.Abstractions.MVI.Reducer.MviReduceAttribute";
        foreach (IMethodSymbol method in type.GetMembers().OfType<IMethodSymbol>())
        {
            AttributeData? attr = method.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == attrName);
            if (attr is null) continue;
            if (attr.ConstructorArguments.Length != 1 || attr.ConstructorArguments[0].Value is not INamedTypeSymbol target) { Error(method, "缺少具体输入类型。"); continue; }
            bool convertible = ((CSharpCompilation)context.SemanticModel.Compilation).ClassifyConversion(target, input).IsImplicit;
            bool valid = convertible && !method.IsGenericMethod && method.Parameters.All(p => p.RefKind == RefKind.None);
            if (intent)
            {
                valid &= method.Parameters.Length == 3
                    && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, target)
                    && method.Parameters[1].Type is INamedTypeSymbol c && c.OriginalDefinition.ToDisplayString() == "MiKiNuo.Mvi.Runtime.MVI.Intent.IIntentContext<TState>"
                    && SymbolEqualityComparer.Default.Equals(c.TypeArguments[0], state)
                    && method.Parameters[2].Type.ToDisplayString() == "System.Threading.CancellationToken"
                    && method.ReturnType.ToDisplayString() == "System.Threading.Tasks.ValueTask";
            }
            else valid &= method.IsStatic && method.Parameters.Length == 2
                && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, state)
                && SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, target)
                && SymbolEqualityComparer.Default.Equals(method.ReturnType, state);
            if (!valid) Error(method, intent ? "Handle 方法必须返回 ValueTask，参数为具体 Intent、IIntentContext<TState>、CancellationToken。" : "Reduce 方法必须为 static，参数为 State、IMviMutation<State> 的具体类型，返回 State。");
            if (methods.Any(item => SymbolEqualityComparer.Default.Equals(item.Input, target))) Error(method, "同一输入不能注册多个处理方法。");
            methods.Add((method, target));
        }
        if (methods.Count == 0) Error(type, "没有可用的类型化处理方法。");
        string hint = type.ToDisplayString() + "." + entry + ".g.cs";
        if (errors.Count != 0) return new Result(hint, null, errors);
        var b = new StringBuilder("// <auto-generated />\n#nullable enable\n");
        if (!type.ContainingNamespace.IsGlobalNamespace) b.Append("namespace ").Append(type.ContainingNamespace.ToDisplayString()).AppendLine(";");
        b.AppendLine("/// <summary>类型化处理入口的生成实现。</summary>");
        b.Append(type.DeclaredAccessibility == Accessibility.Public ? "public" : "internal").Append(" partial class @").Append(type.Name).AppendLine("\n{");
        b.AppendLine("    /// <inheritdoc/>");
        if (intent)
            b.Append("    public override global::System.Threading.Tasks.ValueTask HandleAsync(").Append(S(input)).Append(" intent, global::MiKiNuo.Mvi.Runtime.MVI.Intent.IIntentContext<").Append(stateName).AppendLine("> context, global::System.Threading.CancellationToken cancellationToken)\n    {\n        cancellationToken.ThrowIfCancellationRequested();\n        return intent switch\n        {");
        else b.Append("    public override ").Append(stateName).Append(" Reduce(").Append(stateName).Append(" state, ").Append(S(input)).AppendLine(" mutation) => mutation switch\n    {");
        // 先派生后基类，避免基类分支遮蔽更具体类型。
        foreach (var m in methods.OrderByDescending(m => Depth(m.Input)))
            b.Append("        ").Append(S(m.Input)).Append(" value => @").Append(m.Method.Name).Append(intent ? "(value, context, cancellationToken),\n" : "(state, value),\n");
        b.AppendLine("        _ => throw new global::System.ArgumentException(\"未注册的输入类型。\"),");
        b.AppendLine(intent ? "        };\n    }\n}" : "    };\n}");
        return new Result(hint, b.ToString(), errors);
    }
    private static int Depth(INamedTypeSymbol type) { int depth = 0; for (var c = type.BaseType; c is not null; c = c.BaseType) depth++; return depth; }
    private sealed class Result(string hint, string? source, List<Diagnostic> diagnostics)
    { internal string Hint = hint; internal string? Source = source; internal List<Diagnostic> Diagnostics = diagnostics; }
}
