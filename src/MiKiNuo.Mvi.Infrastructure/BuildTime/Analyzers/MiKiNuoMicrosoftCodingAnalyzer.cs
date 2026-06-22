using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using MiKiNuo.Mvi.Infrastructure.BuildTime.Diagnostics;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.Analyzers;

/// <summary>
/// 表示微软 C# 编码规范分析器。
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MiKiNuoMicrosoftCodingAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor TypeNamingRule = new(
        id: DiagnosticIdCatalog.CodeTypeNaming,
        title: "类型命名必须符合微软 C# 编码规范",
        messageFormat: "类型“{0}”命名不符合微软 C# 编码规范。",
        category: "CodingStyle",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "公共类型应使用 PascalCase，接口应以 I 开头，避免保留关键字和含糊命名.");

    private static readonly DiagnosticDescriptor MemberNamingRule = new(
        id: DiagnosticIdCatalog.CodeMemberNaming,
        title: "成员命名必须符合微软 C# 编码规范",
        messageFormat: "成员“{0}”命名不符合微软 C# 编码规范。",
        category: "CodingStyle",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "公共方法、属性、事件和常量应使用 PascalCase，异步方法应以 Async 结尾.");

    /// <summary>
    /// 获取支持的诊断描述集合。
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(TypeNamingRule, MemberNamingRule);

    /// <summary>
    /// 初始化分析器注册诊断动作。
    /// </summary>
    /// <param name="context">分析上下文。</param>
    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
        context.RegisterSymbolAction(AnalyzeEvent, SymbolKind.Event);
        context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        INamedTypeSymbol symbol = (INamedTypeSymbol)context.Symbol;

        if (!IsExternallyVisible(symbol) || symbol.IsImplicitlyDeclared)
        {
            return;
        }

        if (symbol.TypeKind == TypeKind.Interface && !symbol.Name.StartsWith("I", StringComparison.Ordinal))
        {
            Report(context, TypeNamingRule, symbol.Name, symbol.Locations);
            return;
        }

        if (!IsPascalCase(symbol.Name))
        {
            Report(context, TypeNamingRule, symbol.Name, symbol.Locations);
        }
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        IMethodSymbol symbol = (IMethodSymbol)context.Symbol;

        if (!IsExternallyVisible(symbol) || symbol.IsImplicitlyDeclared)
        {
            return;
        }

        if (symbol.MethodKind is MethodKind.Constructor or MethodKind.StaticConstructor or MethodKind.PropertyGet or MethodKind.PropertySet)
        {
            return;
        }

        if (!IsPascalCase(symbol.Name) || ShouldEndWithAsync(symbol))
        {
            Report(context, MemberNamingRule, symbol.Name, symbol.Locations);
        }
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context)
    {
        IPropertySymbol symbol = (IPropertySymbol)context.Symbol;

        if (IsExternallyVisible(symbol) && !symbol.IsImplicitlyDeclared && !IsPascalCase(symbol.Name))
        {
            Report(context, MemberNamingRule, symbol.Name, symbol.Locations);
        }
    }

    private static void AnalyzeEvent(SymbolAnalysisContext context)
    {
        IEventSymbol symbol = (IEventSymbol)context.Symbol;

        if (IsExternallyVisible(symbol) && !symbol.IsImplicitlyDeclared && !IsPascalCase(symbol.Name))
        {
            Report(context, MemberNamingRule, symbol.Name, symbol.Locations);
        }
    }

    private static void AnalyzeField(SymbolAnalysisContext context)
    {
        IFieldSymbol symbol = (IFieldSymbol)context.Symbol;

        if (!IsExternallyVisible(symbol) || symbol.IsImplicitlyDeclared)
        {
            return;
        }

        if ((symbol.IsConst || symbol.IsStatic) && !IsPascalCase(symbol.Name))
        {
            Report(context, MemberNamingRule, symbol.Name, symbol.Locations);
        }
    }

    private static bool ShouldEndWithAsync(IMethodSymbol symbol)
    {
        bool returnsTask = symbol.ReturnType.Name is "Task" or "ValueTask";
        return returnsTask && !symbol.Name.EndsWith("Async", StringComparison.Ordinal);
    }

    private static bool IsExternallyVisible(ISymbol symbol)
    {
        return symbol.DeclaredAccessibility is Accessibility.Public
            or Accessibility.Protected
            or Accessibility.ProtectedOrInternal;
    }

    private static bool IsPascalCase(string name)
    {
        return name.Length > 0 && char.IsUpper(name[0]);
    }

    private static void Report(
        SymbolAnalysisContext context,
        DiagnosticDescriptor descriptor,
        string symbolName,
        ImmutableArray<Location> locations)
    {
        Location location = locations.Length > 0 ? locations[0] : Location.None;
        Diagnostic diagnostic = Diagnostic.Create(descriptor, location, symbolName);
        context.ReportDiagnostic(diagnostic);
    }
}
