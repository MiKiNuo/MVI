using MiKiNuo.Mvi.SourceGen.Middleware;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.SourceGen.Tests;

public sealed class MviMiddlewarePipelineGeneratorTests
{
    [Test]
    public async Task Generator_emits_ordered_middleware_pipeline_factory()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using MiKiNuo.Mvi.Abstractions;
            using MiKiNuo.Mvi.Abstractions.Generation;
            using MiKiNuo.Mvi.Core.Middleware;
            using MiKiNuo.Mvi.Core.Reducers;

            namespace Demo;

            public sealed record LoginState : IMviState;
            public abstract partial record LoginIntent : IMviIntent
            {
                public abstract int Kind { get; }
            }
            public sealed record LoginEffect : IMviEffect;

            [MviMiddleware(20)]
            public sealed class SecondMiddleware : IMviMiddleware<LoginState, LoginIntent, LoginEffect>
            {
                public ValueTask<ReduceResult<LoginState, LoginEffect>> InvokeAsync(
                    MviMiddlewareContext<LoginState, LoginIntent, LoginEffect> context,
                    MviContinuation<LoginState, LoginIntent, LoginEffect> continuation,
                    CancellationToken cancellationToken) => continuation(context, cancellationToken);
            }

            [MviMiddleware(10)]
            public sealed class FirstMiddleware : IMviMiddleware<LoginState, LoginIntent, LoginEffect>
            {
                public ValueTask<ReduceResult<LoginState, LoginEffect>> InvokeAsync(
                    MviMiddlewareContext<LoginState, LoginIntent, LoginEffect> context,
                    MviContinuation<LoginState, LoginIntent, LoginEffect> continuation,
                    CancellationToken cancellationToken) => continuation(context, cancellationToken);
            }
            """;

        var result = await SourceGeneratorVerifier.VerifyAsync(
            source,
            new MviMiddlewarePipelineGenerator());

        await Assert.That(result.Diagnostics).IsEmpty();
        await Assert.That(result.GeneratedTrees.Select(tree => tree.FilePath)).Contains(filePath => filePath.EndsWith("LoginMiddlewarePipelineFactory.g.cs", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("MviMiddlewarePipeline<LoginState, LoginIntent, LoginEffect>", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.IndexOf("firstMiddleware", StringComparison.Ordinal) < sourceText.IndexOf("secondMiddleware", StringComparison.Ordinal));
    }
}
