using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MiKiNuo.Mvi.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MicrosoftNamingConventionAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.MicrosoftNaming);

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
        if (context.Symbol is not INamedTypeSymbol namedType || namedType.IsImplicitlyDeclared)
        {
            return;
        }

        switch (namedType.TypeKind)
        {
            case TypeKind.Class:
                ReportIfInvalid(context, namedType, IsPascalCase(namedType.Name), "类名必须使用 PascalCase");
                break;
            case TypeKind.Interface:
                ReportIfInvalid(context, namedType, IsInterfaceName(namedType.Name), "接口名必须以 I 开头并使用 PascalCase");
                break;
        }
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IPropertySymbol property || property.IsImplicitlyDeclared)
        {
            return;
        }

        ReportIfInvalid(context, property, IsPascalCase(property.Name), "属性名必须使用 PascalCase");
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IMethodSymbol method
            || method.IsImplicitlyDeclared
            || method.MethodKind != MethodKind.Ordinary)
        {
            return;
        }

        ReportIfInvalid(context, method, IsPascalCase(method.Name), "方法名必须使用 PascalCase");
    }

    private static void AnalyzeField(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IFieldSymbol field
            || field.IsImplicitlyDeclared
            || field.ContainingType?.TypeKind == TypeKind.Enum)
        {
            return;
        }

        if (field.IsConst)
        {
            ReportIfInvalid(context, field, IsPascalCase(field.Name), "常量名必须使用 PascalCase");
            return;
        }

        ReportIfInvalid(context, field, IsFieldName(field.Name), "字段名必须使用 camelCase 或 _camelCase");
    }

    private static void ReportIfInvalid(SymbolAnalysisContext context, ISymbol symbol, bool isValid, string rule)
    {
        if (isValid)
        {
            return;
        }

        var location = GetPrimarySourceLocation(symbol);
        if (location is null)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.MicrosoftNaming,
            location,
            symbol.Name,
            rule));
    }

    private static bool IsInterfaceName(string name)
    {
        return name.Length >= 2 && name[0] == 'I' && IsPascalCase(name.Substring(1));
    }

    private static bool IsFieldName(string name)
    {
        return IsCamelCase(name) || (name.Length > 1 && name[0] == '_' && IsCamelCase(name.Substring(1)));
    }

    private static bool IsPascalCase(string name)
    {
        return HasValidIdentifierCharacters(name) && char.IsUpper(name[0]) && !name.Contains("_");
    }

    private static bool IsCamelCase(string name)
    {
        return HasValidIdentifierCharacters(name) && char.IsLower(name[0]) && !name.Contains("_");
    }

    private static bool HasValidIdentifierCharacters(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        foreach (var character in name)
        {
            if (character != '_' && !char.IsLetterOrDigit(character))
            {
                return false;
            }
        }

        return true;
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
