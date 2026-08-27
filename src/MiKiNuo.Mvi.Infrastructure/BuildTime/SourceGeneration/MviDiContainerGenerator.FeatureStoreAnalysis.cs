using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;

/// <summary>
/// 表示 <see cref="MviDiContainerGenerator"/> 的 Feature Store 发现部分：
/// 扫描 [MviFeatureModule] 标记的 Reducer，解析出可装配的 Feature Store 信息。
/// </summary>
public sealed partial class MviDiContainerGenerator
{
    /// <summary>
    /// 分析阶段的 Feature Store 发现部分。
    /// </summary>
    internal static partial class Analysis
    {
        /// <summary>功能模块状态缺少 static Initial 成员。</summary>
        private static readonly DiagnosticDescriptor FeatureStateMissingInitialRule = new(
            id: Diagnostics.DiagnosticIdCatalog.MviFeatureStateMissingInitial,
            title: "功能模块状态缺少 static Initial 成员",
            messageFormat: "功能模块“{0}”的状态类型“{1}”缺少 public static Initial 成员，无法生成 Store 装配。",
            category: "MviComposition",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>功能模块组件无法唯一解析。</summary>
        private static readonly DiagnosticDescriptor FeatureComponentNotResolvableRule = new(
            id: Diagnostics.DiagnosticIdCatalog.MviFeatureComponentNotResolvable,
            title: "功能模块组件无法唯一解析",
            messageFormat: "功能模块“{0}”的 {1} 未找到唯一具体实现（实际 {2} 个），无法生成 Store 装配。",
            category: "MviComposition",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// 发现所有标记 [MviFeatureModule] 的 Reducer，解析出可装配的 Feature Store 信息。
        /// </summary>
        /// <param name="compilation">编译对象。</param>
        /// <param name="context">源生成上下文。</param>
        /// <returns>可装配的 Feature Store 信息列表。</returns>
        public static List<Models.FeatureStoreInfo> DiscoverFeatures(
            Compilation compilation,
            SourceProductionContext context)
        {
            List<Models.FeatureStoreInfo> result = new();

            INamedTypeSymbol? reducerInterface = compilation.GetTypeByMetadataName(
                "MiKiNuo.Mvi.Application.MVI.Reducer.IMviReducer`3");
            INamedTypeSymbol? handlerInterface = compilation.GetTypeByMetadataName(
                "MiKiNuo.Mvi.Application.MVI.IntentHandler.IMviIntentHandler`3");
            INamedTypeSymbol? dispatcherInterface = compilation.GetTypeByMetadataName(
                "MiKiNuo.Mvi.Application.MVI.Effect.IMviEffectDispatcher`1");

            if (reducerInterface is null || handlerInterface is null || dispatcherInterface is null)
            {
                return result;
            }

            List<INamedTypeSymbol> allTypes = GeneratorSyntaxHelpers
                .EnumerateClassSymbols(compilation, context.CancellationToken)
                .ToList();

            foreach (INamedTypeSymbol classSymbol in allTypes)
            {
                AttributeData? featureAttribute = GeneratorSyntaxHelpers.FindAttribute(
                    classSymbol,
                    "MviFeatureModule");
                if (featureAttribute is null)
                {
                    continue;
                }

                string featureKey = featureAttribute.ConstructorArguments.Length > 0
                    ? featureAttribute.ConstructorArguments[0].Value as string ?? classSymbol.Name
                    : classSymbol.Name;

                if (!StatePathGraph.IsNamespaceAccessible(classSymbol))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        FeatureComponentNotResolvableRule,
                        classSymbol.Locations.FirstOrDefault(),
                        featureKey,
                        "Reducer（特性宿主为私有或受保护类型，无法被容器引用）",
                        0));
                    continue;
                }

                INamedTypeSymbol? reducerImpl = classSymbol.AllInterfaces.FirstOrDefault(
                    i => i.OriginalDefinition.Equals(reducerInterface, SymbolEqualityComparer.Default));
                if (reducerImpl is null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        FeatureComponentNotResolvableRule,
                        classSymbol.Locations.FirstOrDefault(),
                        featureKey,
                        "Reducer（特性宿主未实现 IMviReducer<TState, TIntent, TEffect>）",
                        0));
                    continue;
                }

