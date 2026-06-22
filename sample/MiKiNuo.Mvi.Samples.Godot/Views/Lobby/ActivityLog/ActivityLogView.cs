using System;
using global::Godot;
using MiKiNuo.Mvi.Presentation.Disposables;
using MiKiNuo.Mvi.Platforms.Godot.Binding;
using MiKiNuo.Mvi.Platforms.Godot.Composition;
using MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

namespace MiKiNuo.Mvi.Samples.Godot.Views.Lobby.ActivityLog;

/// <summary>
/// 表示活动日志 View。
/// <para>
/// 标有 <see cref="MviGodotViewAttribute"/>：由 <c>GodotMviViewRegistryGenerator</c> 编译期注册到
/// <see cref="IGodotMviViewRegistry"/>，供父 <c>LobbyView</c> 的 <c>[MviSlot]</c> 槽位通过
/// <see cref="GodotMviViewRegistryAdapter"/> 按 <c>ActivityLogViewModel</c> 类型名解析并挂载。
/// </para>
/// </summary>
[MviGodotView("ActivityLogView", "res://Views/Lobby/ActivityLog/ActivityLogView.tscn")]
public partial class ActivityLogView : GodotMviControlView<ActivityLogViewModel>
{
    /// <summary>
    /// 执行具体 View 的绑定逻辑。
    /// </summary>
    /// <param name="viewModel">当前 ViewModel。</param>
    /// <param name="bindings">绑定生命周期集合。</param>
    protected override void OnBind(ActivityLogViewModel viewModel, MviDisposableBag bindings)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(bindings);
        TextEdit logText = GetNode<TextEdit>("Root/Panel/Margin/Layout/LogText");
        BindPropertyChanged(viewModel, nameof(ActivityLogViewModel.ActivityLog), () => logText.Text = viewModel.ActivityLog, bindings);
    }
}
