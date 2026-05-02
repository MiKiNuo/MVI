using MiKiNuo.Mvi.Analyzers;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Analyzers.Tests;

public sealed class AnalyzerVerifierTests
{
    [Test]
    public async Task Verifier_runs_analyzer_and_returns_only_analyzer_diagnostics()
    {
        const string source = """
            namespace Fixture;

            internal class InternalClass
            {
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new ChineseXmlDocumentationAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }
}
