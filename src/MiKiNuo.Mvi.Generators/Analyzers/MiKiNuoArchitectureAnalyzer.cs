using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using MiKiNuo.Mvi.Generators.Diagnostics;

namespace MiKiNuo.Mvi.Generators.Analyzers;

/// <summary>
/// 表示整洁架构分层引用分析器。
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MiKiNuoArchitectureAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor AbstractionsReferenceRule = new(
        id: DiagnosticIdCatalog.ArchDomainReference,
        title: "Abstractions 层禁止引用外层项目",
        messageFormat: "Abstractions 层项目“{0}”不能引用外层项目“{1}”。",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Abstractions 层只能包含领域概念、基础抽象和纯规则，不能引用 Runtime、Generators、Binding 或 sample.");

    private static readonly DiagnosticDescriptor RuntimeReferenceRule = new(
        id: DiagnosticIdCatalog.ArchApplicationReference,
        title: "Runtime 层禁止引用基础设施和表现层",
        messageFormat: "Runtime 层项目“{0}”不能引用外层项目“{1}”。",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Runtime 层只能依赖 Abstractions，不能依赖 Generators、Binding 或 sample.");

    private static readonly DiagnosticDescriptor GeneratorsReferenceRule = new(
        id: DiagnosticIdCatalog.ArchInfrastructureReference,
        title: "Generators 层禁止引用 Binding 层",
        messageFormat: "Generators 层项目“{0}”不能引用 Binding 层项目“{1}”。",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Generators 层负责外部实现和编译期工具，不能依赖 UI 表现层.");

    private static readonly DiagnosticDescriptor SourceReferenceSampleRule = new(
        id: DiagnosticIdCatalog.ArchSourceReferenceSample,
        title: "src 项目禁止引用 sample 项目",
        messageFormat: "框架源码项目“{0}”不能引用示例项目“{1}”。",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "示例项目只能依赖框架源码，框架源码不能反向依赖示例项目.");

    private static readonly DiagnosticDescriptor SampleReferenceRule = new(
        id: DiagnosticIdCatalog.ArchSampleReference,
        title: "sample 项目禁止被 src 反向引用",
        messageFormat: "项目“{0}”发现了对 sample 项目“{1}”的不合法依赖。",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "sample 是最外层示例，不能被 src 中任何框架项目引用.");

    private static readonly DiagnosticDescriptor TestReferenceRule = new(
        id: DiagnosticIdCatalog.ArchTestReference,
        title: "test 项目禁止被业务或框架项目引用",
        messageFormat: "非测试项目“{0}”不能引用测试项目“{1}”。",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "test 只能依赖 src 和 sample，不能被 src 或 sample 反向依赖.");

    private static readonly DiagnosticDescriptor BindingReferencePlatformRule = new(
        id: DiagnosticIdCatalog.ArchPresentationReferencePlatform,
        title: "Binding 抽象层禁止引用具体平台项目",
        messageFormat: "Binding 抽象层项目“{0}”不能引用具体平台项目“{1}”。",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Binding 层只能定义平台无关抽象，Avalonia、WinForms、Godot、Unity 等实现必须放在独立平台项目中.");

    private static readonly DiagnosticDescriptor BindingPackageIsolationRule = new(
        id: DiagnosticIdCatalog.ArchPresentationPackageIsolation,
        title: "Binding 抽象层禁止引用具体平台 NuGet 包",
        messageFormat: "Binding 抽象层项目“{0}”不能直接引用具体平台 NuGet 包“{1}”。",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Binding 层应保持平台无关，编译期不得通过 PackageReference 引入 Avalonia / Godot 等具体平台包.");

    /// <summary>
    /// 获取支持的诊断描述集合。
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            AbstractionsReferenceRule,
            RuntimeReferenceRule,
            GeneratorsReferenceRule,
            SourceReferenceSampleRule,
            SampleReferenceRule,
            TestReferenceRule,
            BindingReferencePlatformRule,
            BindingPackageIsolationRule);

    /// <summary>
    /// 初始化分析器注册诊断动作。
    /// </summary>
    /// <param name="context">分析上下文。</param>
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
        if (!IsMiKiNuoProject(projectName))
        {
            return;
        }

        // 平台包隔离规则检查外部 NuGet 包引用，
        // 不要求引用本身是 MiKiNuo 项目。
        if (IsBinding(projectName) && IsConcretePlatformPackage(referenceName))
        {
            Report(context, BindingPackageIsolationRule, projectName, referenceName);
            return;
        }

        // 以下规则只针对 MiKiNuo 项目间的引用。
        if (!IsMiKiNuoProject(referenceName))
        {
            return;
        }

        if (IsAbstractions(projectName) && IsOuterThanAbstractions(referenceName))
        {
            Report(context, AbstractionsReferenceRule, projectName, referenceName);
            return;
        }

        if (IsRuntime(projectName) && IsOuterThanRuntime(referenceName))
        {
            Report(context, RuntimeReferenceRule, projectName, referenceName);
            return;
        }

        if (IsGenerators(projectName) && (IsBinding(referenceName) || IsPlatform(referenceName)))
        {
            Report(context, GeneratorsReferenceRule, projectName, referenceName);
            return;
        }

        if (IsBinding(projectName) && IsPlatform(referenceName))
        {
            Report(context, BindingReferencePlatformRule, projectName, referenceName);
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

    private static bool IsAbstractions(string projectName)
    {
        return projectName.Equals("MiKiNuo.Mvi.Abstractions", StringComparison.Ordinal);
    }

    private static bool IsRuntime(string projectName)
    {
        return projectName.Equals("MiKiNuo.Mvi.Runtime", StringComparison.Ordinal);
    }

    private static bool IsGenerators(string projectName)
    {
        return projectName.Equals("MiKiNuo.Mvi.Generators", StringComparison.Ordinal);
    }

    private static bool IsBinding(string projectName)
    {
        return projectName.Equals("MiKiNuo.Mvi.Binding", StringComparison.Ordinal);
    }

    private static bool IsPlatform(string projectName)
    {
        return projectName.StartsWith("MiKiNuo.Mvi.Platforms.", StringComparison.Ordinal);
    }

    /// <summary>
    /// 判断程序集名是否属于具体 UI 平台的 NuGet 包（目前覆盖 Avalonia / Godot 家族）。
    /// 该判定只关心 Binding 抽象层不能直接通过 PackageReference 引入的平台包，
    /// 与 <see cref="IsPlatform"/> 的"MiKiNuo 自家平台项目"互为补集。
    /// </summary>
    private static bool IsConcretePlatformPackage(string assemblyName)
    {
        return assemblyName.StartsWith("Avalonia", StringComparison.Ordinal)
            || assemblyName.StartsWith("Godot", StringComparison.Ordinal);
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
        return IsAbstractions(projectName)
            || IsRuntime(projectName)
            || IsGenerators(projectName)
            || IsBinding(projectName)
            || IsPlatform(projectName);
    }

    private static bool IsOuterThanAbstractions(string referenceName)
    {
        return IsRuntime(referenceName)
            || IsGenerators(referenceName)
            || IsBinding(referenceName)
            || IsPlatform(referenceName)
            || IsSample(referenceName);
    }

    private static bool IsOuterThanRuntime(string referenceName)
    {
        return IsGenerators(referenceName)
            || IsBinding(referenceName)
            || IsPlatform(referenceName)
            || IsSample(referenceName);
    }
}
