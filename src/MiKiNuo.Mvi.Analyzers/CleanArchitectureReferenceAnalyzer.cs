using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MiKiNuo.Mvi.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CleanArchitectureReferenceAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            DiagnosticDescriptors.CleanArchitecture,
            DiagnosticDescriptors.PlatformUiReference);

    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
        context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol namedType || namedType.IsImplicitlyDeclared)
        {
            return;
        }

        if (namedType.BaseType is not null && namedType.BaseType.SpecialType != SpecialType.System_Object)
        {
            AnalyzeReferencedType(context, namedType, namedType.BaseType);
        }

        foreach (var interfaceType in namedType.Interfaces)
        {
            AnalyzeReferencedType(context, namedType, interfaceType);
        }
    }

    private static void AnalyzeField(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IFieldSymbol field || field.IsImplicitlyDeclared)
        {
            return;
        }

        AnalyzeReferencedType(context, field, field.Type);
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IPropertySymbol property || property.IsImplicitlyDeclared)
        {
            return;
        }

        AnalyzeReferencedType(context, property, property.Type);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IMethodSymbol method
            || method.IsImplicitlyDeclared
            || !IsAnalyzedMethodKind(method.MethodKind))
        {
            return;
        }

        if (method.ReturnType.SpecialType != SpecialType.System_Void)
        {
            AnalyzeReferencedType(context, method, method.ReturnType);
        }

        foreach (var parameter in method.Parameters)
        {
            AnalyzeReferencedType(context, method, parameter.Type);
        }
    }

    private static bool IsAnalyzedMethodKind(MethodKind methodKind)
    {
        return methodKind is MethodKind.Ordinary or MethodKind.Constructor;
    }

    private static void AnalyzeReferencedType(SymbolAnalysisContext context, ISymbol sourceSymbol, ITypeSymbol referencedType)
    {
        var sourceLayer = ClassifySymbol(sourceSymbol);
        if (sourceLayer == ArchitectureLayer.Unknown)
        {
            return;
        }

        foreach (var namedType in EnumerateNamedTypes(referencedType))
        {
            var referencedLayer = ClassifySymbol(namedType);
            if (!TryGetViolation(sourceLayer, referencedLayer, out var descriptor))
            {
                continue;
            }

            var location = GetPrimarySourceLocation(sourceSymbol);
            if (location is null)
            {
                continue;
            }

            if (descriptor.Id == DiagnosticIds.PlatformUiReference)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    descriptor,
                    location,
                    GetLayerDisplayName(sourceLayer),
                    namedType.ToDisplayString()));
            }
            else
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    descriptor,
                    location,
                    GetLayerDisplayName(sourceLayer),
                    GetLayerDisplayName(referencedLayer),
                    namedType.ToDisplayString()));
            }
        }
    }

    private static ImmutableArray<INamedTypeSymbol> EnumerateNamedTypes(ITypeSymbol type)
    {
        var builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
        AddNamedTypes(type, builder);
        return builder.ToImmutable();
    }

    private static void AddNamedTypes(ITypeSymbol type, ImmutableArray<INamedTypeSymbol>.Builder builder)
    {
        if (type is IArrayTypeSymbol arrayType)
        {
            AddNamedTypes(arrayType.ElementType, builder);
            return;
        }

        if (type is not INamedTypeSymbol namedType)
        {
            return;
        }

        builder.Add(namedType);

        foreach (var typeArgument in namedType.TypeArguments)
        {
            AddNamedTypes(typeArgument, builder);
        }
    }

    private static bool TryGetViolation(
        ArchitectureLayer sourceLayer,
        ArchitectureLayer referencedLayer,
        out DiagnosticDescriptor descriptor)
    {
        descriptor = DiagnosticDescriptors.CleanArchitecture;

        if (referencedLayer == ArchitectureLayer.Unknown || sourceLayer == referencedLayer)
        {
            return false;
        }

        if ((sourceLayer == ArchitectureLayer.Domain || sourceLayer == ArchitectureLayer.Application)
            && referencedLayer == ArchitectureLayer.PlatformUi)
        {
            descriptor = DiagnosticDescriptors.PlatformUiReference;
            return true;
        }

        if (sourceLayer == ArchitectureLayer.Domain
            && (referencedLayer == ArchitectureLayer.Infrastructure || referencedLayer == ArchitectureLayer.DiRuntime))
        {
            return true;
        }

        if (sourceLayer == ArchitectureLayer.Application && referencedLayer == ArchitectureLayer.Infrastructure)
        {
            return true;
        }

        if (sourceLayer == ArchitectureLayer.Core && referencedLayer == ArchitectureLayer.PlatformUi)
        {
            return true;
        }

        if (sourceLayer == ArchitectureLayer.Presentation
            && (referencedLayer == ArchitectureLayer.PlatformUi || referencedLayer == ArchitectureLayer.Infrastructure))
        {
            return true;
        }

        if (sourceLayer == ArchitectureLayer.PlatformUi && referencedLayer == ArchitectureLayer.Infrastructure)
        {
            return true;
        }

        if (sourceLayer == ArchitectureLayer.SourceGen && referencedLayer != ArchitectureLayer.Abstractions)
        {
            return true;
        }

        return false;
    }

    private static ArchitectureLayer ClassifySymbol(ISymbol symbol)
    {
        var containingNamespace = symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        return ClassifyNamespace(containingNamespace);
    }

    private static ArchitectureLayer ClassifyNamespace(string namespaceName)
    {
        if (string.IsNullOrWhiteSpace(namespaceName))
        {
            return ArchitectureLayer.Unknown;
        }

        if (namespaceName == "MiKiNuo.Mvi.Abstractions" || namespaceName.StartsWith("MiKiNuo.Mvi.Abstractions.", StringComparison.Ordinal))
        {
            return ArchitectureLayer.Abstractions;
        }

        if (namespaceName == "MiKiNuo.Mvi.Core" || namespaceName.StartsWith("MiKiNuo.Mvi.Core.", StringComparison.Ordinal))
        {
            return ArchitectureLayer.Core;
        }

        if (namespaceName == "MiKiNuo.Mvi.DI" || namespaceName.StartsWith("MiKiNuo.Mvi.DI.", StringComparison.Ordinal))
        {
            return ArchitectureLayer.DiRuntime;
        }

        if (namespaceName == "MiKiNuo.Mvi.SourceGen" || namespaceName.StartsWith("MiKiNuo.Mvi.SourceGen.", StringComparison.Ordinal))
        {
            return ArchitectureLayer.SourceGen;
        }

        if (namespaceName == "MiKiNuo.Mvi.Platforms" || namespaceName.StartsWith("MiKiNuo.Mvi.Platforms.", StringComparison.Ordinal))
        {
            return ArchitectureLayer.PlatformUi;
        }

        if (IsExternalPlatformUiNamespace(namespaceName))
        {
            return ArchitectureLayer.PlatformUi;
        }

        if (ContainsLayerSegment(namespaceName, "Domain"))
        {
            return ArchitectureLayer.Domain;
        }

        if (ContainsLayerSegment(namespaceName, "Application"))
        {
            return ArchitectureLayer.Application;
        }

        if (ContainsLayerSegment(namespaceName, "Infrastructure"))
        {
            return ArchitectureLayer.Infrastructure;
        }

        if (ContainsLayerSegment(namespaceName, "Presentation"))
        {
            return ArchitectureLayer.Presentation;
        }

        return ArchitectureLayer.Unknown;
    }

    private static bool ContainsLayerSegment(string namespaceName, string segment)
    {
        return namespaceName == segment
            || namespaceName.EndsWith("." + segment, StringComparison.Ordinal)
            || namespaceName.Contains("." + segment + ".");
    }

    private static bool IsExternalPlatformUiNamespace(string namespaceName)
    {
        return namespaceName == "Avalonia"
            || namespaceName.StartsWith("Avalonia.", StringComparison.Ordinal)
            || namespaceName == "System.Windows.Forms"
            || namespaceName.StartsWith("System.Windows.Forms.", StringComparison.Ordinal)
            || namespaceName == "Godot"
            || namespaceName.StartsWith("Godot.", StringComparison.Ordinal)
            || namespaceName == "UnityEngine"
            || namespaceName.StartsWith("UnityEngine.", StringComparison.Ordinal);
    }

    private static string GetLayerDisplayName(ArchitectureLayer layer)
    {
        return layer switch
        {
            ArchitectureLayer.Domain => "Domain",
            ArchitectureLayer.Application => "Application",
            ArchitectureLayer.Abstractions => "MVI Abstractions",
            ArchitectureLayer.Core => "MVI Core",
            ArchitectureLayer.DiRuntime => "DI Runtime",
            ArchitectureLayer.Infrastructure => "Infrastructure",
            ArchitectureLayer.Presentation => "Presentation",
            ArchitectureLayer.PlatformUi => "Platform UI",
            ArchitectureLayer.SourceGen => "SourceGen",
            _ => "Unknown"
        };
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

    private enum ArchitectureLayer
    {
        Unknown,
        Domain,
        Application,
        Abstractions,
        Core,
        DiRuntime,
        Infrastructure,
        Presentation,
        PlatformUi,
        SourceGen
    }
}
