using System.ComponentModel;
using System.Runtime.CompilerServices;
using MiKiNuo.Mvi.Application.MVI.ViewModel;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

/// <summary>
/// 表示 Form Card 视图中单个输入字段的运行时包装。
/// 对应 CardDefinition.FormFields 中的一项；Value 的修改通过 CardViewModel.SetFormFieldAsync
/// 派发到 CardIntent.SetFormField，进而触发 CardReducer 更新底层 CardState.FormValues。
/// 用作 ItemsControl 的 ItemSource 时需要支持 INotifyPropertyChanged 以驱动 TwoWay 绑定。
/// </summary>
public sealed class CardFormFieldEntry : INotifyPropertyChanged
{
    private readonly CardViewModel _viewModel;
    private string _value;

    /// <summary>
    /// 初始化字段运行时包装。
    /// </summary>
    /// <param name="viewModel">所属 CardViewModel。</param>
    /// <param name="key">字段键。</param>
    /// <param name="label">显示标签。</param>
    /// <param name="inputHint">输入框为空时展示的提示文字。</param>
    /// <param name="initialValue">初始值。</param>
    public CardFormFieldEntry(CardViewModel viewModel, string key, string label, string inputHint, string initialValue)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        _viewModel = viewModel;
        Key = key;
        Label = label;
        InputHint = inputHint;
        _value = initialValue;
    }

    /// <summary>字段键（用于回写到 FormValues 列表）。</summary>
    public string Key { get; }

    /// <summary>显示标签。</summary>
    public string Label { get; }

    /// <summary>输入框为空时展示的提示文字。</summary>
    public string InputHint { get; }

    /// <summary>当前值（TwoWay 绑定到 TextBox.Text）。</summary>
    public string Value
    {
        get => _value;
        set
        {
            if (_value == value)
            {
                return;
            }

            _value = value ?? string.Empty;
            OnPropertyChanged();
#pragma warning disable CA2012
            _ = _viewModel.SetFormFieldAsync(Key, _value);
#pragma warning restore CA2012
        }
    }

    /// <summary>
    /// 由 <see cref="CardViewModel.RebuildDerivedProperties"/> 在 state 抵达时刷新当前值，不触发任何命令派发，
    /// 避免与用户输入路径（<see cref="Value"/> setter）形成回环。引用稳定保证 Avalonia ItemsControl 不重建 TextBox 容器。
    /// </summary>
    /// <param name="newValue">来自最新 state 的字段值。</param>
    internal void RefreshValue(string newValue)
    {
        string normalized = newValue ?? string.Empty;
        if (_value == normalized)
        {
            return;
        }

        _value = normalized;
        OnPropertyChanged();
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
