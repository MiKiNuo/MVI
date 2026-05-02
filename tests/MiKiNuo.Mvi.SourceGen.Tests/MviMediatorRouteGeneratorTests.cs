using MiKiNuo.Mvi.SourceGen.Mediator;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.SourceGen.Tests;

public sealed class MviMediatorRouteGeneratorTests
{
    [Test]
    public async Task Generator_emits_request_response_mediator_routes_without_event_bus_terms()
    {
        const string source = """
            using MiKiNuo.Mvi.Abstractions.Generation;
            using MiKiNuo.Mvi.Core.Mediator;

            namespace Demo;

            public sealed record LoginRequest(string UserName);
            public sealed record LoginResponse(bool Success);

            [MviMediator]
            [MviRoute(typeof(LoginRequest), typeof(LoginResponse))]
            public sealed partial class AppMediator : IMviMediator
            {
            }
            """;

        var result = await SourceGeneratorVerifier.VerifyAsync(
            source,
            new MviMediatorRouteGenerator());

        await Assert.That(result.Diagnostics).IsEmpty();
        await Assert.That(result.GeneratedTrees.Select(tree => tree.FilePath)).Contains(filePath => filePath.EndsWith("AppMediator.Routes.g.cs", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("ValueTask<TResponse> SendAsync<TRequest, TResponse>", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("typeof(TRequest) == typeof(LoginRequest)", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("typeof(TResponse) == typeof(LoginResponse)", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).DoesNotContain(sourceText =>
            sourceText.Contains("Publish", StringComparison.Ordinal) ||
            sourceText.Contains("Subscribe", StringComparison.Ordinal) ||
            sourceText.Contains("Broadcast", StringComparison.Ordinal) ||
            sourceText.Contains("EventBus", StringComparison.Ordinal));
    }
}
