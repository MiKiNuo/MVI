using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using MiKiNuo.Mvi.Infrastructure.BuildTime.Diagnostics;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;

/// <summary>
/// 表示 <see cref="MviDiContainerGenerator"/> 的 [MviFeature] 装配分析部分。
/// </summary>
public sealed partial class MviDiContainerGenerator
{
    /// <summary>
    /// 表示 Feature 装配分析阶段：
    /// 从标记 [MviFeature] 的 Reducer 出发，按类型签名发现 State/Intent/Effect 三件套、
    /// EffectDispatcher、ViewModel 与中间件，生成装配模型。
    /// </summary>
    internal static class FeatureAnalysis
    {
        private static readonly DiagnosticDescriptor MiddlewareOrderRule = new(
            DiagnosticIdCatalog.MviMiddlewareOrderInvalid,
            "中间件执行顺序不明确", "Feature“{0}”的多个中间件必须通过 MviMiddlewareOrder 声明唯一顺序。",
            "MviFeature", DiagnosticSeverity.Error, true);
        private const string ReducerBaseMetadataName =
            "MiKiNuo.Mvi.Application.MVI.Reducer.MviReducerBase<TState, TIntent, TEffect>";

        private const string DispatcherBaseMetadataName =
            "MiKiNuo.Mvi.Application.MVI.Effect.MviEffectDispatcherBase<TIntent, TEffect>";

        private const string ViewModelBaseMetadataName =
            "MiKiNuo.Mvi.Application.MVI.ViewModel.MviViewModelBase<TState, TIntent, TEffect>";

        private const string MiddlewareMetadataName =
            "MiKiNuo.Mvi.Application.MVI.Middleware.IMviMiddleware<TState, TIntent, TEffect>";

        private static readonly DiagnosticDescriptor StateInitialMissingRule = new(
            id: DiagnosticIdCatalog.MviFeatureStateInitialMissing,
            title: "Feature 状态类型缺少公开静态 Initial 属性",
            messageFormat: "Feature 状态类型“{0}”缺少公开静态 Initial 属性，生成的容器无法构造 Store，已跳过该 Feature 装配。",
            category: "MviFeature",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>多个匹配分发器的装配错误。</summary>
        private static readonly DiagnosticDescriptor DispatcherAmbiguousRule = new(
            DiagnosticIdCatalog.MviFeatureDispatcherAmbiguous,
            "Feature 匹配多个副作用分发器",
            "Feature“{0}”匹配多个 EffectDispatcher：{1}。请保留唯一候选，已跳过该 Feature 装配。",
            "MviFeature", DiagnosticSeverity.Error, true);

        /// <summary>多个匹配视图模型的装配错误。</summary>
        private static readonly DiagnosticDescriptor ViewModelAmbiguousRule = new(
            DiagnosticIdCatalog.MviFeatureViewModelAmbiguous,
            "Feature 匹配多个视图模型",
            "Feature“{0}”匹配多个 ViewModel：{1}。请保留唯一候选，已跳过该 Feature 装配。",
            "MviFeature", DiagnosticSeverity.Error, true);

        /// <summary>
        /// 收集全部 Feature 装配模型。
        /// </summary>
        /// <param name="reducerSymbols">标记 [MviFeature] 的 Reducer 类型符号集合。</param>
        /// <param name="compilation">当前编译对象。</param>
        /// <param name="context">源生成上下文。</param>
        /// <returns>Feature 装配模型集合。</returns>
        public static IReadOnlyList<Models.MviFeatureInfo> CollectFeatures(
            IEnumerable<INamedTypeSymbol> reducerSymbols,
            Compilation compilation,
            SourceProductionContext context)
        {
            List<Models.MviFeatureInfo> features = new();
            List<INamedTypeSymbol> allClasses = GeneratorSyntaxHelpers
                .EnumerateClassSymbols(compilation, context.CancellationToken)
                .ToList();

            HashSet<INamedTypeSymbol> seen = new(SymbolEqualityComparer.Default);
            foreach (INamedTypeSymbol reducerSymbol in reducerSymbols)
            {
                if (!seen.Add(reducerSymbol))
                {
                    continue;
                }

                Models.MviFeatureInfo? feature = CollectFeature(
                    reducerSymbol,
                    allClasses,
                    context);
                if (feature is not null)
                {
                    features.Add(feature);
                }
            }

            return features;
        }

        private static Models.MviFeatureInfo? CollectFeature(
            INamedTypeSymbol reducerSymbol,
            IReadOnlyList<INamedTypeSymbol> allClasses,
            SourceProductionContext context)
        {
            INamedTypeSymbol? reducerBase = FindBaseInChain(reducerSymbol, ReducerBaseMetadataName);
            if (reducerBase is null || reducerBase.TypeArguments.Length != 3)
            {
                return null;
            }

            INamedTypeSymbol stateType = (INamedTypeSymbol)reducerBase.TypeArguments[0];
            INamedTypeSymbol intentType = (INamedTypeSymbol)reducerBase.TypeArguments[1];
            INamedTypeSymbol effectType = (INamedTypeSymbol)reducerBase.TypeArguments[2];

            if (!HasStaticInitialProperty(stateType))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    StateInitialMissingRule,
                    reducerSymbol.Locations.FirstOrDefault(),
                    stateType.Name));
                return null;
            }

