using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using MiKiNuo.Mvi.Analyzers;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Analyzers.Tests;

public sealed class AnalyzerRuleCatalogTests
{
    [Test]
    public async Task Diagnostic_ids_are_stable_for_m001_catalog()
    {
        var actualIds = string.Join(",", new[]
        {
            DiagnosticIds.ChineseXmlDocumentation,
            DiagnosticIds.MicrosoftNaming,
            DiagnosticIds.CleanArchitecture,
            DiagnosticIds.PlatformUiReference,
        });

        await Assert.That(actualIds).IsEqualTo("MNK0006,MNK0012,MVI0001,MVI0002");
    }

    [Test]
    public async Task Diagnostic_descriptors_use_expected_error_metadata()
    {
        var descriptorSummary = string.Join("\n", new[]
        {
            Describe(DiagnosticDescriptors.ChineseXmlDocumentation),
            Describe(DiagnosticDescriptors.MicrosoftNaming),
            Describe(DiagnosticDescriptors.CleanArchitecture),
            Describe(DiagnosticDescriptors.PlatformUiReference),
        });

        const string expectedSummary = """
            MNK0006|Documentation|Error|True
            MNK0012|Naming|Error|True
            MVI0001|Architecture|Error|True
            MVI0002|Architecture|Error|True
            """;

        await Assert.That(descriptorSummary).IsEqualTo(expectedSummary);
    }

    [Test]
    public async Task Implemented_analyzers_expose_expected_supported_diagnostics()
    {
        var analyzerSummary = string.Join("\n", new[]
        {
            DescribeSupportedDiagnostics(new ChineseXmlDocumentationAnalyzer()),
            DescribeSupportedDiagnostics(new MicrosoftNamingConventionAnalyzer()),
            DescribeSupportedDiagnostics(new CleanArchitectureReferenceAnalyzer()),
        });

        const string expectedSummary = """
            ChineseXmlDocumentationAnalyzer:MNK0006
            MicrosoftNamingConventionAnalyzer:MNK0012
            CleanArchitectureReferenceAnalyzer:MVI0001,MVI0002
            """;

        await Assert.That(analyzerSummary).IsEqualTo(expectedSummary);
    }

    private static string Describe(DiagnosticDescriptor descriptor)
    {
        return string.Join(
            "|",
            descriptor.Id,
            descriptor.Category,
            descriptor.DefaultSeverity,
            descriptor.IsEnabledByDefault);
    }

    private static string DescribeSupportedDiagnostics(DiagnosticAnalyzer analyzer)
    {
        return $"{analyzer.GetType().Name}:{string.Join(",", analyzer.SupportedDiagnostics.Select(diagnostic => diagnostic.Id))}";
    }
}
