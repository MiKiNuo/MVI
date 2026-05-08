using System;
using global::Godot;
using MiKiNuo.Mvi.Platforms.Godot.Binding;
using MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

namespace MiKiNuo.Mvi.Samples.Godot.Views.Lobby.ActivityLog;

/// <summary>
/// 表示活动日志 View。
/// </summary>
public partial class ActivityLogView : GodotMviControlView<ActivityLogViewModel>
{
    /// <inheritdoc />
    protected override void OnBind(ActivityLogViewModel viewModel, GodotMviDisposableBag bindings)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(bindings);
        TextEdit logText = GetNode<TextEdit>("Root/Panel/Margin/Layout/LogText");
        BindPropertyChanged(viewModel, nameof(ActivityLogViewModel.ActivityLog), () => logText.Text = viewModel.ActivityLog, bindings);
    }
}
