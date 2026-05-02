using MiKiNuo.Mvi.Analyzers;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Analyzers.Tests;

public sealed class CleanArchitectureReferenceAnalyzerTests
{
    [Test]
    public async Task Reports_MVI0001_when_domain_references_infrastructure()
    {
        const string source = """
            namespace Contoso.Domain
            {
                public sealed class Order
                {
                    private Contoso.Infrastructure.SqlOrderRepository? repository;
                }
            }

            namespace Contoso.Infrastructure
            {
                public sealed class SqlOrderRepository
                {
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new CleanArchitectureReferenceAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.CleanArchitecture);
    }

    [Test]
    public async Task Reports_MVI0001_when_domain_references_di_runtime()
    {
        const string source = """
            namespace Contoso.Domain
            {
                public sealed class Order
                {
                    private MiKiNuo.Mvi.DI.GeneratedMviScope? scope;
                }
            }

            namespace MiKiNuo.Mvi.DI
            {
                public sealed class GeneratedMviScope
                {
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new CleanArchitectureReferenceAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.CleanArchitecture);
    }

    [Test]
    public async Task Does_not_report_for_domain_only_references()
    {
        const string source = """
            namespace Contoso.Domain;

            public sealed class Order
            {
                private OrderId id;
            }

            public readonly record struct OrderId(int Value);
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new CleanArchitectureReferenceAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task Reports_MVI0002_when_domain_references_platform_project_type()
    {
        const string source = """
            namespace Contoso.Domain
            {
                public sealed class Order
                {
                    private MiKiNuo.Mvi.Platforms.Avalonia.Controls.MviSlotHost? slotHost;
                }
            }

            namespace MiKiNuo.Mvi.Platforms.Avalonia.Controls
            {
                public sealed class MviSlotHost
                {
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new CleanArchitectureReferenceAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.PlatformUiReference);
    }

    [Test]
    public async Task Reports_MVI0002_when_application_references_external_avalonia_type()
    {
        const string source = """
            namespace Contoso.Application
            {
                public sealed class LoginUseCase
                {
                    private Avalonia.Controls.Control? control;
                }
            }

            namespace Avalonia.Controls
            {
                public class Control
                {
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new CleanArchitectureReferenceAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.PlatformUiReference);
    }

    [Test]
    public async Task Reports_MVI0001_when_application_references_infrastructure_implementation()
    {
        const string source = """
            namespace Contoso.Application
            {
                public sealed class LoginUseCase
                {
                    private Contoso.Infrastructure.SqlLoginGateway? gateway;
                }
            }

            namespace Contoso.Infrastructure
            {
                public sealed class SqlLoginGateway
                {
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new CleanArchitectureReferenceAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.CleanArchitecture);
    }

    [Test]
    public async Task Reports_MVI0001_when_core_references_platform_ui()
    {
        const string source = """
            namespace MiKiNuo.Mvi.Core.Binding
            {
                public sealed class BindingAdapter
                {
                    private Avalonia.Controls.Control? control;
                }
            }

            namespace Avalonia.Controls
            {
                public class Control
                {
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new CleanArchitectureReferenceAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.CleanArchitecture);
    }

    [Test]
    public async Task Reports_MVI0001_when_presentation_references_platform_ui()
    {
        const string source = """
            namespace Contoso.Presentation
            {
                public sealed class LoginViewModel
                {
                    private Avalonia.Controls.Control? control;
                }
            }

            namespace Avalonia.Controls
            {
                public class Control
                {
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new CleanArchitectureReferenceAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.CleanArchitecture);
    }

    [Test]
    public async Task Reports_MVI0001_when_presentation_references_infrastructure()
    {
        const string source = """
            namespace Contoso.Presentation
            {
                public sealed class LoginViewModel
                {
                    private Contoso.Infrastructure.SqlLoginGateway? gateway;
                }
            }

            namespace Contoso.Infrastructure
            {
                public sealed class SqlLoginGateway
                {
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new CleanArchitectureReferenceAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.CleanArchitecture);
    }

    [Test]
    public async Task Reports_MVI0001_when_platform_references_business_infrastructure()
    {
        const string source = """
            namespace MiKiNuo.Mvi.Platforms.Avalonia.Views
            {
                public sealed class LoginView
                {
                    private Contoso.Infrastructure.SqlLoginGateway? gateway;
                }
            }

            namespace Contoso.Infrastructure
            {
                public sealed class SqlLoginGateway
                {
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new CleanArchitectureReferenceAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.CleanArchitecture);
    }

    [Test]
    public async Task Reports_MVI0001_when_sourcegen_references_business_runtime_implementation()
    {
        const string source = """
            namespace MiKiNuo.Mvi.SourceGen.Mvi
            {
                public sealed class GeneratorModel
                {
                    private MiKiNuo.Mvi.Core.Store.MviStore? store;
                }
            }

            namespace MiKiNuo.Mvi.Core.Store
            {
                public sealed class MviStore
                {
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new CleanArchitectureReferenceAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.CleanArchitecture);
    }

    [Test]
    public async Task Does_not_report_for_v1_1_allowed_layer_dependencies()
    {
        const string source = """
            namespace Contoso.Domain
            {
                public sealed class Order
                {
                }
            }

            namespace MiKiNuo.Mvi.Abstractions
            {
                public interface IMviIntent
                {
                }
            }

            namespace MiKiNuo.Mvi.Core.Store
            {
                public sealed class MviStore
                {
                }
            }

            namespace MiKiNuo.Mvi.DI
            {
                public sealed class GeneratedMviScope
                {
                    private MiKiNuo.Mvi.Abstractions.IMviIntent? intent;
                }
            }

            namespace Contoso.Application
            {
                public sealed class LoginUseCase
                {
                    private Contoso.Domain.Order? order;
                    private MiKiNuo.Mvi.Abstractions.IMviIntent? intent;
                }
            }

            namespace Contoso.Presentation
            {
                public sealed class LoginViewModel
                {
                    private Contoso.Application.LoginUseCase? useCase;
                    private MiKiNuo.Mvi.Core.Store.MviStore? store;
                    private MiKiNuo.Mvi.Abstractions.IMviIntent? intent;
                }
            }

            namespace Contoso.Infrastructure
            {
                public sealed class SqlLoginGateway
                {
                    private Contoso.Application.LoginUseCase? useCase;
                }
            }

            namespace MiKiNuo.Mvi.Platforms.Avalonia.Views
            {
                public sealed class LoginView
                {
                    private Contoso.Presentation.LoginViewModel? viewModel;
                    private MiKiNuo.Mvi.Core.Store.MviStore? store;
                }
            }

            namespace MiKiNuo.Mvi.SourceGen.Mvi
            {
                public sealed class GeneratorModel
                {
                    private MiKiNuo.Mvi.Abstractions.IMviIntent? intent;
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new CleanArchitectureReferenceAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task Does_not_report_for_unused_using_directives()
    {
        const string source = """
            using Contoso.Infrastructure;
            using MiKiNuo.Mvi.Platforms.Avalonia.Controls;

            namespace Contoso.Domain
            {
                public sealed class Order
                {
                }
            }

            namespace Contoso.Infrastructure
            {
                public sealed class SqlOrderRepository
                {
                }
            }

            namespace MiKiNuo.Mvi.Platforms.Avalonia.Controls
            {
                public sealed class MviSlotHost
                {
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new CleanArchitectureReferenceAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task Reports_MVI0001_when_domain_property_references_infrastructure()
    {
        const string source = """
            namespace Contoso.Domain
            {
                public sealed class Order
                {
                    public Contoso.Infrastructure.SqlOrderRepository? Repository { get; }
                }
            }

            namespace Contoso.Infrastructure
            {
                public sealed class SqlOrderRepository
                {
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new CleanArchitectureReferenceAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.CleanArchitecture);
    }

    [Test]
    public async Task Reports_MVI0002_when_application_method_parameter_references_platform_ui()
    {
        const string source = """
            namespace Contoso.Application
            {
                public sealed class LoginUseCase
                {
                    public void Execute(Avalonia.Controls.Control control)
                    {
                    }
                }
            }

            namespace Avalonia.Controls
            {
                public class Control
                {
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new CleanArchitectureReferenceAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.PlatformUiReference);
    }

    [Test]
    public async Task Reports_MVI0001_when_domain_extends_infrastructure_base_type()
    {
        const string source = """
            namespace Contoso.Domain
            {
                public sealed class Order : Contoso.Infrastructure.SqlEntity
                {
                }
            }

            namespace Contoso.Infrastructure
            {
                public class SqlEntity
                {
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new CleanArchitectureReferenceAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.CleanArchitecture);
    }

    [Test]
    public async Task Reports_MVI0001_when_presentation_implements_infrastructure_interface()
    {
        const string source = """
            namespace Contoso.Presentation
            {
                public sealed class LoginViewModel : Contoso.Infrastructure.ISqlBackedViewModel
                {
                }
            }

            namespace Contoso.Infrastructure
            {
                public interface ISqlBackedViewModel
                {
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new CleanArchitectureReferenceAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.CleanArchitecture);
    }
}
