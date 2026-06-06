﻿﻿﻿using System.ComponentModel;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Menu;

/// <summary>
/// 表示 Dashboard 左侧菜单 ViewModel。
/// 暴露分组菜单项、当前选中项详情、底部活跃卡片数等给 XAML。
/// <para>
/// SelectedItem 是 XAML 端 ListBox.SelectedItem 的绑定目标（因为 Avalonia 不支持 SelectedValuePath）。
/// 写入时手动派发 <see cref="DashboardMenuIntent.SelectMenuKey"/>；
/// MviBind 标在 SelectedMenuKey 上是为了状态 → UI 的单向回写。
/// </para>
/// </summary>
public sealed partial class DashboardMenuViewModel
    : MviViewModelBase<DashboardMenuState, DashboardMenuIntent, DashboardMenuEffect>
{
    private DashboardMenuItemDescriptor? _selectedItem;

    /// <summary>
    /// 初始化 Dashboard 左侧菜单 ViewModel。
    /// </summary>
    /// <param name="store">菜单状态存储。</param>
    /// <param name="uiDispatcher">UI 调度器（可选，由 DI 容器注入以确保 Avalonia UI 线程触发 CanExecuteChanged）。</param>
    public DashboardMenuViewModel(IMviStore<DashboardMenuState, DashboardMenuIntent, DashboardMenuEffect> store, IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        PropertyChanged += OnViewModelPropertyChanged;
    }

    /// <summary>获取菜单项集合（按展示顺序）。</summary>
    [MviBind(nameof(DashboardMenuState.MenuItems))]
    public partial IReadOnlyList<DashboardMenuItemDescriptor> MenuItems { get; private set; }

    /// <summary>获取分组标题集合（按展示顺序）。</summary>
    [MviBind(nameof(DashboardMenuState.Groups))]
    public partial IReadOnlyList<string> Groups { get; private set; }

    /// <summary>获取或设置当前选中的菜单键（由 MviBind 单向回写；TwoWay 通过 SelectedItem 触发）。</summary>
    [MviBind(
        nameof(DashboardMenuState.SelectedMenuKey),
        BindingMode = MviBindingMode.TwoWay,
        IntentType = typeof(DashboardMenuIntent.SelectMenuKey))]
    public partial string SelectedMenuKey { get; set; }

    /// <summary>获取菜单状态文本。</summary>
    [MviBind(nameof(DashboardMenuState.StatusText))]
    public partial string StatusText { get; private set; }

    /// <summary>
    /// 获取或设置当前选中的菜单项描述符（绑定到 ListBox.SelectedItem）。
    /// 设置时同步派发 <see cref="DashboardMenuIntent.SelectMenuKey"/> intent。
    /// </summary>
    public DashboardMenuItemDescriptor? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (ReferenceEquals(_selectedItem, value))
            {
                return;
            }

            _selectedItem = value;
            OnPropertyChanged(nameof(SelectedItem));
        }
    }

    /// <summary>
    /// 获取当前选中菜单项的描述（无选中时为空字符串）。
    /// </summary>
    public string SelectedItemDescription
    {
        get
        {
            DashboardMenuItemDescriptor? item = SelectedItem;
            if (item is not null)
            {
                return item.Description;
            }

            foreach (DashboardMenuItemDescriptor candidate in MenuItems)
            {
                if (string.Equals(candidate.Key, SelectedMenuKey, StringComparison.Ordinal))
                {
                    return candidate.Description;
                }
            }

            return string.Empty;
        }
    }

    /// <summary>
    /// 获取当前选中菜单项的卡片数（无选中时为 0）。
    /// </summary>
    public int SelectedItemCardCount
    {
        get
        {
            DashboardMenuItemDescriptor? item = SelectedItem;
            if (item is not null)
            {
                return item.CardCount;
            }

            foreach (DashboardMenuItemDescriptor candidate in MenuItems)
            {
                if (string.Equals(candidate.Key, SelectedMenuKey, StringComparison.Ordinal))
                {
                    return candidate.CardCount;
                }
            }

            return 0;
        }
    }

    /// <summary>
    /// 获取当前选中菜单项的图标键（无选中时为空字符串）。
    /// </summary>
    public string SelectedItemIconKey
    {
        get
        {
            DashboardMenuItemDescriptor? item = SelectedItem;
            if (item is not null)
            {
                return item.IconKey;
            }

            foreach (DashboardMenuItemDescriptor candidate in MenuItems)
            {
                if (string.Equals(candidate.Key, SelectedMenuKey, StringComparison.Ordinal))
                {
                    return candidate.IconKey;
                }
            }

            return string.Empty;
        }
    }

    /// <summary>
    /// 获取当前选中菜单项的业务域键（无选中时为 "Other"）。
    /// </summary>
    public string SelectedItemDomainKey
    {
        get
        {
            DashboardMenuItemDescriptor? item = SelectedItem;
            if (item is not null)
            {
                return item.DomainKey;
            }

            foreach (DashboardMenuItemDescriptor candidate in MenuItems)
            {
                if (string.Equals(candidate.Key, SelectedMenuKey, StringComparison.Ordinal))
                {
                    return candidate.DomainKey;
                }
            }

            return "Other";
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(SelectedItem), StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(SelectedItemDescription));
            OnPropertyChanged(nameof(SelectedItemCardCount));
            OnPropertyChanged(nameof(SelectedItemIconKey));
            OnPropertyChanged(nameof(SelectedItemDomainKey));

            DashboardMenuItemDescriptor? item = SelectedItem;
            if (item is not null)
            {
#pragma warning disable CA2012
                _ = DispatchAsync(new DashboardMenuIntent.SelectMenuKey(item.Key));
#pragma warning restore CA2012
            }

            return;
        }

        if (string.Equals(e.PropertyName, nameof(SelectedMenuKey), StringComparison.Ordinal)
            || string.Equals(e.PropertyName, nameof(MenuItems), StringComparison.Ordinal))
        {
            // 状态回写时同步 SelectedItem 以驱动 ListBox 高亮（SelectedMenuKey 改变 → 找对应项）
            if (_selectedItem is null
                || !string.Equals(_selectedItem.Key, SelectedMenuKey, StringComparison.Ordinal))
            {
                DashboardMenuItemDescriptor? match = null;
                foreach (DashboardMenuItemDescriptor candidate in MenuItems)
                {
                    if (string.Equals(candidate.Key, SelectedMenuKey, StringComparison.Ordinal))
                    {
                        match = candidate;
                        break;
                    }
                }

                if (match is not null)
                {
                    _selectedItem = match;
                    OnPropertyChanged(nameof(SelectedItem));
                }
            }

            OnPropertyChanged(nameof(SelectedItemDescription));
            OnPropertyChanged(nameof(SelectedItemCardCount));
            OnPropertyChanged(nameof(SelectedItemIconKey));
            OnPropertyChanged(nameof(SelectedItemDomainKey));
        }
    }
}
