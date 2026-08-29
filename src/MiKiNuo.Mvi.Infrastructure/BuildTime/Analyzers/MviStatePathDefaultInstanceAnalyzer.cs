using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using MiKiNuo.Mvi.Infrastructure.BuildTime.Diagnostics;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.Analyzers;

/// <summary>
/// 表示 StatePath 默认实例拦截分析器。
/// <para>
/// <c>StatePath&lt;TState, TValue&gt;</c> 是 readonly struct，
/// 其默认实例（<c>default</c> 字面量、<c>default(StatePath&lt;,&gt;)</c>、无参 <c>new StatePath&lt;,&gt;()</c>）
/// 未初始化取值委托，访问 <c>Getter</c> 时在运行期抛 <see cref="InvalidOperationException"/>。
/// 本分析器把该陷阱提前到编译期。
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MviStatePathDefaultInstanceAnalyzer : DiagnosticAnalyzer
{
    private const string StatePathMetadataName = "MiKiNuo.Mvi.Domain.MVI.State.StatePath`2";

    private static readonly DiagnosticDescriptor DefaultInstanceRule = new(
        id: DiagnosticIdCatalog.MviStatePathDefaultInstance,
        title: "禁止使用 StatePath 默认实例",
        messageFormat: "StatePath 默认实例在访问 Getter 时会抛 InvalidOperationException；请改用源生成器产出的路径实例（如 <状态名>Paths.<属性>）或 StatePath.Create",
        category: "MviStatePath",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "StatePath 是 readonly struct，默认实例未初始化取值委托。应始终使用源生成器产出的路径实例或 StatePath.Create 工厂方法.");

    /// <summary>
    /// 获取支持的诊断描述集合。
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DefaultInstanceRule);

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
        INamedTypeSymbol? statePathType = context.Compilation.GetTypeByMetadataName(StatePathMetadataName);
        if (statePathType is null)
        {
            return;
        }

        context.RegisterOperationAction(
            operationContext => AnalyzeDefaultValue(operationContext, statePathType),
            OperationKind.DefaultValue);
        context.RegisterOperationAction(
            operationContext => AnalyzeObjectCreation(operationContext, statePathType),
            OperationKind.ObjectCreation);
    }

    private static void AnalyzeDefaultValue(
        OperationAnalysisContext context,
        INamedTypeSymbol statePathType)
    {
        IDefaultValueOperation operation = (IDefaultValueOperation)context.Operation;
        if (IsStatePath(operation.Type, statePathType))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(DefaultInstanceRule, operation.Syntax.GetLocation()));
        }
    }

    private static void AnalyzeObjectCreation(
        OperationAnalysisContext context,
        INamedTypeSymbol statePathType)
    {
        IObjectCreationOperation operation = (IObjectCreationOperation)context.Operation;
        if (operation.Arguments.Length == 0 && IsStatePath(operation.Type, statePathType))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(DefaultInstanceRule, operation.Syntax.GetLocation()));
        }
    }

    private static bool IsStatePath(ITypeSymbol? type, INamedTypeSymbol statePathType) =>
        type is INamedTypeSymbol named
        && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, statePathType);
}
