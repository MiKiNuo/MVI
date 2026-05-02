using MiKiNuo.Mvi.SourceGen.Intent;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.SourceGen.Tests;

public sealed class MviIntentDispatcherGeneratorTests
{
    [Test]
    public async Task Generator_emits_dispatcher_that_routes_to_registered_handlers_by_kind()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using MiKiNuo.Mvi.Abstractions;
            using MiKiNuo.Mvi.Abstractions.Generation;
            using MiKiNuo.Mvi.Core.Dispatching;
            using MiKiNuo.Mvi.Core.Reducers;

            namespace Demo;

            public sealed record LoginState(string Value) : IMviState;

            [MviIntentUnion]
            public abstract partial record LoginIntent : IMviIntent;

            [MviIntentKind(0)]
            public sealed partial record UserNameChangedIntent(string UserName) : LoginIntent;

            [MviIntentKind(1)]
            public sealed partial record SubmitLoginIntent : LoginIntent;

            public sealed record LoginEffect(string Message) : IMviEffect;

            [MviIntentHandler(typeof(UserNameChangedIntent))]
            public sealed class UserNameChangedHandler : IIntentHandler<LoginState, UserNameChangedIntent, LoginEffect>
            {
                public ValueTask<ReduceResult<LoginState, LoginEffect>> HandleAsync(
                    LoginState state,
                    UserNameChangedIntent intent,
                    CancellationToken cancellationToken)
                {
                    return ValueTask.FromResult(ReduceResults.StateOnly<LoginState, LoginEffect>(new LoginState(intent.UserName)));
                }
            }

            [MviIntentHandler(typeof(SubmitLoginIntent))]
            public sealed class SubmitLoginHandler : IIntentHandler<LoginState, SubmitLoginIntent, LoginEffect>
            {
                public ValueTask<ReduceResult<LoginState, LoginEffect>> HandleAsync(
                    LoginState state,
                    SubmitLoginIntent intent,
                    CancellationToken cancellationToken)
                {
                    return ValueTask.FromResult(ReduceResults.WithEffect<LoginState, LoginEffect>(state, new LoginEffect("submit")));
                }
            }
            """;

        var result = await SourceGeneratorVerifier.VerifyAsync(
            source,
            new MviIntentKindGenerator(),
            new MviIntentDispatcherGenerator());

        await Assert.That(result.Diagnostics).IsEmpty();
        await Assert.That(result.GeneratedTrees.Select(tree => tree.FilePath)).Contains(filePath => filePath.EndsWith("LoginIntentDispatcher.g.cs", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText =>
            sourceText.Contains("IIntentDispatcher<", StringComparison.Ordinal) &&
            sourceText.Contains("LoginIntent", StringComparison.Ordinal) &&
            sourceText.Contains("LoginEffect", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("case 0:", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("userNameChangedHandler.HandleAsync", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("submitLoginHandler.HandleAsync", StringComparison.Ordinal));
    }
}
