using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;

/// <summary>
/// 表示泛型编译期 DI 容器源生成器。
/// 通过扫描 [DiService] 特性和 MviFeatureModule 特性，
/// 为任意项目生成 DI 容器注册代码，不再硬编码特定程序集。
/// 分析阶段由 <see cref="MviDiContainerAnalysis"/> 负责，发射阶段由 <see cref="MviDiContainerEmission"/> 负责，
/// 数据模型由 <see cref="MviDiContainerModels"/> 承载。
/// </summary>
[Generator]
public sealed class MviDiContainerGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, Execute);
    }

    private static void Execute(SourceProductionContext context, Compilation compilation)
    {
        if (!MviDiContainerAnalysis.HasDiServices(compilation, context.CancellationToken))
        {
            return;
        }

        System.Collections.Generic.List<MviDiContainerModels.DiServiceInfo> services =
            MviDiContainerAnalysis.Discover(compilation, context.CancellationToken);

        string source = MviDiContainerEmission.GenerateContainerSource(
            compilation.AssemblyName ?? string.Empty,
            services);
        context.AddSource("GeneratedMviContainer.g.cs", SourceText.From(source, System.Text.Encoding.UTF8));
    }
}
