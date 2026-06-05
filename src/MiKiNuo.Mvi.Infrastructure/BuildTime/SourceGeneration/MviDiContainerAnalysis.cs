using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;

/// <summary>
/// 表示 <see cref="MviDiContainerGenerator"/> 的分析阶段：
/// 从 <see cref="INamedTypeSymbol"/> 中提取 [DiService] 特性，转换为 <see cref="MviDiContainerModels.DiServiceInfo"/>。
/// </summary>
internal static class MviDiContainerAnalysis
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
    public static List<MviDiContainerModels.DiServiceInfo> Discover(
        Compilation compilation,
        System.Threading.CancellationToken cancellationToken)
    {
        List<MviDiContainerModels.DiServiceInfo> result = new();

        foreach (INamedTypeSymbol classSymbol in GeneratorSyntaxHelpers.EnumerateClassSymbols(compilation, cancellationToken))
        {
            MviDiContainerModels.DiServiceInfo? info = ParseDiService(classSymbol);
            if (info is not null)
            {
                result.Add(info);
            }
        }

        return result;
    }

    private static MviDiContainerModels.DiServiceInfo? ParseDiService(INamedTypeSymbol classSymbol)
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
            MviDiContainerModels.GeneratedLifetime lifetime = MviDiContainerModels.GeneratedLifetime.Singleton;
            string? @namespace = classSymbol.ContainingNamespace?.ToDisplayString();

            if (attr.ConstructorArguments.Length > 0
                && attr.ConstructorArguments[0].Value is int lifetimeValue)
            {
                lifetime = lifetimeValue switch
                {
                    0 => MviDiContainerModels.GeneratedLifetime.Singleton,
                    1 => MviDiContainerModels.GeneratedLifetime.Scoped,
                    2 => MviDiContainerModels.GeneratedLifetime.Transient,
                    _ => MviDiContainerModels.GeneratedLifetime.Singleton,
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

            return new MviDiContainerModels.DiServiceInfo(serviceTypeName, implementationTypeName, lifetime, @namespace);
        }

        return null;
    }
}
