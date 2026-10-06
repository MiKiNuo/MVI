using Microsoft.CodeAnalysis;
namespace MiKiNuo.Mvi.Generators.SourceGeneration;
/// <summary>生成 Mutation 到纯状态转换方法的分派，不再处理 Intent 或 Effect。</summary>
[Generator]
public sealed class MviReducerDispatchGenerator : IIncrementalGenerator
{
    /// <summary>登记生成流程。</summary>
    /// <param name="context">生成上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context) => MviMethodDispatch.Register(context, false);
}
