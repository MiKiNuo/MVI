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

        IncrementalValuesProvider<INamedTypeSymbol> featureReducers = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "MiKiNuo.Mvi.Domain.DI.MviFeatureAttribute",
                static (node, _) => node is ClassDeclarationSyntax,
                static (syntaxContext, _) => (INamedTypeSymbol)syntaxContext.TargetSymbol);

        IncrementalValueProvider<(
            (System.Collections.Immutable.ImmutableArray<Models.DiServiceInfo>, System.Collections.Immutable.ImmutableArray<INamedTypeSymbol>) Left,
            Compilation Right)> combined =
            services.Collect()
                .Combine(featureReducers.Collect())
                .Combine(context.CompilationProvider);

        context.RegisterSourceOutput(combined, static (productionContext, payload) =>
        {
            System.Collections.Immutable.ImmutableArray<Models.DiServiceInfo> discoveredServices = payload.Left.Item1;
            System.Collections.Immutable.ImmutableArray<INamedTypeSymbol> reducerSymbols = payload.Left.Item2;
            Compilation compilation = payload.Right;

            if (discoveredServices.IsDefaultOrEmpty && reducerSymbols.IsDefaultOrEmpty)
            {
                return;
            }

            IReadOnlyList<Models.MviFeatureInfo> features = FeatureAnalysis.CollectFeatures(
                reducerSymbols,
                compilation,
                productionContext);

            string assemblyName = discoveredServices.IsDefaultOrEmpty
                ? compilation.AssemblyName ?? "GeneratedMviAssembly"
                : discoveredServices[0].AssemblyName;

            string source = Emission.GenerateContainerSource(
                assemblyName,
                discoveredServices.IsDefaultOrEmpty ? System.Array.Empty<Models.DiServiceInfo>() : discoveredServices,
                features);
            productionContext.AddSource(
                "GeneratedMviContainer.g.cs",
                SourceText.From(source, Encoding.UTF8));
        });
    }
}
