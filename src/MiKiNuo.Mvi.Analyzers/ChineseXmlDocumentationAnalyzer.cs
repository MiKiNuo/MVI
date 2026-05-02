using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MiKiNuo.Mvi.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ChineseXmlDocumentationAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.ChineseXmlDocumentation);

    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol namedType)
        {
            return;
        }

        if (!IsEffectivelyPublicApi(namedType) || !IsRequiredNamedType(namedType))
        {
            return;
        }

        ReportIfDocumentationIsMissing(context, namedType);
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IPropertySymbol property || !IsEffectivelyPublicApi(property))
        {
            return;
        }

        ReportIfDocumentationIsMissing(context, property);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IMethodSymbol method
            || !IsEffectivelyPublicApi(method)
            || method.MethodKind != MethodKind.Ordinary)
        {
            return;
        }

        ReportIfDocumentationIsMissing(context, method);
    }

    private static void AnalyzeField(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IFieldSymbol field
            || !IsEffectivelyPublicApi(field)
            || field.IsImplicitlyDeclared)
        {
            return;
        }

        ReportIfDocumentationIsMissing(context, field);
    }

    private static void ReportIfDocumentationIsMissing(SymbolAnalysisContext context, ISymbol symbol)
    {
        if (HasXmlDocumentation(symbol))
        {
            return;
        }

        var location = GetPrimarySourceLocation(symbol);
        if (location is null)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ChineseXmlDocumentation,
            location,
            symbol.Name));
    }

    private static bool IsRequiredNamedType(INamedTypeSymbol namedType)
    {
        return namedType.TypeKind is TypeKind.Class or TypeKind.Interface;
    }

    private static bool IsEffectivelyPublicApi(ISymbol symbol)
    {
        if (symbol.DeclaredAccessibility != Accessibility.Public)
        {
            return false;
        }

        var containingType = symbol.ContainingType;
        while (containingType is not null)
        {
            if (containingType.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }

            containingType = containingType.ContainingType;
        }

        return true;
    }

    private static bool HasXmlDocumentation(ISymbol symbol)
    {
        var xmlDocumentation = symbol.GetDocumentationCommentXml(
            null,
            expandIncludes: false,
            cancellationToken: default);

        if (xmlDocumentation is null || string.IsNullOrWhiteSpace(xmlDocumentation))
        {
            return false;
        }

        return ContainsChineseCharacter(xmlDocumentation);
    }

    private static bool ContainsChineseCharacter(string value)
    {
        foreach (var character in value)
        {
            if (IsChineseCharacter(character))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsChineseCharacter(char character)
    {
        return character is >= '\u4E00' and <= '\u9FFF'
            or >= '\u3400' and <= '\u4DBF'
            or >= '\uF900' and <= '\uFAFF';
    }

    private static Location? GetPrimarySourceLocation(ISymbol symbol)
    {
        foreach (var location in symbol.Locations)
        {
            if (location.IsInSource)
            {
                return location;
            }
        }

        return null;
    }
}
