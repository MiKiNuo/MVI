using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;

/// <summary>
/// 表示规约器源生成器的抽象基类，提取了类型扫描、方法发现和代码生成的公共逻辑。
/// </summary>
public abstract class MviReducerGeneratorBase : IIncrementalGenerator
{
    /// <summary>
    /// 符号显示格式：完整限定名 + 可空修饰符。
    /// </summary>
    protected static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .WithMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
            | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    /// <summary>
    /// 初始化源生成器注册编译回调。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, Execute);
    }

    /// <summary>
    /// 执行生成逻辑：扫描目标类型 → 收集规约方法 → 生成源码。
    /// </summary>
    /// <param name="context">源生成上下文。</param>
    /// <param name="compilation">编译对象。</param>
    private void Execute(SourceProductionContext context, Compilation compilation)
    {
        foreach (INamedTypeSymbol targetType in GeneratorSyntaxHelpers.EnumerateClassSymbols(
            compilation,
            context.CancellationToken))
        {
            if (!IsTargetClass(targetType))
            {
                continue;
            }

            List<ReducerMethodModel> methods = GetReducerMethods(targetType);
            if (methods.Count == 0)
            {
                continue;
            }

            string source = GenerateSource(targetType, methods);
            context.AddSource(
                GetOutputFileName(targetType),
                SourceText.From(source, Encoding.UTF8));
        }
    }

    /// <summary>
    /// 判断指定类是否为生成目标。
    /// </summary>
    /// <param name="symbol">类型符号。</param>
    /// <returns>如果是目标类则返回 true。</returns>
    protected abstract bool IsTargetClass(INamedTypeSymbol symbol);

    /// <summary>
    /// 收集类型中的规约方法信息。
    /// </summary>
    /// <param name="symbol">类型符号。</param>
    /// <returns>规约方法信息列表。</returns>
    protected abstract List<ReducerMethodModel> GetReducerMethods(INamedTypeSymbol symbol);

    /// <summary>
    /// 生成目标源码。
    /// </summary>
    /// <param name="symbol">类型符号。</param>
    /// <param name="methods">规约方法信息列表。</param>
    /// <returns>生成的源码字符串。</returns>
    protected abstract string GenerateSource(
        INamedTypeSymbol symbol,
        IReadOnlyList<ReducerMethodModel> methods);

    /// <summary>
    /// 获取输出文件名。
    /// </summary>
    /// <param name="symbol">类型符号。</param>
    /// <returns>输出文件名。</returns>
    protected abstract string GetOutputFileName(INamedTypeSymbol symbol);

    /// <summary>
    /// 遍历 Intent 类型的继承链，找到根 Intent 类型。
    /// </summary>
    /// <param name="intentType">Intent 类型。</param>
    /// <returns>根 Intent 类型。</returns>
    protected static ITypeSymbol GetRootIntentType(ITypeSymbol intentType)
    {
        if (intentType is null)
        {
            throw new ArgumentNullException(nameof(intentType));
        }

        ITypeSymbol current = intentType;
        while (current.BaseType is not null && current.BaseType.SpecialType != SpecialType.System_Object)
        {
            current = current.BaseType;
        }

        return current;
    }

    /// <summary>
    /// 表示规约方法的信息模型。
    /// </summary>
    protected sealed record ReducerMethodModel
    {
        /// <summary>
        /// 初始化规约方法信息模型。
        /// </summary>
        public ReducerMethodModel(
            string methodName,
            string stateTypeName,
            string intentTypeName,
            string rootIntentTypeName,
            string effectTypeName)
        {
            MethodName = methodName;
            StateTypeName = stateTypeName;
            IntentTypeName = intentTypeName;
            RootIntentTypeName = rootIntentTypeName;
            EffectTypeName = effectTypeName;
        }

        /// <summary>方法名称。</summary>
        public string MethodName { get; }

        /// <summary>状态类型名称（完整限定名）。</summary>
        public string StateTypeName { get; }

        /// <summary>Intent 类型名称（完整限定名）。</summary>
        public string IntentTypeName { get; }

        /// <summary>根 Intent 类型名称（完整限定名）。</summary>
        public string RootIntentTypeName { get; }

        /// <summary>副作用类型名称（完整限定名）。</summary>
        public string EffectTypeName { get; }
    }
}