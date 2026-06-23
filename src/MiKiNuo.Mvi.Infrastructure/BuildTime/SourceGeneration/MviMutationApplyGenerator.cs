using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;

/// <summary>
/// 表示根据 <c>[MviMutation]</c> 特性生成 <c>State.Apply(Mutation)</c> 扩展方法的源生成器。
/// 扫描带 <c>[MviMutation(Path, Op, Source)]</c> 的变更记录，自动生成状态更新代码。
/// </summary>
[Generator]
public sealed class MviMutationApplyGenerator : IIncrementalGenerator
{
    private const string MutationAttributeMetadataName = "MiKiNuo.Mvi.Domain.MVI.Mutation.MviMutationAttribute";
    private const string MutationInterfaceDisplayFormat = "MiKiNuo.Mvi.Domain.MVI.Mutation.IMviMutation<TState>";

    /// <summary>
    /// 初始化源生成器注册变更特性分析。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<MutationModel> mutations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                MutationAttributeMetadataName,
                static (node, _) => node is Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax,
                static (syntaxContext, _) => CollectMutation(syntaxContext))
            .Where(static model => model is not null)
            .Select(static (model, _) => model!);

        IncrementalValueProvider<ImmutableArray<MutationModel>> grouped = mutations.Collect();

        context.RegisterSourceOutput(grouped, static (productionContext, source) =>
        {
            if (source.IsEmpty)
            {
                return;
            }

            Dictionary<INamedTypeSymbol, List<MutationModel>> byState = new(SymbolEqualityComparer.Default);

            foreach (MutationModel mutation in source)
            {
                if (!byState.TryGetValue(mutation.StateType, out List<MutationModel>? list))
                {
                    list = new List<MutationModel>();
                    byState.Add(mutation.StateType, list);
                }

                list.Add(mutation);
            }

            foreach (KeyValuePair<INamedTypeSymbol, List<MutationModel>> entry in byState)
            {
                productionContext.CancellationToken.ThrowIfCancellationRequested();
                string code = EmitExtensions(entry.Key, entry.Value);
                string fileName = $"{entry.Key.Name}MutationExtensions.g.cs";
                productionContext.AddSource(fileName, SourceText.From(code, Encoding.UTF8));
            }
        });
    }

    private static MutationModel? CollectMutation(GeneratorAttributeSyntaxContext syntaxContext)
    {
        if (syntaxContext.TargetSymbol is not INamedTypeSymbol mutationType)
        {
            return null;
        }

        if (syntaxContext.Attributes.Length == 0)
        {
            return null;
        }

        AttributeData attribute = syntaxContext.Attributes[0];
        INamedTypeSymbol? mutationInterface = ResolveMutationInterface(mutationType);

        if (mutationInterface is null || mutationInterface.TypeArguments.Length != 1)
        {
            return null;
        }

        if (mutationInterface.TypeArguments[0] is not INamedTypeSymbol stateType)
        {
            return null;
        }

        return ParseMutationModel(mutationType, attribute, stateType);
    }

    private static INamedTypeSymbol? ResolveMutationInterface(INamedTypeSymbol mutationType)
    {
        INamedTypeSymbol? current = mutationType;

        while (current is not null && current.SpecialType != SpecialType.System_Object)
        {
            foreach (INamedTypeSymbol interfaceType in current.AllInterfaces)
            {
                if (interfaceType.IsGenericType
                    && interfaceType.OriginalDefinition.ToDisplayString() == MutationInterfaceDisplayFormat)
                {
                    return interfaceType;
                }
            }

            current = current.BaseType;
        }

        return null;
    }

    private static string ResolveOpName(TypedConstant value)
    {
        if (value.Value is int intValue)
        {
            return intValue switch
            {
                1 => "Add",
                2 => "Append",
                _ => "Set",
            };
        }

        return "Set";
    }

    private static MutationModel ParseMutationModel(
        INamedTypeSymbol mutationType,
        AttributeData attribute,
        INamedTypeSymbol stateType)
    {
        string path = "Value";
        string op = "Set";
        string source = "Value";

        foreach (KeyValuePair<string, TypedConstant> named in attribute.NamedArguments)
        {
            if (named.Key == "Path")
            {
                path = named.Value.Value?.ToString() ?? "Value";
            }
            else if (named.Key == "Op")
            {
                op = ResolveOpName(named.Value);
            }
            else if (named.Key == "Source")
            {
                source = named.Value.Value?.ToString() ?? "Value";
            }
        }

        return new MutationModel(
            mutationType,
            stateType,
            path,
            op,
            source);
    }

    private static string EmitExtensions(INamedTypeSymbol stateType, IReadOnlyList<MutationModel> mutations)
    {
        string stateTypeName = stateType.ToDisplayString(GeneratorSyntaxHelpers.FullyQualifiedNullableFormat);
        string namespaceName = stateType.ContainingNamespace.ToDisplayString();
        string className = $"{stateType.Name}MutationExtensions";

        StringBuilder builder = new();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.Append("namespace ").Append(namespaceName).AppendLine(";");
        builder.AppendLine();
        builder.AppendLine("/// <summary>");
        builder.AppendLine("/// 表示由 MVI 源生成器生成的变更应用扩展方法。");
        builder.AppendLine("/// </summary>");
        builder.Append("public static class ").Append(className).AppendLine();
        builder.AppendLine("{");

        for (int i = 0; i < mutations.Count; i++)
        {
            MutationModel mutation = mutations[i];
            string mutationTypeName = mutation.MutationType.ToDisplayString(GeneratorSyntaxHelpers.FullyQualifiedNullableFormat);
            string valueExpression = BuildValueExpression(mutation);

            builder.AppendLine("    /// <summary>");
            builder.AppendLine($"    /// 应用 {mutation.MutationType.Name} 变更到状态。");
            builder.AppendLine("    /// </summary>");
            builder.Append("    public static ").Append(stateTypeName).Append(" Apply(this ")
                .Append(stateTypeName).Append(" state, ").Append(mutationTypeName).AppendLine(" mutation)")
                .Append("        => ").Append(valueExpression).AppendLine(";");

            if (i < mutations.Count - 1)
            {
                builder.AppendLine();
            }
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string BuildValueExpression(MutationModel mutation)
    {
        string[] segments = mutation.Path.Split('.');
        string mutationSource = $"mutation.{mutation.Source}";

        if (segments.Length == 1)
        {
            return BuildWithExpression("state", segments[0], mutation.Op, mutationSource);
        }

        return BuildNestedWithExpression("state", segments, 0, mutation.Op, mutationSource);
    }

    private static string BuildNestedWithExpression(
        string root,
        string[] segments,
        int index,
        string op,
        string mutationSource)
    {
        string property = segments[index];
        string childAccess = $"{root}.{property}";

        if (index == segments.Length - 1)
        {
            return BuildWithExpression(root, property, op, mutationSource, childAccess);
        }

        string innerWith = BuildNestedWithExpression(childAccess, segments, index + 1, op, mutationSource);
        return $"{root} with {{ {property} = {innerWith} }}";
    }

    private static string BuildWithExpression(string root, string property, string op, string mutationSource)
    {
        return BuildWithExpression(root, property, op, mutationSource, root);
    }

    private static string BuildWithExpression(
        string root,
        string property,
        string op,
        string mutationSource,
        string stateAccess)
    {
        string value = op switch
        {
            "Add" => $"{stateAccess}.{property} + {mutationSource}",
            "Append" => $"{stateAccess}.{property} + {mutationSource}",
            _ => mutationSource,
        };

        return $"{root} with {{ {property} = {value} }}";
    }

    private sealed record MutationModel
    {
        public MutationModel(
            INamedTypeSymbol mutationType,
            INamedTypeSymbol stateType,
            string path,
            string op,
            string source)
        {
            MutationType = mutationType;
            StateType = stateType;
            Path = path;
            Op = op;
            Source = source;
        }

        /// <summary>变更类型符号。</summary>
        public INamedTypeSymbol MutationType { get; }

        /// <summary>状态类型符号。</summary>
        public INamedTypeSymbol StateType { get; }

        /// <summary>状态字段路径。</summary>
        public string Path { get; }

        /// <summary>变更操作类型。</summary>
        public string Op { get; }

        /// <summary>变更值来源字段名。</summary>
        public string Source { get; }
    }
}
