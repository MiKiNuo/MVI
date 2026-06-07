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
public sealed class AvaloniaSampleDiContainerGenerator : IIncrementalGenerator
{
    private static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .WithMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
            | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    /// <inheritdoc />
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

        if (model.Views.Count > 0)
        {
            string registrySource = GenerateViewRegistrySource(model, registryName);
            context.AddSource(registryName + ".g.cs", SourceText.From(registrySource, Encoding.UTF8));
        }

        if (model.LoginFeature is not null && model.ShellFeature is not null)
        {
            string containerSource = GenerateContainerSource(model, containerName, registryName);
            context.AddSource(containerName + ".g.cs", SourceText.From(containerSource, Encoding.UTF8));
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
        CardStoreFactoryInfo? cardStoreFactory = DiscoverCardStoreFactory(classes, cancellationToken);

        return new GenerationModel(
            compilation.AssemblyName ?? "GeneratedMviAssembly",
            features,
            views,
            authService,
            loginNavigationService,
            cardStoreFactory);
    }

    private static List<INamedTypeSymbol> DiscoverClasses(
        Compilation compilation,
        System.Threading.CancellationToken cancellationToken)
    {
        List<INamedTypeSymbol> classes = new List<INamedTypeSymbol>();

        foreach (SyntaxTree tree in compilation.SyntaxTrees)
        {
            SyntaxNode root = tree.GetRoot(cancellationToken);
            SemanticModel semanticModel = compilation.GetSemanticModel(tree);

            foreach (ClassDeclarationSyntax classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken) is INamedTypeSymbol symbol
                    && symbol.DeclaredAccessibility == Accessibility.Public)
                {
                    classes.Add(symbol);
                }
            }
        }

        return classes;
    }

    private static List<FeatureInfo> DiscoverFeatures(
        IEnumerable<INamedTypeSymbol> classes,
        IReadOnlyDictionary<string, INamedTypeSymbol> classesByFullName)
    {
        List<FeatureInfo> features = new List<FeatureInfo>();

        foreach (INamedTypeSymbol classSymbol in classes)
        {
            INamedTypeSymbol? viewModelBase = FindGenericBase(classSymbol, "MviViewModelBase", 3);
            if (viewModelBase is null)
            {
                continue;
            }

            string baseName = RemoveSuffix(classSymbol.Name, "ViewModel");
            string containingNamespace = classSymbol.ContainingNamespace.ToDisplayString();
            INamedTypeSymbol stateType = (INamedTypeSymbol)viewModelBase.TypeArguments[0];
            INamedTypeSymbol intentType = (INamedTypeSymbol)viewModelBase.TypeArguments[1];
            INamedTypeSymbol effectType = (INamedTypeSymbol)viewModelBase.TypeArguments[2];

            INamedTypeSymbol? reducerType = FindByMetadataName(
                classesByFullName,
                containingNamespace + "." + baseName + "Reducer");
            INamedTypeSymbol? dispatcherType = FindByMetadataName(
                classesByFullName,
                containingNamespace + "." + baseName + "EffectDispatcher");

            if (reducerType is null || dispatcherType is null)
            {
                continue;
            }

            features.Add(new FeatureInfo(
                baseName,
                classSymbol,
                stateType,
                intentType,
                effectType,
                reducerType,
                dispatcherType,
                GetDispatcherConstructorKind(dispatcherType),
                HasStaticInitial(stateType),
                HasStringCreateInitial(stateType)));
        }

        return features
            .OrderBy(static feature => feature.BaseName, StringComparer.Ordinal)
            .ToList();
    }

    private static List<ViewInfo> DiscoverViews(IEnumerable<INamedTypeSymbol> classes)
    {
        List<ViewInfo> views = new List<ViewInfo>();

        foreach (INamedTypeSymbol classSymbol in classes)
        {
            INamedTypeSymbol? viewBase = FindGenericBase(classSymbol, "MviAvaloniaView", 1);
            if (viewBase is null)
            {
                continue;
            }

            INamedTypeSymbol viewModelType = (INamedTypeSymbol)viewBase.TypeArguments[0];
            bool requiresRegistry = HasSingleViewRegistryConstructor(classSymbol);
            views.Add(new ViewInfo(classSymbol, viewModelType, requiresRegistry));
        }

        return views
            .OrderBy(static view => view.ViewModelType.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static ServiceInfo? DiscoverService(
        IEnumerable<INamedTypeSymbol> classes,
        string serviceName,
        string preferredImplementationName)
    {
        foreach (INamedTypeSymbol implementation in classes)
        {
            if (implementation.Name != preferredImplementationName)
            {
                continue;
            }

            INamedTypeSymbol? serviceType = implementation.AllInterfaces
                .FirstOrDefault(item => item.Name == serviceName);
            if (serviceType is not null)
            {
                return new ServiceInfo(serviceType, implementation);
            }
        }

        return null;
    }

    private static INamedTypeSymbol? DiscoverInterface(
        Compilation compilation,
        System.Threading.CancellationToken cancellationToken,
        string interfaceName)
    {
        foreach (SyntaxTree tree in compilation.SyntaxTrees)
        {
            SyntaxNode root = tree.GetRoot(cancellationToken);
            SemanticModel semanticModel = compilation.GetSemanticModel(tree);

            foreach (InterfaceDeclarationSyntax interfaceDeclaration in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (semanticModel.GetDeclaredSymbol(interfaceDeclaration, cancellationToken) is INamedTypeSymbol symbol
                    && symbol.DeclaredAccessibility == Accessibility.Public
                    && symbol.Name == interfaceName)
                {
                    return symbol;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 发现 <c>CardStoreFactory</c> 类型（按名称匹配），用于将其注入到 <c>BusinessCompositePageViewModel</c> 构造中。
    /// 工厂依赖 <c>IMviMediator</c>，由 <c>SampleGeneratedContainer</c> 通过 <c>this</c> 满足。
    /// </summary>
    private static CardStoreFactoryInfo? DiscoverCardStoreFactory(
        List<INamedTypeSymbol> classes,
        System.Threading.CancellationToken cancellationToken)
    {
        foreach (INamedTypeSymbol classSymbol in classes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (classSymbol.Name != "CardStoreFactory")
            {
                continue;
            }

            IMethodSymbol? constructor = classSymbol.Constructors
                .Where(static ctor => ctor.DeclaredAccessibility == Accessibility.Public)
                .OrderByDescending(static ctor => ctor.Parameters.Length)
                .FirstOrDefault();

            if (constructor is null)
            {
                continue;
            }

            return new CardStoreFactoryInfo(classSymbol, constructor);
        }

        return null;
    }

    private static INamedTypeSymbol? FindByMetadataName(
        IReadOnlyDictionary<string, INamedTypeSymbol> classesByFullName,
        string metadataName)
    {
        string key = "global::" + metadataName;
        return classesByFullName.TryGetValue(key, out INamedTypeSymbol? symbol)
            ? symbol
            : null;
    }

    private static INamedTypeSymbol? FindGenericBase(
        INamedTypeSymbol classSymbol,
        string baseName,
        int arity)
    {
        for (INamedTypeSymbol? current = classSymbol.BaseType; current is not null; current = current.BaseType)
        {
            if (current.Name == baseName && current.TypeArguments.Length == arity)
            {
                return current;
            }
        }

        return null;
    }

    private static bool HasSingleViewRegistryConstructor(INamedTypeSymbol viewType)
    {
        foreach (IMethodSymbol constructor in viewType.Constructors)
        {
            if (constructor.DeclaredAccessibility != Accessibility.Public || constructor.Parameters.Length != 1)
            {
                continue;
            }

            ITypeSymbol parameterType = constructor.Parameters[0].Type;
            if (parameterType.Name == "IMviViewRegistry")
            {
                return true;
            }
        }

        return false;
    }

    private static DispatcherConstructorKind GetDispatcherConstructorKind(INamedTypeSymbol dispatcherType)
    {
        foreach (IMethodSymbol constructor in dispatcherType.Constructors)
        {
            if (constructor.DeclaredAccessibility != Accessibility.Public)
            {
                continue;
            }

            if (constructor.Parameters.Length == 0)
            {
                return DispatcherConstructorKind.Parameterless;
            }

            if (constructor.Parameters.Length == 1 && constructor.Parameters[0].Type.Name == "IMviMediator")
            {
                return DispatcherConstructorKind.Mediator;
            }

            if (constructor.Parameters.Any(static parameter => parameter.Type.Name == "IAuthService")
                && constructor.Parameters.Any(static parameter => parameter.Type.Name == "ILoginNavigationService"))
            {
                return DispatcherConstructorKind.Login;
            }
        }

        return DispatcherConstructorKind.Unsupported;
    }

    private static bool HasStaticInitial(INamedTypeSymbol stateType)
    {
        return stateType.GetMembers("Initial")
            .OfType<IPropertySymbol>()
            .Any(member => member.IsStatic && SymbolEqualityComparer.Default.Equals(member.Type, stateType));
    }

    private static bool HasStringCreateInitial(INamedTypeSymbol stateType)
    {
        return stateType.GetMembers("CreateInitial")
            .OfType<IMethodSymbol>()
            .Any(member => member.IsStatic
                && member.Parameters.Length == 1
                && member.Parameters[0].Type.SpecialType == SpecialType.System_String
                && SymbolEqualityComparer.Default.Equals(member.ReturnType, stateType));
    }

    private static string GenerateViewRegistrySource(GenerationModel model, string registryName)
    {
        StringBuilder builder = new StringBuilder();

        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine("using System;");
        builder.AppendLine("using MiKiNuo.Mvi.Presentation.ViewRegistry;");
        builder.AppendLine();
        builder.Append("namespace ").Append(model.CompositionNamespace).AppendLine(";");
        builder.AppendLine();
        builder.AppendLine("/// <summary>");
        builder.AppendLine("/// 表示由源生成器生成的 Avalonia 视图注册表。");
        builder.AppendLine("/// </summary>");
        builder.Append("public sealed class ").Append(registryName).AppendLine(" : IMviViewRegistry");
        builder.AppendLine("{");
        builder.AppendLine("    /// <inheritdoc />");
        builder.AppendLine("    public object CreateView(object viewModel)");
        builder.AppendLine("    {");
        builder.AppendLine("        ArgumentNullException.ThrowIfNull(viewModel);");
        builder.AppendLine();
        builder.AppendLine("        switch (viewModel)");
        builder.AppendLine("        {");

        for (int index = 0; index < model.Views.Count; index++)
        {
            ViewInfo view = model.Views[index];
            builder.Append("            case ").Append(view.ViewModelTypeName).Append(" viewModel").Append(index).AppendLine(":");
            builder.Append("                return CreateView").Append(index).Append("(viewModel").Append(index).AppendLine(");");
        }

        builder.AppendLine("            default:");
        builder.AppendLine("                throw new MviViewNotFoundException(viewModel.GetType());");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine();

        for (int index = 0; index < model.Views.Count; index++)
        {
            ViewInfo view = model.Views[index];
            bool instanceMethod = view.RequiresRegistry;
            builder.Append("    private ");
            if (!instanceMethod)
            {
                builder.Append("static ");
            }

            builder.Append(view.ViewTypeName).Append(" CreateView").Append(index)
                .Append('(').Append(view.ViewModelTypeName).AppendLine(" viewModel)");
            builder.AppendLine("    {");
            builder.Append("        ").Append(view.ViewTypeName).Append(" view = new(");
            if (view.RequiresRegistry)
            {
                builder.Append("this");
            }

            builder.AppendLine(");");
            builder.AppendLine("        view.Bind(viewModel);");
            builder.AppendLine("        return view;");
            builder.AppendLine("    }");
            builder.AppendLine();
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string GenerateContainerSource(
        GenerationModel model,
        string containerName,
        string registryName)
    {
        FeatureInfo login = model.LoginFeature!;
        FeatureInfo shell = model.ShellFeature!;
        FeatureInfo? dashboard = model.GetFeature("Dashboard");

        StringBuilder builder = new StringBuilder();

        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine("using System;");
        builder.AppendLine("using System.Collections.Generic;");
        builder.AppendLine("using System.Reflection;");
        builder.AppendLine("using System.Threading;");
        builder.AppendLine("using System.Threading.Tasks;");
        builder.AppendLine("using MiKiNuo.Mvi.Application.DI;");
        builder.AppendLine("using MiKiNuo.Mvi.Application.MVI.Diagnostics;");
        builder.AppendLine("using MiKiNuo.Mvi.Application.MVI.Mediator;");
        builder.AppendLine("using MiKiNuo.Mvi.Application.MVI.Middleware;");
        builder.AppendLine("using MiKiNuo.Mvi.Application.MVI.Store;");
        builder.AppendLine("using MiKiNuo.Mvi.Application.MVI.Threading;");
        builder.AppendLine("using MiKiNuo.Mvi.Domain.DI;");
        builder.AppendLine("using MiKiNuo.Mvi.Presentation.ViewRegistry;");
        builder.AppendLine();
        builder.Append("namespace ").Append(model.CompositionNamespace).AppendLine(";");
        builder.AppendLine();
        builder.AppendLine("/// <summary>");
        builder.AppendLine("/// 表示由源生成器生成的示例组合容器。");
        builder.AppendLine("/// </summary>");
        builder.Append("public sealed class ").Append(containerName)
            .Append(" : IMviResolver, IMviServiceGraph, IMviMediator, IMviDiagnosticSink");
        if (model.LoginNavigationService is not null)
        {
            builder.Append(", ").Append(Format(model.LoginNavigationService));
        }

        builder.AppendLine();
        builder.AppendLine("{");
        builder.AppendLine("    private readonly Dictionary<Type, object> _singletons = new();");
        builder.AppendLine("    private readonly IReadOnlyList<MviServiceDescriptor> _serviceDescriptors;");
        builder.AppendLine("    private IMviViewRegistry? _viewRegistry;");
        // Avalonia 平台专用 UI 调度器单例；所有 ViewModel 与 CardStoreFactory 共用同一实例，
        // 确保 PropertyChanged/CanExecuteChanged 都能 marshal 到 Avalonia UI 线程。
        builder.AppendLine("    private IMviUiDispatcher? _uiDispatcher;");
        builder.AppendLine("    private IMviUiDispatcher ResolveUiDispatcher() => _uiDispatcher ??= new global::MiKiNuo.Mvi.Platforms.Avalonia.Threading.AvaloniaMviUiDispatcher();");
        builder.AppendLine();

        if (dashboard is not null)
        {
            builder.Append("    private ").Append(StoreType(dashboard)).AppendLine("? _dashboardStore;");
            builder.Append("    private ").Append(dashboard.ViewModelTypeName).AppendLine("? _dashboardViewModel;");
        }

        FeatureInfo? fieldClinicalEditor = model.GetFeature("ClinicalEditor");
        FeatureInfo? fieldClinicalReminder = model.GetFeature("ClinicalReminder");
        FeatureInfo? fieldBusinessCompositePage = model.GetFeature("BusinessCompositePage");
        if (fieldClinicalEditor is not null)
        {
            builder.Append("    private ").Append(StoreType(fieldClinicalEditor)).AppendLine("? _clinicalEditorStore;");
        }
        if (fieldClinicalReminder is not null)
        {
            builder.Append("    private ").Append(StoreType(fieldClinicalReminder)).AppendLine("? _clinicalReminderStore;");
        }
        if (fieldBusinessCompositePage is not null)
        {
            builder.Append("    private ").Append(StoreType(fieldBusinessCompositePage)).AppendLine("? _businessCompositePageStore;");
        }

        if (model.CardStoreFactory is not null)
        {
            builder.Append("    private ").Append(model.CardStoreFactory.TypeName).AppendLine("? _cardStoreFactory;");
        }

        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// 初始化由源生成器生成的示例组合容器。");
        builder.AppendLine("    /// </summary>");
        builder.Append("    public ").Append(containerName).AppendLine("()");
        builder.AppendLine("    {");
        builder.AppendLine("        _serviceDescriptors = new MviServiceDescriptor[]");
        builder.AppendLine("        {");
        AppendDescriptor(builder, model.AuthService?.ServiceTypeName, model.AuthService?.ImplementationTypeName, "Singleton");
        if (model.LoginNavigationService is not null)
        {
            AppendDescriptor(builder, Format(model.LoginNavigationService), containerName, "Singleton");
        }

        AppendDescriptor(builder, "global::MiKiNuo.Mvi.Application.MVI.Mediator.IMviMediator", containerName, "Singleton");
        AppendDescriptor(builder, "global::MiKiNuo.Mvi.Application.MVI.Diagnostics.IMviDiagnosticSink", containerName, "Singleton");
        AppendDescriptor(builder, "global::MiKiNuo.Mvi.Presentation.ViewRegistry.IMviViewRegistry", registryName, "Singleton");
        AppendDescriptor(builder, shell.ViewModelTypeName, shell.ViewModelTypeName, "Singleton");
        AppendDescriptor(builder, login.ViewModelTypeName, login.ViewModelTypeName, "Scoped");
        AppendDescriptor(builder, StoreType(login), "global::MiKiNuo.Mvi.Application.MVI.Store.MviStore<" + login.StateTypeName + ", " + login.IntentTypeName + ", " + login.EffectTypeName + ">", "Scoped");
        AppendDescriptor(builder, ReducerInterfaceType(login), login.ReducerTypeName, "Scoped");
        if (dashboard is not null)
        {
            AppendDescriptor(builder, dashboard.ViewModelTypeName, dashboard.ViewModelTypeName, "Singleton");
        }

        builder.AppendLine("        };");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    /// <inheritdoc />");
        builder.AppendLine("    public IReadOnlyList<MviServiceDescriptor> ServiceDescriptors => _serviceDescriptors;");
        builder.AppendLine();
        builder.AppendLine("    /// <inheritdoc />");
        builder.AppendLine("    public TService Resolve<TService>()");
        builder.AppendLine("        where TService : notnull");
        builder.AppendLine("    {");
        builder.AppendLine("        return (TService)Resolve(typeof(TService));");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    /// <inheritdoc />");
        builder.AppendLine("    public object Resolve(Type serviceType)");
        builder.AppendLine("    {");
        builder.AppendLine("        ArgumentNullException.ThrowIfNull(serviceType);");
        builder.AppendLine("        return ResolveCore(serviceType);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    /// <inheritdoc />");
        builder.AppendLine("    public IMviScope CreateScope()");
        builder.AppendLine("    {");
        builder.Append("        return new ").Append(containerName).AppendLine("Scope(this);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    /// <inheritdoc />");
        builder.AppendLine("    public void Record(MviDiagnosticEntry entry)");
        builder.AppendLine("    {");
        builder.AppendLine("        ArgumentNullException.ThrowIfNull(entry);");
        builder.AppendLine("    }");
        builder.AppendLine();

        if (model.LoginNavigationService is not null)
        {
            AppendNavigateToDashboard(builder, shell, dashboard);
        }

        FeatureInfo? mediatorClinicalEditor = model.GetFeature("ClinicalEditor");
        FeatureInfo? mediatorClinicalReminder = model.GetFeature("ClinicalReminder");
        FeatureInfo? mediatorBusinessCompositePage = model.GetFeature("BusinessCompositePage");
        AppendMediator(builder, dashboard, mediatorClinicalEditor, mediatorClinicalReminder, mediatorBusinessCompositePage);
        AppendResolveCore(builder, model, containerName, registryName, login, shell, dashboard);
        AppendStoreHelpers(builder, login, shell);
        AppendDashboardHelpers(builder, model, dashboard);
        AppendCardStoreFactoryResolver(builder, model);
        AppendScope(builder, containerName, login);

        builder.AppendLine("}");
        return builder.ToString();
    }

    private static void AppendNavigateToDashboard(StringBuilder builder, FeatureInfo shell, FeatureInfo? dashboard)
    {
        builder.AppendLine("    /// <inheritdoc />");
        builder.AppendLine("    public async ValueTask NavigateToDashboardAsync(string displayName, CancellationToken cancellationToken = default)");
        builder.AppendLine("    {");
        if (dashboard is not null)
        {
            builder.Append("        ").Append(dashboard.ViewModelTypeName)
                .AppendLine(" dashboardViewModel = CreateDashboardViewModel(displayName);");
            builder.Append("        ").Append(shell.ViewModelTypeName)
                .AppendLine(" shellViewModel = ResolveShellViewModel();");
            builder.AppendLine("        await shellViewModel.ShowPageAsync(\"HIS/EMR 组合式 Dashboard\", dashboardViewModel).ConfigureAwait(false);");
        }
        else
        {
            builder.Append("        ").Append(shell.ViewModelTypeName)
                .AppendLine(" shellViewModel = ResolveShellViewModel();");
            builder.AppendLine("        await shellViewModel.ShowPageAsync(\"Dashboard\", displayName).ConfigureAwait(false);");
        }

        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void AppendMediator(StringBuilder builder, FeatureInfo? dashboard, FeatureInfo? clinicalEditor, FeatureInfo? clinicalReminder, FeatureInfo? businessPage)
    {
        builder.AppendLine("    /// <inheritdoc />");
        builder.AppendLine("    public async ValueTask<TResponse> SendAsync<TRequest, TResponse>(");
        builder.AppendLine("        TRequest request,");
        builder.AppendLine("        CancellationToken cancellationToken = default)");
        builder.AppendLine("        where TRequest : notnull");
        builder.AppendLine("    {");
        builder.AppendLine("        ArgumentNullException.ThrowIfNull(request);");
        builder.AppendLine("        string requestName = request.GetType().Name;");
        builder.AppendLine();
        builder.AppendLine("        if (requestName == \"NavigateDashboardPageRequest\")");
        builder.AppendLine("        {");
        builder.AppendLine("            string pageKey = GetStringProperty(request, \"PageKey\") ?? string.Empty;");
        if (dashboard is not null)
        {
            builder.AppendLine("            return await NavigateDashboardPageAsync<TResponse>(pageKey, cancellationToken).ConfigureAwait(false);");
        }
        else
        {
            builder.AppendLine("            return await CreateResponse<TResponse>(pageKey, true).ConfigureAwait(false);");
        }

        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        if (requestName == \"OpenPatientEncounterRequest\")");
        builder.AppendLine("        {");
        builder.AppendLine("            string patientName = GetStringProperty(request, \"PatientName\") ?? string.Empty;");
        if (clinicalEditor is not null)
        {
            builder.Append("            if (_clinicalEditorStore is not null) await _clinicalEditorStore.DispatchAsync(new ")
                .Append(clinicalEditor.IntentTypeName)
                .AppendLine(".LoadPatient(patientName), cancellationToken).ConfigureAwait(false);");
        }
        if (clinicalReminder is not null)
        {
            builder.Append("            if (_clinicalReminderStore is not null) await _clinicalReminderStore.DispatchAsync(new ")
                .Append(clinicalReminder.IntentTypeName)
                .AppendLine(".LoadPatient(patientName), cancellationToken).ConfigureAwait(false);");
        }
        builder.AppendLine("            return await CreateResponse<TResponse>(patientName, true).ConfigureAwait(false);");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        if (requestName == \"DashboardComponentInteractionRequest\")");
        builder.AppendLine("        {");
        builder.AppendLine("            string source = GetStringProperty(request, \"SourceComponent\") ?? string.Empty;");
        builder.AppendLine("            string action = GetStringProperty(request, \"ActionKey\") ?? string.Empty;");
        if (businessPage is not null)
        {
            builder.Append("            if (_businessCompositePageStore is not null) await _businessCompositePageStore.DispatchAsync(new ")
                .Append(businessPage.IntentTypeName)
                .AppendLine(".AppendInteractionLog($\"子组件 {source} 触发 {action}\"), cancellationToken).ConfigureAwait(false);");
        }
        builder.AppendLine("            return await CreateResponse<TResponse>(source + \":\" + action, true).ConfigureAwait(false);");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        throw new InvalidOperationException($\"未注册中介者路由：{typeof(TRequest).FullName} -> {typeof(TResponse).FullName}\");");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static ValueTask<TResponse> CreateResponse<TResponse>(string text, bool changed)");
        builder.AppendLine("    {");
        builder.AppendLine("        object? response = Activator.CreateInstance(typeof(TResponse), text, changed);");
        builder.AppendLine("        if (response is null)");
        builder.AppendLine("        {");
        builder.AppendLine("            throw new InvalidOperationException($\"无法创建中介者响应：{typeof(TResponse).FullName}\");");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        return new ValueTask<TResponse>((TResponse)response);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static string? GetStringProperty(object value, string propertyName)");
        builder.AppendLine("    {");
        builder.AppendLine("        PropertyInfo? property = value.GetType().GetProperty(propertyName);");
        builder.AppendLine("        return property?.GetValue(value) as string;");
        builder.AppendLine("    }");
        builder.AppendLine();

        if (dashboard is not null)
        {
            builder.AppendLine("    private async ValueTask<TResponse> NavigateDashboardPageAsync<TResponse>(");
            builder.AppendLine("        string pageKey,");
            builder.AppendLine("        CancellationToken cancellationToken)");
            builder.AppendLine("    {");
            builder.AppendLine("        if (_dashboardStore is null)");
            builder.AppendLine("        {");
            builder.AppendLine("            CreateDashboardViewModel(string.Empty);");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        (string title, string description, object pageViewModel) = CreateDashboardPage(pageKey);");
            builder.Append("        await _dashboardStore!.DispatchAsync(new ")
                .Append(dashboard.IntentTypeName)
                .AppendLine(".ShowPage(title, description, pageViewModel), cancellationToken).ConfigureAwait(false);");
            builder.AppendLine("        return await CreateResponse<TResponse>(title, true).ConfigureAwait(false);");
            builder.AppendLine("    }");
            builder.AppendLine();
        }
    }

    private static void AppendResolveCore(
        StringBuilder builder,
        GenerationModel model,
        string containerName,
        string registryName,
        FeatureInfo login,
        FeatureInfo shell,
        FeatureInfo? dashboard)
    {
        builder.AppendLine("    private object ResolveCore(Type serviceType)");
        builder.AppendLine("    {");

        if (model.AuthService is not null)
        {
            builder.Append("        if (serviceType == typeof(").Append(model.AuthService.ServiceTypeName).AppendLine("))");
            builder.AppendLine("        {");
            builder.Append("            return GetSingleton(serviceType, static () => new ")
                .Append(model.AuthService.ImplementationTypeName).AppendLine("());");
            builder.AppendLine("        }");
            builder.AppendLine();
        }

        if (model.LoginNavigationService is not null)
        {
            builder.Append("        if (serviceType == typeof(").Append(Format(model.LoginNavigationService)).AppendLine("))");
            builder.AppendLine("        {");
            builder.AppendLine("            return this;");
            builder.AppendLine("        }");
            builder.AppendLine();
        }

        builder.AppendLine("        if (serviceType == typeof(IMviMediator))");
        builder.AppendLine("        {");
        builder.AppendLine("            return this;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        if (serviceType == typeof(IMviDiagnosticSink))");
        builder.AppendLine("        {");
        builder.AppendLine("            return this;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        if (serviceType == typeof(IMviViewRegistry))");
        builder.AppendLine("        {");
        builder.Append("            return _viewRegistry ??= new ").Append(registryName).AppendLine("();");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        if (serviceType == typeof(IMviUiDispatcher))");
        builder.AppendLine("        {");
        // AvaloniaMviUiDispatcher 来自 MiKiNuo.Mvi.Platforms.Avalonia 程序集，
        // 该程序集不在本生成器的 Compilation 内可枚举类型范围（[DiService] 跨程序集不可见），
        // 因此显式硬编码以保证示例在 Avalonia 平台上能将状态变更安全地 marshal 到 UI 线程。
        // 若 MviCommandBase 回退到 MviInlineUiDispatcher，Avalonia 的 Button.CanExecuteChanged
        // 处理器会在 R3 订阅线程上访问 button.Command 触发 VerifyAccess 异常。
        // 复用 ResolveUiDispatcher() 缓存的实例，确保容器外部 Resolve<IMviUiDispatcher>()
        // 与 ViewModel 构造函数注入拿到的是同一对象。
        builder.AppendLine("            return GetSingleton(serviceType, ResolveUiDispatcher);");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.Append("        if (serviceType == typeof(").Append(shell.ViewModelTypeName).AppendLine("))");
        builder.AppendLine("        {");
        builder.AppendLine("            return ResolveShellViewModel();");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.Append("        if (serviceType == typeof(").Append(login.ViewModelTypeName).AppendLine("))");
        builder.AppendLine("        {");
        builder.Append("            return GetSingleton(serviceType, () => new ").Append(login.ViewModelTypeName)
            .AppendLine("(ResolveLoginStore(), ResolveUiDispatcher()));");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.Append("        if (serviceType == typeof(").Append(StoreType(login)).AppendLine("))");
        builder.AppendLine("        {");
        builder.AppendLine("            return ResolveLoginStore();");
        builder.AppendLine("        }");

        if (dashboard is not null)
        {
            builder.AppendLine();
            builder.Append("        if (serviceType == typeof(").Append(dashboard.ViewModelTypeName).AppendLine("))");
            builder.AppendLine("        {");
            builder.AppendLine("            return CreateDashboardViewModel(string.Empty);");
            builder.AppendLine("        }");
        }

        builder.AppendLine();
        builder.AppendLine("        throw new InvalidOperationException($\"未注册服务：{serviceType.FullName}\");");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private object GetSingleton(Type serviceType, Func<object> factory)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (_singletons.TryGetValue(serviceType, out object? instance))");
        builder.AppendLine("        {");
        builder.AppendLine("            return instance;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        object created = factory();");
        builder.AppendLine("        _singletons[serviceType] = created;");
        builder.AppendLine("        return created;");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void AppendStoreHelpers(StringBuilder builder, FeatureInfo login, FeatureInfo shell)
    {
        builder.Append("    private ").Append(shell.ViewModelTypeName).AppendLine(" ResolveShellViewModel()");
        builder.AppendLine("    {");
        builder.Append("        return (").Append(shell.ViewModelTypeName).Append(")GetSingleton(typeof(")
            .Append(shell.ViewModelTypeName).AppendLine("), CreateShellViewModel);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.Append("    private ").Append(shell.ViewModelTypeName).AppendLine(" CreateShellViewModel()");
        builder.AppendLine("    {");
        builder.Append("        ").Append(StoreType(shell)).AppendLine(" store = new MviStore<")
            .Append("            ").Append(shell.StateTypeName).AppendLine(",")
            .Append("            ").Append(shell.IntentTypeName).AppendLine(",")
            .Append("            ").Append(shell.EffectTypeName).AppendLine(">(")
            .Append("            ").Append(shell.StateTypeName).AppendLine(".Initial,")
            .Append("            new ").Append(shell.ReducerTypeName).AppendLine("(),")
            .Append("            new ").Append(shell.DispatcherTypeName).AppendLine("());");
        builder.Append("        return new ").Append(shell.ViewModelTypeName).AppendLine("(store, ResolveUiDispatcher());");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.Append("    private ").Append(StoreType(login)).AppendLine(" ResolveLoginStore()");
        builder.AppendLine("    {");
        builder.Append("        return (").Append(StoreType(login)).Append(")GetSingleton(typeof(")
            .Append(StoreType(login)).AppendLine("), () => CreateLoginStore(ResolveLoginStore));");
        builder.AppendLine("    }");
        builder.AppendLine();
        AppendLoginStoreFactory(builder, login, "    ");
    }

    private static void AppendLoginStoreFactory(StringBuilder builder, FeatureInfo login, string indent)
    {
        builder.Append(indent).Append("private ").Append(StoreType(login)).Append(" CreateLoginStore(Func<")
            .Append(StoreType(login)).AppendLine("> storeFactory)");
        builder.Append(indent).AppendLine("{");
        builder.Append(indent).Append("    ").Append(login.DispatcherTypeName).Append(" dispatcher = new(");
        builder.Append("Resolve<").Append("global::").Append(login.ViewModelType.ContainingNamespace.ToDisplayString())
            .Append(".IAuthService>(), this, storeFactory);").AppendLine();
        builder.Append(indent).Append("    return new MviStore<").Append(login.StateTypeName).Append(", ")
            .Append(login.IntentTypeName).Append(", ").Append(login.EffectTypeName).AppendLine(">(");
        builder.Append(indent).Append("        ").Append(login.StateTypeName).AppendLine(".Initial,");
        builder.Append(indent).Append("        new ").Append(login.ReducerTypeName).AppendLine("(),");
        builder.Append(indent).AppendLine("        dispatcher);");
        builder.Append(indent).AppendLine("}");
        builder.AppendLine();
    }

    private static void AppendDashboardHelpers(
        StringBuilder builder,
        GenerationModel model,
        FeatureInfo? dashboard)
    {
        if (dashboard is null)
        {
            return;
        }

        FeatureInfo? dashboardMenu = model.GetFeature("DashboardMenu");
        FeatureInfo? header = model.GetFeature("Header");
        FeatureInfo? outpatient = model.GetFeature("OutpatientWorkstation");
        FeatureInfo? patientQueue = model.GetFeature("PatientQueue");
        FeatureInfo? clinicalEditor = model.GetFeature("ClinicalEditor");
        FeatureInfo? clinicalReminder = model.GetFeature("ClinicalReminder");
        FeatureInfo? businessPage = model.GetFeature("BusinessCompositePage");
        FeatureInfo? architectureValidation = model.GetFeature("ArchitectureValidation");
        FeatureInfo? patientSearch = model.GetFeature("PatientSearch");
        FeatureInfo? auditTimeline = model.GetFeature("AuditTimeline");
        FeatureInfo? metricCard = model.GetFeature("Card");
        FeatureInfo? bedOverview = model.GetFeature("BedOverview");
        FeatureInfo? admissionCoordinator = model.GetFeature("AdmissionCoordinator");
        FeatureInfo? nursingTaskBoard = model.GetFeature("NursingTaskBoard");
        FeatureInfo? wardRiskPanel = model.GetFeature("WardRiskPanel");
        FeatureInfo? labOrderComposer = model.GetFeature("LabOrderComposer");
        FeatureInfo? specimenTracker = model.GetFeature("SpecimenTracker");
        FeatureInfo? criticalValueMonitor = model.GetFeature("CriticalValueMonitor");
        FeatureInfo? labTurnaroundBoard = model.GetFeature("LabTurnaroundBoard");
        FeatureInfo? prescriptionReviewBoard = model.GetFeature("PrescriptionReviewBoard");
        FeatureInfo? drugStockMonitor = model.GetFeature("DrugStockMonitor");
        FeatureInfo? replenishmentPlanner = model.GetFeature("ReplenishmentPlanner");
        FeatureInfo? medicationSafetyPanel = model.GetFeature("MedicationSafetyPanel");
        FeatureInfo? qualityKpiBoard = model.GetFeature("QualityKpiBoard");
        FeatureInfo? medicalRecordAuditBoard = model.GetFeature("MedicalRecordAuditBoard");
        FeatureInfo? riskEventBoard = model.GetFeature("RiskEventBoard");
        FeatureInfo? rectificationTracker = model.GetFeature("RectificationTracker");

        builder.Append("    private ").Append(dashboard.ViewModelTypeName).AppendLine(" CreateDashboardViewModel(string displayName)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (_dashboardViewModel is not null)");
        builder.AppendLine("        {");
        builder.AppendLine("            return _dashboardViewModel;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        object menuViewModel = CreateDashboardMenuViewModel();");
        builder.AppendLine("        object headerViewModel = CreateHeaderViewModel(displayName);");
        builder.AppendLine("        object currentPageViewModel = CreateInitialDashboardPageViewModel();");
        builder.Append("        _dashboardStore = new MviStore<").Append(dashboard.StateTypeName).Append(", ")
            .Append(dashboard.IntentTypeName).Append(", ").Append(dashboard.EffectTypeName).AppendLine(">(");
        builder.Append("            new ").Append(dashboard.StateTypeName).AppendLine("(");
        builder.AppendLine("                displayName,");
        builder.AppendLine("                menuViewModel,");
        builder.AppendLine("                headerViewModel,");
        builder.AppendLine("                \"门诊工作站\",");
        builder.AppendLine("                \"通过源生成组合根创建的 Dashboard 初始页面。\",");
        builder.AppendLine("                currentPageViewModel),");
        builder.Append("            new ").Append(dashboard.ReducerTypeName).AppendLine("(),");
        builder.Append("            new ").Append(dashboard.DispatcherTypeName).AppendLine("());");
        builder.Append("        _dashboardViewModel = new ").Append(dashboard.ViewModelTypeName).AppendLine("(_dashboardStore, ResolveUiDispatcher());");
        builder.AppendLine("        return _dashboardViewModel;");
        builder.AppendLine("    }");
        builder.AppendLine();

        AppendKnownFeatureFactory(builder, dashboardMenu, "CreateDashboardMenuViewModel", dashboardMenu?.StateTypeName + ".Initial");
        if (header is not null)
        {
            builder.Append("    private ").Append(header.ViewModelTypeName).AppendLine(" CreateHeaderViewModel(string displayName)");
            builder.AppendLine("    {");
            builder.Append("        ").Append(StoreType(header)).Append(" store = new MviStore<")
                .Append(header.StateTypeName).Append(", ").Append(header.IntentTypeName).Append(", ")
                .Append(header.EffectTypeName).AppendLine(">(");
            builder.Append("            new ").Append(header.StateTypeName)
                .AppendLine("(\"HIS/EMR 组合式 Dashboard\", $\"欢迎，{displayName}\"),");
            builder.Append("            new ").Append(header.ReducerTypeName).AppendLine("(),");
            builder.Append("            new ").Append(header.DispatcherTypeName).AppendLine("());");
            builder.Append("        return new ").Append(header.ViewModelTypeName).AppendLine("(store, ResolveUiDispatcher());");
            builder.AppendLine("    }");
            builder.AppendLine();
        }

        builder.AppendLine("    private object CreateInitialDashboardPageViewModel()");
        builder.AppendLine("    {");
        if (outpatient is not null)
        {
            builder.AppendLine("        return CreateOutpatientWorkstationViewModel();");
        }
        else if (dashboardMenu is not null)
        {
            builder.AppendLine("        return CreateDashboardMenuViewModel();");
        }
        else
        {
            builder.AppendLine("        return new object();");
        }

        builder.AppendLine("    }");
        builder.AppendLine();

        AppendDashboardPageFactory(
            builder,
            outpatient,
            businessPage,
            architectureValidation,
            bedOverview,
            admissionCoordinator,
            nursingTaskBoard,
            wardRiskPanel,
            labOrderComposer,
            specimenTracker,
            criticalValueMonitor,
            labTurnaroundBoard,
            prescriptionReviewBoard,
            drugStockMonitor,
            replenishmentPlanner,
            medicationSafetyPanel,
            qualityKpiBoard,
            medicalRecordAuditBoard,
            riskEventBoard,
            rectificationTracker,
            patientSearch,
            auditTimeline,
            metricCard);

        if (outpatient is not null)
        {
            builder.Append("    private ").Append(outpatient.ViewModelTypeName).AppendLine(" CreateOutpatientWorkstationViewModel()");
            builder.AppendLine("    {");
            builder.AppendLine("        object queueViewModel = CreatePatientQueueViewModel();");
            if (clinicalEditor is not null)
            {
                builder.Append("        ").Append(StoreType(clinicalEditor)).AppendLine(" editorStore = new MviStore<")
                    .Append(clinicalEditor.StateTypeName).Append(", ").Append(clinicalEditor.IntentTypeName).Append(", ")
                    .Append(clinicalEditor.EffectTypeName).AppendLine(">(");
                builder.Append("            ").Append(clinicalEditor.StateTypeName).AppendLine(".Initial,");
                builder.Append("            new ").Append(clinicalEditor.ReducerTypeName).AppendLine("(),");
                builder.Append("            ").Append(CreateDispatcherExpression(clinicalEditor)).AppendLine(");");
                builder.AppendLine("        _clinicalEditorStore = editorStore;");
                builder.Append("        object editorViewModel = new ").Append(clinicalEditor.ViewModelTypeName).AppendLine("(editorStore, ResolveUiDispatcher());");
            }
            else
            {
                builder.AppendLine("        object editorViewModel = CreateClinicalEditorViewModel();");
            }
            if (clinicalReminder is not null)
            {
                builder.Append("        ").Append(StoreType(clinicalReminder)).AppendLine(" reminderStore = new MviStore<")
                    .Append(clinicalReminder.StateTypeName).Append(", ").Append(clinicalReminder.IntentTypeName).Append(", ")
                    .Append(clinicalReminder.EffectTypeName).AppendLine(">(");
                builder.Append("            ").Append(clinicalReminder.StateTypeName).AppendLine(".Initial,");
                builder.Append("            new ").Append(clinicalReminder.ReducerTypeName).AppendLine("(),");
                builder.Append("            ").Append(CreateDispatcherExpression(clinicalReminder)).AppendLine(");");
                builder.AppendLine("        _clinicalReminderStore = reminderStore;");
                builder.Append("        object reminderViewModel = new ").Append(clinicalReminder.ViewModelTypeName).AppendLine("(reminderStore, ResolveUiDispatcher());");
            }
            else
            {
                builder.AppendLine("        object reminderViewModel = CreateClinicalReminderViewModel();");
            }
            builder.Append("        ").Append(StoreType(outpatient)).Append(" store = new MviStore<")
                .Append(outpatient.StateTypeName).Append(", ").Append(outpatient.IntentTypeName).Append(", ")
                .Append(outpatient.EffectTypeName).AppendLine(">(");
            builder.Append("            new ").Append(outpatient.StateTypeName)
                .AppendLine("(queueViewModel, editorViewModel, reminderViewModel),");
            builder.Append("            new ").Append(outpatient.ReducerTypeName).AppendLine("(),");
            builder.Append("            new ").Append(outpatient.DispatcherTypeName).AppendLine("());");
            builder.Append("        return new ").Append(outpatient.ViewModelTypeName).AppendLine("(store, ResolveUiDispatcher());");
            builder.AppendLine("    }");
            builder.AppendLine();
        }
        
        AppendKnownFeatureFactory(builder, patientQueue, "CreatePatientQueueViewModel", patientQueue?.StateTypeName + ".Initial");
        if (clinicalEditor is null)
        {
            AppendKnownFeatureFactory(builder, clinicalEditor, "CreateClinicalEditorViewModel", clinicalEditor?.StateTypeName + ".Initial");
        }
        if (clinicalReminder is null)
        {
            AppendKnownFeatureFactory(builder, clinicalReminder, "CreateClinicalReminderViewModel", clinicalReminder?.StateTypeName + ".Initial");
        }
        AppendKnownFeatureFactory(builder, bedOverview, "CreateBedOverviewViewModel", bedOverview?.StateTypeName + ".Initial");
        AppendKnownFeatureFactory(builder, admissionCoordinator, "CreateAdmissionCoordinatorViewModel", admissionCoordinator?.StateTypeName + ".Initial");
        AppendKnownFeatureFactory(builder, nursingTaskBoard, "CreateNursingTaskBoardViewModel", nursingTaskBoard?.StateTypeName + ".Initial");
        AppendKnownFeatureFactory(builder, wardRiskPanel, "CreateWardRiskPanelViewModel", wardRiskPanel?.StateTypeName + ".Initial");
        AppendKnownFeatureFactory(builder, labOrderComposer, "CreateLabOrderComposerViewModel", labOrderComposer?.StateTypeName + ".Initial");
        AppendKnownFeatureFactory(builder, specimenTracker, "CreateSpecimenTrackerViewModel", specimenTracker?.StateTypeName + ".Initial");
        AppendKnownFeatureFactory(builder, criticalValueMonitor, "CreateCriticalValueMonitorViewModel", criticalValueMonitor?.StateTypeName + ".Initial");
        AppendKnownFeatureFactory(builder, labTurnaroundBoard, "CreateLabTurnaroundBoardViewModel", labTurnaroundBoard?.StateTypeName + ".Initial");
        AppendKnownFeatureFactory(builder, prescriptionReviewBoard, "CreatePrescriptionReviewBoardViewModel", prescriptionReviewBoard?.StateTypeName + ".Initial");
        AppendKnownFeatureFactory(builder, drugStockMonitor, "CreateDrugStockMonitorViewModel", drugStockMonitor?.StateTypeName + ".Initial");
        AppendKnownFeatureFactory(builder, replenishmentPlanner, "CreateReplenishmentPlannerViewModel", replenishmentPlanner?.StateTypeName + ".Initial");
        AppendKnownFeatureFactory(builder, medicationSafetyPanel, "CreateMedicationSafetyPanelViewModel", medicationSafetyPanel?.StateTypeName + ".Initial");
        AppendKnownFeatureFactory(builder, qualityKpiBoard, "CreateQualityKpiBoardViewModel", qualityKpiBoard?.StateTypeName + ".Initial");
        AppendKnownFeatureFactory(builder, medicalRecordAuditBoard, "CreateMedicalRecordAuditBoardViewModel", medicalRecordAuditBoard?.StateTypeName + ".Initial");
        AppendKnownFeatureFactory(builder, riskEventBoard, "CreateRiskEventBoardViewModel", riskEventBoard?.StateTypeName + ".Initial");
        AppendKnownFeatureFactory(builder, rectificationTracker, "CreateRectificationTrackerViewModel", rectificationTracker?.StateTypeName + ".Initial");
        AppendBusinessPageFactory(builder, businessPage);
        AppendPageKeyFeatureFactory(builder, patientSearch, "CreatePatientSearchViewModel");
        AppendPageKeyFeatureFactory(builder, auditTimeline, "CreateAuditTimelineViewModel");
        AppendArchitectureValidationFactory(builder, architectureValidation, patientSearch, auditTimeline);
    }

    private static void AppendDashboardPageFactory(
        StringBuilder builder,
        FeatureInfo? outpatient,
        FeatureInfo? businessPage,
        FeatureInfo? architectureValidation,
        FeatureInfo? bedOverview,
        FeatureInfo? admissionCoordinator,
        FeatureInfo? nursingTaskBoard,
        FeatureInfo? wardRiskPanel,
        FeatureInfo? labOrderComposer,
        FeatureInfo? specimenTracker,
        FeatureInfo? criticalValueMonitor,
        FeatureInfo? labTurnaroundBoard,
        FeatureInfo? prescriptionReviewBoard,
        FeatureInfo? drugStockMonitor,
        FeatureInfo? replenishmentPlanner,
        FeatureInfo? medicationSafetyPanel,
        FeatureInfo? qualityKpiBoard,
        FeatureInfo? medicalRecordAuditBoard,
        FeatureInfo? riskEventBoard,
        FeatureInfo? rectificationTracker,
        FeatureInfo? patientSearch,
        FeatureInfo? auditTimeline,
        FeatureInfo? metricCard)
    {
        builder.AppendLine("    private (string Title, string Description, object PageViewModel) CreateDashboardPage(string pageKey)");
        builder.AppendLine("    {");
        builder.AppendLine("        switch (pageKey)");
        builder.AppendLine("        {");
        if (outpatient is not null)
        {
            builder.AppendLine("            case \"门诊工作站\":");
            builder.AppendLine("                return (\"门诊工作站\", \"门诊医生工作站组合页面。\", CreateOutpatientWorkstationViewModel());");
        }

        AppendBusinessPageCase(
            builder,
            businessPage,
            "住院床位",
            "住院床位 · 数据流",
            "床位总览 → 入院协调 → 护理任务 → 病区风险，4 节点 Z 形数据流。",
            "Inpatient",
            "住院病区 ① 床位状态",
            "数据流 ① → ② → ③ → ④ 协同中。",
            bedOverview,
            admissionCoordinator,
            nursingTaskBoard,
            wardRiskPanel);
        AppendBusinessPageCase(
            builder,
            businessPage,
            "检验医嘱",
            "检验医嘱 · 数据流",
            "医嘱开立 → 标本流转 → 危急值 → TAT 监控，4 节点 Z 形数据流。",
            "Lab",
            "检验流程 ① 申请入口",
            "数据流 ① → ② → ③ → ④ 协同中。",
            labOrderComposer,
            specimenTracker,
            criticalValueMonitor,
            labTurnaroundBoard);
        AppendBusinessPageCase(
            builder,
            businessPage,
            "药房库存",
            "药房库存 · 数据流",
            "处方审核 → 库存水位 → 补货计划 → 用药拦截，4 节点 Z 形数据流。",
            "Pharmacy",
            "药品业务 ① 处方审核",
            "数据流 ① → ② → ③ → ④ 协同中。",
            prescriptionReviewBoard,
            drugStockMonitor,
            replenishmentPlanner,
            medicationSafetyPanel);
        AppendBusinessPageCase(
            builder,
            businessPage,
            "运营质控",
            "运营质控 · 数据流",
            "院级 KPI → 病历抽查 → 风险分级 → 整改闭环，4 节点 Z 形数据流。",
            "Quality",
            "质量管理 ① 指标总览",
            "数据流 ① → ② → ③ → ④ 协同中。",
            qualityKpiBoard,
            medicalRecordAuditBoard,
            riskEventBoard,
            rectificationTracker);
        if (architectureValidation is not null && patientSearch is not null && auditTimeline is not null && metricCard is not null)
        {
            builder.AppendLine("            case \"架构验证中心\":");
            builder.AppendLine("                return (\"架构验证中心\", \"验证组合模式、复用特性、中介者和事件绑定的复杂页面。\", CreateArchitectureValidationViewModel());");
        }

        builder.AppendLine("            default:");
        if (outpatient is not null)
        {
            builder.AppendLine("                return (\"门诊工作站\", \"门诊医生工作站组合页面。\", CreateOutpatientWorkstationViewModel());");
        }
        else
        {
            builder.AppendLine("                return (pageKey, \"未注册 Dashboard 页面。\", new object());");
        }

        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void AppendBusinessPageCase(
        StringBuilder builder,
        FeatureInfo? businessPage,
        string menuKey,
        string title,
        string summary,
        string layout,
        string activeContext,
        string flowStatus,
        FeatureInfo? primary,
        FeatureInfo? secondary,
        FeatureInfo? tertiary,
        FeatureInfo? quaternary)
    {
        _ = primary;
        _ = secondary;
        _ = tertiary;
        _ = quaternary;
        if (businessPage is null)
        {
            return;
        }

        builder.Append("            case \"").Append(menuKey).AppendLine("\":");
        builder.Append("                return (\"").Append(title).Append("\", \"").Append(summary).Append("\", ");
        builder.Append("CreateBusinessCompositePageViewModel(\"").Append(title).Append("\", \"")
            .Append(summary).Append("\", \"").Append(layout).Append("\", \"")
            .Append(activeContext).Append("\", \"").Append(flowStatus).AppendLine("\"));");
    }

    private static void AppendBusinessPageFactory(StringBuilder builder, FeatureInfo? feature)
    {
        if (feature is null)
        {
            return;
        }

        builder.Append("    private ").Append(feature.ViewModelTypeName).AppendLine(" CreateBusinessCompositePageViewModel(");
        builder.AppendLine("        string title,");
        builder.AppendLine("        string summary,");
        builder.AppendLine("        string layout,");
        builder.AppendLine("        string activeContext,");
        builder.AppendLine("        string flowStatus)");
        builder.AppendLine("    {");
        builder.Append("        ").Append(StoreType(feature)).Append(" store = new MviStore<")
            .Append(feature.StateTypeName).Append(", ").Append(feature.IntentTypeName).Append(", ")
            .Append(feature.EffectTypeName).AppendLine(">(");
        builder.Append("            new ").Append(feature.StateTypeName).AppendLine("(");
        builder.AppendLine("                title,");
        builder.AppendLine("                summary,");
        builder.AppendLine("                new object(),");
        builder.AppendLine("                new object(),");
        builder.AppendLine("                new object(),");
        builder.AppendLine("                new object(),");
        builder.AppendLine("                layout,");
        builder.AppendLine("                activeContext,");
        builder.AppendLine("                flowStatus,");
        builder.AppendLine("                \"等待子组件交互。\"),");
        builder.Append("            new ").Append(feature.ReducerTypeName).AppendLine("(),");
        builder.Append("            ").Append(CreateDispatcherExpression(feature)).AppendLine(");");
        builder.AppendLine("        _businessCompositePageStore = store;");
        builder.Append("        return new ").Append(feature.ViewModelTypeName).AppendLine("(store, ResolveCardStoreFactory(), ResolveUiDispatcher());");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    /// <summary>
    /// 生成按需构造的 <c>CardStoreFactory</c> 解析器，缓存为单例。
    /// 工厂仅依赖 <c>IMviMediator</c>，因此通过 <c>this</c> 满足依赖。
    /// </summary>
    /// <param name="builder">字符串构建器。</param>
    /// <param name="model">生成模型。</param>
    private static void AppendCardStoreFactoryResolver(StringBuilder builder, GenerationModel model)
    {
        if (model.CardStoreFactory is null)
        {
            return;
        }

        builder.Append("    private ").Append(model.CardStoreFactory.TypeName).AppendLine(" ResolveCardStoreFactory()");
        builder.AppendLine("    {");
        builder.AppendLine("        if (_cardStoreFactory is not null)");
        builder.AppendLine("        {");
        builder.AppendLine("            return _cardStoreFactory;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.Append("        _cardStoreFactory = new ").Append(model.CardStoreFactory.TypeName)
            .Append("(this, (global::MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.PatientRegistry.IMviPatientRegistry)GetSingleton(typeof(global::MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.PatientRegistry.IMviPatientRegistry), ")
            .Append("static () => new global::MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.PatientRegistry.InMemoryPatientRegistry()));").AppendLine();
        builder.AppendLine("        return _cardStoreFactory;");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void AppendPageKeyFeatureFactory(
        StringBuilder builder,
        FeatureInfo? feature,
        string methodName)
    {
        if (feature is null || !feature.HasStringCreateInitial)
        {
            return;
        }

        builder.Append("    private ").Append(feature.ViewModelTypeName).Append(' ').Append(methodName)
            .AppendLine("(string pageKey)");
        builder.AppendLine("    {");
        builder.Append("        ").Append(StoreType(feature)).Append(" store = new MviStore<")
            .Append(feature.StateTypeName).Append(", ").Append(feature.IntentTypeName).Append(", ")
            .Append(feature.EffectTypeName).AppendLine(">(");
        builder.Append("            ").Append(feature.StateTypeName).AppendLine(".CreateInitial(pageKey),");
        builder.Append("            new ").Append(feature.ReducerTypeName).AppendLine("(),");
        builder.Append("            ").Append(CreateDispatcherExpression(feature)).AppendLine(");");
        builder.Append("        return new ").Append(feature.ViewModelTypeName).AppendLine("(store, ResolveUiDispatcher());");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void AppendArchitectureValidationFactory(
        StringBuilder builder,
        FeatureInfo? feature,
        FeatureInfo? patientSearch,
        FeatureInfo? auditTimeline)
    {
        if (feature is null
            || patientSearch is null
            || auditTimeline is null)
        {
            return;
        }

        // 4 张指标卡通过 CardStoreFactory.GetViewModel(PageKey.X) 解析，
        // 与业务页 16 张卡片共享同一 store/VM 实例（含完整 14 参 CardState），
        // 因此源代码生成器不再独立构造 5 参简化版 CardState——之前 AppendMetricCardFactory
        // 调用 `new CardState(title, value, status, detail, true)` 在 CardState 构造已扩展为
        // 14 参后会导致整页编译失败、菜单项路由被默认回退到门诊工作站，
        // 这就是"门诊工作站和架构验证中心两个菜单进去界面完全一样"的根本原因。
        builder.Append("    private ").Append(feature.ViewModelTypeName).AppendLine(" CreateArchitectureValidationViewModel()");
        builder.AppendLine("    {");
        builder.Append("        ").Append(patientSearch.ViewModelTypeName).AppendLine(" patientSearchViewModel = CreatePatientSearchViewModel(\"架构验证中心\");");
        builder.Append("        ").Append(auditTimeline.ViewModelTypeName).AppendLine(" auditTimelineViewModel = CreateAuditTimelineViewModel(\"架构验证中心\");");
        builder.Append("        global::MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards.CardViewModel middlewareMetricViewModel = ResolveCardStoreFactory().GetViewModel(global::MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards.PageKey.MiddlewareMetric);");
        builder.Append("        global::MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards.CardViewModel reuseMetricViewModel = ResolveCardStoreFactory().GetViewModel(global::MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards.PageKey.ReuseMetric);");
        builder.Append("        global::MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards.CardViewModel mediatorMetricViewModel = ResolveCardStoreFactory().GetViewModel(global::MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards.PageKey.MediatorMetric);");
        builder.Append("        global::MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards.CardViewModel effectMetricViewModel = ResolveCardStoreFactory().GetViewModel(global::MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards.PageKey.EffectMetric);");
        builder.Append("        ").Append(StoreType(feature)).Append(" store = new MviStore<")
            .Append(feature.StateTypeName).Append(", ").Append(feature.IntentTypeName).Append(", ")
            .Append(feature.EffectTypeName).AppendLine(">(");
        builder.Append("            new ").Append(feature.StateTypeName).AppendLine("(");
        builder.AppendLine("                \"架构验证中心\",");
        builder.AppendLine("                \"MVI 4 大架构特性（中间件 / 复用组件 / 中介者 / 副作用）的静态展示，端到端验证请到 4 个生产页面。\",");
        builder.AppendLine("                patientSearchViewModel,");
        builder.AppendLine("                auditTimelineViewModel,");
        builder.AppendLine("                middlewareMetricViewModel,");
        builder.AppendLine("                reuseMetricViewModel,");
        builder.AppendLine("                mediatorMetricViewModel,");
        builder.AppendLine("                effectMetricViewModel,");
        builder.AppendLine("                \"架构验证中心\",");
        builder.AppendLine("                \"组合根已由源生成器装配。\",");
        builder.AppendLine("                \"等待子组件交互。\"),");
        builder.Append("            new ").Append(feature.ReducerTypeName).AppendLine("(),");
        builder.Append("            ").Append(CreateDispatcherExpression(feature)).AppendLine(");");
        builder.Append("        return new ").Append(feature.ViewModelTypeName).AppendLine("(store, ResolveUiDispatcher());");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void AppendKnownFeatureFactory(
        StringBuilder builder,
        FeatureInfo? feature,
        string methodName,
        string? stateExpression)
    {
        if (feature is null || stateExpression is null)
        {
            return;
        }

        builder.Append("    private ").Append(feature.ViewModelTypeName).Append(' ').Append(methodName).AppendLine("()");
        builder.AppendLine("    {");
        builder.Append("        ").Append(StoreType(feature)).Append(" store = new MviStore<")
            .Append(feature.StateTypeName).Append(", ").Append(feature.IntentTypeName).Append(", ")
            .Append(feature.EffectTypeName).AppendLine(">(");
        builder.Append("            ").Append(stateExpression).AppendLine(",");
        builder.Append("            new ").Append(feature.ReducerTypeName).AppendLine("(),");
        builder.Append("            ").Append(CreateDispatcherExpression(feature)).AppendLine(");");
        builder.Append("        return new ").Append(feature.ViewModelTypeName).AppendLine("(store, ResolveUiDispatcher());");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void AppendScope(StringBuilder builder, string containerName, FeatureInfo login)
    {
        builder.Append("    private sealed class ").Append(containerName).AppendLine("Scope : IMviScope");
        builder.AppendLine("    {");
        builder.Append("        private readonly ").Append(containerName).AppendLine(" _container;");
        builder.AppendLine("        private readonly Dictionary<Type, object> _scoped = new();");
        builder.AppendLine();
        builder.Append("        public ").Append(containerName).Append("Scope(").Append(containerName).AppendLine(" container)");
        builder.AppendLine("        {");
        builder.AppendLine("            _container = container;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        public TService Resolve<TService>()");
        builder.AppendLine("            where TService : notnull");
        builder.AppendLine("        {");
        builder.AppendLine("            return (TService)Resolve(typeof(TService));");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        public object Resolve(Type serviceType)");
        builder.AppendLine("        {");
        builder.AppendLine("            ArgumentNullException.ThrowIfNull(serviceType);");
        builder.Append("            if (serviceType == typeof(").Append(StoreType(login)).AppendLine("))");
        builder.AppendLine("            {");
        builder.AppendLine("                return ResolveLoginStore();");
        builder.AppendLine("            }");
        builder.AppendLine();
        builder.Append("            if (serviceType == typeof(").Append(login.ViewModelTypeName).AppendLine("))");
        builder.AppendLine("            {");
        builder.AppendLine("                return GetScoped(serviceType, () => new " + login.ViewModelTypeName + "(ResolveLoginStore()));");
        builder.AppendLine("            }");
        builder.AppendLine();
        builder.AppendLine("            return _container.Resolve(serviceType);");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        public void Dispose()");
        builder.AppendLine("        {");
        builder.AppendLine("            foreach (object instance in _scoped.Values)");
        builder.AppendLine("            {");
        builder.AppendLine("                if (instance is IDisposable disposable)");
        builder.AppendLine("                {");
        builder.AppendLine("                    disposable.Dispose();");
        builder.AppendLine("                }");
        builder.AppendLine("            }");
        builder.AppendLine();
        builder.AppendLine("            _scoped.Clear();");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.Append("        private ").Append(StoreType(login)).AppendLine(" ResolveLoginStore()");
        builder.AppendLine("        {");
        builder.Append("            return (").Append(StoreType(login)).Append(")GetScoped(typeof(")
            .Append(StoreType(login)).AppendLine("), () => _container.CreateLoginStore(ResolveLoginStore));");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        private object GetScoped(Type serviceType, Func<object> factory)");
        builder.AppendLine("        {");
        builder.AppendLine("            if (_scoped.TryGetValue(serviceType, out object? instance))");
        builder.AppendLine("            {");
        builder.AppendLine("                return instance;");
        builder.AppendLine("            }");
        builder.AppendLine();
        builder.AppendLine("            object created = factory();");
        builder.AppendLine("            _scoped[serviceType] = created;");
        builder.AppendLine("            return created;");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
    }

    private static void AppendDescriptor(
        StringBuilder builder,
        string? serviceType,
        string? implementationType,
        string lifetime)
    {
        if (serviceType is null || implementationType is null)
        {
            return;
        }

        builder.Append("            new(typeof(").Append(serviceType).Append("), typeof(")
            .Append(implementationType).Append("), ServiceLifetime.").Append(lifetime).AppendLine("),");
    }

    private static string CreateDispatcherExpression(FeatureInfo feature)
    {
        switch (feature.DispatcherConstructorKind)
        {
            case DispatcherConstructorKind.Mediator:
                return "new " + feature.DispatcherTypeName + "(this)";
            case DispatcherConstructorKind.Parameterless:
                return "new " + feature.DispatcherTypeName + "()";
            default:
                return "new " + feature.DispatcherTypeName + "()";
        }
    }

    private static string ReducerInterfaceType(FeatureInfo feature)
    {
        return "global::MiKiNuo.Mvi.Application.MVI.Reducer.IMviReducer<"
            + feature.StateTypeName + ", "
            + feature.IntentTypeName + ", "
            + feature.EffectTypeName + ">";
    }

    private static string StoreType(FeatureInfo feature)
    {
        return "global::MiKiNuo.Mvi.Application.MVI.Store.IMviStore<"
            + feature.StateTypeName + ", "
            + feature.IntentTypeName + ", "
            + feature.EffectTypeName + ">";
    }

    private static string Format(ITypeSymbol symbol)
    {
        return symbol.ToDisplayString(TypeFormat);
    }

    private static string RemoveSuffix(string value, string suffix)
    {
        return value.EndsWith(suffix, StringComparison.Ordinal)
            ? value.Substring(0, value.Length - suffix.Length)
            : value;
    }

    private static string GetCompositionPrefix(string? assemblyName)
    {
        if (assemblyName is not null
            && assemblyName.IndexOf(".Samples.", StringComparison.Ordinal) >= 0)
        {
            return "Sample";
        }

        return "Mvi";
    }

    private sealed class GenerationModel
    {
        private readonly Dictionary<string, FeatureInfo> _featuresByName;

        public GenerationModel(
            string assemblyName,
            IReadOnlyList<FeatureInfo> features,
            IReadOnlyList<ViewInfo> views,
            ServiceInfo? authService,
            INamedTypeSymbol? loginNavigationService,
            CardStoreFactoryInfo? cardStoreFactory)
        {
            CompositionNamespace = assemblyName + ".Composition";
            Features = features;
            Views = views;
            AuthService = authService;
            LoginNavigationService = loginNavigationService;
            CardStoreFactory = cardStoreFactory;
            _featuresByName = features.ToDictionary(static feature => feature.BaseName, StringComparer.Ordinal);
        }

        public string CompositionNamespace { get; }

        public IReadOnlyList<FeatureInfo> Features { get; }

        public IReadOnlyList<ViewInfo> Views { get; }

        public ServiceInfo? AuthService { get; }

        public INamedTypeSymbol? LoginNavigationService { get; }

        public CardStoreFactoryInfo? CardStoreFactory { get; }

        public FeatureInfo? LoginFeature => GetFeature("Login");

        public FeatureInfo? ShellFeature => GetFeature("AppShell");

        public FeatureInfo? GetFeature(string baseName)
        {
            return _featuresByName.TryGetValue(baseName, out FeatureInfo? feature)
                ? feature
                : null;
        }
    }

    /// <summary>
    /// 表示 <c>CardStoreFactory</c> 的源生成器视图：仅记录类型完整名和已选中的构造函数。
    /// </summary>
    private sealed class CardStoreFactoryInfo
    {
        public CardStoreFactoryInfo(INamedTypeSymbol type, IMethodSymbol constructor)
        {
            Type = type;
            TypeName = Format(type);
            Constructor = constructor;
        }

        public INamedTypeSymbol Type { get; }

        public string TypeName { get; }

        public IMethodSymbol Constructor { get; }
    }

    private sealed class FeatureInfo
    {
        public FeatureInfo(
            string baseName,
            INamedTypeSymbol viewModelType,
            INamedTypeSymbol stateType,
            INamedTypeSymbol intentType,
            INamedTypeSymbol effectType,
            INamedTypeSymbol reducerType,
            INamedTypeSymbol dispatcherType,
            DispatcherConstructorKind dispatcherConstructorKind,
            bool hasInitial,
            bool hasStringCreateInitial)
        {
            BaseName = baseName;
            ViewModelType = viewModelType;
            ViewModelTypeName = Format(viewModelType);
            StateTypeName = Format(stateType);
            IntentTypeName = Format(intentType);
            EffectTypeName = Format(effectType);
            ReducerTypeName = Format(reducerType);
            DispatcherTypeName = Format(dispatcherType);
            DispatcherConstructorKind = dispatcherConstructorKind;
            HasInitial = hasInitial;
            HasStringCreateInitial = hasStringCreateInitial;
        }

        public string BaseName { get; }

        public INamedTypeSymbol ViewModelType { get; }

        public string ViewModelTypeName { get; }

        public string StateTypeName { get; }

        public string IntentTypeName { get; }

        public string EffectTypeName { get; }

        public string ReducerTypeName { get; }

        public string DispatcherTypeName { get; }

        public DispatcherConstructorKind DispatcherConstructorKind { get; }

        public bool HasInitial { get; }

        public bool HasStringCreateInitial { get; }
    }

    private sealed class ViewInfo
    {
        public ViewInfo(INamedTypeSymbol viewType, INamedTypeSymbol viewModelType, bool requiresRegistry)
        {
            ViewTypeName = Format(viewType);
            ViewModelTypeName = Format(viewModelType);
            RequiresRegistry = requiresRegistry;
            ViewModelType = viewModelType;
        }

        public string ViewTypeName { get; }

        public string ViewModelTypeName { get; }

        public bool RequiresRegistry { get; }

        public INamedTypeSymbol ViewModelType { get; }
    }

    private sealed class ServiceInfo
    {
        public ServiceInfo(INamedTypeSymbol serviceType, INamedTypeSymbol implementationType)
        {
            ServiceTypeName = Format(serviceType);
            ImplementationTypeName = Format(implementationType);
        }

        public string ServiceTypeName { get; }

        public string ImplementationTypeName { get; }
    }

    private enum DispatcherConstructorKind
    {
        Unsupported,
        Parameterless,
        Mediator,
        Login
    }
}
