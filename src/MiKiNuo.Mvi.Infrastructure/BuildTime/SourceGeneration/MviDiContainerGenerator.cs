using System.Collections.Generic;
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
    /// <inheritdoc />
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

        string source = Emission.GenerateContainerSource(
            compilation.AssemblyName ?? string.Empty,
            services);
        context.AddSource("GeneratedMviContainer.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    /// <summary>
    /// 表示 <see cref="MviDiContainerGenerator"/> 的分析阶段：
    /// 从 <see cref="INamedTypeSymbol"/> 中提取 [DiService] 特性，转换为 <see cref="Models.DiServiceInfo"/>。
    /// </summary>
    internal static class Analysis
    {
        private static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.FullyQualifiedFormat
            .WithMiscellaneousOptions(
                SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
                | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

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
            foreach (AttributeData attr in classSymbol.GetAttributes())
            {
                string? attrName = attr.AttributeClass?.Name;
                if (attrName is not ("DiService" or "DiServiceAttribute"))
                {
                    continue;
                }

                string serviceTypeName = classSymbol.ToDisplayString(TypeFormat);
                string implementationTypeName = classSymbol.ToDisplayString(TypeFormat);
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
                        serviceTypeName = serviceType.ToDisplayString(TypeFormat);
                    }
                }

                IReadOnlyList<string> constructorArguments = BuildConstructorArguments(classSymbol);

                return new Models.DiServiceInfo(
                    serviceTypeName,
                    implementationTypeName,
                    lifetime,
                    @namespace,
                    constructorArguments);
            }

            return null;
        }

        /// <summary>
        /// 选择应使用的构造函数并生成 C# 实参表达式列表。
        /// 优先使用 <c>[DiConstructor]</c> 标记的构造函数；否则挑选参数数量最多的可解析构造函数。
        /// 每个参数生成 <c>this.Resolve&lt;T&gt;()</c>，由容器在运行时解析依赖。
        /// </summary>
        /// <param name="classSymbol">实现类符号。</param>
        /// <returns>实参表达式列表；如果无可用构造函数则返回空集合。</returns>
        private static IReadOnlyList<string> BuildConstructorArguments(INamedTypeSymbol classSymbol)
        {
            IMethodSymbol? selected = null;

            foreach (AttributeData attribute in classSymbol.GetAttributes())
            {
                if (attribute.AttributeClass?.Name is "DiConstructor" or "DiConstructorAttribute")
                {
                    if (attribute.AttributeConstructor is not null
                        && attribute.ApplicationSyntaxReference?.GetSyntax() is { } syntax)
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
                }
            }

            selected ??= classSymbol.Constructors
                .Where(static constructor => constructor.DeclaredAccessibility == Accessibility.Public)
                .OrderByDescending(static constructor => constructor.Parameters.Length)
                .FirstOrDefault();

            if (selected is null || selected.Parameters.Length == 0)
            {
                return System.Array.Empty<string>();
            }

            List<string> arguments = new(selected.Parameters.Length);
            foreach (IParameterSymbol parameter in selected.Parameters)
            {
                arguments.Add("this.Resolve<" + parameter.Type.ToDisplayString(TypeFormat) + ">()");
            }

            return arguments;
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
            public DiServiceInfo(
                string serviceTypeName,
                string implementationTypeName,
                GeneratedLifetime lifetime,
                string? @namespace,
                IReadOnlyList<string> constructorArgumentExpressions)
            {
                ServiceTypeName = serviceTypeName;
                ImplementationTypeName = implementationTypeName;
                Lifetime = lifetime;
                Namespace = @namespace;
                ConstructorArgumentExpressions = constructorArgumentExpressions;
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
        /// <returns>生成的 C# 源码。</returns>
        public static string GenerateContainerSource(
            string assemblyName,
            IReadOnlyList<Models.DiServiceInfo> services)
        {
            StringBuilder builder = new();
            string containerNamespace = string.IsNullOrEmpty(assemblyName) ? "GeneratedContainer" : assemblyName;

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

            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// 初始化由源生成器生成的泛型 DI 容器。");
            builder.AppendLine("    /// 仅注册服务描述符，不做服务实例化，避免构造期出现未注册依赖的级联失败。");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    public GeneratedMviContainer()");
            builder.AppendLine("    {");
            builder.AppendLine("    }");
            builder.AppendLine();
            builder.AppendLine("    /// <inheritdoc />");
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

            builder.AppendLine();
            builder.AppendLine("        throw new InvalidOperationException($\"未注册服务：{serviceType.FullName}\");");
            builder.AppendLine("    }");
            builder.AppendLine();
            builder.AppendLine("    /// <inheritdoc />");
            builder.AppendLine("    public IMviScope CreateScope()");
            builder.AppendLine("    {");
            builder.AppendLine("        return new GeneratedMviScope(this);");
            builder.AppendLine("    }");
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
            builder.AppendLine("    public void Dispose() { }");
            builder.AppendLine("}");

            return builder.ToString();
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
