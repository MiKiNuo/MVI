using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;

/// <summary>
/// 表示泛型编译期 DI 容器源生成器。
/// 通过扫描 [DiService] 特性和 MviFeatureModule 特性，
/// 为任意项目生成 DI 容器注册代码，不再硬编码特定程序集。
/// 分析阶段由嵌套类型 <see cref="Analysis"/> 负责，发射阶段由 <see cref="Emission"/> 负责，
/// 数据模型由 <see cref="Models"/> 承载。
/// </summary>
[Generator]
public sealed class MviDiContainerGenerator : IIncrementalGenerator
{
    /// <summary>
    /// 初始化源生成器注册编译回调。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, Execute);
    }

    private static void Execute(SourceProductionContext context, Compilation compilation)
    {
        if (!Analysis.HasDiServices(compilation, context.CancellationToken))
        {
            return;
        }

        List<Models.DiServiceInfo> services = Analysis.Discover(compilation, context.CancellationToken);
        List<Models.FeatureStoreInfo> features = Analysis.DiscoverFeatures(compilation, context);

        string source = Emission.GenerateContainerSource(
            compilation.AssemblyName ?? string.Empty,
            services,
            features);
        context.AddSource("GeneratedMviContainer.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    /// <summary>
    /// 表示 <see cref="MviDiContainerGenerator"/> 的分析阶段：
    /// 从 <see cref="INamedTypeSymbol"/> 中提取 [DiService] 特性，转换为 <see cref="Models.DiServiceInfo"/>。
    /// </summary>
    internal static class Analysis
    {
        /// <summary>
        /// 编译中是否包含 [DiService] 或 [MviFeatureModule] 标记的类。
        /// 用于在确认无需生成代码时短路 <see cref="IIncrementalGenerator"/>，避免空跑分析。
        /// </summary>
        public static bool HasDiServices(
            Compilation compilation,
            System.Threading.CancellationToken cancellationToken)
        {
            return GeneratorSyntaxHelpers.CompilationHasAttribute(
                compilation,
                cancellationToken,
                "DiService",
                "MviFeatureModule");
        }

        /// <summary>
        /// 发现所有 [DiService] 标记的服务。
        /// </summary>
        /// <param name="compilation">编译对象。</param>
        /// <param name="cancellationToken">取消标记。</param>
        /// <returns>发现的 DI 服务信息列表。</returns>
        public static List<Models.DiServiceInfo> Discover(
            Compilation compilation,
            System.Threading.CancellationToken cancellationToken)
        {
            List<Models.DiServiceInfo> result = new();

            foreach (INamedTypeSymbol classSymbol in GeneratorSyntaxHelpers.EnumerateClassSymbols(compilation, cancellationToken))
            {
                Models.DiServiceInfo? info = ParseDiService(classSymbol);
                if (info is not null)
                {
                    result.Add(info);
                }
            }

            return result;
        }

        private static Models.DiServiceInfo? ParseDiService(INamedTypeSymbol classSymbol)
        {
            AttributeData? attr = GeneratorSyntaxHelpers.FindAttribute(classSymbol, "DiService");
            if (attr is null)
            {
                return null;
            }

            string serviceTypeName = classSymbol.ToDisplayString(GeneratorSyntaxHelpers.FullyQualifiedNullableFormat);
            string implementationTypeName = classSymbol.ToDisplayString(GeneratorSyntaxHelpers.FullyQualifiedNullableFormat);
            Models.GeneratedLifetime lifetime = Models.GeneratedLifetime.Singleton;
            string? @namespace = classSymbol.ContainingNamespace?.ToDisplayString();

            if (attr.ConstructorArguments.Length > 0
                && attr.ConstructorArguments[0].Value is int lifetimeValue)
            {
                lifetime = lifetimeValue switch
                {
                    0 => Models.GeneratedLifetime.Singleton,
                    1 => Models.GeneratedLifetime.Scoped,
                    2 => Models.GeneratedLifetime.Transient,
                    _ => Models.GeneratedLifetime.Singleton,
                };
            }

            foreach (KeyValuePair<string, TypedConstant> namedArgument in attr.NamedArguments)
            {
                if (namedArgument.Key == "ServiceType"
                    && namedArgument.Value.Value is INamedTypeSymbol serviceType)
                {
                    serviceTypeName = serviceType.ToDisplayString(GeneratorSyntaxHelpers.FullyQualifiedNullableFormat);
                }
            }

            (IReadOnlyList<string> constructorExpressions, IReadOnlyList<string> constructorParameterTypeNames) =
                BuildConstructorArguments(classSymbol);

            return new Models.DiServiceInfo(
                serviceTypeName,
                implementationTypeName,
                lifetime,
                @namespace,
                constructorExpressions,
                constructorParameterTypeNames);
        }

        /// <summary>
        /// 选择应使用的构造函数并生成 C# 实参表达式列表与参数类型完整限定名列表。
        /// 优先使用 <c>[DiConstructor]</c> 标记的构造函数；否则挑选参数数量最多的可解析构造函数。
        /// 每个参数生成 <c>this.Resolve&lt;T&gt;()</c>，由容器在运行时解析依赖；
        /// 同时记录每个参数的类型完整限定名，供 <c>CreateWith</c> 做 <c>args[i] is T</c> 模式匹配。
        /// </summary>
        /// <param name="classSymbol">实现类符号。</param>
        /// <returns>
        /// 元组：(实参表达式列表, 参数类型完整限定名列表)。两者按构造函数参数顺序一一对应。
        /// 如果无可用构造函数则两个列表都为空。
        /// </returns>
        private static (IReadOnlyList<string> Expressions, IReadOnlyList<string> ParameterTypeNames) BuildConstructorArguments(INamedTypeSymbol classSymbol)
        {
            IMethodSymbol? selected = null;

            AttributeData? diConstructorAttribute = GeneratorSyntaxHelpers.FindAttribute(classSymbol, "DiConstructor");
            if (diConstructorAttribute is not null
                && diConstructorAttribute.ApplicationSyntaxReference?.GetSyntax() is { } syntax)
            {
                foreach (IMethodSymbol constructor in classSymbol.Constructors)
                {
                    if (constructor.Locations.Any(location => location.SourceTree == syntax.SyntaxTree)
                        && constructor.Locations.Any(location => location.SourceSpan == syntax.Span))
                    {
                        selected = constructor;
                        break;
                    }
                }
            }

            selected ??= classSymbol.Constructors
                .Where(static constructor => constructor.DeclaredAccessibility == Accessibility.Public)
                .OrderByDescending(static constructor => constructor.Parameters.Length)
                .FirstOrDefault();

            if (selected is null || selected.Parameters.Length == 0)
            {
                return (System.Array.Empty<string>(), System.Array.Empty<string>());
            }

            List<string> arguments = new(selected.Parameters.Length);
            List<string> parameterTypeNames = new(selected.Parameters.Length);
            foreach (IParameterSymbol parameter in selected.Parameters)
            {
                string typeName = parameter.Type.ToDisplayString(GeneratorSyntaxHelpers.FullyQualifiedNullableFormat);
                arguments.Add("this.Resolve<" + typeName + ">()");
                parameterTypeNames.Add(typeName);
            }

            return (arguments, parameterTypeNames);
        }

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

    /// <summary>
    /// 表示 <see cref="MviDiContainerGenerator"/> 的数据模型集合。
    /// 与分析/发射逻辑解耦。
    /// </summary>
    internal static class Models
    {
        /// <summary>
        /// 表示发现的 DI 服务信息。
        /// </summary>
        public sealed class DiServiceInfo
        {
            /// <summary>
            /// 初始化 DI 服务信息。
            /// </summary>
            /// <param name="serviceTypeName">服务类型（完整限定名）。</param>
            /// <param name="implementationTypeName">实现类型（完整限定名）。</param>
            /// <param name="lifetime">生命周期。</param>
            /// <param name="namespace">类型所在命名空间（用于补全 using）。</param>
            /// <param name="constructorArgumentExpressions">
            /// 构造实现类型时需要传入的参数表达式集合（已生成 C# 表达式字符串）。
            /// 为空集合时发射端回退为 <c>new T()</c>。
            /// </param>
            /// <param name="constructorParameterTypeNames">
            /// 与 <paramref name="constructorArgumentExpressions"/> 一一对应的参数类型完整限定名集合，
            /// 供 <c>CreateWith</c> 反射式按参数实例化时做 <c>args[i] is T</c> 模式匹配。
            /// </param>
            public DiServiceInfo(
                string serviceTypeName,
                string implementationTypeName,
                GeneratedLifetime lifetime,
                string? @namespace,
                IReadOnlyList<string> constructorArgumentExpressions,
                IReadOnlyList<string> constructorParameterTypeNames)
            {
                ServiceTypeName = serviceTypeName;
                ImplementationTypeName = implementationTypeName;
                Lifetime = lifetime;
                Namespace = @namespace;
                ConstructorArgumentExpressions = constructorArgumentExpressions;
                ConstructorParameterTypeNames = constructorParameterTypeNames;
            }

            /// <summary>服务类型（完整限定名）。</summary>
            public string ServiceTypeName { get; }

            /// <summary>实现类型（完整限定名）。</summary>
            public string ImplementationTypeName { get; }

            /// <summary>类型所在命名空间。</summary>
            public string? Namespace { get; }

            /// <summary>生命周期。</summary>
            public GeneratedLifetime Lifetime { get; }

            /// <summary>
            /// 构造实现类型时需要传入的参数表达式集合（已生成 C# 表达式字符串）。
            /// 为空集合时发射端回退为 <c>new T()</c>。
            /// </summary>
            public IReadOnlyList<string> ConstructorArgumentExpressions { get; }

            /// <summary>
            /// 与 <see cref="ConstructorArgumentExpressions"/> 一一对应的参数类型完整限定名集合。
            /// </summary>
            public IReadOnlyList<string> ConstructorParameterTypeNames { get; }

            /// <summary>
            /// 根据服务类型末段名生成可作为实例字段的私有字段名（带下划线前缀）。
            /// </summary>
            public string GetFieldName()
            {
                string name = ServiceTypeName.Split('.').Last();
                return "_" + char.ToLowerInvariant(name[0]) + name.Substring(1);
            }
        }

        /// <summary>
        /// 镜像 Domain 层 <c>ServiceLifetime</c> 枚举的本地副本。
        /// Infrastructure 是 Analyzer/SourceGenerator 源，运行时不应引用 Domain 程序集，
        /// 因此在此处单独定义并显式映射到生成代码中的 <c>ServiceLifetime.X</c>。
        /// </summary>
        internal enum GeneratedLifetime
        {
            /// <summary>单例生命周期。</summary>
            Singleton = 0,

            /// <summary>作用域生命周期。</summary>
            Scoped = 1,

            /// <summary>瞬态生命周期。</summary>
            Transient = 2,
        }

        /// <summary>
        /// 表示一个可装配的 Feature Store 信息。
        /// </summary>
        internal sealed class FeatureStoreInfo
        {
            /// <summary>
            /// 初始化 Feature Store 信息。
            /// </summary>
            /// <param name="featureKey">功能模块键。</param>
            /// <param name="stateTypeName">状态类型（完整限定名）。</param>
            /// <param name="intentTypeName">意图类型（完整限定名）。</param>
            /// <param name="effectTypeName">副作用类型（完整限定名）。</param>
            /// <param name="reducerTypeName">规约器类型（完整限定名）。</param>
            /// <param name="handlerTypeName">意图处理器类型（完整限定名）。</param>
            /// <param name="dispatcherTypeName">副作用分发器类型（完整限定名）。</param>
            /// <param name="reducerConstructorArguments">规约器构造实参表达式。</param>
            /// <param name="handlerConstructorArguments">意图处理器构造实参表达式。</param>
            /// <param name="dispatcherConstructorArguments">副作用分发器构造实参表达式。</param>
            public FeatureStoreInfo(
                string featureKey,
                string stateTypeName,
                string intentTypeName,
                string effectTypeName,
                string reducerTypeName,
                string handlerTypeName,
                string dispatcherTypeName,
                IReadOnlyList<string> reducerConstructorArguments,
                IReadOnlyList<string> handlerConstructorArguments,
                IReadOnlyList<string> dispatcherConstructorArguments)
            {
                FeatureKey = featureKey;
                StateTypeName = stateTypeName;
                IntentTypeName = intentTypeName;
                EffectTypeName = effectTypeName;
                ReducerTypeName = reducerTypeName;
                HandlerTypeName = handlerTypeName;
                DispatcherTypeName = dispatcherTypeName;
                ReducerConstructorArguments = reducerConstructorArguments;
                HandlerConstructorArguments = handlerConstructorArguments;
                DispatcherConstructorArguments = dispatcherConstructorArguments;
            }

            /// <summary>功能模块键。</summary>
            public string FeatureKey { get; }

            /// <summary>状态类型（完整限定名）。</summary>
            public string StateTypeName { get; }

            /// <summary>意图类型（完整限定名）。</summary>
            public string IntentTypeName { get; }

            /// <summary>副作用类型（完整限定名）。</summary>
            public string EffectTypeName { get; }

            /// <summary>规约器类型（完整限定名）。</summary>
            public string ReducerTypeName { get; }

            /// <summary>意图处理器类型（完整限定名）。</summary>
            public string HandlerTypeName { get; }

            /// <summary>副作用分发器类型（完整限定名）。</summary>
            public string DispatcherTypeName { get; }

            /// <summary>规约器构造实参表达式。</summary>
            public IReadOnlyList<string> ReducerConstructorArguments { get; }

            /// <summary>意图处理器构造实参表达式。</summary>
            public IReadOnlyList<string> HandlerConstructorArguments { get; }

            /// <summary>副作用分发器构造实参表达式。</summary>
            public IReadOnlyList<string> DispatcherConstructorArguments { get; }

            /// <summary>
            /// 获取可作为方法名片段的功能模块键（仅保留字母数字与下划线）。
            /// </summary>
            /// <returns>安全的方法名片段。</returns>
            public string GetSafeMethodKey()
            {
                StringBuilder builder = new();
                foreach (char character in FeatureKey)
                {
                    builder.Append(char.IsLetterOrDigit(character) || character == '_' ? character : '_');
                }

                return builder.Length == 0 ? "Feature" : builder.ToString();
            }
        }
    }

    /// <summary>
    /// 表示 <see cref="MviDiContainerGenerator"/> 的代码发射阶段：
    /// 根据 <see cref="Models.DiServiceInfo"/> 列表生成 DI 容器源码字符串。
    /// </summary>
    internal static class Emission
    {
        /// <summary>
        /// 生成 DI 容器源码。
        /// </summary>
        /// <param name="assemblyName">目标程序集名称（用作容器命名空间）。</param>
        /// <param name="services">分析得到的 DI 服务信息。</param>
        /// <param name="features">分析得到的 Feature Store 装配信息。</param>
        /// <returns>生成的 C# 源码。</returns>
        public static string GenerateContainerSource(
            string assemblyName,
            IReadOnlyList<Models.DiServiceInfo> services,
            IReadOnlyList<Models.FeatureStoreInfo> features)
        {
            StringBuilder builder = new();
            string containerNamespace = string.IsNullOrEmpty(assemblyName) ? "GeneratedContainer" : assemblyName;

            EmitFileHeader(builder, containerNamespace, services);
            EmitConstructor(builder, services);
            EmitResolveMethods(builder, services, features);
            EmitFeatureStoreFactories(builder, features);
            EmitCreateScope(builder);
            EmitCreateWith(builder, services);
            EmitScopeClass(builder);

            return builder.ToString();
        }

        /// <summary>
        /// 生成文件头：using、namespace、类声明与字段。
        /// </summary>
        private static void EmitFileHeader(
            StringBuilder builder,
            string containerNamespace,
            IReadOnlyList<Models.DiServiceInfo> services)
        {
            builder.AppendLine("// <auto-generated />");
            builder.AppendLine("#nullable enable");
            builder.AppendLine();
            builder.AppendLine("using System;");
            builder.AppendLine("using System.Collections.Generic;");
            builder.AppendLine("using MiKiNuo.Mvi.Application.DI;");
            builder.AppendLine("using MiKiNuo.Mvi.Domain.DI;");
            builder.AppendLine();

            HashSet<string> addedNamespaces = new HashSet<string>();
            foreach (Models.DiServiceInfo service in services)
            {
                if (service.Namespace is { Length: > 0 } ns && addedNamespaces.Add(ns))
                {
                    builder.Append("using ").Append(ns).AppendLine(";");
                }
            }

            builder.AppendLine();
            builder.Append("namespace ").Append(containerNamespace).AppendLine(".Composition;");
            builder.AppendLine();
            builder.AppendLine("/// <summary>");
            builder.AppendLine("/// 表示由 MVI 源生成器自动生成的泛型 DI 容器。");
            builder.AppendLine("/// 所有服务通过 [DiService] 特性自动注册。");
            builder.AppendLine("/// 单例使用按需懒加载，避免构造期因未注册依赖而失败。");
            builder.AppendLine("/// </summary>");
            builder.Append("public sealed class GeneratedMviContainer : IMviResolver, IMviServiceGraph");
            builder.AppendLine();
            builder.AppendLine("{");
            builder.AppendLine("    private readonly Dictionary<Type, object> _singletons = new();");
            builder.AppendLine();
        }

        /// <summary>
        /// 生成构造函数与服务描述符字段。
        /// </summary>
        private static void EmitConstructor(StringBuilder builder, IReadOnlyList<Models.DiServiceInfo> services)
        {
            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// 初始化由源生成器生成的泛型 DI 容器。");
            builder.AppendLine("    /// 仅注册服务描述符，不做服务实例化，避免构造期出现未注册依赖的级联失败。");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    public GeneratedMviContainer()");
            builder.AppendLine("    {");
            builder.AppendLine("    }");
            builder.AppendLine();
            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// 获取服务描述集合。");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    public IReadOnlyList<MviServiceDescriptor> ServiceDescriptors => _descriptors;");
            builder.AppendLine();
            builder.AppendLine("    private static readonly IReadOnlyList<MviServiceDescriptor> _descriptors = new MviServiceDescriptor[]");
            builder.AppendLine("    {");

            foreach (Models.DiServiceInfo service in services)
            {
                builder.Append("        new(typeof(").Append(service.ServiceTypeName).Append("), typeof(")
                    .Append(service.ImplementationTypeName).Append("), ServiceLifetime.")
                    .Append(service.Lifetime.ToServiceLifetimeName()).AppendLine("),");
            }

            builder.AppendLine("    };");
            builder.AppendLine();
        }

        /// <summary>
        /// 生成 Resolve&lt;T&gt; 与 Resolve(Type) 方法。
        /// </summary>
        private static void EmitResolveMethods(
            StringBuilder builder,
            IReadOnlyList<Models.DiServiceInfo> services,
            IReadOnlyList<Models.FeatureStoreInfo> features)
        {
            EmitResolveGeneric(builder);
            EmitResolveByType(builder, services, features);
        }

        /// <summary>
        /// 生成 Resolve&lt;T&gt; 泛型方法。
        /// </summary>
        private static void EmitResolveGeneric(StringBuilder builder)
        {
            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// 解析服务。");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    /// <typeparam name=\"TService\">服务类型。</typeparam>");
            builder.AppendLine("    /// <returns>服务实例。</returns>");
            builder.AppendLine("    public TService Resolve<TService>()");
            builder.AppendLine("        where TService : notnull");
            builder.AppendLine("    {");
            builder.AppendLine("        return (TService)Resolve(typeof(TService));");
            builder.AppendLine("    }");
            builder.AppendLine();
        }

        /// <summary>
        /// 生成 Resolve(Type) 非泛型方法。
        /// </summary>
        private static void EmitResolveByType(
            StringBuilder builder,
            IReadOnlyList<Models.DiServiceInfo> services,
            IReadOnlyList<Models.FeatureStoreInfo> features)
        {
            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// 解析指定类型的服务。");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    /// <param name=\"serviceType\">服务类型。</param>");
            builder.AppendLine("    /// <returns>服务实例。</returns>");
            builder.AppendLine("    public object Resolve(Type serviceType)");
            builder.AppendLine("    {");
            builder.AppendLine("        if (serviceType is null)");
            builder.AppendLine("        {");
            builder.AppendLine("            throw new ArgumentNullException(nameof(serviceType));");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        if (_singletons.TryGetValue(serviceType, out object? existing))");
            builder.AppendLine("        {");
            builder.AppendLine("            return existing;");
            builder.AppendLine("        }");
            builder.AppendLine();
            foreach (Models.DiServiceInfo service in services)
            {
                builder.Append("        if (serviceType == typeof(").Append(service.ServiceTypeName)
                    .AppendLine("))");
                if (service.Lifetime == Models.GeneratedLifetime.Singleton)
                {
                    builder.Append("        {").AppendLine();
                    builder.Append("            object created = new ").Append(service.ImplementationTypeName).Append('(');
                    builder.Append(string.Join(", ", service.ConstructorArgumentExpressions));
                    builder.AppendLine(");");
                    builder.Append("            _singletons[serviceType] = created;").AppendLine();
                    builder.Append("            return created;").AppendLine();
                    builder.Append("        }").AppendLine();
                }
                else
                {
                    builder.Append("            return new ").Append(service.ImplementationTypeName).Append('(');
                    builder.Append(string.Join(", ", service.ConstructorArgumentExpressions));
                    builder.AppendLine(");");
                }
            }

            foreach (Models.FeatureStoreInfo feature in features)
            {
                builder.Append("        if (serviceType == typeof(global::MiKiNuo.Mvi.Application.MVI.Store.IMviStore<")
                    .Append(feature.StateTypeName).Append(", ")
                    .Append(feature.IntentTypeName).Append(", ")
                    .Append(feature.EffectTypeName).AppendLine(">))");
                builder.AppendLine("        {");
                builder.Append("            object created = Create").Append(feature.GetSafeMethodKey()).AppendLine("FeatureStore();");
                builder.AppendLine("            _singletons[serviceType] = created;");
                builder.AppendLine("            return created;");
                builder.AppendLine("        }");
                builder.AppendLine();
            }

            builder.AppendLine();
            builder.AppendLine("        throw new InvalidOperationException($\"未注册服务：{serviceType.FullName}\");");
            builder.AppendLine("    }");
            builder.AppendLine();
        }

        /// <summary>
        /// 生成 Feature Store 工厂方法。
        /// </summary>
        private static void EmitFeatureStoreFactories(
            StringBuilder builder,
            IReadOnlyList<Models.FeatureStoreInfo> features)
        {
            foreach (Models.FeatureStoreInfo feature in features)
            {
                string storeType = "global::MiKiNuo.Mvi.Application.MVI.Store.IMviStore<"
                    + feature.StateTypeName + ", "
                    + feature.IntentTypeName + ", "
                    + feature.EffectTypeName + ">";

                builder.AppendLine("    /// <summary>");
                builder.Append("    /// 创建功能模块“").Append(feature.FeatureKey).AppendLine("”的 Store（由 [MviFeatureModule] 驱动生成）。");
                builder.AppendLine("    /// </summary>");
                builder.Append("    /// <returns>功能模块 Store 实例。</returns>");
                builder.AppendLine();
                builder.Append("    private ").Append(storeType).Append(" Create")
                    .Append(feature.GetSafeMethodKey()).AppendLine("FeatureStore()");
                builder.AppendLine("    {");
                builder.Append("        return new global::MiKiNuo.Mvi.Application.MVI.Store.MviStore<")
                    .Append(feature.StateTypeName).Append(", ")
                    .Append(feature.IntentTypeName).Append(", ")
                    .Append(feature.EffectTypeName).AppendLine(">(");
                builder.Append("            ").Append(feature.StateTypeName).AppendLine(".Initial,");
                builder.Append("            new ").Append(feature.HandlerTypeName).Append('(')
                    .Append(string.Join(", ", feature.HandlerConstructorArguments)).AppendLine("),");
                builder.Append("            new ").Append(feature.ReducerTypeName).Append('(')
                    .Append(string.Join(", ", feature.ReducerConstructorArguments)).AppendLine("),");
                builder.Append("            new ").Append(feature.DispatcherTypeName).Append('(')
                    .Append(string.Join(", ", feature.DispatcherConstructorArguments)).AppendLine("));");
                builder.AppendLine("    }");
                builder.AppendLine();
            }
        }

        /// <summary>
        /// 生成 CreateScope 方法。
        /// </summary>
        private static void EmitCreateScope(StringBuilder builder)
        {
            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// 创建作用域。");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    /// <returns>服务作用域。</returns>");
            builder.AppendLine("    public IMviScope CreateScope()");
            builder.AppendLine("    {");
            builder.AppendLine("        return new GeneratedMviScope(this);");
            builder.AppendLine("    }");
            builder.AppendLine();
        }

        /// <summary>
        /// 生成 CreateWith&lt;T&gt; 与 CreateWithCore 方法。
        /// </summary>
        private static void EmitCreateWith(StringBuilder builder, IReadOnlyList<Models.DiServiceInfo> services)
        {
            EmitCreateWithGeneric(builder);
            EmitCreateWithCore(builder, services);
        }

        /// <summary>
        /// 生成 CreateWith&lt;T&gt; 泛型方法。
        /// </summary>
        private static void EmitCreateWithGeneric(StringBuilder builder)
        {
            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// 按构造参数即时构造服务实例。");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    /// <typeparam name=\"TService\">要实例化的服务类型。</typeparam>");
            builder.AppendLine("    /// <param name=\"args\">构造函数实参。</param>");
            builder.AppendLine("    /// <returns>新构造的实例。</returns>");
            builder.AppendLine("    public TService CreateWith<TService>(params object[] args)");
            builder.AppendLine("        where TService : notnull");
            builder.AppendLine("    {");
            builder.AppendLine("        if (args is null)");
            builder.AppendLine("        {");
            builder.AppendLine("            throw new ArgumentNullException(nameof(args));");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        return (TService)CreateWithCore(typeof(TService), args);");
            builder.AppendLine("    }");
            builder.AppendLine();
        }

        /// <summary>
        /// 生成 CreateWithCore 核心方法。
        /// </summary>
        private static void EmitCreateWithCore(StringBuilder builder, IReadOnlyList<Models.DiServiceInfo> services)
        {
            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// 按构造参数即时构造并返回服务实例的核心实现。");
            builder.AppendLine("    /// 不走单例缓存，按运行时参数匹配公共构造函数。");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    private object CreateWithCore(Type serviceType, object[] args)");
            builder.AppendLine("    {");
            builder.AppendLine("        if (serviceType is null)");
            builder.AppendLine("        {");
            builder.AppendLine("            throw new ArgumentNullException(nameof(serviceType));");
            builder.AppendLine("        }");
            builder.AppendLine();
            EmitCreateWithZeroArgs(builder, services);
            EmitCreateWithOneArg(builder, services);
            builder.AppendLine("        throw new InvalidOperationException($\"CreateWith 暂不支持 {args.Length} 个参数的构造：{serviceType.FullName}\");");
            builder.AppendLine("    }");
        }

        /// <summary>
        /// 生成 CreateWithCore 零参分支。
        /// </summary>
        private static void EmitCreateWithZeroArgs(StringBuilder builder, IReadOnlyList<Models.DiServiceInfo> services)
        {
            builder.AppendLine("        if (args.Length == 0)");
            builder.AppendLine("        {");
            foreach (Models.DiServiceInfo service in services)
            {
                builder.Append("            if (serviceType == typeof(").Append(service.ImplementationTypeName)
                    .AppendLine("))");
                builder.Append("            {").AppendLine();
                builder.Append("                return new ").Append(service.ImplementationTypeName).Append('(');
                builder.Append(string.Join(", ", service.ConstructorArgumentExpressions));
                builder.AppendLine(");");
                builder.Append("            }").AppendLine();
            }
            builder.AppendLine("            throw new InvalidOperationException($\"CreateWith 未注册实现类型：{serviceType.FullName}\");");
            builder.AppendLine("        }");
            builder.AppendLine();
        }

        /// <summary>
        /// 生成 CreateWithCore 单参分支。
        /// </summary>
        private static void EmitCreateWithOneArg(StringBuilder builder, IReadOnlyList<Models.DiServiceInfo> services)
        {
            builder.AppendLine("        if (args.Length == 1)");
            builder.AppendLine("        {");
            foreach (Models.DiServiceInfo service in services)
            {
                if (service.ConstructorParameterTypeNames.Count != 1)
                {
                    continue;
                }

                builder.Append("            if (serviceType == typeof(").Append(service.ImplementationTypeName)
                    .Append(") && args[0] is ").Append(service.ConstructorParameterTypeNames[0])
                    .AppendLine(" a0)");
                builder.Append("            {").AppendLine();
                builder.Append("                return new ").Append(service.ImplementationTypeName).Append("(a0")
                    .AppendLine(");");
                builder.Append("            }").AppendLine();
            }
            builder.AppendLine("            throw new InvalidOperationException($\"CreateWith 未找到匹配 1 个参数的构造函数：{serviceType.FullName}\");");
            builder.AppendLine("        }");
            builder.AppendLine();
        }

        /// <summary>
        /// 生成作用域嵌套类。
        /// </summary>
        private static void EmitScopeClass(StringBuilder builder)
        {
            builder.AppendLine("}");
            builder.AppendLine();
            builder.AppendLine("/// <summary>");
            builder.AppendLine("/// 表示由源生成器生成的作用域实现。");
            builder.AppendLine("/// </summary>");
            builder.AppendLine("internal sealed class GeneratedMviScope : IMviScope");
            builder.AppendLine();
            builder.AppendLine("{");
            builder.AppendLine("    private readonly GeneratedMviContainer _container;");
            builder.AppendLine();
            builder.AppendLine("    public GeneratedMviScope(GeneratedMviContainer container)");
            builder.AppendLine("    {");
            builder.AppendLine("        _container = container;");
            builder.AppendLine("    }");
            builder.AppendLine();
            builder.AppendLine("    public TService Resolve<TService>() where TService : notnull => _container.Resolve<TService>();");
            builder.AppendLine("    public object Resolve(Type serviceType) => _container.Resolve(serviceType);");
            builder.AppendLine("    public TService CreateWith<TService>(params object[] args) where TService : notnull => _container.CreateWith<TService>(args);");
            builder.AppendLine("    public void Dispose() { }");
            builder.AppendLine("}");
        }
    }
}

/// <summary>
/// 为 <see cref="MviDiContainerGenerator.Models.GeneratedLifetime"/> 提供到 <c>ServiceLifetime</c> 枚举名的显式映射扩展。
/// 名称必须与 Domain 层 <c>ServiceLifetime</c> 枚举成员保持完全一致。
/// </summary>
internal static class GeneratedLifetimeExtensions
{
    /// <summary>
    /// 将 <see cref="MviDiContainerGenerator.Models.GeneratedLifetime"/> 映射为生成代码中 <c>ServiceLifetime</c> 枚举的成员名。
    /// </summary>
    /// <param name="lifetime">生成器内部生命周期枚举。</param>
    /// <returns><c>ServiceLifetime</c> 枚举成员名。</returns>
    public static string ToServiceLifetimeName(this MviDiContainerGenerator.Models.GeneratedLifetime lifetime)
    {
        return lifetime switch
        {
            MviDiContainerGenerator.Models.GeneratedLifetime.Singleton => "Singleton",
            MviDiContainerGenerator.Models.GeneratedLifetime.Scoped => "Scoped",
            MviDiContainerGenerator.Models.GeneratedLifetime.Transient => "Transient",
            _ => "Singleton",
        };
    }
}