                INamedTypeSymbol stateType = (INamedTypeSymbol)reducerImpl.TypeArguments[0];
                INamedTypeSymbol intentType = (INamedTypeSymbol)reducerImpl.TypeArguments[1];
                INamedTypeSymbol effectType = (INamedTypeSymbol)reducerImpl.TypeArguments[2];

                if (!HasStaticInitial(stateType))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        FeatureStateMissingInitialRule,
                        classSymbol.Locations.FirstOrDefault(),
                        featureKey,
                        stateType.Name));
                    continue;
                }

                List<INamedTypeSymbol> handlers = FindUniqueImplementation(
                    allTypes,
                    handlerInterface,
                    reducerImpl.TypeArguments);
                if (handlers.Count != 1)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        FeatureComponentNotResolvableRule,
                        classSymbol.Locations.FirstOrDefault(),
                        featureKey,
                        "IntentHandler",
                        handlers.Count));
                    continue;
                }

                List<INamedTypeSymbol> dispatchers = FindUniqueImplementation(
                    allTypes,
                    dispatcherInterface,
                    ImmutableArray.Create(reducerImpl.TypeArguments[2]));
                if (dispatchers.Count != 1)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        FeatureComponentNotResolvableRule,
                        classSymbol.Locations.FirstOrDefault(),
                        featureKey,
                        "EffectDispatcher",
                        dispatchers.Count));
                    continue;
                }

                result.Add(new Models.FeatureStoreInfo(
                    featureKey,
                    GeneratorSyntaxHelpers.FormatFullyQualified(stateType),
                    GeneratorSyntaxHelpers.FormatFullyQualified(intentType),
                    GeneratorSyntaxHelpers.FormatFullyQualified(effectType),
                    GeneratorSyntaxHelpers.FormatFullyQualified(classSymbol),
                    GeneratorSyntaxHelpers.FormatFullyQualified(handlers[0]),
                    GeneratorSyntaxHelpers.FormatFullyQualified(dispatchers[0]),
                    BuildConstructorArguments(classSymbol).Expressions,
                    BuildConstructorArguments(handlers[0]).Expressions,
                    BuildConstructorArguments(dispatchers[0]).Expressions));
            }

            return result;
        }

        private static bool HasStaticInitial(INamedTypeSymbol stateType)
        {
            foreach (ISymbol member in stateType.GetMembers("Initial"))
            {
                if (!member.IsStatic || member.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }

                if (member is IPropertySymbol property
                    && property.Type.Equals(stateType, SymbolEqualityComparer.Default))
                {
                    return true;
                }

                if (member is IFieldSymbol field
                    && field.Type.Equals(stateType, SymbolEqualityComparer.Default))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<INamedTypeSymbol> FindUniqueImplementation(
            List<INamedTypeSymbol> allTypes,
            INamedTypeSymbol interfaceDefinition,
            System.Collections.Immutable.ImmutableArray<ITypeSymbol> typeArguments)
        {
            List<INamedTypeSymbol> matches = new();
            foreach (INamedTypeSymbol candidate in allTypes)
            {
                if (candidate.IsAbstract || candidate.IsGenericType)
                {
                    continue;
                }

                foreach (INamedTypeSymbol implemented in candidate.AllInterfaces)
                {
                    if (!implemented.OriginalDefinition.Equals(
                        interfaceDefinition,
                        SymbolEqualityComparer.Default))
                    {
                        continue;
                    }

                    bool typeArgumentsMatch = true;
                    for (int index = 0; index < typeArguments.Length; index++)
                    {
                        if (!implemented.TypeArguments[index].Equals(
                            typeArguments[index],
                            SymbolEqualityComparer.Default))
                        {
                            typeArgumentsMatch = false;
                            break;
                        }
                    }

                    if (typeArgumentsMatch)
                    {
                        matches.Add(candidate);
                        break;
                    }
                }
            }

            return matches;
        }
    }
}
