using System;
using global::Godot;
using MiKiNuo.Mvi.Presentation.Disposables;
using MiKiNuo.Mvi.Platforms.Godot.Binding;
using MiKiNuo.Mvi.Platforms.Godot.Composition;
using MiKiNuo.Mvi.Samples.Godot.Composition;
using MiKiNuo.Mvi.Samples.Godot.Features.AppShell;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;
using MiKiNuo.Mvi.Samples.Godot.Views.Lobby;
using MiKiNuo.Mvi.Samples.Godot.Views.Login;

namespace MiKiNuo.Mvi.Samples.Godot.Views.AppShell;

/// <summary>
/// 表示 Godot 游戏应用壳 View。
/// </summary>
[MviGodotView(GodotViewKeys.AppShell, "res://Views/AppShell/AppShellView.tscn")]
public partial class AppShellView : GodotMviControlView<AppShellViewModel>
{
    /// <inheritdoc />
    protected override void OnBind(AppShellViewModel viewModel, MviDisposableBag bindings)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(bindings);
        Label titleLabel = GetNode<Label>("Root/TitleLabel");
        Label messageLabel = GetNode<Label>("Root/MessageLabel");
        LoginView loginView = GetNode<LoginView>("Root/PageContainer/PageSlot/LoginView");
        LobbyView lobbyView = GetNode<LobbyView>("Root/PageContainer/PageSlot/LobbyView");

        if (viewModel.LoginViewModel is not null)
        {
            loginView.Bind(viewModel.LoginViewModel);
        }

        if (viewModel.LobbyViewModel is not null)
        {
            lobbyView.Bind(viewModel.LobbyViewModel);
        }

        BindPropertyChanged(viewModel, nameof(AppShellViewModel.CurrentTitle), () => titleLabel.Text = viewModel.CurrentTitle, bindings);
        BindPropertyChanged(viewModel, nameof(AppShellViewModel.ShellMessage), () => messageLabel.Text = viewModel.ShellMessage, bindings);
        BindPropertyChanged(
            viewModel,
            nameof(AppShellViewModel.CurrentScreen),
            () =>
            {
                loginView.Visible = string.Equals(viewModel.CurrentScreen, GameScreenKeys.Login, StringComparison.Ordinal);
                lobbyView.Visible = string.Equals(viewModel.CurrentScreen, GameScreenKeys.Lobby, StringComparison.Ordinal);
            },
            bindings);
    }
}