            string featureName = reducerSymbol.Name.EndsWith("Reducer", System.StringComparison.Ordinal)
                ? reducerSymbol.Name.Substring(0, reducerSymbol.Name.Length - "Reducer".Length)
                : reducerSymbol.Name;

            List<INamedTypeSymbol> dispatchers = allClasses
                .Where(candidate => MatchesDispatcher(candidate, intentType, effectType))
                .ToList();
            if (dispatchers.Count > 1)
            {
                context.ReportDiagnostic(Diagnostic.Create(DispatcherAmbiguousRule,
                    reducerSymbol.Locations.FirstOrDefault(), featureName,
                    string.Join(", ", dispatchers.Select(static candidate => candidate.ToDisplayString()))));
                return null;
            }

            INamedTypeSymbol? dispatcher = dispatchers.SingleOrDefault();

            List<INamedTypeSymbol> viewModels = allClasses
                .Where(candidate => MatchesViewModel(candidate, stateType, intentType, effectType))
                .ToList();
            if (viewModels.Count > 1)
            {
                context.ReportDiagnostic(Diagnostic.Create(ViewModelAmbiguousRule,
                    reducerSymbol.Locations.FirstOrDefault(), featureName,
                    string.Join(", ", viewModels.Select(static candidate => candidate.ToDisplayString()))));
                return null;
            }

            INamedTypeSymbol? viewModel = viewModels.SingleOrDefault();

            List<INamedTypeSymbol> middlewareTypes = allClasses
                .Where(candidate => MatchesMiddleware(candidate, stateType, intentType, effectType))
                .ToList();
            if (middlewareTypes.Count > 1 && (middlewareTypes.Any(candidate => GetMiddlewareOrder(candidate) is null)
                || middlewareTypes.Select(GetMiddlewareOrder).Distinct().Count() != middlewareTypes.Count))
            {
                context.ReportDiagnostic(Diagnostic.Create(MiddlewareOrderRule, reducerSymbol.Locations.FirstOrDefault(), featureName));
                return null;
            }
            List<Models.FeatureComponentInfo> middlewares = middlewareTypes
                .OrderBy(candidate => GetMiddlewareOrder(candidate) ?? 0)
                .Select(static candidate => new Models.FeatureComponentInfo(
                    candidate.ToDisplayString(GeneratorSyntaxHelpers.FullyQualifiedNullableFormat),
                    BuildConstructorParameterTypeNames(candidate)))
                .ToList();

