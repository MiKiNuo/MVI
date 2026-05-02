using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.Analyzers;

/// <summary>
/// 表示中文 XML 注释分析器。
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MiKiNuoChineseDocumentationAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor TypeDocumentationRule = new(
        id: "DOC0001",
        title: "公共类型必须提供中文 XML 注释",
        messageFormat: "公共类型“{0}”必须提供良好的中文 XML 注释。",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "类、接口、枚举、委托和记录类型等公共 API 必须提供中文 XML 注释.");

    private static readonly DiagnosticDescriptor MethodDocumentationRule = new(
        id: "DOC0002",
        title: "公共方法必须提供中文 XML 注释",
        messageFormat: "公共方法“{0}”必须提供良好的中文 XML 注释。",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "公共方法、构造函数、虚方法和抽象方法必须提供中文 XML 注释.");

    private static readonly DiagnosticDescriptor PropertyDocumentationRule = new(
        id: "DOC0003",
        title: "公共属性必须提供中文 XML 注释",
        messageFormat: "公共属性“{0}”必须提供良好的中文 XML 注释。",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "公共属性和需要维护的受保护属性必须提供中文 XML 注释.");

    private static readonly DiagnosticDescriptor FieldDocumentationRule = new(
        id: "DOC0004",
        title: "公共字段和常量必须提供中文 XML 注释",
        messageFormat: "公共字段或常量“{0}”必须提供良好的中文 XML 注释。",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "公共字段、公共常量和公共静态字段必须提供中文 XML 注释.");

    private static readonly DiagnosticDescriptor InterfaceMemberDocumentationRule = new(
        id: "DOC0005",
        title: "接口成员必须提供中文 XML 注释",
        messageFormat: "接口成员“{0}”必须提供良好的中文 XML 注释。",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "接口成员属于公共契约，必须提供中文 XML 注释.");

    private static readonly DiagnosticDescriptor ParameterDocumentationRule = new(
        id: "DOC0006",
        title: "公共方法参数必须提供中文说明",
        messageFormat: "公共方法“{0}”的参数必须提供中文 param 注释。",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "公共方法参数必须使用中文 param 说明.");

    private static readonly DiagnosticDescriptor ReturnDocumentationRule = new(
        id: "DOC0007",
        title: "公共方法返回值必须提供中文说明",
        messageFormat: "公共方法“{0}”的返回值必须提供中文 returns 注释。",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "有返回值的公共方法必须使用中文 returns 说明.");

    private static readonly DiagnosticDescriptor InvalidDocumentationRule = new(
        id: "DOC0008",
        title: "XML 注释不能为空或占位内容",
        messageFormat: "成员“{0}”的 XML 注释不能是空白、TODO 或无意义占位内容。",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "XML 注释需要具备维护价值，不能使用空白、TODO 或占位文本.");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            TypeDocumentationRule,
            MethodDocumentationRule,
            PropertyDocumentationRule,
            FieldDocumentationRule,
            InterfaceMemberDocumentationRule,
            ParameterDocumentationRule,
            ReturnDocumentationRule,
            InvalidDocumentationRule);

    /// <inheritdoc />
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
        context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
        context.RegisterSymbolAction(AnalyzeEvent, SymbolKind.Event);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        INamedTypeSymbol symbol = (INamedTypeSymbol)context.Symbol;

        if (ShouldSkip(symbol))
        {
            return;
        }

        AnalyzeSummary(context, symbol, TypeDocumentationRule);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        IMethodSymbol symbol = (IMethodSymbol)context.Symbol;

        if (ShouldSkip(symbol) || symbol.MethodKind is MethodKind.PropertyGet or MethodKind.PropertySet)
        {
            return;
        }

        AnalyzeSummary(context, symbol, MethodDocumentationRule);
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context)
    {
        IPropertySymbol symbol = (IPropertySymbol)context.Symbol;

        if (ShouldSkip(symbol))
        {
            return;
        }

        DiagnosticDescriptor descriptor = symbol.ContainingType.TypeKind == TypeKind.Interface
            ? InterfaceMemberDocumentationRule
            : PropertyDocumentationRule;
        AnalyzeSummary(context, symbol, descriptor);
    }

    private static void AnalyzeField(SymbolAnalysisContext context)
    {
        IFieldSymbol symbol = (IFieldSymbol)context.Symbol;

        if (ShouldSkip(symbol))
        {
            return;
        }

        AnalyzeSummary(context, symbol, FieldDocumentationRule);
    }

    private static void AnalyzeEvent(SymbolAnalysisContext context)
    {
        IEventSymbol symbol = (IEventSymbol)context.Symbol;

        if (ShouldSkip(symbol))
        {
            return;
        }

        AnalyzeSummary(context, symbol, PropertyDocumentationRule);
    }

    private static void AnalyzeSummary(
        SymbolAnalysisContext context,
        ISymbol symbol,
        DiagnosticDescriptor missingDescriptor)
    {
        string documentation = symbol.GetDocumentationCommentXml() ?? string.Empty;

        if (documentation.IndexOf("inheritdoc", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(documentation))
        {
            Report(context, missingDescriptor, symbol);
            return;
        }

        if (ContainsInvalidPlaceholder(documentation) || !ContainsChineseCharacter(documentation))
        {
            Report(context, InvalidDocumentationRule, symbol);
        }
    }

    private static bool ShouldSkip(ISymbol symbol)
    {
        return symbol.IsImplicitlyDeclared
            || symbol.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal)
            || symbol.Locations.Length == 0;
    }

    private static bool ContainsInvalidPlaceholder(string documentation)
    {
        return documentation.IndexOf("TODO", StringComparison.OrdinalIgnoreCase) >= 0
            || documentation.IndexOf("待补充", StringComparison.Ordinal) >= 0
            || documentation.IndexOf("placeholder", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool ContainsChineseCharacter(string text)
    {
        foreach (char character in text)
        {
            if (character >= '\u4e00' && character <= '\u9fff')
            {
                return true;
            }
        }

        return false;
    }

    private static void Report(
        SymbolAnalysisContext context,
        DiagnosticDescriptor descriptor,
        ISymbol symbol)
    {
        Location location = symbol.Locations.Length > 0 ? symbol.Locations[0] : Location.None;
        Diagnostic diagnostic = Diagnostic.Create(descriptor, location, symbol.Name);
        context.ReportDiagnostic(diagnostic);
    }
}
