using MiKiNuo.Mvi.Core.Commands;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Core.Tests;

public sealed class MviIntentCommandTests
{
    [Test]
    public async Task CanExecute_uses_can_execute_delegate_when_present()
    {
        var owner = new CommandOwner { CanSubmit = false };
        var command = new MviIntentCommand<CommandOwner, CommandIntent>(
            owner,
            _ => Task.CompletedTask,
            static viewModel => viewModel.CanSubmit);

        await Assert.That(command.CanExecute(null)).IsFalse();

        owner.CanSubmit = true;

        await Assert.That(command.CanExecute(null)).IsTrue();
    }

    [Test]
    public async Task CanExecute_returns_true_when_delegate_is_absent()
    {
        var command = new MviIntentCommand<CommandOwner, CommandIntent>(
            new CommandOwner(),
            _ => Task.CompletedTask,
            null);

        await Assert.That(command.CanExecute(null)).IsTrue();
    }

    [Test]
    public async Task Execute_invokes_execute_delegate()
    {
        var executed = false;
        var owner = new CommandOwner();
        var command = new MviIntentCommand<CommandOwner, CommandIntent>(
            owner,
            viewModel =>
            {
                executed = ReferenceEquals(viewModel, owner);
                return Task.CompletedTask;
            },
            null);

        command.Execute(null);

        await Assert.That(executed).IsTrue();
    }

    [Test]
    public async Task RaiseCanExecuteChanged_raises_CanExecuteChanged()
    {
        var command = new MviIntentCommand<CommandOwner, CommandIntent>(
            new CommandOwner(),
            _ => Task.CompletedTask,
            null);
        var raised = 0;
        command.CanExecuteChanged += (_, _) => raised++;

        command.RaiseCanExecuteChanged();

        await Assert.That(raised).IsEqualTo(1);
    }

    private sealed class CommandOwner
    {
        public bool CanSubmit { get; set; }
    }

    private sealed record CommandIntent(int Kind);
}
