using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;

/// <summary>
/// 表示泛型编译期 DI 容器源生成器。
/// 通过扫描 [DiService] 特性，
/// 为任意项目生成 DI 容器注册代码，不再硬编码特定程序集。
/// 分析阶段由嵌套类型 <see cref="Analysis"/> 负责，
/// 发射阶段由嵌套类型 <see cref="Emission"/> 负责，
/// 数据模型由 <see cref="Models"/> 承载（MviDiContainerGenerator.Models.cs）。
/// </summary>
[Generator]
public sealed partial class MviDiContainerGenerator : IIncrementalGenerator
{
    /// <summary>
    /// 初始化源生成器注册编译回调。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<Models.DiServiceInfo> services = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "MiKiNuo.Mvi.Domain.DI.DiServiceAttribute",
                static (node, _) => node is TypeDeclarationSyntax,
                static (syntaxContext, _) => Analysis.ParseDiService((INamedTypeSymbol)syntaxContext.TargetSymbol))
            .Where(static service => service is not null)
            .Select(static (service, _) => service!);

        context.RegisterSourceOutput(services.Collect(), static (productionContext, discoveredServices) =>
        {
            if (discoveredServices.IsDefaultOrEmpty)
            {
                return;
            }

            string source = Emission.GenerateContainerSource(
                discoveredServices[0].AssemblyName,
                discoveredServices);
            productionContext.AddSource(
                "GeneratedMviContainer.g.cs",
                SourceText.From(source, Encoding.UTF8));
        });
    }
}
