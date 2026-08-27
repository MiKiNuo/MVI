using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using MiKiNuo.Mvi.Infrastructure.BuildTime.Diagnostics;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;

/// <summary>
/// 表示为标记 [MviStateSlice] 的切片 record 生成 StatePath 入口的源生成器。
/// <para>
/// 切片按“构造参数名匹配状态属性图节点名称（忽略大小写）+ 类型一致”的规则解析，
/// 生成 &lt;StateName&gt;Slices 静态类，形成 DashboardStateSlices.MachinePanel 式的访问形态。
/// 解析不到唯一匹配时在编译期报告诊断错误。
/// </para>
/// </summary>
[Generator]
public sealed class MviStateSliceGenerator : IIncrementalGenerator
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
        INamedTypeSymbol? stateMarker = compilation.GetTypeByMetadataName(
            "MiKiNuo.Mvi.Domain.MVI.State.IMviState");
        if (stateMarker is null)
        {
            return;
        }

        Dictionary<SliceGroupKey, List<ResolvedSlice>> groups = new();
        foreach (INamedTypeSymbol sliceSymbol in GeneratorSyntaxHelpers.EnumerateTypeSymbols(
            compilation,
            context.CancellationToken))
        {
            AttributeData? attribute = GeneratorSyntaxHelpers.FindAttribute(sliceSymbol, "MviStateSlice");
            if (attribute is null)
            {
                continue;
            }

            ResolvedSlice? slice = Analysis.Resolve(sliceSymbol, attribute, stateMarker, context);
            if (slice is null)
            {
                continue;
            }

            SliceGroupKey key = new(slice.StateType, slice.NamespaceName);
            if (!groups.TryGetValue(key, out List<ResolvedSlice>? list))
            {
                list = new List<ResolvedSlice>();
                groups[key] = list;
            }

            list.Add(slice);
        }

        foreach (KeyValuePair<SliceGroupKey, List<ResolvedSlice>> group in groups)
        {
            string source = Emission.Emit(group.Key.StateType, group.Key.NamespaceName, group.Value);
            context.AddSource(
                GetHintName(group.Key),
                SourceText.From(source, Encoding.UTF8));
        }
    }

    private static string GetHintName(SliceGroupKey key)
    {
        string fullName = key.StateType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            + "." + (key.NamespaceName ?? "global");
        StringBuilder builder = new(fullName.Length + 20);
        foreach (char character in fullName)
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        return builder.Append(".Slices.g.cs").ToString();
    }

    private sealed class SliceGroupKey : System.IEquatable<SliceGroupKey>
    {
        public SliceGroupKey(INamedTypeSymbol stateType, string? namespaceName)
        {
            StateType = stateType;
            NamespaceName = namespaceName;
        }

        public INamedTypeSymbol StateType { get; }

        public string? NamespaceName { get; }

        public bool Equals(SliceGroupKey? other)
        {
            return other is not null
                && StateType.Equals(other.StateType, SymbolEqualityComparer.Default)
                && string.Equals(NamespaceName, other.NamespaceName, System.StringComparison.Ordinal);
        }

        public override bool Equals(object? obj) => Equals(obj as SliceGroupKey);

        public override int GetHashCode()
        {
            unchecked
            {
                return (SymbolEqualityComparer.Default.GetHashCode(StateType) * 397)
                    ^ (NamespaceName is null ? 0 : NamespaceName.GetHashCode());
            }
        }
    }

    /// <summary>
    /// 表示一个已解析的切片。
    /// </summary>
    internal sealed class ResolvedSlice
    {
        /// <summary>
        /// 初始化已解析切片。
        /// </summary>
        /// <param name="sliceSymbol">切片类型符号。</param>
        /// <param name="stateType">来源状态类型。</param>
        /// <param name="entryName">切片入口名。</param>
        /// <param name="namespaceName">切片所在命名空间。</param>
        /// <param name="argumentExpressions">按构造参数顺序排列的取值表达式。</param>
        public ResolvedSlice(
            INamedTypeSymbol sliceSymbol,
            INamedTypeSymbol stateType,
            string entryName,
            string? namespaceName,
            IReadOnlyList<string> argumentExpressions)
        {
            SliceSymbol = sliceSymbol;
            StateType = stateType;
            EntryName = entryName;
            NamespaceName = namespaceName;
            ArgumentExpressions = argumentExpressions;
        }

        /// <summary>切片类型符号。</summary>
        public INamedTypeSymbol SliceSymbol { get; }

        /// <summary>来源状态类型。</summary>
        public INamedTypeSymbol StateType { get; }

        /// <summary>切片入口名。</summary>
        public string EntryName { get; }

        /// <summary>切片所在命名空间。</summary>
        public string? NamespaceName { get; }

        /// <summary>按构造参数顺序排列的取值表达式。</summary>
        public IReadOnlyList<string> ArgumentExpressions { get; }
    }

    /// <summary>
    /// 表示分析阶段：解析切片构造参数到状态属性路径。
    /// </summary>
    internal static class Analysis
    {
        /// <summary>切片构造参数无法解析。</summary>
        public static readonly DiagnosticDescriptor SliceParameterUnresolvedRule = new(
            id: DiagnosticIdCatalog.MviStateSliceParameterUnresolved,
            title: "切片构造参数无法解析到状态属性路径",
            messageFormat: "切片“{0}”的构造参数“{1}”在状态“{2}”中找不到名称与类型均匹配的属性路径。",
            category: "MviStateSlice",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>切片构造参数存在歧义。</summary>
        public static readonly DiagnosticDescriptor SliceParameterAmbiguousRule = new(
            id: DiagnosticIdCatalog.MviStateSliceParameterAmbiguous,
            title: "切片构造参数匹配到多个状态属性路径",
            messageFormat: "切片“{0}”的构造参数“{1}”在状态“{2}”中匹配到多个属性路径：{3}。请重命名参数以消除歧义。",
            category: "MviStateSlice",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>切片声明不符合约定。</summary>
        public static readonly DiagnosticDescriptor SliceDeclarationInvalidRule = new(
            id: DiagnosticIdCatalog.MviStateSliceDeclarationInvalid,
            title: "切片声明不符合约定",
            messageFormat: "切片“{0}”必须声明位置参数主构造函数，且其 StateType 必须是实现 IMviState 的非泛型类型。",
            category: "MviStateSlice",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// 解析切片类型，返回可用于发射的模型。
        /// </summary>
        /// <param name="sliceSymbol">切片类型符号。</param>
        /// <param name="attribute">切片特性数据。</param>
        /// <param name="stateMarker">IMviState 标记接口符号。</param>
        /// <param name="context">源生成上下文。</param>
        /// <returns>解析成功的切片模型，失败返回 null。</returns>
        public static ResolvedSlice? Resolve(
            INamedTypeSymbol sliceSymbol,
            AttributeData attribute,
            INamedTypeSymbol stateMarker,
            SourceProductionContext context)
        {
            INamedTypeSymbol? stateType = attribute.ConstructorArguments.Length > 0
                ? attribute.ConstructorArguments[0].Value as INamedTypeSymbol
                : null;

            IMethodSymbol? primaryConstructor = FindPrimaryConstructor(sliceSymbol);
            if (stateType is null
                || IsGeneric(stateType)
                || IsGeneric(sliceSymbol)
                || !StatePathGraph.IsNamespaceAccessible(stateType)
                || !StatePathGraph.IsNamespaceAccessible(sliceSymbol)
                || !stateType.AllInterfaces.Any(i => i.Equals(stateMarker, SymbolEqualityComparer.Default))
                || primaryConstructor is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    SliceDeclarationInvalidRule,
                    sliceSymbol.Locations.FirstOrDefault(),
                    sliceSymbol.Name));
                return null;
            }

            List<StatePathNode> candidates = StatePathGraph.Flatten(
                StatePathGraph.Expand(stateType, static _ => { }));

            List<string> arguments = new();
            foreach (IParameterSymbol parameter in primaryConstructor.Parameters)
            {
                List<StatePathNode> matches = candidates
                    .Where(node => string.Equals(
                        node.Name,
                        parameter.Name,
                        System.StringComparison.OrdinalIgnoreCase)
                        && node.ValueType.Equals(parameter.Type, SymbolEqualityComparer.Default))
                    .ToList();

                if (matches.Count == 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        SliceParameterUnresolvedRule,
                        sliceSymbol.Locations.FirstOrDefault(),
                        sliceSymbol.Name,
                        parameter.Name,
                        stateType.Name));
                    return null;
                }

                if (matches.Count > 1)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        SliceParameterAmbiguousRule,
                        sliceSymbol.Locations.FirstOrDefault(),
                        sliceSymbol.Name,
                        parameter.Name,
                        stateType.Name,
                        string.Join(", ", matches.Select(static node => node.DisplayPath))));
                    return null;
                }

                arguments.Add(matches[0].AccessPath);
            }

            return new ResolvedSlice(
                sliceSymbol,
                stateType,
                GetEntryName(sliceSymbol),
                GeneratorSyntaxHelpers.GetNamespaceForEmit(sliceSymbol),
                arguments);
        }

        private static IMethodSymbol? FindPrimaryConstructor(INamedTypeSymbol sliceSymbol)
        {
            IMethodSymbol? best = null;
            foreach (IMethodSymbol constructor in sliceSymbol.InstanceConstructors)
            {
                if (constructor.Parameters.Length == 0)
                {
                    continue;
                }

                // 排除 record 的拷贝构造函数。
                if (constructor.Parameters.Length == 1
                    && constructor.Parameters[0].Type.Equals(sliceSymbol, SymbolEqualityComparer.Default))
                {
                    continue;
                }

                if (best is null || constructor.Parameters.Length > best.Parameters.Length)
                {
                    best = constructor;
                }
            }

            return best;
        }

        private static string GetEntryName(INamedTypeSymbol sliceSymbol)
        {
            const string suffix = "State";
            string name = sliceSymbol.Name;
            return name.Length > suffix.Length && name.EndsWith(suffix, System.StringComparison.Ordinal)
                ? name.Substring(0, name.Length - suffix.Length)
                : name;
        }

        private static bool IsGeneric(INamedTypeSymbol symbol)
        {
            for (INamedTypeSymbol? current = symbol; current is not null; current = current.ContainingType)
            {
                if (current.IsGenericType)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// 表示代码发射阶段：按状态与命名空间分组生成 Slices 静态类。
    /// </summary>
    internal static class Emission
    {
        /// <summary>
        /// 发射一组切片的 Slices 静态类源码。
        /// </summary>
        /// <param name="stateType">来源状态类型。</param>
        /// <param name="namespaceName">目标命名空间。</param>
        /// <param name="slices">切片列表。</param>
        /// <returns>生成的源代码。</returns>
        public static string Emit(
            INamedTypeSymbol stateType,
            string? namespaceName,
            IReadOnlyList<ResolvedSlice> slices)
        {
            string className = MviStatePathsGenerator.Emission.GetGeneratedBaseName(stateType) + "Slices";
            string stateTypeName = GeneratorSyntaxHelpers.FormatFullyQualified(stateType);

            StringBuilder builder = new();
            builder.AppendLine("// <auto-generated/>");
            builder.AppendLine("#nullable enable");
            builder.AppendLine("#pragma warning disable");
            builder.AppendLine();

            if (namespaceName is not null)
            {
                builder.Append("namespace ").Append(namespaceName).AppendLine(";");
                builder.AppendLine();
            }

            builder.AppendLine("/// <summary>");
            builder.Append("/// 表示 ").Append(stateType.Name).AppendLine(" 的状态切片入口，由源生成器产出。");
            builder.AppendLine("/// </summary>");
            builder.Append("public static class ").AppendLine(className);
            builder.AppendLine("{");

            foreach (ResolvedSlice slice in slices)
            {
                string sliceTypeName = GeneratorSyntaxHelpers.FormatFullyQualified(slice.SliceSymbol);
                string statePathType = "global::MiKiNuo.Mvi.Domain.MVI.State.StatePath<"
                    + stateTypeName + ", " + sliceTypeName + ">";

                builder.AppendLine("    /// <summary>");
                builder.Append("    /// 表示切片 ").Append(slice.EntryName).AppendLine("。");
                builder.AppendLine("    /// </summary>");
                builder.Append("    public static readonly ").Append(statePathType).Append(' ')
                    .Append(slice.EntryName).AppendLine(" =");
                builder.Append("        ").Append(statePathType).AppendLine(".Create(");
                builder.Append("            \"")
                    .Append(GeneratorSyntaxHelpers.EscapeStringLiteral(slice.EntryName)).AppendLine("\",");
                builder.Append("            static state => new ").Append(sliceTypeName).Append('(');
                builder.Append(string.Join(", ", slice.ArgumentExpressions));
                builder.AppendLine("));");
                builder.AppendLine();
            }

            builder.AppendLine("}");
            return builder.ToString();
        }
    }
}
