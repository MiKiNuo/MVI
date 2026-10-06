using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace MiKiNuo.Mvi.Generators.SourceGeneration;

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

            IReadOnlyList<string> constructorParameterTypeNames = BuildConstructorParameterTypeNames(
                classSymbol,
                honorDiConstructorAttribute: true,
                stripTopLevelNullableAnnotation: false);

            return new Models.DiServiceInfo(
                classSymbol.ContainingAssembly.Name,
                serviceTypeName,
                implementationTypeName,
                lifetime,
                @namespace,
                constructorParameterTypeNames);
        }

        /// <summary>
        /// 构造依赖事实的共享构建：选择构造函数并记录参数类型的完整限定名。
        /// 普通 DI 服务传入 <paramref name="honorDiConstructorAttribute"/> 为真，优先使用
        /// <c>[DiConstructor]</c> 标记的构造函数；Feature 组件传入为假，仅挑选参数数量最多的公共构造函数。
        /// <paramref name="stripTopLevelNullableAnnotation"/> 为真时移除参数类型的顶层可空注解
        /// （Feature 组件策略：实例服务表按去注解后的类型解析）。
        /// 实参表达式由发射端按解析接收方（容器 / 作用域 / 实例服务表）渲染；
        /// 参数类型名同时供 <c>CreateWith</c> 做 <c>args[i] is T</c> 模式匹配。
        /// </summary>
        /// <param name="classSymbol">实现类符号。</param>
        /// <param name="honorDiConstructorAttribute">是否优先采用 <c>[DiConstructor]</c> 标记的构造函数。</param>
        /// <param name="stripTopLevelNullableAnnotation">是否移除参数类型的顶层可空注解。</param>
        /// <returns>参数类型完整限定名列表（按构造函数参数顺序）；无可用构造函数时为空。</returns>
        internal static IReadOnlyList<string> BuildConstructorParameterTypeNames(
            INamedTypeSymbol classSymbol,
            bool honorDiConstructorAttribute,
            bool stripTopLevelNullableAnnotation)
        {
            IMethodSymbol? selected = null;

            if (honorDiConstructorAttribute)
            {
                selected = classSymbol.Constructors
                    .FirstOrDefault(static constructor =>
                        GeneratorSyntaxHelpers.FindAttribute(constructor, "DiConstructor") is not null);
            }

            selected ??= classSymbol.Constructors
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
                if (stripTopLevelNullableAnnotation
                    && parameterType.NullableAnnotation == NullableAnnotation.Annotated)
                {
                    parameterType = parameterType.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
                }

                parameterTypeNames.Add(parameterType.ToDisplayString(GeneratorSyntaxHelpers.FullyQualifiedNullableFormat));
            }

            return parameterTypeNames;
        }
    }
}
