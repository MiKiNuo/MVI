using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;

/// <summary>
/// 表示 MVI 源生成器共用的语法/符号工具集。
/// 消除 MviViewModelGenerator、MviReducerGeneratorBase、MviDiContainerGenerator 中重复的
/// "扫描语法树 → 提取类型符号" 模板代码。
/// </summary>
internal static class GeneratorSyntaxHelpers
{
    /// <summary>全限定可空类型格式。</summary>
    public static readonly SymbolDisplayFormat FullyQualifiedNullableFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .WithMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
            | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    /// <summary>格式化为全限定可空类型字符串。</summary>
    /// <param name="symbol">类型符号。</param>
    /// <returns>格式化后的字符串。</returns>
    public static string FormatFullyQualified(ITypeSymbol symbol)
    {
        return symbol.ToDisplayString(FullyQualifiedNullableFormat);
    }

    /// <summary>将首字母转为小写。</summary>
    /// <param name="value">原始字符串。</param>
    /// <returns>首字母小写的字符串。</returns>
    public static string ToCamelCase(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value.Length == 1
            ? value.ToLowerInvariant()
            : char.ToLowerInvariant(value[0]) + value.Substring(1);
    }

    /// <summary>
    /// 遍历编译中所有语法树的类声明，返回对应的 <see cref="INamedTypeSymbol"/>。
    /// 期间响应 <paramref name="cancellationToken"/> 并跳过无法解析语义符号的节点。
    /// </summary>
    /// <param name="compilation">编译对象。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>所有命名类型符号。</returns>
    public static IEnumerable<INamedTypeSymbol> EnumerateClassSymbols(
        Compilation compilation,
        System.Threading.CancellationToken cancellationToken)
    {
        if (compilation is null)
        {
            throw new ArgumentNullException(nameof(compilation));
        }

        foreach (SyntaxTree tree in compilation.SyntaxTrees)
        {
            SyntaxNode root = tree.GetRoot(cancellationToken);
            SemanticModel semanticModel = compilation.GetSemanticModel(tree);

            foreach (ClassDeclarationSyntax declaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (semanticModel.GetDeclaredSymbol(declaration, cancellationToken) is INamedTypeSymbol symbol)
                {
                    yield return symbol;
                }
            }
        }
    }

    /// <summary>
    /// 判断类声明上是否挂载了 <paramref name="candidateAttributeShortNames"/> 中任一名称的特性。
    /// 名称匹配忽略 "<c>Attribute</c>" 后缀，例如传入 "<c>DiService</c>" 可匹配 <c>[DiService]</c> 与 <c>[DiServiceAttribute]</c>。
    /// </summary>
    /// <param name="classDeclaration">待检查的类声明。</param>
    /// <param name="candidateAttributeShortNames">候选特性短名称集合。</param>
    /// <returns>如果类声明挂载了任一候选特性则返回 true。</returns>
    public static bool HasAttribute(
        ClassDeclarationSyntax classDeclaration,
        params string[] candidateAttributeShortNames)
    {
        if (classDeclaration is null)
        {
            throw new ArgumentNullException(nameof(classDeclaration));
        }

        if (candidateAttributeShortNames is null)
        {
            throw new ArgumentNullException(nameof(candidateAttributeShortNames));
        }

        foreach (AttributeListSyntax attributeList in classDeclaration.AttributeLists)
        {
            foreach (AttributeSyntax attribute in attributeList.Attributes)
            {
                string attributeName = attribute.Name.ToString();
                if (candidateAttributeShortNames.Any(name => IsAttributeMatch(attributeName, name)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>判断符号是否挂载任一候选特性。</summary>
    /// <param name="symbol">要检查的符号。</param>
    /// <param name="candidateAttributeShortNames">候选特性短名称。</param>
    /// <returns>是否匹配。</returns>
    public static bool HasAttribute(ISymbol symbol, params string[] candidateAttributeShortNames)
    {
        if (symbol is null)
        {
            throw new ArgumentNullException(nameof(symbol));
        }

        if (candidateAttributeShortNames is null)
        {
            throw new ArgumentNullException(nameof(candidateAttributeShortNames));
        }

        foreach (AttributeData attribute in symbol.GetAttributes())
        {
            string? attrName = attribute.AttributeClass?.Name;
            if (attrName is null)
            {
                continue;
            }

            foreach (string candidate in candidateAttributeShortNames)
            {
                if (IsAttributeMatch(attrName, candidate))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>查找符号上的首个匹配特性。</summary>
    /// <param name="symbol">要检查的符号。</param>
    /// <param name="candidateAttributeShortNames">候选特性短名称。</param>
    /// <returns>匹配的特性数据，未匹配返回 null。</returns>
    public static AttributeData? FindAttribute(ISymbol symbol, params string[] candidateAttributeShortNames)
    {
        if (symbol is null)
        {
            throw new ArgumentNullException(nameof(symbol));
        }

        if (candidateAttributeShortNames is null)
        {
            throw new ArgumentNullException(nameof(candidateAttributeShortNames));
        }

        foreach (AttributeData attribute in symbol.GetAttributes())
        {
            string? attrName = attribute.AttributeClass?.Name;
            if (attrName is null)
            {
                continue;
            }

            foreach (string candidate in candidateAttributeShortNames)
            {
                if (IsAttributeMatch(attrName, candidate))
                {
                    return attribute;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 判断编译中是否存在类挂载了 <paramref name="candidateAttributeShortNames"/> 中任一名称的特性。
    /// 用于在确认无需生成代码时短路 <see cref="IIncrementalGenerator"/>，避免空跑分析。
    /// </summary>
    /// <param name="compilation">编译对象。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <param name="candidateAttributeShortNames">候选特性短名称集合。</param>
    /// <returns>存在符合条件的类则返回 true。</returns>
    public static bool CompilationHasAttribute(
        Compilation compilation,
        System.Threading.CancellationToken cancellationToken,
        params string[] candidateAttributeShortNames)
    {
        if (compilation is null)
        {
            throw new ArgumentNullException(nameof(compilation));
        }

        if (candidateAttributeShortNames is null)
        {
            throw new ArgumentNullException(nameof(candidateAttributeShortNames));
        }

        foreach (SyntaxTree tree in compilation.SyntaxTrees)
        {
            SyntaxNode root = tree.GetRoot(cancellationToken);

            foreach (ClassDeclarationSyntax classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (HasAttribute(classDecl, candidateAttributeShortNames))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsAttributeMatch(string attributeName, string candidateShortName)
    {
        if (string.Equals(attributeName, candidateShortName, System.StringComparison.Ordinal))
        {
            return true;
        }

        return attributeName.Length == candidateShortName.Length + "Attribute".Length
            && attributeName.EndsWith("Attribute", System.StringComparison.Ordinal)
            && attributeName.StartsWith(candidateShortName, System.StringComparison.Ordinal);
    }
}
