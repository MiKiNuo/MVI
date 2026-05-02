using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MiKiNuo.Mvi.Analyzers.Tests;

internal static class AnalyzerVerifier
{
    public static async Task<Diagnostic[]> GetAnalyzerDiagnosticsAsync(DiagnosticAnalyzer analyzer, string source)
    {
        ArgumentNullException.ThrowIfNull(analyzer);
        ArgumentNullException.ThrowIfNull(source);

        var parseOptions = CSharpParseOptions.Default
            .WithLanguageVersion(LanguageVersion.Preview)
            .WithDocumentationMode(DocumentationMode.Parse);

        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);

        var compilation = CSharpCompilation.Create(
            assemblyName: "AnalyzerVerifierFixture",
            syntaxTrees: [syntaxTree],
            references: GetTrustedPlatformReferences(),
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        var compilerErrors = compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();

        if (compilerErrors.Length > 0)
        {
            var errorText = string.Join(Environment.NewLine, compilerErrors.Select(diagnostic => diagnostic.ToString()));
            throw new InvalidOperationException($"Fixture source did not compile:{Environment.NewLine}{errorText}");
        }

        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);

        return diagnostics
            .OrderBy(diagnostic => diagnostic.Location.SourceSpan.Start)
            .ThenBy(diagnostic => diagnostic.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<MetadataReference> GetTrustedPlatformReferences()
    {
        var trustedPlatformAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");

        if (string.IsNullOrWhiteSpace(trustedPlatformAssemblies))
        {
            return [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)];
        }

        return trustedPlatformAssemblies
            .Split(Path.PathSeparator)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();
    }
}
