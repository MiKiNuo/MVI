using Microsoft.CodeAnalysis;
namespace MiKiNuo.Mvi.Generators.SourceGeneration;
/// <summary>生成 Intent 到业务 Handler 方法的分派。</summary>
[Generator]
public sealed class MviIntentDispatchGenerator : IIncrementalGenerator
{
    /// <summary>登记生成流程。</summary>
    /// <param name="context">生成上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context) => MviMethodDispatch.Register(context, true);
}
