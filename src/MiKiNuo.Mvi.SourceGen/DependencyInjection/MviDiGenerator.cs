using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MiKiNuo.Mvi.SourceGen.DependencyInjection;

/// <summary>
/// 生成 MVI 依赖注入容器。
/// </summary>
[Generator]
public sealed class MviDiGenerator : IIncrementalGenerator
{
    private const string ServiceAttributeMetadataName = "MiKiNuo.Mvi.Abstractions.DependencyInjection.MviServiceAttribute";

    private static readonly DiagnosticDescriptor UnresolvedDependencyDescriptor = new(
        "MVIDI0001",
        "Unresolved generated DI dependency",
        "Service '{0}' depends on unregistered service '{1}'",
        "MiKiNuo.Mvi.DependencyInjection",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DuplicateRegistrationDescriptor = new(
        "MVIDI0002",
        "Duplicate generated DI service registration",
        "Service '{0}' has multiple generated DI registrations",
        "MiKiNuo.Mvi.DependencyInjection",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor AmbiguousConstructorDescriptor = new(
        "MVIDI0003",
        "Ambiguous generated DI constructor selection",
        "Implementation '{0}' has multiple public constructors with the same selected arity",
        "MiKiNuo.Mvi.DependencyInjection",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor CircularDependencyDescriptor = new(
        "MVIDI0004",
        "Circular generated DI dependency",
        "Generated DI service graph contains a cycle: {0}",
        "MiKiNuo.Mvi.DependencyInjection",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var services = context.SyntaxProvider.ForAttributeWithMetadataName(
            ServiceAttributeMetadataName,
            static (node, _) => node is ClassDeclarationSyntax,
            static (generatorContext, _) => ServiceModel.From(generatorContext))
            .Where(static model => model is not null)
            .Collect();

        context.RegisterSourceOutput(services, static (sourceProductionContext, models) =>
        {
            var grouped = models
                .Where(model => model is not null)
                .Cast<ServiceModel>()
                .GroupBy(model => model.NamespaceName);

            foreach (var group in grouped)
            {
                var servicesForNamespace = group
                    .OrderBy(model => model.ServiceMethodSuffix, StringComparer.Ordinal)
                    .ToArray();
                if (servicesForNamespace.Length == 0)
                {
                    continue;
                }

                if (!ValidateServices(sourceProductionContext, servicesForNamespace))
                {
                    continue;
                }

                sourceProductionContext.AddSource(
                    "GeneratedAppContainer.g.cs",
                    SourceText.From(RenderContainer(group.Key, servicesForNamespace), Encoding.UTF8));
                sourceProductionContext.AddSource(
                    "GeneratedMviScope.g.cs",
                    SourceText.From(RenderScope(group.Key, servicesForNamespace), Encoding.UTF8));
            }
        });
    }

    private static bool ValidateServices(SourceProductionContext context, IReadOnlyList<ServiceModel> services)
    {
        var isValid = true;

        foreach (var duplicateGroup in services.GroupBy(service => service.ServiceTypeKey, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            var first = duplicateGroup.First();
            context.ReportDiagnostic(Diagnostic.Create(
                DuplicateRegistrationDescriptor,
                first.Location,
                first.ServiceTypeName));
            isValid = false;
        }

        foreach (var service in services.Where(service => service.HasAmbiguousConstructors))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                AmbiguousConstructorDescriptor,
                service.Location,
                service.ImplementationTypeName));
            isValid = false;
        }

        if (!isValid)
        {
            return false;
        }

        var servicesByType = services.ToDictionary(service => service.ServiceTypeKey, StringComparer.Ordinal);

        foreach (var service in services)
        {
            foreach (var parameterTypeName in service.ConstructorParameterTypeNames)
            {
                if (servicesByType.ContainsKey(parameterTypeName))
                {
                    continue;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    UnresolvedDependencyDescriptor,
                    service.Location,
                    service.ServiceTypeName,
                    parameterTypeName));
                isValid = false;
            }
        }

        if (!isValid)
        {
            return false;
        }

        var cycle = FindCycle(services, servicesByType);
        if (cycle is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                CircularDependencyDescriptor,
                cycle.Start.Location,
                string.Join(" -> ", cycle.Path)));
            return false;
        }

        return true;
    }

    private static CycleResult? FindCycle(IReadOnlyList<ServiceModel> services, IReadOnlyDictionary<string, ServiceModel> servicesByType)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);

        foreach (var service in services)
        {
            var result = Visit(service, servicesByType, visited, new List<ServiceModel>());
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    private static CycleResult? Visit(
        ServiceModel service,
        IReadOnlyDictionary<string, ServiceModel> servicesByType,
        HashSet<string> visited,
        List<ServiceModel> stack)
    {
        var existingIndex = stack.FindIndex(item => string.Equals(item.ServiceTypeKey, service.ServiceTypeKey, StringComparison.Ordinal));
        if (existingIndex >= 0)
        {
            var cycleServices = stack.Skip(existingIndex).Concat(new[] { service }).ToArray();
            return new CycleResult(
                stack[existingIndex],
                cycleServices.Select(item => item.ServiceTypeName).ToArray());
        }

        if (!visited.Add(service.ServiceTypeKey))
        {
            return null;
        }

        stack.Add(service);
        foreach (var parameterTypeName in service.ConstructorParameterTypeNames)
        {
            if (!servicesByType.TryGetValue(parameterTypeName, out var dependency))
            {
                continue;
            }

            var result = Visit(dependency, servicesByType, visited, stack);
            if (result is not null)
            {
                return result;
            }
        }

        stack.RemoveAt(stack.Count - 1);
        return null;
    }

    private sealed class CycleResult
    {
        public CycleResult(ServiceModel start, IReadOnlyList<string> path)
        {
            Start = start;
            Path = path;
        }

        public ServiceModel Start { get; }

        public IReadOnlyList<string> Path { get; }
    }

    private static string RenderContainer(string namespaceName, IReadOnlyList<ServiceModel> services)
    {
        var servicesByType = services.ToDictionary(service => service.ServiceTypeKey, StringComparer.Ordinal);
        var singletonServices = services.Where(service => service.Lifetime == ServiceLifetime.Singleton).ToArray();
        var singletonFields = string.Join("\n", singletonServices.Select(service => $"    private {service.ServiceTypeName}? singleton{service.ServiceMethodSuffix};"));
        var singletonMethods = string.Join("\n\n", singletonServices.Select(service => RenderSingletonResolveMethod(service, servicesByType)));
        var disposeExpression = RenderDisposalExpression(singletonServices.Select(service => $"singleton{service.ServiceMethodSuffix}"));
        return $$"""
            // <auto-generated />
            #nullable enable

            using System.Threading.Tasks;
            using MiKiNuo.Mvi.Core.DependencyInjection;

            namespace {{namespaceName}};

            /// <summary>
            /// Auto：由 MiKiNuo.Mvi.SourceGen 自动生成，请勿手动修改。
            /// </summary>
            public sealed partial class GeneratedAppContainer : IMviServiceContainer
            {
            {{singletonFields}}

                /// <summary>
                /// 创建新的 MVI 服务作用域。
                /// </summary>
                public IMviServiceScope CreateScope()
                {
                    return new GeneratedMviScope(this);
                }

            {{singletonMethods}}

                /// <inheritdoc />
                public ValueTask DisposeAsync()
                {
                    return {{disposeExpression}};
                }
            }
            """;
    }

    private static string RenderScope(string namespaceName, IReadOnlyList<ServiceModel> services)
    {
        var servicesByType = services.ToDictionary(service => service.ServiceTypeKey, StringComparer.Ordinal);
        var scopedServices = services.Where(service => service.Lifetime == ServiceLifetime.Scoped).ToArray();
        var scopedFields = string.Join("\n", scopedServices.Select(service => $"    private {service.ServiceTypeName}? scoped{service.ServiceMethodSuffix};"));
        var resolveMethods = string.Join("\n\n", services.Select(service => RenderResolveMethod(service, servicesByType)));
        var disposeExpression = RenderDisposalExpression(scopedServices.Select(service => $"scoped{service.ServiceMethodSuffix}"));
        return $$"""
            // <auto-generated />
            #nullable enable

            using System.Threading.Tasks;
            using MiKiNuo.Mvi.Core.DependencyInjection;

            namespace {{namespaceName}};

            /// <summary>
            /// Auto：由 MiKiNuo.Mvi.SourceGen 自动生成，请勿手动修改。
            /// </summary>
            public sealed partial class GeneratedMviScope : IMviServiceScope
            {
                private readonly GeneratedAppContainer container;
            {{scopedFields}}

                /// <summary>
                /// 初始化 MVI 服务作用域。
                /// </summary>
                public GeneratedMviScope(GeneratedAppContainer container)
                {
                    this.container = container;
                }

            {{resolveMethods}}

                /// <inheritdoc />
                public ValueTask DisposeAsync()
                {
                    return {{disposeExpression}};
                }
            }
            """;
    }

    private static string RenderDisposalExpression(IEnumerable<string> fieldNames)
    {
        var fields = fieldNames.ToArray();
        return fields.Length == 0
            ? "default"
            : $"MviGeneratedDisposal.DisposeAsync(new object?[] {{ {string.Join(", ", fields)} }})";
    }

    private static string RenderSingletonResolveMethod(ServiceModel service, IReadOnlyDictionary<string, ServiceModel> servicesByType)
    {
        return $$"""
                /// <summary>
                /// 解析单例 {{service.ServiceTypeName}}。
                /// </summary>
                internal {{service.ServiceTypeName}} ResolveSingleton{{service.ServiceMethodSuffix}}()
                {
                    return singleton{{service.ServiceMethodSuffix}} ??= {{RenderConstructionExpression(service, servicesByType, ResolveContext.Container)}};
                }
            """;
    }

    private static string RenderResolveMethod(ServiceModel service, IReadOnlyDictionary<string, ServiceModel> servicesByType)
    {
        if (service.Lifetime == ServiceLifetime.Singleton)
        {
            return $$"""
                /// <summary>
                /// 解析 {{service.ServiceTypeName}}。
                /// </summary>
                public {{service.ServiceTypeName}} Resolve{{service.ServiceMethodSuffix}}()
                {
                    return container.ResolveSingleton{{service.ServiceMethodSuffix}}();
                }
            """;
        }

        if (service.Lifetime == ServiceLifetime.Scoped)
        {
            return $$"""
                /// <summary>
                /// 解析 {{service.ServiceTypeName}}。
                /// </summary>
                public {{service.ServiceTypeName}} Resolve{{service.ServiceMethodSuffix}}()
                {
                    return scoped{{service.ServiceMethodSuffix}} ??= {{RenderConstructionExpression(service, servicesByType, ResolveContext.Scope)}};
                }
            """;
        }

        return $$"""
                /// <summary>
                /// 解析 {{service.ServiceTypeName}}。
                /// </summary>
                public {{service.ServiceTypeName}} Resolve{{service.ServiceMethodSuffix}}()
                {
                    return {{RenderConstructionExpression(service, servicesByType, ResolveContext.Scope)}};
                }
            """;
    }

    private static string RenderConstructionExpression(ServiceModel service, IReadOnlyDictionary<string, ServiceModel> servicesByType, ResolveContext context)
    {
        var arguments = string.Join(", ", service.ConstructorParameterTypeNames.Select(typeName =>
        {
            if (!servicesByType.TryGetValue(typeName, out var dependency))
            {
                return $"default({typeName})!";
            }

            if (context == ResolveContext.Container && dependency.Lifetime == ServiceLifetime.Singleton)
            {
                return $"ResolveSingleton{dependency.ServiceMethodSuffix}()";
            }

            return $"Resolve{dependency.ServiceMethodSuffix}()";
        }));
        return $"new {service.ImplementationTypeName}({arguments})";
    }

    private enum ResolveContext
    {
        Scope,
        Container,
    }

    private enum ServiceLifetime
    {
        Transient = 0,
        Scoped = 1,
        Singleton = 2,
    }

    private sealed class ServiceModel
    {
        public ServiceModel(
            string namespaceName,
            string implementationTypeName,
            string serviceTypeName,
            string serviceTypeKey,
            string serviceMethodSuffix,
            ServiceLifetime lifetime,
            IReadOnlyList<string> constructorParameterTypeNames,
            bool hasAmbiguousConstructors,
            Location? location)
        {
            NamespaceName = namespaceName;
            ImplementationTypeName = implementationTypeName;
            ServiceTypeName = serviceTypeName;
            ServiceTypeKey = serviceTypeKey;
            ServiceMethodSuffix = serviceMethodSuffix;
            Lifetime = lifetime;
            ConstructorParameterTypeNames = constructorParameterTypeNames;
            HasAmbiguousConstructors = hasAmbiguousConstructors;
            Location = location;
        }

        public string NamespaceName { get; }

        public string ImplementationTypeName { get; }

        public string ServiceTypeName { get; }

        public string ServiceTypeKey { get; }

        public string ServiceMethodSuffix { get; }

        public ServiceLifetime Lifetime { get; }

        public IReadOnlyList<string> ConstructorParameterTypeNames { get; }

        public bool HasAmbiguousConstructors { get; }

        public Location? Location { get; }

        public static ServiceModel? From(GeneratorAttributeSyntaxContext context)
        {
            if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
            {
                return null;
            }

            var attribute = context.Attributes.FirstOrDefault(attributeData =>
                attributeData.AttributeClass?.ToDisplayString() == ServiceAttributeMetadataName);
            if (attribute?.ConstructorArguments.Length != 2 ||
                attribute.ConstructorArguments[0].Value is not INamedTypeSymbol serviceType)
            {
                return null;
            }

            var lifetime = attribute.ConstructorArguments[1].Value is int lifetimeValue
                ? (ServiceLifetime)lifetimeValue
                : ServiceLifetime.Transient;
            var publicConstructors = typeSymbol.Constructors
                .Where(constructor => constructor.DeclaredAccessibility == Accessibility.Public)
                .ToArray();
            var selectedConstructor = publicConstructors
                .OrderByDescending(constructor => constructor.Parameters.Length)
                .FirstOrDefault();
            var hasAmbiguousConstructors = false;
            if (selectedConstructor is not null)
            {
                hasAmbiguousConstructors = publicConstructors.Count(constructor => constructor.Parameters.Length == selectedConstructor.Parameters.Length) > 1;
            }

            var constructorParameterTypeNames = selectedConstructor
                ?.Parameters
                .Select(parameter => GetTypeName(parameter.Type, typeSymbol))
                .ToArray() ?? Array.Empty<string>();

            return new ServiceModel(
                GetNamespaceName(typeSymbol),
                GetTypeName(typeSymbol, typeSymbol),
                GetTypeName(serviceType, typeSymbol),
                GetTypeName(serviceType, typeSymbol),
                serviceType.Name,
                lifetime,
                constructorParameterTypeNames,
                hasAmbiguousConstructors,
                typeSymbol.Locations.FirstOrDefault());
        }
    }

    private static string GetNamespaceName(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : typeSymbol.ContainingNamespace.ToDisplayString();
    }

    private static string GetTypeName(ITypeSymbol typeSymbol, INamedTypeSymbol containingType)
    {
        if (SymbolEqualityComparer.Default.Equals(typeSymbol.ContainingNamespace, containingType.ContainingNamespace))
        {
            return typeSymbol.Name;
        }

        return typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", string.Empty);
    }
}
