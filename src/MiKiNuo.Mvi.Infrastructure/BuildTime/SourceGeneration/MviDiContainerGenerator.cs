using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;

/// <summary>
/// 表示泛型编译期 DI 容器源生成器。
/// 通过扫描 [DiService] 特性和 MviFeatureModule 特性，
/// 为任意项目生成 DI 容器注册代码，不再硬编码特定程序集。
/// 分析阶段由嵌套类型 <see cref="Analysis"/> 负责（DiService 发现在 MviDiContainerGenerator.Analysis.cs，
/// Feature Store 发现在 MviDiContainerGenerator.FeatureStoreAnalysis.cs），
/// 发射阶段由嵌套类型 <see cref="Emission"/> 负责（容器主体在 MviDiContainerGenerator.Emission.cs，
/// Feature Store 工厂在 MviDiContainerGenerator.FeatureStoreEmission.cs），
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
        context.RegisterSourceOutput(context.CompilationProvider, Execute);
    }

    private static void Execute(SourceProductionContext context, Compilation compilation)
    {
        if (!Analysis.HasDiServices(compilation, context.CancellationToken))
        {
            return;
        }

        List<Models.DiServiceInfo> services = Analysis.Discover(compilation, context.CancellationToken);
        List<Models.FeatureStoreInfo> features = Analysis.DiscoverFeatures(compilation, context);

        string source = Emission.GenerateContainerSource(
            compilation.AssemblyName ?? string.Empty,
            services,
            features);
        context.AddSource("GeneratedMviContainer.g.cs", SourceText.From(source, Encoding.UTF8));
    }
}
