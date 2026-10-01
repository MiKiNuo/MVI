using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MiKiNuo.Mvi.Generators;

namespace MiKiNuo.Mvi.V2.Tests;

internal static class GeneratorTestHost
{
    private static readonly ImmutableArray<MetadataReference> References = CreateReferences();

    internal static CSharpCompilation Compilation(params string[] sources)
        => CSharpCompilation.Create("ConsumerDeclarations", sources.Select((source, index) =>
            CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest), "Consumer" + index + ".cs")),
            References, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

    internal static GeneratorDriver Driver()
        => CSharpGeneratorDriver.Create([new FeatureGenerator().AsSourceGenerator()],
            parseOptions: new CSharpParseOptions(LanguageVersion.Latest),
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

    internal static (Compilation Compilation, GeneratorDriverRunResult Result) Run(params string[] sources)
    {
        GeneratorDriver driver = Driver().RunGeneratorsAndUpdateCompilation(Compilation(sources), out Compilation output, out _);
        return (output, driver.GetRunResult());
    }

    private static ImmutableArray<MetadataReference> CreateReferences()
    {
        string paths = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        return paths.Split(Path.PathSeparator).Append(typeof(Feature<>).Assembly.Location).Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path)).ToImmutableArray();
    }
}
