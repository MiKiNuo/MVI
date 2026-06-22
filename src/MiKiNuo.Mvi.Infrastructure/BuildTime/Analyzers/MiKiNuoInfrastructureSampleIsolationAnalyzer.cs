using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using MiKiNuo.Mvi.Infrastructure.BuildTime.Diagnostics;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.Analyzers;

/// <summary>
/// 表示 Infrastructure 源生成器与分析器的示例代码隔离分析器。
/// <para>
/// 框架 Infrastructure 层是 Source Generator / Analyzer 的寄主项目。
/// 当示例专属代码（命名空间、类型名、字符串字面量）反向渗透到 Infrastructure 时，
/// 框架将无法被多个互不相关的示例项目复用。
/// </para>
/// <para>
/// 本分析器仅在 Infrastructure 程序集自身编译时启用，外部引用 Infrastructure 的项目不会触发。
/// 分析器自身、DiagnosticIdCatalog、AnalyzerReleases 文本等豁免点用 <see cref="IsExemptType"/> 跳过，
/// 避免误报基础设施自身。
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MiKiNuoInfrastructureSampleIsolationAnalyzer : DiagnosticAnalyzer
{
    private const string SamplesNamespace = "MiKiNuo.Mvi.Samples.";
    private const string InfrastructureAssemblyName = "MiKiNuo.Mvi.Infrastructure";

    private static readonly DiagnosticDescriptor SampleIsolationRule = new(
        id: DiagnosticIdCatalog.ArchInfrastructureSampleIsolation,
        title: "Infrastructure 禁止出现示例项目专属代码",
        messageFormat: "Infrastructure 源生成器或分析器内出现示例项目专属代码“{0}”，应迁出到 sample 项目。",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "示例项目专属代码必须放在 sample 项目下，框架 src/MiKiNuo.Mvi.Infrastructure 不能反向依赖示例项目.");

    /// <summary>
    /// 获取支持的诊断描述集合。
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(SampleIsolationRule);

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

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationStartAnalysisContext context)
    {
        string assemblyName = context.Compilation.AssemblyName ?? string.Empty;
        if (!string.Equals(assemblyName, InfrastructureAssemblyName, StringComparison.Ordinal))
        {
            return;
        }

        context.RegisterSymbolAction(
            static symbolContext => AnalyzeType((INamedTypeSymbol)symbolContext.Symbol, symbolContext),
            SymbolKind.NamedType);
        context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
    }

    private static void AnalyzeType(INamedTypeSymbol type, SymbolAnalysisContext context)
    {
        if (type is null || IsExemptType(type))
        {
            return;
        }

        if (type.Name.Contains("Sample", StringComparison.Ordinal))
        {
            Location location = type.Locations.Length > 0 ? type.Locations[0] : Location.None;
            context.ReportDiagnostic(Diagnostic.Create(SampleIsolationRule, location, type.Name));
        }
    }

    private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
    {
        SyntaxNode root = context.Tree.GetRoot(context.CancellationToken);
        foreach (LiteralExpressionSyntax literal in root.DescendantNodes().OfType<LiteralExpressionSyntax>())
        {
            if (!literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                continue;
            }

            string text = literal.Token.ValueText;
            if (text.Contains(SamplesNamespace, StringComparison.Ordinal))
            {
                Location location = literal.GetLocation();
                context.ReportDiagnostic(Diagnostic.Create(SampleIsolationRule, location, text));
            }
        }
    }

    /// <summary>
    /// 判断类型是否属于豁免范围：分析器/诊断 ID 目录/AnalyzerReleases 等基础设施自身。
    /// </summary>
    private static bool IsExemptType(INamedTypeSymbol type)
    {
        string fullName = type.ToDisplayString();
        return fullName.StartsWith("MiKiNuo.Mvi.Infrastructure.BuildTime.Diagnostics.", StringComparison.Ordinal)
            || fullName.StartsWith("MiKiNuo.Mvi.Infrastructure.BuildTime.Analyzers.MiKiNuo", StringComparison.Ordinal)
            || type.Name.Contains("InfrastructureSampleIsolation", StringComparison.Ordinal)
            || type.Name == "DiagnosticIdCatalog"
            || type.Name == "MviArchitectureAnalyzerTests"
            || type.Name.Contains("DiagnosticReleaseTracking", StringComparison.Ordinal)
            || type.Name.Contains("ArchitectureDirectoryTests", StringComparison.Ordinal);
    }
}
