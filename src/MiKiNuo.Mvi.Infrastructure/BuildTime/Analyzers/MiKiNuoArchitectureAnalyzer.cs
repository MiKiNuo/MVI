using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.Analyzers;

/// <summary>
/// 表示整洁架构分层引用分析器。
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MiKiNuoArchitectureAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor DomainReferenceRule = new(
        id: "ARCH0001",
        title: "Domain 层禁止引用外层项目",
        messageFormat: "Domain 层项目“{0}”不能引用外层项目“{1}”。",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Domain 层只能包含领域概念、基础抽象和纯规则，不能引用 Application、Infrastructure、Presentation 或 sample.");

    private static readonly DiagnosticDescriptor ApplicationReferenceRule = new(
        id: "ARCH0002",
        title: "Application 层禁止引用基础设施和表现层",
        messageFormat: "Application 层项目“{0}”不能引用外层项目“{1}”。",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Application 层只能依赖 Domain，不能依赖 Infrastructure、Presentation 或 sample.");

    private static readonly DiagnosticDescriptor InfrastructureReferenceRule = new(
        id: "ARCH0003",
        title: "Infrastructure 层禁止引用 Presentation 层",
        messageFormat: "Infrastructure 层项目“{0}”不能引用 Presentation 层项目“{1}”。",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Infrastructure 层负责外部实现和编译期工具，不能依赖 UI 表现层.");

    private static readonly DiagnosticDescriptor SourceReferenceSampleRule = new(
        id: "ARCH0004",
        title: "src 项目禁止引用 sample 项目",
        messageFormat: "框架源码项目“{0}”不能引用示例项目“{1}”。",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "示例项目只能依赖框架源码，框架源码不能反向依赖示例项目.");

    private static readonly DiagnosticDescriptor SampleReferenceRule = new(
        id: "ARCH0005",
        title: "sample 项目禁止被 src 反向引用",
        messageFormat: "项目“{0}”发现了对 sample 项目“{1}”的不合法依赖。",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "sample 是最外层示例，不能被 src 中任何框架项目引用.");

    private static readonly DiagnosticDescriptor TestReferenceRule = new(
        id: "ARCH0006",
        title: "test 项目禁止被业务或框架项目引用",
        messageFormat: "非测试项目“{0}”不能引用测试项目“{1}”。",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "test 只能依赖 src 和 sample，不能被 src 或 sample 反向依赖.");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            DomainReferenceRule,
            ApplicationReferenceRule,
            InfrastructureReferenceRule,
            SourceReferenceSampleRule,
            SampleReferenceRule,
            TestReferenceRule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        string projectName = context.Compilation.AssemblyName ?? string.Empty;

        foreach (AssemblyIdentity reference in context.Compilation.ReferencedAssemblyNames)
        {
            string referenceName = reference.Name;
            AnalyzeReference(context, projectName, referenceName);
        }
    }

    private static void AnalyzeReference(
        CompilationAnalysisContext context,
        string projectName,
        string referenceName)
    {
        if (!IsMiKiNuoProject(projectName) || !IsMiKiNuoProject(referenceName))
        {
            return;
        }

        if (IsDomain(projectName) && IsOuterThanDomain(referenceName))
        {
            Report(context, DomainReferenceRule, projectName, referenceName);
            return;
        }

        if (IsApplication(projectName) && IsOuterThanApplication(referenceName))
        {
            Report(context, ApplicationReferenceRule, projectName, referenceName);
            return;
        }

        if (IsInfrastructure(projectName) && IsPresentation(referenceName))
        {
            Report(context, InfrastructureReferenceRule, projectName, referenceName);
            return;
        }

        if (IsSourceProject(projectName) && IsSample(referenceName))
        {
            Report(context, SourceReferenceSampleRule, projectName, referenceName);
            Report(context, SampleReferenceRule, projectName, referenceName);
            return;
        }

        if (!IsTest(projectName) && IsTest(referenceName))
        {
            Report(context, TestReferenceRule, projectName, referenceName);
        }
    }

    private static void Report(
        CompilationAnalysisContext context,
        DiagnosticDescriptor descriptor,
        string projectName,
        string referenceName)
    {
        Diagnostic diagnostic = Diagnostic.Create(descriptor, Location.None, projectName, referenceName);
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsMiKiNuoProject(string projectName)
    {
        return projectName.StartsWith("MiKiNuo.", StringComparison.Ordinal);
    }

    private static bool IsDomain(string projectName)
    {
        return projectName.Equals("MiKiNuo.Mvi.Domain", StringComparison.Ordinal);
    }

    private static bool IsApplication(string projectName)
    {
        return projectName.Equals("MiKiNuo.Mvi.Application", StringComparison.Ordinal);
    }

    private static bool IsInfrastructure(string projectName)
    {
        return projectName.Equals("MiKiNuo.Mvi.Infrastructure", StringComparison.Ordinal);
    }

    private static bool IsPresentation(string projectName)
    {
        return projectName.Equals("MiKiNuo.Mvi.Presentation", StringComparison.Ordinal);
    }

    private static bool IsSample(string projectName)
    {
        return projectName.IndexOf(".Samples.", StringComparison.Ordinal) >= 0;
    }

    private static bool IsTest(string projectName)
    {
        return projectName.EndsWith(".Tests", StringComparison.Ordinal);
    }

    private static bool IsSourceProject(string projectName)
    {
        return IsDomain(projectName)
            || IsApplication(projectName)
            || IsInfrastructure(projectName)
            || IsPresentation(projectName);
    }

    private static bool IsOuterThanDomain(string referenceName)
    {
        return IsApplication(referenceName)
            || IsInfrastructure(referenceName)
            || IsPresentation(referenceName)
            || IsSample(referenceName);
    }

    private static bool IsOuterThanApplication(string referenceName)
    {
        return IsInfrastructure(referenceName)
            || IsPresentation(referenceName)
            || IsSample(referenceName);
    }
}
