using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;

/// <summary>
/// 表示 MVI 状态属性图的展开规则与结果模型。
/// <para>
/// 供 <c>MviStatePathsGenerator</c> 与 <c>MviViewModelGenerator</c>
/// 共用，保证“哪些属性是叶子、哪些是分支”
/// 在所有生成器中判定一致。
/// </para>
/// </summary>
internal static class StatePathGraph
{
    /// <summary>分支节点上指向自身的路径成员名。</summary>
    public const string SelfPathName = "Path";

    /// <summary>当分支下已存在名为 Path 的子成员时，自身路径改用的备用名。</summary>
    public const string SelfPathFallbackName = "Self";

    /// <summary>
    /// 判断属性是否为可展开的分支节点。
    /// 规则：可读、非静态、非索引器、非可空引用，且属性类型为可展开类型。
    /// </summary>
    /// <param name="property">属性符号。</param>
    /// <returns>为分支节点返回 true。</returns>
    public static bool IsExpandableProperty(IPropertySymbol property)
    {
        if (property is null)
        {
            throw new ArgumentNullException(nameof(property));
        }

        if (property.IsStatic || property.IsIndexer || property.GetMethod is null)
        {
            return false;
        }

        // 可空引用类型中间节点不展开：避免生成空传播路径，保持 TValue 非空语义。
        if (property.Type.IsReferenceType
            && property.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return false;
        }

        return IsExpandableType(property.Type);
    }

    /// <summary>
    /// 判断类型是否可展开为路径分支。
    /// 规则：源码内声明的非泛型 class/struct，非 BCL 特殊类型，非集合。
    /// </summary>
    /// <param name="type">类型符号。</param>
    /// <returns>可展开返回 true。</returns>
    public static bool IsExpandableType(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol named)
        {
            return false;
        }

        if (named.IsGenericType || named.SpecialType != SpecialType.None)
        {
            return false;
        }

        if (named.TypeKind != TypeKind.Class && named.TypeKind != TypeKind.Struct)
        {
            return false;
        }

        if (!named.Locations.Any(static location => location.IsInSource))
        {
            return false;
        }

        if (named.AllInterfaces.Any(static i => i.SpecialType == SpecialType.System_Collections_IEnumerable))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 展开状态属性图，返回按声明顺序组织的根级节点。
    /// 沿当前路径检测循环引用，命中时通过 <paramref name="onCycle"/> 回调并跳过该分支。
    /// </summary>
    /// <param name="stateType">状态类型符号。</param>
    /// <param name="onCycle">循环引用回调，参数为触发循环的类型符号。</param>
    /// <returns>根级路径节点列表。</returns>
    public static List<StatePathNode> Expand(
        INamedTypeSymbol stateType,
        Action<INamedTypeSymbol> onCycle)
    {
        if (stateType is null)
        {
            throw new ArgumentNullException(nameof(stateType));
        }

        List<StatePathNode> roots = new();
        HashSet<INamedTypeSymbol> branchStack = new(SymbolEqualityComparer.Default)
        {
            stateType,
        };

        ExpandInto(stateType, "state", string.Empty, branchStack, roots, onCycle);
        return roots;
    }

    /// <summary>
    /// 收集节点树中的全部叶子节点（含分支自身，按先序遍历）。
    /// </summary>
    /// <param name="nodes">节点列表。</param>
    /// <returns>先序展开的全部节点。</returns>
    public static List<StatePathNode> Flatten(IReadOnlyList<StatePathNode> nodes)
    {
        List<StatePathNode> result = new();
        foreach (StatePathNode node in nodes)
        {
            result.Add(node);
            if (node.IsBranch)
            {
                result.AddRange(Flatten(node.Children));
            }
        }

        return result;
    }

    /// <summary>
    /// 转义 C# 关键字标识符。
    /// </summary>
    /// <param name="identifier">原始标识符。</param>
    /// <returns>可安全放入源码的标识符。</returns>
    public static string EscapeIdentifier(string identifier)
    {
        return SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None
            ? "@" + identifier
            : identifier;
    }

