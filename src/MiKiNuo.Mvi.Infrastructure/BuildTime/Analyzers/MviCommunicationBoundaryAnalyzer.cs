using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using MiKiNuo.Mvi.Infrastructure.BuildTime.Diagnostics;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.Analyzers;

/// <summary>阻止业务 Feature 中可静态识别的跨 Store 直接访问。</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MviCommunicationBoundaryAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIdCatalog.MviCrossFeatureCommunication, "跨 Feature 通信必须经过中介者",
        "Feature“{0}”不得直接使用其他 Store；请通过 Mediator 契约通信。",
        "MviComposition", DiagnosticSeverity.Error, true);

    /// <summary>获取支持的规则。</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <summary>注册符号与调用边界检查。</summary>
    /// <param name="context">分析上下文。</param>
    public override void Initialize(AnalysisContext context)
    {
        if (context is null) throw new System.ArgumentNullException(nameof(context));
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeCall, OperationKind.Invocation);
        context.RegisterSymbolAction(AnalyzeDependency, SymbolKind.Field, SymbolKind.Parameter, SymbolKind.Property);
    }

    private static void AnalyzeCall(OperationAnalysisContext context)
    {
        IInvocationOperation operation = (IInvocationOperation)context.Operation;
        INamedTypeSymbol? feature = FindFeature(context.ContainingSymbol.ContainingType);
        if (feature is null) return;
        if (IsForeignStore(operation.Instance?.Type, feature)
            || IsForeignStore(operation.Type, feature))
            context.ReportDiagnostic(Diagnostic.Create(Rule, operation.Syntax.GetLocation(), context.ContainingSymbol.ContainingType.Name));
    }

    private static void AnalyzeDependency(SymbolAnalysisContext context)
    {
        INamedTypeSymbol? feature = FindFeature(context.Symbol.ContainingType);
        if (feature is null) return;
        ITypeSymbol? dependency = context.Symbol switch
        {
            IFieldSymbol field => field.Type,
            IParameterSymbol parameter => parameter.Type,
            IPropertySymbol property => property.Type,
            _ => null,
        };
        if (IsForeignStore(dependency, feature))
            context.ReportDiagnostic(Diagnostic.Create(Rule, context.Symbol.Locations[0], context.Symbol.ContainingType.Name));
    }

    private static INamedTypeSymbol? FindFeature(INamedTypeSymbol? type)
    {
        for (INamedTypeSymbol? current = type?.BaseType; current is not null; current = current.BaseType)
        {
            string space = current.ContainingNamespace.ToDisplayString();
            if ((current.Name == "MviViewModelBase" && space == "MiKiNuo.Mvi.Application.MVI.ViewModel")
                || (current.Name == "MviReducerBase" && space == "MiKiNuo.Mvi.Application.MVI.Reducer")
                || (current.Name == "MviEffectDispatcherBase" && space == "MiKiNuo.Mvi.Application.MVI.Effect")) return current;
        }
        return null;
    }

    private static bool IsForeignStore(ITypeSymbol? type, INamedTypeSymbol feature)
    {
        if (type is not INamedTypeSymbol store || store.TypeArguments.Length != 3
            || (store.Name != "IMviStore" && store.Name != "MviStore")
            || store.ContainingNamespace.ToDisplayString() != "MiKiNuo.Mvi.Application.MVI.Store") return false;
        int offset = feature.TypeArguments.Length == 2 ? 1 : 0;
        for (int index = offset; index < 3; index++)
            if (!SymbolEqualityComparer.Default.Equals(store.TypeArguments[index], feature.TypeArguments[index - offset])) return true;
        return false;
    }
}
