using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MiKiNuo.Mvi.Samples.Avalonia.BuildTime.SourceGeneration;

/// <summary>
/// 表示 Avalonia 示例项目专属的 DI 容器与 ViewRegistry 组合源生成器。
/// 该生成器硬编码了示例的特性结构（Login、Shell、Dashboard、ClinicalEditor 等），
/// 仅供 <c>MiKiNuo.Mvi.Samples.Avalonia</c> 项目使用，不属于 MiKiNuo.Mvi 框架层。
/// </summary>
[Generator]
public sealed partial class AvaloniaSampleDiContainerGenerator : IIncrementalGenerator
{
    private static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .WithMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
            | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    /// <summary>
    /// 初始化源生成器。
    /// </summary>
    /// <param name="context">源生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, Execute);
    }

    private static void Execute(SourceProductionContext context, Compilation compilation)
    {
        GenerationModel model = DiscoverModel(compilation, context.CancellationToken);
        if (model.Views.Count == 0)
        {
            return;
        }

        string prefix = GetCompositionPrefix(compilation.AssemblyName);
        string containerName = prefix + "Generated" + "Container";
        string registryName = prefix + "Generated" + "ViewRegistry";

        if (model.LoginFeature is not null && model.ShellFeature is not null)
        {
            string containerSource = GenerateContainerSource(model, containerName);
            context.AddSource(containerName + ".g.cs", SourceText.From(containerSource, Encoding.UTF8));

            // 容器已同时实现 IMviViewRegistry（参见 GenerateContainerSource），
            // 不再单独生成 *GeneratedViewRegistry 类——避免出现两份互不感知的 View 路由表。
            // 为保持源码兼容（部分旧测试可能仍 Resolve<IMviViewRegistry>() 拿到独立对象），
            // 这里继续写一份类型别名壳，方法体全部 forward 到容器。
            string viewRegistrySource = GenerateViewRegistryForwarder(model, containerName, registryName);
            context.AddSource(registryName + ".g.cs", SourceText.From(viewRegistrySource, Encoding.UTF8));
        }
    }

    private static GenerationModel DiscoverModel(
        Compilation compilation,
        System.Threading.CancellationToken cancellationToken)
    {
        List<INamedTypeSymbol> classes = DiscoverClasses(compilation, cancellationToken);
        Dictionary<string, INamedTypeSymbol> classesByFullName = classes
            .GroupBy(static symbol => symbol.ToDisplayString(TypeFormat))
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.Ordinal);
        List<FeatureInfo> features = DiscoverFeatures(classes, classesByFullName);
        List<ViewInfo> views = DiscoverViews(classes);
        ServiceInfo? authService = DiscoverService(classes, "IAuthService", "FakeAuthService");
        INamedTypeSymbol? loginNavigationService = DiscoverInterface(compilation, cancellationToken, "ILoginNavigationService");
        INamedTypeSymbol? dashboardPageFactory = DiscoverInterface(compilation, cancellationToken, "IDashboardPageFactory");
        INamedTypeSymbol? shellPageFactory = DiscoverInterface(compilation, cancellationToken, "IShellPageFactory");
        CardStoreFactoryInfo? cardStoreFactory = DiscoverCardStoreFactory(classes, cancellationToken);
        INamedTypeSymbol? sampleGreetingViewModel = DiscoverSampleGreetingViewModel(classes, cancellationToken);

        return new GenerationModel(
            compilation.AssemblyName ?? "GeneratedMviAssembly",
            features,
            views,
            authService,
            loginNavigationService,
            dashboardPageFactory,
            shellPageFactory,
            cardStoreFactory,
            sampleGreetingViewModel);
    }
}
