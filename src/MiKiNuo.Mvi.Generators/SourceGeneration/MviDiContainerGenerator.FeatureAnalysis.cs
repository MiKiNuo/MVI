using MiKiNuo.Mvi.Generators.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
namespace MiKiNuo.Mvi.Generators.SourceGeneration;
/// <summary>以 Handler 为 Feature 入口发现纯 Reducer、绑定模型和中间件。</summary>
public sealed partial class MviDiContainerGenerator
{
    internal static class FeatureAnalysis
    {
        private static readonly DiagnosticDescriptor Invalid = new(DiagnosticIdCatalog.MviFeatureDeclarationInvalid, "组件装配无效", "{0}", "MviComposition", DiagnosticSeverity.Error, true);
        internal static IReadOnlyList<Models.MviFeatureInfo> CollectFeatures(IEnumerable<INamedTypeSymbol> handlers,
            IReadOnlyList<INamedTypeSymbol> candidates, SourceProductionContext context)
        {
            var result = new List<Models.MviFeatureInfo>();
            var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            var classes = candidates.Distinct(SymbolEqualityComparer.Default).OfType<INamedTypeSymbol>().Where(c => !c.IsAbstract && !c.IsStatic && c.Arity == 0).ToArray();
            foreach (INamedTypeSymbol handler in handlers)
            {
                if (!seen.Add(handler)) continue;
                void Error(string message) => context.ReportDiagnostic(Diagnostic.Create(Invalid, handler.Locations.FirstOrDefault(), message));
                INamedTypeSymbol? parent = FindBase(handler, "MiKiNuo.Mvi.Runtime.MVI.Intent.MviIntentHandler", 2);
                if (parent is null || handler.IsAbstract || handler.IsStatic || handler.Arity != 0) { Error("MviFeature 必须标记 MviIntentHandler<TState,TIntent> 的派生类。"); continue; }
                var state = (INamedTypeSymbol)parent.TypeArguments[0]; var intent = (INamedTypeSymbol)parent.TypeArguments[1];
                if (!state.GetMembers("Initial").OfType<IPropertySymbol>().Any(p => p.IsStatic && p.GetMethod?.DeclaredAccessibility == Accessibility.Public && SymbolEqualityComparer.Default.Equals(p.Type, state)))
                { Error("组件状态需要公开静态同类型 Initial 属性。"); continue; }
                var reducers = classes.Where(c => FindBase(c, "MiKiNuo.Mvi.Runtime.MVI.Reducer.MviReducerBase", 1) is INamedTypeSymbol b && SymbolEqualityComparer.Default.Equals(b.TypeArguments[0], state)).ToArray();
                var viewModels = classes.Where(c => FindBase(c, "MiKiNuo.Mvi.Binding.ViewModel.MviViewModelBase", 2) is INamedTypeSymbol b && SymbolEqualityComparer.Default.Equals(b.TypeArguments[0], state) && SymbolEqualityComparer.Default.Equals(b.TypeArguments[1], intent)).ToArray();
                if (reducers.Length != 1 || viewModels.Length > 1) { Error("每个 Feature 必须有唯一 Reducer，最多一个默认 ViewModel。"); continue; }
                var middleware = classes.Where(c => c.AllInterfaces.Any(i => i.Name == "IMviMiddleware" && i.ContainingNamespace.ToDisplayString() == "MiKiNuo.Mvi.Runtime.MVI.Middleware" && i.TypeArguments.Length == 2 && SymbolEqualityComparer.Default.Equals(i.TypeArguments[0], state) && SymbolEqualityComparer.Default.Equals(i.TypeArguments[1], intent))).ToArray();
                if (middleware.Length > 1 && (middleware.Any(c => Order(c) is null) || middleware.Select(Order).Distinct().Count() != middleware.Length))
                { Error("多个中间件必须通过 MviMiddlewareOrder 声明唯一顺序。"); continue; }
                string name = handler.Name.EndsWith("Handler", StringComparison.Ordinal) ? handler.Name.Substring(0, handler.Name.Length - 7) : handler.Name;
                if (result.Any(feature => feature.FeatureName == name)) { Error("自动装配的 Feature 名称重复，请使用不同的 Handler 类型名：" + name); continue; }
                string stateName = Display(state), intentName = Display(intent);
                Models.FeatureComponentInfo? vm = null;
                if (viewModels.Length == 1)
                {
                    INamedTypeSymbol symbol = viewModels[0];
                    // 同轮生成器不能读取另一个生成器产物，因此从绑定契约推导生成构造参数。
                    vm = !symbol.InstanceConstructors.Any(c => !c.IsImplicitlyDeclared)
                        ? new Models.FeatureComponentInfo(Display(symbol), new[] {
                            "global::MiKiNuo.Mvi.Runtime.MVI.Store.IMviStore<" + stateName + ", " + intentName + ">",
                            "global::MiKiNuo.Mvi.Binding.Threading.IMviUiDispatcher" })
                        : Component(symbol);
                }
                result.Add(new Models.MviFeatureInfo(name, stateName, intentName, Component(reducers[0]), Component(handler), vm,
                    middleware.OrderBy(c => Order(c) ?? 0).Select(Component).ToArray()));
            }
            return result;
        }
        private static string Display(ITypeSymbol symbol) => symbol.ToDisplayString(GeneratorSyntaxHelpers.FullyQualifiedNullableFormat);
        private static Models.FeatureComponentInfo Component(INamedTypeSymbol symbol) => new(Display(symbol),
            Analysis.BuildConstructorParameterTypeNames(symbol, honorDiConstructorAttribute: true, stripTopLevelNullableAnnotation: true));
        private static int? Order(INamedTypeSymbol type)
        {
            AttributeData? attr = type.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "MiKiNuo.Mvi.Abstractions.DI.MviMiddlewareOrderAttribute");
            return attr?.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is int value ? value : null;
        }
        private static INamedTypeSymbol? FindBase(INamedTypeSymbol type, string fullName, int arity)
        {
            for (INamedTypeSymbol? current = type.BaseType; current is not null; current = current.BaseType)
                if (current.Arity == arity && current.ContainingNamespace.ToDisplayString() + "." + current.Name == fullName) return current;
            return null;
        }
    }
}