    /// <summary>
    /// 判断类型是否可被命名空间级别的生成类引用（全链 public/internal）。
    /// 私有或受保护的嵌套类型无法被生成代码引用，生成器应直接跳过。
    /// </summary>
    /// <param name="symbol">类型符号。</param>
    /// <returns>可被生成代码引用返回 true。</returns>
    public static bool IsNamespaceAccessible(INamedTypeSymbol symbol)
    {
        for (INamedTypeSymbol? current = symbol; current is not null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 解析状态顶层属性名到生成的 StatePaths 类成员访问表达式。
    /// 供 ViewModel 生成器把 CanExecute 观察流改接到生成的 StatePath 上。
    /// </summary>
    /// <param name="stateType">状态类型符号。</param>
    /// <param name="propertyName">顶层属性名。</param>
    /// <returns>成员访问表达式（如 global::Ns.LoginStatePaths.CanSubmit）；无法解析时返回 null。</returns>
    public static string? TryGetTopLevelPathExpression(
        INamedTypeSymbol? stateType,
        string propertyName)
    {
        if (stateType is null || string.IsNullOrWhiteSpace(propertyName))
        {
            return null;
        }

        for (INamedTypeSymbol? current = stateType; current is not null; current = current.ContainingType)
        {
            if (current.IsGenericType)
            {
                return null;
            }
        }

        List<StatePathNode> roots = Expand(stateType, static _ => { });
        StatePathNode? node = roots.FirstOrDefault(
            n => string.Equals(n.Name, propertyName, System.StringComparison.Ordinal));
        if (node is null)
        {
            return null;
        }

        string className = MviStatePathsGenerator.Emission.GetGeneratedBaseName(stateType) + "Paths";
        string? namespaceName = GeneratorSyntaxHelpers.GetNamespaceForEmit(stateType);
        string classReference = namespaceName is null
            ? "global::" + className
            : "global::" + namespaceName + "." + className;

        string memberName = EscapeIdentifier(node.Name);
        if (!node.IsBranch)
        {
            return classReference + "." + memberName;
        }

        string selfName = node.Children.Any(static child => child.Name == SelfPathName)
            ? SelfPathFallbackName
            : SelfPathName;
        return classReference + "." + memberName + "." + selfName;
    }

    private static void ExpandInto(
        INamedTypeSymbol current,
        string accessPrefix,
        string displayPrefix,
        HashSet<INamedTypeSymbol> branchStack,
        List<StatePathNode> output,
        Action<INamedTypeSymbol> onCycle)
    {
        foreach (IPropertySymbol property in EnumerateReadableProperties(current))
        {
            string accessPath = accessPrefix + "." + EscapeIdentifier(property.Name);
            string displayPath = displayPrefix.Length == 0
                ? property.Name
                : displayPrefix + "." + property.Name;

            if (!IsExpandableProperty(property))
            {
                output.Add(new StatePathNode(
                    property.Name,
                    displayPath,
                    accessPath,
                    property.Type,
                    isBranch: false));
                continue;
            }

            INamedTypeSymbol branchType = (INamedTypeSymbol)property.Type;
            if (!branchStack.Add(branchType))
            {
                onCycle(branchType);
                continue;
            }

            StatePathNode branch = new(
                property.Name,
                displayPath,
                accessPath,
                property.Type,
                isBranch: true);
            output.Add(branch);

            ExpandInto(branchType, accessPath, displayPath, branchStack, branch.Children, onCycle);
            branchStack.Remove(branchType);
        }
    }

    private static IEnumerable<IPropertySymbol> EnumerateReadableProperties(INamedTypeSymbol type)
    {
        HashSet<string> seen = new(System.StringComparer.Ordinal);
        for (INamedTypeSymbol? current = type;
            current is not null && current.SpecialType != SpecialType.System_Object;
            current = current.BaseType)
        {
            foreach (IPropertySymbol property in current.GetMembers().OfType<IPropertySymbol>())
            {
                if (property.IsStatic || property.IsIndexer || property.GetMethod is null)
                {
                    continue;
                }

                if (property.GetMethod.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }

                if (seen.Add(property.Name))
                {
                    yield return property;
                }
            }
        }
    }
}

/// <summary>
/// 表示状态属性图中的一个节点。
/// </summary>
internal sealed class StatePathNode
{
    /// <summary>
    /// 初始化状态路径节点。
    /// </summary>
    /// <param name="name">属性名。</param>
    /// <param name="displayPath">显示路径，例如 "Machine.Speed"。</param>
    /// <param name="accessPath">取值表达式，例如 "state.Machine.Speed"。</param>
    /// <param name="valueType">节点值类型。</param>
    /// <param name="isBranch">是否为可展开的分支节点。</param>
    public StatePathNode(
        string name,
        string displayPath,
        string accessPath,
        ITypeSymbol valueType,
        bool isBranch)
    {
        Name = name;
        DisplayPath = displayPath;
        AccessPath = accessPath;
        ValueType = valueType;
        IsBranch = isBranch;
    }

    /// <summary>属性名。</summary>
    public string Name { get; }

    /// <summary>显示路径。</summary>
    public string DisplayPath { get; }

    /// <summary>取值表达式。</summary>
    public string AccessPath { get; }

    /// <summary>节点值类型。</summary>
    public ITypeSymbol ValueType { get; }

    /// <summary>是否为分支节点。</summary>
    public bool IsBranch { get; }

    /// <summary>分支节点的子节点。</summary>
    public List<StatePathNode> Children { get; } = new();
}
