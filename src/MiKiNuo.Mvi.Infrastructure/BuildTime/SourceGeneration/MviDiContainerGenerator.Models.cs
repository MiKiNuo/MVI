using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;

/// <summary>
/// 表示 <see cref="MviDiContainerGenerator"/> 的数据模型部分。
/// </summary>
public sealed partial class MviDiContainerGenerator
{
    /// <summary>
    /// 表示 <see cref="MviDiContainerGenerator"/> 的数据模型集合。
    /// 与分析/发射逻辑解耦。
    /// </summary>
    internal static partial class Models
    {
        /// <summary>
        /// 表示发现的 DI 服务信息。
        /// </summary>
        public sealed class DiServiceInfo
        {
            /// <summary>
            /// 初始化 DI 服务信息。
            /// </summary>
            /// <param name="assemblyName">目标程序集名称。</param>
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
                string assemblyName,
                string serviceTypeName,
                string implementationTypeName,
                GeneratedLifetime lifetime,
                string? @namespace,
                IReadOnlyList<string> constructorArgumentExpressions,
                IReadOnlyList<string> constructorParameterTypeNames)
            {
                AssemblyName = assemblyName;
                ServiceTypeName = serviceTypeName;
                ImplementationTypeName = implementationTypeName;
                Lifetime = lifetime;
                Namespace = @namespace;
                ConstructorArgumentExpressions = constructorArgumentExpressions;
                ConstructorParameterTypeNames = constructorParameterTypeNames;
            }

            /// <summary>目标程序集名称。</summary>
            public string AssemblyName { get; }

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
