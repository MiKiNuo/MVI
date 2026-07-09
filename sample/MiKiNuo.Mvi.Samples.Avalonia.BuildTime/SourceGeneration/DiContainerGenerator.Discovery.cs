using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MiKiNuo.Mvi.Samples.Avalonia.BuildTime.SourceGeneration;

public sealed partial class AvaloniaSampleDiContainerGenerator
{
    private static List<INamedTypeSymbol> DiscoverClasses(
        Compilation compilation,
        System.Threading.CancellationToken cancellationToken)
    {
        List<INamedTypeSymbol> classes = new List<INamedTypeSymbol>();

        foreach (SyntaxTree tree in compilation.SyntaxTrees)
        {
            SyntaxNode root = tree.GetRoot(cancellationToken);
            SemanticModel semanticModel = compilation.GetSemanticModel(tree);

            // 同时查找 class 和 record 声明，record 用于 State/Intent/Effect 等类型定义。
            foreach (TypeDeclarationSyntax typeDeclaration in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken) is INamedTypeSymbol symbol
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

            INamedTypeSymbol? intentHandlerType = FindByMetadataName(
                classesByFullName,
                containingNamespace + "." + baseName + "IntentHandler");
            INamedTypeSymbol? reducerType = FindByMetadataName(
                classesByFullName,
                containingNamespace + "." + baseName + "Reducer");
            INamedTypeSymbol? dispatcherType = FindByMetadataName(
                classesByFullName,
                containingNamespace + "." + baseName + "EffectDispatcher");

            bool isUnitEffect = effectType.Name == "UnitEffect";
            // UnitEffect 表示无副作用，无需自定义 EffectDispatcher；
            // 其他 Feature 仍要求显式 dispatcherType。
            if (reducerType is null || (!isUnitEffect && dispatcherType is null))
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
                HasStringCreateInitial(stateType),
                intentHandlerType));
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
            ViewConstructorKind constructorKind = DetectViewConstructorKind(classSymbol);
            views.Add(new ViewInfo(classSymbol, viewModelType, constructorKind));
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

    /// <summary>
    /// 发现 <c>SampleGreetingViewModel</c> 类型（按名称匹配），用于 <c>CreateWith</c> 测试场景。
    /// </summary>
    private static INamedTypeSymbol? DiscoverSampleGreetingViewModel(
        List<INamedTypeSymbol> classes,
        System.Threading.CancellationToken cancellationToken)
    {
        foreach (INamedTypeSymbol classSymbol in classes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (classSymbol.Name != "SampleGreetingViewModel")
            {
                continue;
            }

            return classSymbol;
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

    private static ViewConstructorKind DetectViewConstructorKind(INamedTypeSymbol viewType)
    {
        foreach (IMethodSymbol constructor in viewType.Constructors)
        {
            if (constructor.DeclaredAccessibility != Accessibility.Public || constructor.Parameters.Length == 0)
            {
                continue;
            }

            bool hasRegistry = constructor.Parameters.Any(static p => p.Type.Name == "IMviViewRegistry");
            bool hasCardStoreFactory = constructor.Parameters.Any(static p => p.Type.Name == "CardStoreFactory");

            if (hasRegistry && hasCardStoreFactory)
            {
                return ViewConstructorKind.RegistryAndCardStoreFactory;
            }

            if (hasRegistry)
            {
                return ViewConstructorKind.Registry;
            }
        }

        return ViewConstructorKind.Parameterless;
    }

    private static DispatcherConstructorKind GetDispatcherConstructorKind(INamedTypeSymbol? dispatcherType)
    {
        if (dispatcherType is null)
        {
            return DispatcherConstructorKind.Unsupported;
        }

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
}
