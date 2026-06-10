using System;
using global::Godot;
using MiKiNuo.Mvi.Presentation.Disposables;
using MiKiNuo.Mvi.Platforms.Godot.Binding;
using MiKiNuo.Mvi.Samples.Godot.Features.AppShell;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;
using MiKiNuo.Mvi.Samples.Godot.Views.Lobby;
using MiKiNuo.Mvi.Samples.Godot.Views.Login;

namespace MiKiNuo.Mvi.Samples.Godot.Views.AppShell;

/// <summary>
/// 表示 Godot 游戏应用壳 View。
/// <para>
/// 顶层页面 VM（Login / Lobby）通过 <see cref="AppShellViewModel.CreateCurrentScreenViewModel"/> 解析，
/// <c>CurrentScreen</c> 变化时按需重新绑定到当前可见页面，另一个页面 VM 保留在 <see cref="IGameScreenFactory"/> 内部缓存。
/// </para>
/// </summary>
[MviGodotView(GodotViewKeys.AppShell, "res://Views/AppShell/AppShellView.tscn")]
public partial class AppShellView : GodotMviControlView<AppShellViewModel>
{
    private Control? _currentScreenView;
    private object? _currentScreenViewModel;

    /// <inheritdoc />
    protected override void OnBind(AppShellViewModel viewModel, MviDisposableBag bindings)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(bindings);
        Label titleLabel = GetNode<Label>("Root/TitleLabel");
        Label messageLabel = GetNode<Label>("Root/MessageLabel");
        LoginView loginView = GetNode<LoginView>("Root/PageContainer/PageSlot/LoginView");
        LobbyView lobbyView = GetNode<LobbyView>("Root/PageContainer/PageSlot/LobbyView");

        BindPropertyChanged(viewModel, nameof(AppShellViewModel.CurrentTitle), () => titleLabel.Text = viewModel.CurrentTitle, bindings);
        BindPropertyChanged(viewModel, nameof(AppShellViewModel.ShellMessage), () => messageLabel.Text = viewModel.ShellMessage, bindings);
        BindPropertyChanged(
            viewModel,
            nameof(AppShellViewModel.CurrentScreen),
            () => SwitchScreen(viewModel, loginView, lobbyView),
            bindings);

        // 初始绑定：构造 View 时的 CurrentScreen（默认 Login）
        SwitchScreen(viewModel, loginView, lobbyView);

        bindings.Add(() => UnbindCurrentScreen());
    }

    private void SwitchScreen(AppShellViewModel viewModel, LoginView loginView, LobbyView lobbyView)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        object? screenViewModel = viewModel.CreateCurrentScreenViewModel();
        if (ReferenceEquals(screenViewModel, _currentScreenViewModel))
        {
            return;
        }

        UnbindCurrentScreen();
        _currentScreenViewModel = screenViewModel;
        if (screenViewModel is null)
        {
            _currentScreenView = null;
            return;
        }

        _currentScreenView = viewModel.CurrentScreen switch
        {
            GameScreenKeys.Login => BindLoginScreen(loginView, (LoginViewModel)screenViewModel, viewModel),
            GameScreenKeys.Lobby => BindLobbyScreen(lobbyView, (LobbyViewModel)screenViewModel, viewModel),
            _ => throw new InvalidOperationException($"未识别的 Game 屏幕键：{viewModel.CurrentScreen}"),
        };
    }

    private static LoginView BindLoginScreen(LoginView view, LoginViewModel viewModel, AppShellViewModel owner)
    {
        return BindScreen(view, viewModel, owner);
    }

    private static LobbyView BindLobbyScreen(LobbyView view, LobbyViewModel viewModel, AppShellViewModel owner)
    {
        return BindScreen(view, viewModel, owner);
    }

    private static TView BindScreen<TView, TViewModel>(TView view, TViewModel viewModel, AppShellViewModel owner)
        where TView : Control, IMviGodotBindable<TViewModel>
        where TViewModel : class
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(owner);
        view.Bind(viewModel);
        view.Visible = true;
        return view;
    }

    private void UnbindCurrentScreen()
    {
        if (_currentScreenView is null)
        {
            return;
        }

        _currentScreenView.Visible = false;
        if (_currentScreenView is LoginView login)
        {
            login.Unbind();
        }
        else if (_currentScreenView is LobbyView lobby)
        {
            lobby.Unbind();
        }

        _currentScreenView = null;
        _currentScreenViewModel = null;
    }
}
