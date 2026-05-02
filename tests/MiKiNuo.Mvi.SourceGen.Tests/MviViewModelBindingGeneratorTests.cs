using MiKiNuo.Mvi.SourceGen.ViewModels;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.SourceGen.Tests;

public sealed class MviViewModelBindingGeneratorTests
{
    [Test]
    public async Task Generator_emits_state_bound_property_and_apply_state_core()
    {
        const string source = """
            using MiKiNuo.Mvi.Abstractions;
            using MiKiNuo.Mvi.Abstractions.Binding;
            using MiKiNuo.Mvi.Core.Store;
            using MiKiNuo.Mvi.Core.ViewModels;

            namespace Demo;

            public sealed record LoginState(string UserName) : IMviState;

            public abstract partial record LoginIntent : IMviIntent
            {
                public abstract int Kind { get; }
            }

            public sealed record LoginEffect(string Message) : IMviEffect;

            [MviViewModel]
            public sealed partial class LoginViewModel : MviViewModelBase<LoginState, LoginIntent, LoginEffect>
            {
                [MviStateBinding(nameof(LoginState.UserName))]
                public partial string UserName { get; }
            }
            """;

        var result = await SourceGeneratorVerifier.VerifyAsync(
            source,
            new MviViewModelBindingGenerator());

        await Assert.That(result.Diagnostics).IsEmpty();
        await Assert.That(result.GeneratedTrees.Select(tree => tree.FilePath)).Contains(filePath => filePath.EndsWith("LoginViewModel.Binding.g.cs", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("public LoginViewModel(IMviStore<LoginState, LoginIntent, LoginEffect> store)", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("private string userName = default!;", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("public partial string UserName => userName;", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("protected override void ApplyStateCore(LoginState state)", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("SetUserNameFromState(state.UserName);", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("Auto：由 MiKiNuo.Mvi.SourceGen 自动生成", StringComparison.Ordinal));
    }

    [Test]
    public async Task Generator_emits_state_first_two_way_binding_setter()
    {
        const string source = """
            using MiKiNuo.Mvi.Abstractions;
            using MiKiNuo.Mvi.Abstractions.Binding;
            using MiKiNuo.Mvi.Core.Store;
            using MiKiNuo.Mvi.Core.ViewModels;

            namespace Demo;

            public sealed record LoginState(string UserName) : IMviState;

            public abstract partial record LoginIntent : IMviIntent
            {
                public abstract int Kind { get; }
            }

            public sealed record UserNameChangedIntent(string UserName) : LoginIntent
            {
                public override int Kind => 0;
            }

            public sealed record LoginEffect(string Message) : IMviEffect;

            [MviViewModel]
            public sealed partial class LoginViewModel : MviViewModelBase<LoginState, LoginIntent, LoginEffect>
            {
                [MviTwoWayBinding(nameof(LoginState.UserName), typeof(UserNameChangedIntent))]
                public partial string UserName { get; set; }
            }
            """;

        var result = await SourceGeneratorVerifier.VerifyAsync(
            source,
            new MviViewModelBindingGenerator());

        await Assert.That(result.Diagnostics).IsEmpty();
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("public partial string UserName", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("get => userName;", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("_ = DispatchAsync(new UserNameChangedIntent(value));", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("SetUserNameFromState(state.UserName);", StringComparison.Ordinal));
    }

    [Test]
    public async Task Generator_emits_command_property_and_can_execute_notification()
    {
        const string source = """
            using System.Windows.Input;
            using MiKiNuo.Mvi.Abstractions;
            using MiKiNuo.Mvi.Abstractions.Binding;
            using MiKiNuo.Mvi.Core.Store;
            using MiKiNuo.Mvi.Core.ViewModels;

            namespace Demo;

            public sealed record LoginState(bool CanSubmit) : IMviState;

            public abstract partial record LoginIntent : IMviIntent
            {
                public abstract int Kind { get; }
            }

            public sealed record SubmitIntent : LoginIntent
            {
                public override int Kind => 1;
            }

            public sealed record LoginEffect(string Message) : IMviEffect;

            [MviViewModel]
            public sealed partial class LoginViewModel : MviViewModelBase<LoginState, LoginIntent, LoginEffect>
            {
                [MviStateBinding(nameof(LoginState.CanSubmit))]
                public partial bool CanSubmit { get; }

                [MviCommand(typeof(SubmitIntent), CanExecuteStatePropertyName = nameof(LoginState.CanSubmit))]
                public partial ICommand SubmitCommand { get; }
            }
            """;

        var result = await SourceGeneratorVerifier.VerifyAsync(
            source,
            new MviViewModelBindingGenerator());

        await Assert.That(result.Diagnostics).IsEmpty();
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("private readonly MviIntentCommand<LoginViewModel, LoginIntent> submitCommand;", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("submitCommand = new MviIntentCommand<LoginViewModel, LoginIntent>", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("static viewModel => viewModel.DispatchAsync(new SubmitIntent()).AsTask()", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("static viewModel => viewModel.CanSubmit", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("public partial ICommand SubmitCommand => submitCommand;", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("submitCommand.RaiseCanExecuteChanged();", StringComparison.Ordinal));
    }
}
