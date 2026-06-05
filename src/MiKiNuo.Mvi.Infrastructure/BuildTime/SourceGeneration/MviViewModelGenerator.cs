
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;

/// <summary>
/// 表示根据 MVI 绑定特性生成 ViewModel 重复代码的源生成器。
/// 分析阶段由 <see cref="MviViewModelAnalysis"/> 负责，发射阶段由 <see cref="MviViewModelEmission"/> 负责，
/// 数据模型由 <see cref="MviViewModelModels"/> 承载。
/// </summary>
[Generator]
public sealed class MviViewModelGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, Execute);
    }

    private static void Execute(SourceProductionContext context, Compilation compilation)
    {
        INamedTypeSymbol? viewModelBaseSymbol = compilation.GetTypeByMetadataName(
            "MiKiNuo.Mvi.Application.MVI.ViewModel.MviViewModelBase`3");

        if (viewModelBaseSymbol is null)
        {
            return;
        }

        foreach (INamedTypeSymbol viewModelSymbol in GeneratorSyntaxHelpers.EnumerateClassSymbols(
            compilation,
            context.CancellationToken))
        {
            MviViewModelModels.ViewModelDescriptor? descriptor = MviViewModelAnalysis.Collect(
                viewModelSymbol,
                viewModelBaseSymbol,
                context);

            if (descriptor is null)
            {
                continue;
            }

            string source = MviViewModelEmission.Emit(descriptor);
            context.AddSource(
                $"{descriptor.ViewModelSymbol.Name}.g.cs",
                SourceText.From(source, System.Text.Encoding.UTF8));
        }
    }
}
