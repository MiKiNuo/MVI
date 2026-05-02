using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MiKiNuo.Mvi.SourceGen.Tests;

public static class SourceGeneratorVerifier
{
    public static Task<SourceGeneratorVerifierResult> VerifyAsync(
        string source,
        params IIncrementalGenerator[] generators)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var references = GetMetadataReferences();
        var compilation = CSharpCompilation.Create(
            "SourceGeneratorVerifierAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var sourceGenerators = generators
            .Select(generator => generator.AsSourceGenerator())
            .ToArray();
        var driver = CSharpGeneratorDriver.Create(
            sourceGenerators,
            parseOptions: parseOptions);
        driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var updatedCompilation,
            out var generatorDiagnostics);

        var emitDiagnostics = updatedCompilation.GetDiagnostics();
        var diagnostics = generatorDiagnostics
            .Concat(emitDiagnostics)
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        var generatedTrees = updatedCompilation.SyntaxTrees
            .Where(tree => !ReferenceEquals(tree, syntaxTree))
            .ToArray();
        var generatedSources = generatedTrees
            .Select(tree => tree.GetText().ToString())
            .ToArray();

        return Task.FromResult(new SourceGeneratorVerifierResult(generatedTrees, generatedSources, diagnostics));
    }

    private static MetadataReference[] GetMetadataReferences()
    {
        var references = new List<MetadataReference>();
        var trustedPlatformAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        if (!string.IsNullOrWhiteSpace(trustedPlatformAssemblies))
        {
            references.AddRange(trustedPlatformAssemblies
                .Split(Path.PathSeparator)
                .Select(path => MetadataReference.CreateFromFile(path)));
        }

        AddAssemblyReference(references, typeof(MiKiNuo.Mvi.Abstractions.IMviIntent).Assembly);
        AddAssemblyReference(references, typeof(MiKiNuo.Mvi.Core.Dispatching.IIntentDispatcher<,,>).Assembly);

        return references
            .GroupBy(reference => reference.Display)
            .Select(group => group.First())
            .ToArray();
    }

    private static void AddAssemblyReference(List<MetadataReference> references, System.Reflection.Assembly assembly)
    {
        if (!string.IsNullOrWhiteSpace(assembly.Location))
        {
            references.Add(MetadataReference.CreateFromFile(assembly.Location));
        }
    }
}
