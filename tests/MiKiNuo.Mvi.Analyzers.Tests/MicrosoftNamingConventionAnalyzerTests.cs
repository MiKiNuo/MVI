using MiKiNuo.Mvi.Analyzers;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Analyzers.Tests;

public sealed class MicrosoftNamingConventionAnalyzerTests
{
    [Test]
    public async Task Reports_MNK0012_for_class_name_that_is_not_pascal_case()
    {
        const string source = """
            namespace Fixture;

            public class invalidClassName
            {
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new MicrosoftNamingConventionAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.MicrosoftNaming);
    }

    [Test]
    public async Task Does_not_report_for_pascal_case_class_name()
    {
        const string source = """
            namespace Fixture;

            public class ValidClassName
            {
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new MicrosoftNamingConventionAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task Reports_MNK0012_for_interface_name_without_required_prefix()
    {
        const string source = """
            namespace Fixture;

            public interface UserService
            {
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new MicrosoftNamingConventionAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.MicrosoftNaming);
    }

    [Test]
    public async Task Reports_MNK0012_for_interface_name_with_lowercase_name_after_prefix()
    {
        const string source = """
            namespace Fixture;

            public interface IuserService
            {
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new MicrosoftNamingConventionAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.MicrosoftNaming);
    }

    [Test]
    public async Task Does_not_report_for_interface_name_with_required_prefix_and_pascal_case_name()
    {
        const string source = """
            namespace Fixture;

            public interface IUserService
            {
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new MicrosoftNamingConventionAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task Reports_MNK0012_for_property_name_that_is_not_pascal_case()
    {
        const string source = """
            namespace Fixture;

            public class PropertyFixture
            {
                public int currentCount { get; set; }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new MicrosoftNamingConventionAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.MicrosoftNaming);
    }

    [Test]
    public async Task Does_not_report_for_pascal_case_property_name()
    {
        const string source = """
            namespace Fixture;

            public class PropertyFixture
            {
                public int CurrentCount { get; set; }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new MicrosoftNamingConventionAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task Reports_MNK0012_for_method_name_that_is_not_pascal_case()
    {
        const string source = """
            namespace Fixture;

            public class MethodFixture
            {
                public void execute()
                {
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new MicrosoftNamingConventionAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.MicrosoftNaming);
    }

    [Test]
    public async Task Does_not_report_for_pascal_case_method_name()
    {
        const string source = """
            namespace Fixture;

            public class MethodFixture
            {
                public void Execute()
                {
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new MicrosoftNamingConventionAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task Reports_MNK0012_for_field_name_that_is_not_camel_case()
    {
        const string source = """
            namespace Fixture;

            public class FieldFixture
            {
                private int CurrentCount;
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new MicrosoftNamingConventionAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.MicrosoftNaming);
    }

    [Test]
    public async Task Reports_MNK0012_for_underscore_field_name_with_uppercase_first_word()
    {
        const string source = """
            namespace Fixture;

            public class FieldFixture
            {
                private int _CurrentCount;
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new MicrosoftNamingConventionAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.MicrosoftNaming);
    }

    [Test]
    public async Task Does_not_report_for_camel_case_field_name()
    {
        const string source = """
            namespace Fixture;

            public class FieldFixture
            {
                private int currentCount;
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new MicrosoftNamingConventionAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task Does_not_report_for_underscore_camel_case_field_name()
    {
        const string source = """
            namespace Fixture;

            public class FieldFixture
            {
                private int _currentCount;
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new MicrosoftNamingConventionAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task Reports_MNK0012_for_constant_name_that_is_not_pascal_case()
    {
        const string source = """
            namespace Fixture;

            public class ConstantFixture
            {
                public const int maxCount = 10;
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new MicrosoftNamingConventionAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.MicrosoftNaming);
    }

    [Test]
    public async Task Does_not_report_for_pascal_case_constant_name()
    {
        const string source = """
            namespace Fixture;

            public class ConstantFixture
            {
                public const int MaxCount = 10;
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new MicrosoftNamingConventionAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task Does_not_report_for_full_valid_naming_matrix()
    {
        const string source = """
            namespace Fixture;

            public interface IUserService
            {
                int CurrentCount { get; }

                void Execute();
            }

            public class UserService : IUserService
            {
                private int currentCount;

                private int _cachedCount;

                public const int MaxCount = 10;

                public int CurrentCount { get; }

                public void Execute()
                {
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new MicrosoftNamingConventionAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task Does_not_report_for_constructors_accessors_or_out_of_scope_symbols()
    {
        const string source = """
            namespace Fixture;

            public struct invalidStructName
            {
            }

            public delegate void invalidDelegateName();

            public enum invalidEnumName
            {
                invalidValue
            }

            public class EventFixture
            {
                public EventFixture()
                {
                }

                public int CurrentCount { get; set; }

                public event System.EventHandler? invalidEventName;
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new MicrosoftNamingConventionAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }
}
