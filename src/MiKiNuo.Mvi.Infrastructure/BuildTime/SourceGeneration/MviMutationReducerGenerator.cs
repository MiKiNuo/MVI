using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;

/// <summary>
/// 表示根据 <c>[MviReduceMutation]</c> 特性生成 <c>Reduce</c> 分发逻辑的源生成器。
/// 扫描继承 <c>MviMutationReducerBase&lt;,,&gt;</c> 的类中带特性的方法，生成分发 override。
/// </summary>
[Generator]
public sealed class MviMutationReducerGenerator : IIncrementalGenerator
{
    private const string ReduceMutationAttributeMetadataName = "MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceMutationAttribute";
    private const string MutationReducerBaseMetadataName = "MiKiNuo.Mvi.Application.MVI.Reducer.MviMutationReducerBase`3";
    private const string MutationReducerBaseDisplayFormat = "MiKiNuo.Mvi.Application.MVI.Reducer.MviMutationReducerBase<TState, TMutation, TEffect>";

    /// <summary>
    /// 初始化源生成器注册规约方法分析。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ReducerMethodModel> methods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ReduceMutationAttributeMetadataName,
                static (node, _) => node is MethodDeclarationSyntax,
                static (syntaxContext, _) => CollectMethod(syntaxContext))
            .Where(static model => model is not null)
            .Select(static (model, _) => model!);

        IncrementalValueProvider<ImmutableArray<ReducerMethodModel>> grouped = methods.Collect();

        context.RegisterSourceOutput(grouped, static (productionContext, source) =>
        {
            if (source.IsEmpty)
            {
                return;
            }

            Dictionary<INamedTypeSymbol, List<ReducerMethodModel>> byClass = new(SymbolEqualityComparer.Default);

            foreach (ReducerMethodModel method in source)
            {
                if (!byClass.TryGetValue(method.ContainingType, out List<ReducerMethodModel>? list))
                {
                    list = new List<ReducerMethodModel>();
                    byClass.Add(method.ContainingType, list);
                }

                list.Add(method);
            }

            foreach (KeyValuePair<INamedTypeSymbol, List<ReducerMethodModel>> entry in byClass)
            {
                productionContext.CancellationToken.ThrowIfCancellationRequested();
                ReducerClassModel? classModel = BuildClassModel(entry.Key, entry.Value);

                if (classModel is null)
                {
                    continue;
                }

                string code = EmitReducer(classModel);
                string fileName = $"{entry.Key.Name}.MutationReducer.g.cs";
                productionContext.AddSource(fileName, SourceText.From(code, Encoding.UTF8));
            }
        });
    }

    private static ReducerMethodModel? CollectMethod(GeneratorAttributeSyntaxContext syntaxContext)
    {
        if (syntaxContext.TargetSymbol is not IMethodSymbol methodSymbol)
        {
            return null;
        }

        if (methodSymbol.Parameters.Length != 2)
        {
            return null;
        }

        if (methodSymbol.ContainingType is null)
        {
            return null;
        }

        ITypeSymbol mutationType = methodSymbol.Parameters[1].Type;
        return new ReducerMethodModel(
            methodSymbol.ContainingType,
            methodSymbol.Name,
            mutationType.ToDisplayString(GeneratorSyntaxHelpers.FullyQualifiedNullableFormat));
    }

    private static ReducerClassModel? BuildClassModel(
        INamedTypeSymbol reducerType,
        IReadOnlyList<ReducerMethodModel> methods)
    {
        INamedTypeSymbol? baseType = ResolveMutationReducerBase(reducerType);

        if (baseType is null || baseType.TypeArguments.Length != 3)
        {
            return null;
        }

        if (baseType.TypeArguments[0] is not INamedTypeSymbol stateType)
        {
            return null;
        }

        if (baseType.TypeArguments[1] is not INamedTypeSymbol mutationType)
        {
            return null;
        }

        if (baseType.TypeArguments[2] is not INamedTypeSymbol effectType)
        {
            return null;
        }

        return new ReducerClassModel(
            reducerType,
            stateType,
            mutationType,
            effectType,
            methods);
    }

    private static INamedTypeSymbol? ResolveMutationReducerBase(INamedTypeSymbol reducerType)
    {
        INamedTypeSymbol? current = reducerType.BaseType;

        while (current is not null && current.SpecialType != SpecialType.System_Object)
        {
            if (current.IsGenericType
                && current.OriginalDefinition.ToDisplayString() == MutationReducerBaseDisplayFormat)
            {
                return current;
            }

            current = current.BaseType;
        }

        return null;
    }

    private static string EmitReducer(ReducerClassModel classModel)
    {
        string namespaceName = classModel.ReducerType.ContainingNamespace.ToDisplayString();
        string stateTypeName = classModel.StateType.ToDisplayString(GeneratorSyntaxHelpers.FullyQualifiedNullableFormat);
        string mutationTypeName = classModel.MutationType.ToDisplayString(GeneratorSyntaxHelpers.FullyQualifiedNullableFormat);
        string effectTypeName = classModel.EffectType.ToDisplayString(GeneratorSyntaxHelpers.FullyQualifiedNullableFormat);
        string reduceResultTypeName = $"global::MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult<{stateTypeName}, {effectTypeName}>";

        StringBuilder builder = new();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.Append("namespace ").Append(namespaceName).AppendLine(";");
        builder.AppendLine();
        builder.AppendLine("/// <summary>");
        builder.AppendLine("/// 表示由 MVI 源生成器生成的变更规约分发逻辑。");
        builder.AppendLine("/// </summary>");
        builder.Append("public sealed partial class ").Append(classModel.ReducerType.Name).AppendLine();
        builder.AppendLine("{");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// 执行变更到状态的规约分发。");
        builder.AppendLine("    /// </summary>");
        builder.Append("    public override ").Append(reduceResultTypeName).Append(" Reduce(")
            .Append(stateTypeName).Append(" state, ").Append(mutationTypeName).AppendLine(" mutation)");
        builder.AppendLine("    {");
        builder.AppendLine("        global::System.ArgumentNullException.ThrowIfNull(state);");
        builder.AppendLine("        global::System.ArgumentNullException.ThrowIfNull(mutation);");
        builder.AppendLine();
        builder.AppendLine("        return mutation switch");
        builder.AppendLine("        {");

        foreach (ReducerMethodModel method in classModel.Methods)
        {
            builder.Append("            ").Append(method.MutationTypeName).Append(" typedMutation => ")
                .Append(method.MethodName).AppendLine("(state, typedMutation),");
        }

        builder.Append("            _ => global::MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult.State<")
            .Append(stateTypeName).Append(", ").Append(effectTypeName).AppendLine(">(state)");
        builder.AppendLine("        };");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private sealed record ReducerMethodModel
    {
        public ReducerMethodModel(
            INamedTypeSymbol containingType,
            string methodName,
            string mutationTypeName)
        {
            ContainingType = containingType;
            MethodName = methodName;
            MutationTypeName = mutationTypeName;
        }

        /// <summary>所属规约器类型符号。</summary>
        public INamedTypeSymbol ContainingType { get; }

        /// <summary>方法名称。</summary>
        public string MethodName { get; }

        /// <summary>变更类型名称。</summary>
        public string MutationTypeName { get; }
    }

    private sealed record ReducerClassModel
    {
        public ReducerClassModel(
            INamedTypeSymbol reducerType,
            INamedTypeSymbol stateType,
            INamedTypeSymbol mutationType,
            INamedTypeSymbol effectType,
            IReadOnlyList<ReducerMethodModel> methods)
        {
            ReducerType = reducerType;
            StateType = stateType;
            MutationType = mutationType;
            EffectType = effectType;
            Methods = methods;
        }

        /// <summary>规约器类型符号。</summary>
        public INamedTypeSymbol ReducerType { get; }

        /// <summary>状态类型符号。</summary>
        public INamedTypeSymbol StateType { get; }

        /// <summary>变更类型符号。</summary>
        public INamedTypeSymbol MutationType { get; }

        /// <summary>副作用类型符号。</summary>
        public INamedTypeSymbol EffectType { get; }

        /// <summary>规约方法列表。</summary>
        public IReadOnlyList<ReducerMethodModel> Methods { get; }
    }
}
