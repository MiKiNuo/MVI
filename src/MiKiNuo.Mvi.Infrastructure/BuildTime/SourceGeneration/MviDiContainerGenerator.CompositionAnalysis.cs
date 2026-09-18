using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MiKiNuo.Mvi.Infrastructure.BuildTime.Diagnostics;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;

/// <summary>
/// 表示 <see cref="MviDiContainerGenerator"/> 的 [MviComposition] 组合装配分析部分。
/// </summary>
public sealed partial class MviDiContainerGenerator
{
    /// <summary>
    /// 表示组合装配分析阶段：
    /// 从标记 [MviComposition] 的声明类出发，解析成员 Feature，
    /// 发现路由处理器与通知接纳器，并按唯一提供方规则推导请求绑定。
    /// </summary>
    internal static class CompositionAnalysis
    {
        private const string CompositionAttributeMetadataName =
            "MiKiNuo.Mvi.Domain.DI.MviCompositionAttribute";

        private const string RouteHandlerAttributeName =
            "MiKiNuo.Mvi.Domain.MVI.Mediator.MviRouteHandlerAttribute";

        private const string NotificationAcceptorAttributeName =
            "MiKiNuo.Mvi.Domain.MVI.Mediator.MviNotificationAcceptorAttribute";

        private const string RequestInterfaceMetadataName =
            "MiKiNuo.Mvi.Domain.MVI.Mediator.IMviRequest`1";

        private const string ValueTaskMetadataName =
            "System.Threading.Tasks.ValueTask<TResult>";

        private static readonly DiagnosticDescriptor RouteAmbiguousRule = new(
            DiagnosticIdCatalog.MviCompositionRouteAmbiguous,
            "组合内请求契约存在多个提供方",
            "组合“{0}”内请求契约“{1}”存在多个提供方，拒绝自动绑定。",
            "MviComposition", DiagnosticSeverity.Error, true);

        private static readonly DiagnosticDescriptor RouteMissingRule = new(
            DiagnosticIdCatalog.MviCompositionRouteMissing,
            "组合内请求契约缺少提供方",
            "组合“{0}”内请求契约“{1}”存在消费方但未找到提供方，请求将无路由可达。",
            "MviComposition", DiagnosticSeverity.Error, true);

        private static readonly DiagnosticDescriptor HandlerInvalidRule = new(
            DiagnosticIdCatalog.MviCompositionHandlerInvalid,
            "组合接线方法签名或可见性非法",
            "组合接线方法“{0}”非法：路由处理器须为 (TRequest, CancellationToken) => ValueTask<TResponse>，接纳器须为 (TNotification) => void，可见性为 internal 或 public。",
            "MviComposition", DiagnosticSeverity.Error, true);

        private static readonly DiagnosticDescriptor NotPartialRule = new(
            DiagnosticIdCatalog.MviCompositionNotPartial,
            "组合声明类未标记 partial 修饰符",
            "组合声明类“{0}”必须标记 partial 修饰符，否则源生成器无法 emit 组合句柄。",
            "MviComposition", DiagnosticSeverity.Error, true);

        private static readonly DiagnosticDescriptor MemberUnknownRule = new(
            DiagnosticIdCatalog.MviCompositionMemberUnknown,
            "组合成员不是已发现的 Feature",
            "组合“{0}”的成员“{1}”不是已发现的 [MviFeature] Reducer（或缺少匹配 ViewModel），已跳过该成员。",
            "MviComposition", DiagnosticSeverity.Error, true);

        /// <summary>
        /// 收集全部组合装配模型。
        /// </summary>
        /// <param name="declarationSymbols">标记 [MviComposition] 的声明类符号集合。</param>
        /// <param name="features">已发现的 Feature 装配模型集合。</param>
        /// <param name="compilation">当前编译对象。</param>
        /// <param name="context">源生成上下文。</param>
        /// <returns>组合装配模型集合。</returns>
        public static IReadOnlyList<Models.CompositionInfo> CollectCompositions(
            IEnumerable<INamedTypeSymbol> declarationSymbols,
            IReadOnlyList<Models.MviFeatureInfo> features,
            Compilation compilation,
            SourceProductionContext context)
        {
            List<Models.CompositionInfo> compositions = new();
            HashSet<INamedTypeSymbol> seen = new(SymbolEqualityComparer.Default);
            foreach (INamedTypeSymbol declaration in declarationSymbols)
            {
                if (!seen.Add(declaration))
                {
                    continue;
                }

                Models.CompositionInfo? composition = CollectComposition(
                    declaration,
                    features,
                    compilation,
                    context);
                if (composition is not null)
                {
                    compositions.Add(composition);
                }
            }

            return compositions;
        }

