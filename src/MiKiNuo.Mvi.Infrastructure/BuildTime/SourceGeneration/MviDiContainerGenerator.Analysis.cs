using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;

/// <summary>
/// 表示 <see cref="MviDiContainerGenerator"/> 的 DiService 发现部分。
/// </summary>
public sealed partial class MviDiContainerGenerator
{
    /// <summary>
    /// 表示 <see cref="MviDiContainerGenerator"/> 的分析阶段：
    /// 从 <see cref="INamedTypeSymbol"/> 中提取 [DiService] 特性，转换为 <see cref="Models.DiServiceInfo"/>。
    /// </summary>
    internal static partial class Analysis
    {
        /// <summary>
        /// 将单个标记 [DiService] 的类型解析为生成模型。
        /// </summary>
        /// <param name="classSymbol">类型符号。</param>
        /// <returns>DI 服务信息；特性缺失时返回 <c>null</c>。</returns>
        public static Models.DiServiceInfo? ParseDiService(INamedTypeSymbol classSymbol)
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
                classSymbol.ContainingAssembly.Name,
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
    }
}
