using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MiKiNuo.Mvi.Samples.Avalonia.BuildTime.SourceGeneration;

public sealed partial class AvaloniaSampleDiContainerGenerator
{
    private sealed class GenerationModel
    {
        private readonly Dictionary<string, FeatureInfo> _featuresByName;

        public GenerationModel(
            string assemblyName,
            IReadOnlyList<FeatureInfo> features,
            IReadOnlyList<ViewInfo> views,
            ServiceInfo? authService,
            INamedTypeSymbol? loginNavigationService,
            INamedTypeSymbol? dashboardPageFactory,
            INamedTypeSymbol? shellPageFactory,
            CardStoreFactoryInfo? cardStoreFactory,
            INamedTypeSymbol? sampleGreetingViewModelType)
        {
            CompositionNamespace = assemblyName + ".Composition";
            Features = features;
            Views = views;
            AuthService = authService;
            LoginNavigationService = loginNavigationService;
            DashboardPageFactory = dashboardPageFactory;
            ShellPageFactory = shellPageFactory;
            CardStoreFactory = cardStoreFactory;
            SampleGreetingViewModelType = sampleGreetingViewModelType;
            _featuresByName = features.ToDictionary(static feature => feature.BaseName, StringComparer.Ordinal);
        }

        public string CompositionNamespace { get; }

        public IReadOnlyList<FeatureInfo> Features { get; }

        public IReadOnlyList<ViewInfo> Views { get; }

        public ServiceInfo? AuthService { get; }

        public INamedTypeSymbol? LoginNavigationService { get; }

        /// <summary>
        /// 表示由源生成器自动发现的 <c>IDashboardPageFactory</c> 接口。
        /// 容器在实现 <c>IMviViewRegistry</c> 的同时也会实现该接口，
        /// 把 <c>menuKey</c> 解析为具体的顶层页面 ViewModel（不依赖子 VM 在父 VM 的 State 中持有）。
        /// </summary>
        public INamedTypeSymbol? DashboardPageFactory { get; }

        /// <summary>
        /// 表示由源生成器自动发现的 <c>IShellPageFactory</c> 接口。
        /// 容器直接实现该接口，把 <c>ShellPageKeys</c> 判别器解析为具体顶层页面 ViewModel，
        /// 避免 <c>AppShellState</c> 在自身字段中持有页面 VM 引用。
        /// </summary>
        public INamedTypeSymbol? ShellPageFactory { get; }

        public CardStoreFactoryInfo? CardStoreFactory { get; }

        /// <summary>
        /// 表示 <c>SampleGreetingViewModel</c> 类型，用于 <c>CreateWith</c> 测试场景。
        /// </summary>
        public INamedTypeSymbol? SampleGreetingViewModelType { get; }

        public FeatureInfo? LoginFeature => GetFeature("Login");

        public FeatureInfo? ShellFeature => GetFeature("AppShell");

        public FeatureInfo? EventBindingWorkbenchFeature => GetFeature("EventBindingWorkbench");

        public FeatureInfo? GetFeature(string baseName)
        {
            return _featuresByName.TryGetValue(baseName, out FeatureInfo? feature)
                ? feature
                : null;
        }

        /// <summary>返回 <c>IDashboardPageFactory</c> 的源生成器全限定名（用于类声明与 Resolve 分支）。</summary>
        public string? DashboardPageFactoryTypeName =>
            DashboardPageFactory is null ? null : Format(DashboardPageFactory);

        /// <summary>返回 <c>IShellPageFactory</c> 的源生成器全限定名（用于类声明与 Resolve 分支）。</summary>
        public string? ShellPageFactoryTypeName =>
            ShellPageFactory is null ? null : Format(ShellPageFactory);
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
            INamedTypeSymbol? dispatcherType,
            DispatcherConstructorKind dispatcherConstructorKind,
            bool hasInitial,
            bool hasStringCreateInitial,
            INamedTypeSymbol? intentHandlerType = null)
        {
            BaseName = baseName;
            ViewModelType = viewModelType;
            ViewModelTypeName = Format(viewModelType);
            StateTypeName = Format(stateType);
            IntentTypeName = Format(intentType);
            EffectTypeName = Format(effectType);
            ReducerTypeName = Format(reducerType);
            DispatcherTypeName = dispatcherType is null ? null : Format(dispatcherType);
            DispatcherConstructorKind = dispatcherConstructorKind;
            HasInitial = hasInitial;
            HasStringCreateInitial = hasStringCreateInitial;
            IntentHandlerTypeName = intentHandlerType is not null ? Format(intentHandlerType) : null;
            IsUnitEffect = effectType.Name == "UnitEffect";
        }

        public string BaseName { get; }

        public INamedTypeSymbol ViewModelType { get; }

        public string ViewModelTypeName { get; }

        public string StateTypeName { get; }

        public string IntentTypeName { get; }

        public string EffectTypeName { get; }

        public string ReducerTypeName { get; }

        /// <summary>EffectDispatcher 类型完整限定名；UnitEffect Feature 为 null。</summary>
        public string? DispatcherTypeName { get; }

        public DispatcherConstructorKind DispatcherConstructorKind { get; }

        public bool HasInitial { get; }

        public bool HasStringCreateInitial { get; }

        /// <summary>意图处理器类型完整限定名；为 null 表示无意图处理器。</summary>
        public string? IntentHandlerTypeName { get; }

        /// <summary>是否为 UnitEffect（无副作用）Feature。</summary>
        public bool IsUnitEffect { get; }
    }

    private sealed class ViewInfo
    {
        public ViewInfo(INamedTypeSymbol viewType, INamedTypeSymbol viewModelType, ViewConstructorKind constructorKind)
        {
            ViewTypeName = Format(viewType);
            ViewModelTypeName = Format(viewModelType);
            ConstructorKind = constructorKind;
            ViewModelType = viewModelType;
        }

        public string ViewTypeName { get; }

        public string ViewModelTypeName { get; }

        public ViewConstructorKind ConstructorKind { get; }

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

    /// <summary>
    /// 表示 Avalonia View 构造函数的形态（影响源生成器如何调用 new View(...)）。
    /// </summary>
    private enum ViewConstructorKind
    {
        /// <summary>无参构造函数（View 自取依赖）。</summary>
        Parameterless,

        /// <summary>仅需 <c>IMviViewRegistry</c>（递归嵌入场景）。</summary>
        Registry,

        /// <summary>需要 <c>IMviViewRegistry</c> 与 <c>CardStoreFactory</c>（数据流节点页等）。</summary>
        RegistryAndCardStoreFactory,
    }
}
