using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MiKiNuo.Mvi.Infrastructure.Generators.Godot;

/// <summary>
/// 为 Godot MVI ViewRegistry 生成编译期 View 注册代码。
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class GodotMviViewRegistryGenerator : IIncrementalGenerator
{
    private const string ViewAttributeName = "MiKiNuo.Mvi.Platforms.Godot.Composition.MviGodotViewAttribute";
    private const string RegistryAttributeName = "MiKiNuo.Mvi.Platforms.Godot.Composition.MviGodotGeneratedRegistryAttribute";

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ViewRegistration> views = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ViewAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (syntaxContext, cancellationToken) => CreateViewRegistration(syntaxContext, cancellationToken));

        IncrementalValuesProvider<RegistryDeclaration> registries = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                RegistryAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (syntaxContext, cancellationToken) => CreateRegistryDeclaration(syntaxContext, cancellationToken));

        IncrementalValueProvider<ImmutableArray<ViewRegistration>> collectedViews = views.Collect();
        IncrementalValuesProvider<(RegistryDeclaration Registry, ImmutableArray<ViewRegistration> Views)> generationInputs = registries.Combine(collectedViews);

        context.RegisterSourceOutput(generationInputs, static (productionContext, source) =>
        {
            if (source.Views.IsDefaultOrEmpty)
            {
                return;
            }

            ImmutableArray<ViewRegistration> distinctViews = DeduplicateByKey(source.Views);
            string generatedSource = GenerateRegistrySource(source.Registry, distinctViews);
            productionContext.AddSource(source.Registry.HintName, SourceText.From(generatedSource, Encoding.UTF8));
        });
    }

    private static ViewRegistration CreateViewRegistration(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        INamedTypeSymbol typeSymbol = (INamedTypeSymbol)context.TargetSymbol;
        string key = typeSymbol.Name;
        string? scenePath = null;

        foreach (AttributeData attribute in context.Attributes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.Equals(attribute.AttributeClass?.ToDisplayString(), ViewAttributeName, StringComparison.Ordinal))
            {
                continue;
            }

            if (attribute.ConstructorArguments.Length > 0 &&
                attribute.ConstructorArguments[0].Value is string attributeKey &&
                !string.IsNullOrWhiteSpace(attributeKey))
            {
                key = attributeKey;
            }

            if (attribute.ConstructorArguments.Length > 1 &&
                attribute.ConstructorArguments[1].Value is string attributeScenePath &&
                !string.IsNullOrWhiteSpace(attributeScenePath))
            {
                scenePath = attributeScenePath;
            }
        }

        return new ViewRegistration(
            key,
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            scenePath);
    }

    private static RegistryDeclaration CreateRegistryDeclaration(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        INamedTypeSymbol typeSymbol = (INamedTypeSymbol)context.TargetSymbol;
        string? namespaceName = typeSymbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : typeSymbol.ContainingNamespace.ToDisplayString();

        string fullName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", string.Empty);

        string hintName = fullName.Replace('.', '_') + ".GodotMviViewRegistry.g.cs";

        return new RegistryDeclaration(
            namespaceName,
            GetAccessibilityText(typeSymbol.DeclaredAccessibility),
            typeSymbol.Name,
            hintName);
    }

    private static ImmutableArray<ViewRegistration> DeduplicateByKey(ImmutableArray<ViewRegistration> views)
    {
        Dictionary<string, ViewRegistration> registrations = new(StringComparer.Ordinal);

        foreach (ViewRegistration view in views)
        {
            if (!registrations.ContainsKey(view.Key))
            {
                registrations.Add(view.Key, view);
            }
        }

        return registrations.Values
            .OrderBy(static item => item.Key, StringComparer.Ordinal)
            .ToImmutableArray();
    }

    private static string GenerateRegistrySource(RegistryDeclaration registry, ImmutableArray<ViewRegistration> views)
    {
        StringBuilder builder = new();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();

        if (!string.IsNullOrWhiteSpace(registry.NamespaceName))
        {
            builder.Append("namespace ").Append(registry.NamespaceName).AppendLine(";");
            builder.AppendLine();
        }

        builder.Append(registry.Accessibility).Append(" partial class ").Append(registry.Name).AppendLine();
        builder.AppendLine("{");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Auto-generated: 注册 Godot MVI View 到编译期生成的 ViewRegistry。");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <param name=\"builder\">Godot View 注册表构建器。</param>");
        builder.AppendLine("    partial void RegisterGeneratedViews(global::MiKiNuo.Mvi.Platforms.Godot.Composition.GodotMviViewRegistryBuilder builder)");
        builder.AppendLine("    {");

        foreach (ViewRegistration view in views)
        {
            if (string.IsNullOrWhiteSpace(view.ScenePath))
            {
                builder.Append("        builder.Register(\"")
                    .Append(Escape(view.Key))
                    .Append("\", static () => new ")
                    .Append(view.TypeName)
                    .AppendLine("());");
            }
            else
            {
                builder.Append("        builder.RegisterScene(\"")
                    .Append(Escape(view.Key))
                    .Append("\", \"")
                    .Append(Escape(view.ScenePath!))
                    .AppendLine("\");");
            }
        }

        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string GetAccessibilityText(Accessibility accessibility)
    {
        return accessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            Accessibility.Private => "private",
            Accessibility.Protected => "protected",
            Accessibility.ProtectedAndInternal => "private protected",
            Accessibility.ProtectedOrInternal => "protected internal",
            _ => "internal",
        };
    }

    private static string Escape(string text)
    {
        return text.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private readonly struct RegistryDeclaration
    {
        public RegistryDeclaration(string? namespaceName, string accessibility, string name, string hintName)
        {
            NamespaceName = namespaceName;
            Accessibility = accessibility;
            Name = name;
            HintName = hintName;
        }

        public string? NamespaceName { get; }

        public string Accessibility { get; }

        public string Name { get; }

        public string HintName { get; }
    }

    private readonly struct ViewRegistration
    {
        public ViewRegistration(string key, string typeName, string? scenePath)
        {
            Key = key;
            TypeName = typeName;
            ScenePath = scenePath;
        }

        public string Key { get; }

        public string TypeName { get; }

        public string? ScenePath { get; }
    }
}