            return new Models.MviFeatureInfo(
                featureName,
                stateType.ToDisplayString(GeneratorSyntaxHelpers.FullyQualifiedNullableFormat),
                intentType.ToDisplayString(GeneratorSyntaxHelpers.FullyQualifiedNullableFormat),
                effectType.ToDisplayString(GeneratorSyntaxHelpers.FullyQualifiedNullableFormat),
                new Models.FeatureComponentInfo(
                    reducerSymbol.ToDisplayString(GeneratorSyntaxHelpers.FullyQualifiedNullableFormat),
                    BuildConstructorParameterTypeNames(reducerSymbol)),
                dispatcher is null
                    ? null
                    : new Models.FeatureComponentInfo(
                        dispatcher.ToDisplayString(GeneratorSyntaxHelpers.FullyQualifiedNullableFormat),
                        BuildConstructorParameterTypeNames(dispatcher)),
                viewModel is null
                    ? null
                    : new Models.FeatureComponentInfo(
                        viewModel.ToDisplayString(GeneratorSyntaxHelpers.FullyQualifiedNullableFormat),
                        BuildConstructorParameterTypeNames(viewModel)),
                middlewares);
        }

        private static bool HasStaticInitialProperty(INamedTypeSymbol stateType)
        {
            foreach (ISymbol member in stateType.GetMembers("Initial"))
            {
                if (member is IPropertySymbol property
                    && property.IsStatic
                    && property.DeclaredAccessibility == Accessibility.Public
                    && property.Type.Equals(stateType, SymbolEqualityComparer.Default))
                {
                    return true;
                }
            }

            return false;
        }

        private static int? GetMiddlewareOrder(INamedTypeSymbol symbol)
        {
            AttributeData? attribute = symbol.GetAttributes().FirstOrDefault(candidate =>
                candidate.AttributeClass?.ToDisplayString() == "MiKiNuo.Mvi.Domain.DI.MviMiddlewareOrderAttribute");
            return attribute is not null && attribute.ConstructorArguments.Length == 1
                && attribute.ConstructorArguments[0].Value is int order ? order : null;
        }

        private static bool MatchesDispatcher(
            INamedTypeSymbol candidate,
            INamedTypeSymbol intentType,
            INamedTypeSymbol effectType)
        {
            INamedTypeSymbol? baseType = FindBaseInChain(candidate, DispatcherBaseMetadataName);
            return baseType is not null
                && baseType.TypeArguments.Length == 2
                && baseType.TypeArguments[0].Equals(intentType, SymbolEqualityComparer.Default)
                && baseType.TypeArguments[1].Equals(effectType, SymbolEqualityComparer.Default);
        }

        private static bool MatchesViewModel(
            INamedTypeSymbol candidate,
            INamedTypeSymbol stateType,
            INamedTypeSymbol intentType,
            INamedTypeSymbol effectType)
        {
            INamedTypeSymbol? baseType = FindBaseInChain(candidate, ViewModelBaseMetadataName);
            return baseType is not null
                && baseType.TypeArguments.Length == 3
                && baseType.TypeArguments[0].Equals(stateType, SymbolEqualityComparer.Default)
                && baseType.TypeArguments[1].Equals(intentType, SymbolEqualityComparer.Default)
                && baseType.TypeArguments[2].Equals(effectType, SymbolEqualityComparer.Default);
        }

        private static bool MatchesMiddleware(
            INamedTypeSymbol candidate,
            INamedTypeSymbol stateType,
            INamedTypeSymbol intentType,
            INamedTypeSymbol effectType)
        {
            if (candidate.IsAbstract || candidate.TypeKind != TypeKind.Class)
            {
                return false;
            }

            foreach (INamedTypeSymbol iface in candidate.AllInterfaces)
            {
                if (iface.OriginalDefinition.ToDisplayString() != MiddlewareMetadataName)
                {
                    continue;
                }

                if (iface.TypeArguments.Length == 3
                    && iface.TypeArguments[0].Equals(stateType, SymbolEqualityComparer.Default)
                    && iface.TypeArguments[1].Equals(intentType, SymbolEqualityComparer.Default)
                    && iface.TypeArguments[2].Equals(effectType, SymbolEqualityComparer.Default))
                {
                    return true;
                }
            }

            return false;
        }

        private static INamedTypeSymbol? FindBaseInChain(
            INamedTypeSymbol symbol,
            string baseMetadataDisplayName)
        {
            INamedTypeSymbol? current = symbol.BaseType;
            while (current is not null)
            {
                if (current.OriginalDefinition.ToDisplayString() == baseMetadataDisplayName)
                {
                    return current;
                }

                current = current.BaseType;
            }

            return null;
        }

        private static IReadOnlyList<string> BuildConstructorParameterTypeNames(INamedTypeSymbol classSymbol)
        {
            IMethodSymbol? selected = classSymbol.Constructors
                .Where(static constructor => constructor.DeclaredAccessibility == Accessibility.Public)
                .OrderByDescending(static constructor => constructor.Parameters.Length)
                .FirstOrDefault();

            if (selected is null || selected.Parameters.Length == 0)
            {
                return System.Array.Empty<string>();
            }

            List<string> parameterTypeNames = new(selected.Parameters.Length);
            foreach (IParameterSymbol parameter in selected.Parameters)
            {
                ITypeSymbol parameterType = parameter.Type;
                if (parameterType.NullableAnnotation == NullableAnnotation.Annotated)
                {
                    parameterType = parameterType.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
                }

                parameterTypeNames.Add(parameterType.ToDisplayString(
                    GeneratorSyntaxHelpers.FullyQualifiedNullableFormat));
            }

            return parameterTypeNames;
        }
    }
}
