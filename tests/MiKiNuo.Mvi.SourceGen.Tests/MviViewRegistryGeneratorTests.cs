using MiKiNuo.Mvi.SourceGen.Registry;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.SourceGen.Tests;

public sealed class MviViewRegistryGeneratorTests
{
    [Test]
    public async Task Generator_emits_explicit_view_registry_without_reflection_scanning()
    {
        const string source = """
            using MiKiNuo.Mvi.Abstractions.Generation;

            namespace Demo;

            public sealed class LoginView;
            public sealed class LoginViewModel;

            [MviViewRegistry(typeof(LoginView), typeof(LoginViewModel))]
            public sealed partial class AppViewRegistry
            {
            }
            """;

        var result = await SourceGeneratorVerifier.VerifyAsync(
            source,
            new MviViewRegistryGenerator());

        await Assert.That(result.Diagnostics).IsEmpty();
        await Assert.That(result.GeneratedTrees.Select(tree => tree.FilePath)).Contains(filePath => filePath.EndsWith("AppViewRegistry.Views.g.cs", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("typeof(LoginView)", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("typeof(LoginViewModel)", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("TryGetViewModelType", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).DoesNotContain(sourceText =>
            sourceText.Contains("Assembly", StringComparison.Ordinal) ||
            sourceText.Contains("GetTypes", StringComparison.Ordinal) ||
            sourceText.Contains("Activator", StringComparison.Ordinal));
    }
}
