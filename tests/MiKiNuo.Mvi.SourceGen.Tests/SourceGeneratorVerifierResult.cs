using Microsoft.CodeAnalysis;

namespace MiKiNuo.Mvi.SourceGen.Tests;

public sealed record SourceGeneratorVerifierResult(
    IReadOnlyList<SyntaxTree> GeneratedTrees,
    IReadOnlyList<string> GeneratedSources,
    IReadOnlyList<Diagnostic> Diagnostics);