        private static Models.CompositionInfo? CollectComposition(
            INamedTypeSymbol declaration,
            IReadOnlyList<Models.MviFeatureInfo> features,
            Compilation compilation,
            SourceProductionContext context)
        {
            AttributeData? compositionAttribute = declaration.GetAttributes().FirstOrDefault(candidate =>
                candidate.AttributeClass?.ToDisplayString() == CompositionAttributeMetadataName);
            if (compositionAttribute is null
                || compositionAttribute.ConstructorArguments.Length != 1
                || compositionAttribute.ConstructorArguments[0].Values.IsDefaultOrEmpty)
            {
                return null;
            }

            bool isPartial = declaration.DeclaringSyntaxReferences
                .Select(reference => reference.GetSyntax(context.CancellationToken))
                .OfType<ClassDeclarationSyntax>()
                .Any(syntax => syntax.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword)));
            if (!isPartial)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    NotPartialRule,
                    declaration.Locations.FirstOrDefault(),
                    declaration.Name));
                return null;
            }

            List<Models.CompositionMemberInfo> members = new();
            HashSet<string> usedVariableNames = new(System.StringComparer.Ordinal);
            HashSet<string> usedPropertyNames = new(System.StringComparer.Ordinal);
            foreach (TypedConstant memberConstant in compositionAttribute.ConstructorArguments[0].Values)
            {
                if (memberConstant.Value is not INamedTypeSymbol reducerType)
                {
                    continue;
                }

                string reducerTypeName = reducerType.ToDisplayString(GeneratorSyntaxHelpers.FullyQualifiedNullableFormat);
                Models.MviFeatureInfo? feature = features.FirstOrDefault(candidate =>
                    candidate.Reducer.TypeName == reducerTypeName);
                if (feature is null || feature.ViewModel is null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        MemberUnknownRule,
                        declaration.Locations.FirstOrDefault(),
                        declaration.Name,
                        reducerType.ToDisplayString()));
                    continue;
                }

                members.Add(new Models.CompositionMemberInfo(
                    feature,
                    DeduplicateName(ToCamelCase(feature.FeatureName), usedVariableNames),
                    DeduplicateName(feature.FeatureName, usedPropertyNames)));
            }

            if (members.Count == 0)
            {
                return null;
            }

            List<RouteHandlerCandidate> routeHandlers = new();
            List<Models.CompositionSubscriptionInfo> subscriptions = new();
            List<(Models.CompositionMemberInfo Member, INamedTypeSymbol RequestType)> consumers = new();
            INamedTypeSymbol? requestInterface = compilation.GetTypeByMetadataName(RequestInterfaceMetadataName);

            foreach (Models.CompositionMemberInfo member in members)
            {
                if (member.Feature.EffectDispatcher is null)
                {
                    continue;
                }

                INamedTypeSymbol? dispatcherSymbol = compilation.GetTypeByMetadataName(
                    member.Feature.EffectDispatcher.TypeName.Replace("global::", string.Empty));
                if (dispatcherSymbol is null)
                {
                    continue;
                }

                CollectDispatcherDeclarations(member, dispatcherSymbol, routeHandlers, subscriptions, context);
                if (requestInterface is not null)
                {
                    CollectConsumedRequests(member, dispatcherSymbol, compilation, requestInterface, context.CancellationToken, consumers);
                }
            }

            List<Models.CompositionRouteInfo> routes = BuildRoutes(
                routeHandlers,
                consumers,
                declaration,
                context);
            return new Models.CompositionInfo(
                declaration.Name,
                declaration.ToDisplayString(GeneratorSyntaxHelpers.FullyQualifiedNullableFormat),
                members,
                routes,
                subscriptions);
        }

        private static void CollectDispatcherDeclarations(
            Models.CompositionMemberInfo member,
            INamedTypeSymbol dispatcherSymbol,
            List<RouteHandlerCandidate> routeHandlers,
            List<Models.CompositionSubscriptionInfo> subscriptions,
            SourceProductionContext context)
        {
            foreach (IMethodSymbol method in dispatcherSymbol.GetMembers().OfType<IMethodSymbol>())
            {
                foreach (AttributeData attribute in method.GetAttributes())
                {
                    string? attributeName = attribute.AttributeClass?.ToDisplayString();
                    if (attributeName == RouteHandlerAttributeName
                        && attribute.ConstructorArguments.Length == 1
                        && attribute.ConstructorArguments[0].Value is INamedTypeSymbol requestType)
                    {
                        if (!IsValidRouteHandler(method, requestType, out INamedTypeSymbol? responseType))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                HandlerInvalidRule,
                                method.Locations.FirstOrDefault(),
                                method.Name));
                            continue;
                        }

                        routeHandlers.Add(new RouteHandlerCandidate(
                            requestType,
                            responseType!,
                            member,
                            method.Name));
                    }
                    else if (attributeName == NotificationAcceptorAttributeName
                        && attribute.ConstructorArguments.Length == 1
                        && attribute.ConstructorArguments[0].Value is INamedTypeSymbol notificationType)
                    {
                        if (!IsValidNotificationAcceptor(method, notificationType))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                HandlerInvalidRule,
                                method.Locations.FirstOrDefault(),
                                method.Name));
                            continue;
                        }

                        subscriptions.Add(new Models.CompositionSubscriptionInfo(
                            notificationType.ToDisplayString(GeneratorSyntaxHelpers.FullyQualifiedNullableFormat),
                            member,
                            method.Name));
                    }
                }
            }
        }

        private static bool IsValidRouteHandler(
            IMethodSymbol method,
            INamedTypeSymbol requestType,
            out INamedTypeSymbol? responseType)
        {
            responseType = null;
            if (!IsExternallyCallable(method)
                || method.Parameters.Length != 2
                || !method.Parameters[0].Type.Equals(requestType, SymbolEqualityComparer.Default)
                || method.Parameters[1].Type.ToDisplayString() != "System.Threading.CancellationToken")
            {
                return false;
            }

            return TryGetResponseType(method, out responseType);
        }

        private static bool IsValidNotificationAcceptor(IMethodSymbol method, INamedTypeSymbol notificationType)
        {
            return IsExternallyCallable(method)
                && method.ReturnsVoid
                && method.Parameters.Length == 1
                && method.Parameters[0].Type.Equals(notificationType, SymbolEqualityComparer.Default);
        }

        private static bool IsExternallyCallable(IMethodSymbol method)
        {
            return method.DeclaredAccessibility == Accessibility.Internal
                || method.DeclaredAccessibility == Accessibility.Public;
        }

        private static bool TryGetResponseType(IMethodSymbol method, out INamedTypeSymbol? responseType)
        {
            responseType = null;
            if (method.ReturnType is not INamedTypeSymbol returnType
                || returnType.OriginalDefinition.ToDisplayString() != ValueTaskMetadataName
                || returnType.TypeArguments.Length != 1
                || returnType.TypeArguments[0] is not INamedTypeSymbol namedResponse)
            {
                return false;
            }

            responseType = namedResponse;
            return true;
        }

        private static void CollectConsumedRequests(
            Models.CompositionMemberInfo member,
            INamedTypeSymbol dispatcherSymbol,
            Compilation compilation,
            INamedTypeSymbol requestInterface,
            System.Threading.CancellationToken cancellationToken,
            List<(Models.CompositionMemberInfo Member, INamedTypeSymbol RequestType)> consumers)
        {
            foreach (SyntaxReference reference in dispatcherSymbol.DeclaringSyntaxReferences)
            {
                SemanticModel model = compilation.GetSemanticModel(reference.SyntaxTree);
                SyntaxNode root = reference.GetSyntax(cancellationToken);
                foreach (InvocationExpressionSyntax invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    if (invocation.Expression is not MemberAccessExpressionSyntax access
                        || access.Name.Identifier.Text != "SendAsync"
                        || invocation.ArgumentList.Arguments.Count == 0)
                    {
                        continue;
                    }

                    TypeInfo argumentType = model.GetTypeInfo(
                        invocation.ArgumentList.Arguments[0].Expression,
                        cancellationToken);
                    if (argumentType.Type is INamedTypeSymbol requestType
                        && requestType.AllInterfaces.Any(candidate =>
                            candidate.OriginalDefinition.Equals(requestInterface, SymbolEqualityComparer.Default))
                        && !consumers.Any(existing =>
                            ReferenceEquals(existing.Member, member)
                            && existing.RequestType.Equals(requestType, SymbolEqualityComparer.Default)))
                    {
                        consumers.Add((member, requestType));
                    }
                }
            }
        }

        private static List<Models.CompositionRouteInfo> BuildRoutes(
            List<RouteHandlerCandidate> routeHandlers,
            List<(Models.CompositionMemberInfo Member, INamedTypeSymbol RequestType)> consumers,
            INamedTypeSymbol declaration,
            SourceProductionContext context)
        {
            List<Models.CompositionRouteInfo> routes = new();
            List<INamedTypeSymbol> contractTypes = new();
            foreach (RouteHandlerCandidate candidate in routeHandlers)
            {
                if (!contractTypes.Any(existing => existing.Equals(candidate.RequestType, SymbolEqualityComparer.Default)))
                {
                    contractTypes.Add(candidate.RequestType);
                }
            }

            foreach ((Models.CompositionMemberInfo _, INamedTypeSymbol requestType) in consumers)
            {
                if (!contractTypes.Any(existing => existing.Equals(requestType, SymbolEqualityComparer.Default)))
                {
                    contractTypes.Add(requestType);
                }
            }

            foreach (INamedTypeSymbol contractType in contractTypes)
            {
                List<RouteHandlerCandidate> providers = routeHandlers
                    .Where(candidate => candidate.RequestType.Equals(contractType, SymbolEqualityComparer.Default))
                    .ToList();
                List<Models.CompositionMemberInfo> contractConsumers = consumers
                    .Where(consumer => consumer.RequestType.Equals(contractType, SymbolEqualityComparer.Default))
                    .Select(consumer => consumer.Member)
                    .ToList();
                string contractTypeName = contractType.ToDisplayString(
                    GeneratorSyntaxHelpers.FullyQualifiedNullableFormat);

                if (providers.Count > 1)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        RouteAmbiguousRule,
                        declaration.Locations.FirstOrDefault(),
                        declaration.Name,
                        contractTypeName));
                    continue;
                }

                if (providers.Count == 0)
                {
                    if (contractConsumers.Count > 0)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            RouteMissingRule,
                            declaration.Locations.FirstOrDefault(),
                            declaration.Name,
                            contractTypeName));
                    }

                    continue;
                }

                RouteHandlerCandidate provider = providers[0];
                routes.Add(new Models.CompositionRouteInfo(
                    contractTypeName,
                    provider.ResponseType.ToDisplayString(GeneratorSyntaxHelpers.FullyQualifiedNullableFormat),
                    provider.Member,
                    provider.HandlerMethodName,
                    contractConsumers
                        .Where(consumer => !ReferenceEquals(consumer, provider.Member))
                        .ToList()));
            }

            return routes;
        }

        private static string ToCamelCase(string name)
        {
            return name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name.Substring(1);
        }

        private static string DeduplicateName(string baseName, HashSet<string> usedNames)
        {
            string candidate = baseName;
            int suffix = 2;
            while (!usedNames.Add(candidate))
            {
                candidate = baseName + suffix;
                suffix++;
            }

            return candidate;
        }

        private sealed class RouteHandlerCandidate
        {
            public RouteHandlerCandidate(
                INamedTypeSymbol requestType,
                INamedTypeSymbol responseType,
                Models.CompositionMemberInfo member,
                string handlerMethodName)
            {
                RequestType = requestType;
                ResponseType = responseType;
                Member = member;
                HandlerMethodName = handlerMethodName;
            }

            public INamedTypeSymbol RequestType { get; }

            public INamedTypeSymbol ResponseType { get; }

            public Models.CompositionMemberInfo Member { get; }

            public string HandlerMethodName { get; }
        }
    }
}
