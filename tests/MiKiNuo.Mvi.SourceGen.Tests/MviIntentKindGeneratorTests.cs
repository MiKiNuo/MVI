using MiKiNuo.Mvi.SourceGen.Intent;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.SourceGen.Tests;

public sealed class MviIntentKindGeneratorTests
{
    [Test]
    public async Task Generator_emits_kind_property_for_annotated_partial_intent()
    {
        const string source = """
            using MiKiNuo.Mvi.Abstractions;
            using MiKiNuo.Mvi.Abstractions.Generation;

            namespace Demo;

            [MviIntentUnion]
            public abstract partial record LoginIntent : IMviIntent;

            [MviIntentKind(7)]
            public sealed partial record SubmitLoginIntent : LoginIntent;
            """;

        var result = await SourceGeneratorVerifier.VerifyAsync(
            source,
            new MviIntentKindGenerator());

        await Assert.That(result.Diagnostics).IsEmpty();
        await Assert.That(result.GeneratedTrees.Select(tree => tree.FilePath)).Contains(filePath => filePath.EndsWith("SubmitLoginIntent.Kind.g.cs", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("public override int Kind => 7;", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("Auto：由 MiKiNuo.Mvi.SourceGen 自动生成", StringComparison.Ordinal));
    }
}
