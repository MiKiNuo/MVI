using System;
using global::Godot;
using MiKiNuo.Mvi.Presentation.Disposables;
using MiKiNuo.Mvi.Platforms.Godot.Binding;
using MiKiNuo.Mvi.Samples.Godot.Features.Login;

namespace MiKiNuo.Mvi.Samples.Godot.Views.Login;

/// <summary>
/// 表示 Godot 游戏登录 View。
/// </summary>
public partial class LoginView : GodotMviControlView<LoginViewModel>
{
    /// <summary>
    /// 执行具体 View 的绑定逻辑。
    /// </summary>
    /// <param name="viewModel">当前 ViewModel。</param>
    /// <param name="bindings">绑定生命周期集合。</param>
    protected override void OnBind(LoginViewModel viewModel, MviDisposableBag bindings)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(bindings);
        LineEdit userNameEdit = GetNode<LineEdit>("Root/Panel/Margin/Form/UserNameEdit");
        LineEdit passwordEdit = GetNode<LineEdit>("Root/Panel/Margin/Form/PasswordEdit");
        Button submitButton = GetNode<Button>("Root/Panel/Margin/Form/SubmitButton");
        Label statusLabel = GetNode<Label>("Root/Panel/Margin/Form/StatusLabel");
        Label errorLabel = GetNode<Label>("Root/Panel/Margin/Form/ErrorLabel");

        void ChangeUserName(string text)
        {
            viewModel.UserName = text;
        }

        void ChangePassword(string text)
        {
            viewModel.Password = text;
        }

        userNameEdit.TextChanged += ChangeUserName;
        passwordEdit.TextChanged += ChangePassword;
        bindings.Add(() => userNameEdit.TextChanged -= ChangeUserName);
        bindings.Add(() => passwordEdit.TextChanged -= ChangePassword);
        BindButton(submitButton, viewModel.SubmitCommand, bindings);

        BindPropertyChanged(viewModel, nameof(LoginViewModel.UserName), () => SetLineEditText(userNameEdit, viewModel.UserName), bindings);
        BindPropertyChanged(viewModel, nameof(LoginViewModel.Password), () => SetLineEditText(passwordEdit, viewModel.Password), bindings);
        BindPropertyChanged(viewModel, nameof(LoginViewModel.LoginStatus), () => statusLabel.Text = viewModel.LoginStatus, bindings);
        BindPropertyChanged(viewModel, nameof(LoginViewModel.ErrorMessage), () => errorLabel.Text = viewModel.ErrorMessage ?? string.Empty, bindings);
    }

    private static void SetLineEditText(LineEdit lineEdit, string text)
    {
        if (!string.Equals(lineEdit.Text, text, StringComparison.Ordinal))
        {
            lineEdit.Text = text;
        }
    }
}
