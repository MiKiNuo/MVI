using System.ComponentModel;
using System.Runtime.CompilerServices;
using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedCatalog;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.PatientRegistry;
using R3;


namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

/// <summary>
/// 表示仪表板卡片的统一 ViewModel。
/// 同一 VM 实例被 16 张卡共用：差异由 state.PageKey 决定（每个 card 持有自己的 IMviStore&lt;CardState,...&gt;）。
/// [MviBind] 覆盖 CardState 14 个固定字段（[生成] 由源代码生成器实现 ApplyStateCore 与命令）；FormFields/IsFormCard 等派生属性在状态变更后由 RebuildDerivedProperties 重新计算。
/// </summary>
public sealed partial class CardViewModel
    : MviViewModelBase<CardState, CardIntent, CardEffect>
{
    private IReadOnlyList<CardFormFieldEntry> _formFields = Array.Empty<CardFormFieldEntry>();
    private readonly Dictionary<string, CardFormFieldEntry> _formFieldsCache = new(StringComparer.Ordinal);
    private readonly IDisposable _derivedPropertiesSubscription;
    private BedFilterOption _selectedBedFilterOption = BedFilterOption.All[0];
    private IReadOnlyList<BedRecord> _filteredBeds = Array.Empty<BedRecord>();
    private IReadOnlyList<BedRecordRow> _filteredBedRows = Array.Empty<BedRecordRow>();
    private IReadOnlySet<BedType> _selectedBedTypes = new HashSet<BedType>();
    private IReadOnlySet<BedStatus> _selectedBedStatuses = new HashSet<BedStatus>();
    private bool _isFormCard;
    private bool _showBedCatalog;

    /// <summary>
    /// 初始化仪表板卡片 ViewModel。
    /// </summary>
    /// <param name="store">状态存储。</param>
    /// <param name="uiDispatcher">UI 调度器（可选，由 DI 容器注入以确保 Avalonia UI 线程触发 CanExecuteChanged）。</param>
    public CardViewModel(IMviStore<CardState, CardIntent, CardEffect> store, IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        ArgumentNullException.ThrowIfNull(store);

        InitializeGeneratedCommands();
        RebuildDerivedProperties(store.CurrentState);
        _derivedPropertiesSubscription = store.States.Subscribe(
            this,
            static (state, self) => self.RebuildDerivedProperties(state));
    }

    /// <summary>获取 PageKey。</summary>
    [MviBind(nameof(CardState.PageKey))]
    public partial PageKey PageKey { get; private set; }

    /// <summary>获取来源键。</summary>
    [MviBind(nameof(CardState.SourceKey))]
    public partial string SourceKey { get; private set; }

    /// <summary>获取来源显示名。</summary>
    [MviBind(nameof(CardState.SourceDisplayName))]
    public partial string SourceDisplayName { get; private set; }

    /// <summary>获取标题。</summary>
    [MviBind(nameof(CardState.Title))]
    public partial string Title { get; private set; }

    /// <summary>获取核心指标文本。</summary>
    [MviBind(nameof(CardState.MainValue))]
    public partial string MainValue { get; private set; }

    /// <summary>获取状态文本。</summary>
    [MviBind(nameof(CardState.StatusText))]
    public partial string StatusText { get; private set; }

    /// <summary>获取详情文本。</summary>
    [MviBind(nameof(CardState.DetailText))]
    public partial string DetailText { get; private set; }

    /// <summary>获取动作日志。</summary>
    [MviBind(nameof(CardState.ActionLog))]
    public partial string ActionLog { get; private set; }

    /// <summary>获取主动作按钮文本。</summary>
    [MviBind(nameof(CardState.PrimaryActionText))]
    public partial string PrimaryActionText { get; private set; }

    /// <summary>获取辅助动作按钮文本。</summary>
    [MviBind(nameof(CardState.SecondaryActionText))]
    public partial string SecondaryActionText { get; private set; }

    /// <summary>获取主动作是否可执行。</summary>
    [MviBind(nameof(CardState.CanPrimaryAction))]
    public partial bool CanPrimaryAction { get; private set; }

    /// <summary>获取辅助动作是否可执行。</summary>
    [MviBind(nameof(CardState.CanSecondaryAction))]
    public partial bool CanSecondaryAction { get; private set; }

    /// <summary>获取 FormValues 集合（顺序保留）。</summary>
    [MviBind(nameof(CardState.FormValues))]
    public partial IReadOnlyList<CardFormValueEntry> FormValues { get; private set; }

    /// <summary>获取 Form 错误提示（无错误时为空串）。</summary>
    [MviBind(nameof(CardState.FormErrorMessage))]
    public partial string FormErrorMessage { get; private set; }

    /// <summary>获取最近一次入院登记卡提交后流入本卡片的患者记录（同 SourceKey 组内共享）。未触发时为 null。</summary>
    [MviBind(nameof(CardState.RecentAdmittedPatient))]
    public partial Patient? RecentPatient { get; private set; }

    /// <summary>获取当前卡片是否有最近入院的患者记录（RecentPatient != null）。</summary>
    public bool HasRecentPatient => RecentPatient is not null;

    /// <summary>获取床位筛选维度（仅 BedOverview 卡片消费；其他卡片保持 <see cref="BedFilter.All"/>）。</summary>
    [MviBind(nameof(CardState.CurrentBedFilter))]
    public partial BedFilter CurrentBedFilter { get; private set; }

    /// <summary>获取 <see cref="CurrentBedFilter"/> 命中的床位条数（仅 BedOverview 卡片消费；其他卡片为 0）。</summary>
    [MviBind(nameof(CardState.FilteredBedCount))]
    public partial int FilteredBedCount { get; private set; }

    /// <summary>获取 ComboBox 用的床位筛选选项集合（仅在 BedOverview 卡片显示）。</summary>
    public IReadOnlyList<BedFilterOption> AvailableBedFilters => BedFilterOption.All;

    /// <summary>获取 CheckBox 用的床位类型多选展示项集合（仅在 BedOverview 卡片显示）。</summary>
    public IReadOnlyList<BedTypeOption> AvailableBedTypes => BedTypeOption.All;

    /// <summary>获取 CheckBox 用的床位状态多选展示项集合（仅在 BedOverview 卡片显示）。</summary>
    public IReadOnlyList<BedStatusOption> AvailableBedStatuses => BedStatusOption.All;

    /// <summary>获取当前选中的床位类型集合（与 <see cref="CardState.SelectedBedTypes"/> 同步；由 <see cref="RebuildDerivedProperties"/> 维护）。</summary>
    public IReadOnlySet<BedType> SelectedBedTypes
    {
        get => _selectedBedTypes;
        private set => SetProperty(ref _selectedBedTypes, value);
    }

    /// <summary>获取当前选中的床位状态集合（与 <see cref="CardState.SelectedBedStatuses"/> 同步；由 <see cref="RebuildDerivedProperties"/> 维护）。</summary>
    public IReadOnlySet<BedStatus> SelectedBedStatuses
    {
        get => _selectedBedStatuses;
        private set => SetProperty(ref _selectedBedStatuses, value);
    }

    /// <summary>判断指定 <see cref="BedType"/> 是否在当前多选集合内（供 XAML CheckBox.IsChecked 绑定）。</summary>
    /// <param name="type">床位类型。</param>
    /// <returns>包含返回 true。</returns>
    public bool IsBedTypeSelected(BedType type) => _selectedBedTypes.Contains(type);

    /// <summary>判断指定 <see cref="BedStatus"/> 是否在当前多选集合内（供 XAML CheckBox.IsChecked 绑定）。</summary>
    /// <param name="status">床位状态。</param>
    /// <returns>包含返回 true。</returns>
    public bool IsBedStatusSelected(BedStatus status) => _selectedBedStatuses.Contains(status);

    /// <summary>获取按 <see cref="CurrentBedFilter"/> 过滤后的床位记录集合（DataGrid 数据源）。</summary>
    public IReadOnlyList<BedRecord> FilteredBeds
    {
        get => _filteredBeds;
        private set => SetProperty(ref _filteredBeds, value);
    }

    /// <summary>
    /// 获取 <see cref="FilteredBeds"/> 对应的 <see cref="BedRecordRow"/> 包装集合
    /// （DataGrid 实际绑定源；把枚举 / 可空字段翻译为中文展示名）。
    /// </summary>
    public IReadOnlyList<BedRecordRow> FilteredBedRows
    {
        get => _filteredBedRows;
        private set => SetProperty(ref _filteredBedRows, value);
    }

    /// <summary>获取当前 ComboBox 选中的筛选选项（与 <see cref="CurrentBedFilter"/> 同步；由 <see cref="RebuildDerivedProperties"/> 维护）。</summary>
    public BedFilterOption SelectedBedFilterOption
    {
        get => _selectedBedFilterOption;
        private set => SetProperty(ref _selectedBedFilterOption, value);
    }

    /// <summary>获取当前卡片是否需要展示床位目录 ComboBox（PageKey == BedOverview）。</summary>
    public bool ShowBedCatalog
    {
        get => _showBedCatalog;
        private set => SetProperty(ref _showBedCatalog, value);
    }

    /// <summary>获取当前卡片是否为 Form Card（由 PageKey 推导）。</summary>
    public bool IsFormCard
    {
        get => _isFormCard;
        private set => SetProperty(ref _isFormCard, value);
    }

    /// <summary>获取当前是否有 Form 错误。</summary>
    public bool HasFormError => !string.IsNullOrEmpty(FormErrorMessage);

    /// <summary>获取 Form 字段运行时包装列表（供 ItemsControl 绑定；非 Form Card 返回空集合）。</summary>
    public IReadOnlyList<CardFormFieldEntry> FormFields
    {
        get => _formFields;
        private set => SetProperty(ref _formFields, value);
    }

    /// <summary>获取主动作命令。</summary>
    [MviCommand(typeof(CardIntent.ExecutePrimaryAction), CanExecuteProperty = nameof(CanPrimaryAction), IsAsync = true)]
    public partial IMviAsyncCommand PrimaryActionCommand { get; private set; }

    /// <summary>获取辅助动作命令。</summary>
    [MviCommand(typeof(CardIntent.ExecuteSecondaryAction), CanExecuteProperty = nameof(CanSecondaryAction), IsAsync = true)]
    public partial IMviAsyncCommand SecondaryActionCommand { get; private set; }

    /// <summary>获取 Form 字段设置命令（仅 Form Card 触发；SetFormField 携带 2-param payload，由 [MviCommand] 自动绑定不适用，XAML code-behind 通过 <see cref="SetFormFieldAsync"/> 派发）。</summary>
    [MviCommand(typeof(CardIntent.SubmitForm), IsAsync = true)]
    public partial IMviAsyncCommand SubmitFormCommand { get; private set; }

    /// <summary>
    /// 设置 FormValues 中指定 Key 的值并派发 <see cref="CardIntent.SetFormField"/> 意图。
    /// 由 Form Card 视图的 CardFormFieldEntry.Value setter 调用；不能走 [MviCommand] 因为该 Intent 携带 2-param payload（Key, Value）而不是 1-param。
    /// </summary>
    /// <param name="key">字段键。</param>
    /// <param name="value">新值。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步派发过程的任务。</returns>
    public ValueTask SetFormFieldAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        return DispatchAsync(new CardIntent.SetFormField(key, value), cancellationToken);
    }

    /// <summary>
    /// 派发 <see cref="CardIntent.SetBedFilter"/> 意图以切换床位筛选维度。
    /// 由 BedOverview 卡片上 ComboBox.SelectionChanged 事件触发；reducer 内部对非 BedOverview 卡片短路忽略。
    /// </summary>
    /// <param name="filter">新筛选维度。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步派发过程的任务。</returns>
    public ValueTask SetBedFilterAsync(BedFilter filter, CancellationToken cancellationToken = default)
    {
        return DispatchAsync(new CardIntent.SetBedFilter(filter), cancellationToken);
    }

    /// <summary>
    /// 派发 <see cref="CardIntent.ToggleBedType"/> 意图以切换床位类型多选。
    /// 由 BedOverview 卡片上 CheckBox 点击事件触发；reducer 内部对非 BedOverview 卡片短路忽略。
    /// </summary>
    /// <param name="type">被切换的床位类型。</param>
    /// <param name="isSelected">true = 加入筛选；false = 移除。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步派发过程的任务。</returns>
    public ValueTask ToggleBedTypeAsync(BedType type, bool isSelected, CancellationToken cancellationToken = default)
    {
        return DispatchAsync(new CardIntent.ToggleBedType(type, isSelected), cancellationToken);
    }

    /// <summary>
    /// 派发 <see cref="CardIntent.ToggleBedStatus"/> 意图以切换床位状态多选。
    /// 由 BedOverview 卡片上 CheckBox 点击事件触发；reducer 内部对非 BedOverview 卡片短路忽略。
    /// </summary>
    /// <param name="status">被切换的床位状态。</param>
    /// <param name="isSelected">true = 加入筛选；false = 移除。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步派发过程的任务。</returns>
    public ValueTask ToggleBedStatusAsync(BedStatus status, bool isSelected, CancellationToken cancellationToken = default)
    {
        return DispatchAsync(new CardIntent.ToggleBedStatus(status, isSelected), cancellationToken);
    }

    /// <summary>
    /// 由 Store.States 订阅触发：每条新状态抵达后重建派生属性（IsFormCard、FormFields、HasFormError）。
    /// <para>
    /// FormFields 列表引用稳定：首次进入 isFormCard 时一次性建好所有 <see cref="CardFormFieldEntry"/> 并加入 <see cref="_formFieldsCache"/>；
    /// 后续 state 抵达只对 entry 调 <c>RefreshValue</c>，不动 list 引用。这是 Avalonia TextBox 输入不丢焦点的核心约束。
    /// </para>
    /// </summary>
    /// <param name="state">新状态。</param>
    private void RebuildDerivedProperties(CardState state)
    {
        CardDefinition? definition = DashboardCardRegistry.GetDefinition(state.PageKey);
        bool isFormCard = definition is { IsFormCard: true };
        bool showBedCatalog = state.PageKey == PageKey.BedOverview;

        if (IsFormCard != isFormCard)
        {
            IsFormCard = isFormCard;
        }

        if (ShowBedCatalog != showBedCatalog)
        {
            ShowBedCatalog = showBedCatalog;
        }

        BedFilterOption targetOption = BedFilterOption.All[0];
        foreach (BedFilterOption option in BedFilterOption.All)
        {
            if (option.Value == state.CurrentBedFilter)
            {
                targetOption = option;
                break;
            }
        }

        SelectedBedFilterOption = targetOption;

        // 同步多选集合（reducer 内部会替换为新 HashSet，所以按引用比较也能正确触发）
        if (!ReferenceEquals(state.SelectedBedTypes, _selectedBedTypes))
        {
            SelectedBedTypes = state.SelectedBedTypes;
        }

        if (!ReferenceEquals(state.SelectedBedStatuses, _selectedBedStatuses))
        {
            SelectedBedStatuses = state.SelectedBedStatuses;
        }

        IReadOnlyList<BedRecord> nextFilteredBeds = showBedCatalog
            ? QueryCombined(state.CurrentBedFilter, state.SelectedBedTypes, state.SelectedBedStatuses)
            : Array.Empty<BedRecord>();
        if (!ReferenceEquals(nextFilteredBeds, _filteredBeds))
        {
            FilteredBeds = nextFilteredBeds;
        }

        if (nextFilteredBeds.Count == 0)
        {
            if (_filteredBedRows.Count != 0)
            {
                FilteredBedRows = Array.Empty<BedRecordRow>();
            }
        }
        else
        {
            BedRecordRow[] nextRows = new BedRecordRow[nextFilteredBeds.Count];
            for (int i = 0; i < nextFilteredBeds.Count; i++)
            {
                nextRows[i] = new BedRecordRow(nextFilteredBeds[i]);
            }

            FilteredBedRows = nextRows;
        }

        if (!isFormCard)
        {
            if (FormFields.Count != 0)
            {
                _formFieldsCache.Clear();
                FormFields = Array.Empty<CardFormFieldEntry>();
            }

            return;
        }

        IReadOnlyList<CardFormField> fields = definition!.FormFields!;
        bool needsNewList = _formFieldsCache.Count != fields.Count;
        if (!needsNewList)
        {
            foreach (CardFormField field in fields)
            {
                if (!_formFieldsCache.ContainsKey(field.Key))
                {
                    needsNewList = true;
                    break;
                }
            }
        }

        if (needsNewList)
        {
            Dictionary<string, CardFormFieldEntry> newCache = new(fields.Count, StringComparer.Ordinal);
            List<CardFormFieldEntry> newList = new(fields.Count);
            foreach (CardFormField field in fields)
            {
                CardFormValueEntry? valueEntry = null;
                foreach (CardFormValueEntry formValue in state.FormValues)
                {
                    if (formValue.Key == field.Key)
                    {
                        valueEntry = formValue;
                        break;
                    }
                }

                CardFormFieldEntry newEntry = new(
                    this,
                    field.Key,
                    field.Label,
                    field.InputHint,
                    valueEntry?.Value ?? field.InitialValue);
                newCache[field.Key] = newEntry;
                newList.Add(newEntry);
            }

            _formFieldsCache.Clear();
            foreach (KeyValuePair<string, CardFormFieldEntry> pair in newCache)
            {
                _formFieldsCache[pair.Key] = pair.Value;
            }

            FormFields = newList;
            return;
        }

        foreach (CardFormField field in fields)
        {
            string currentValue = field.InitialValue;
            foreach (CardFormValueEntry formValue in state.FormValues)
            {
                if (formValue.Key == field.Key)
                {
                    currentValue = formValue.Value;
                    break;
                }
            }

            _formFieldsCache[field.Key].RefreshValue(currentValue);
        }
    }

    /// <inheritdoc />
    protected override void OnDispose()
    {
        _derivedPropertiesSubscription.Dispose();
        base.OnDispose();
    }

    /// <summary>
    /// 合并三种筛选维度：ComboBox 单选状态（<paramref name="filter"/>） + CheckBox 多选类型
    /// （<paramref name="typeFilter"/>） + CheckBox 多选状态（<paramref name="statusFilter"/>）。
    /// 三类筛选独立生效（单选状态优先取对应的 <see cref="BedStatus"/>，再与多选状态取交集）；
    /// 多选集合为空表示该维度不过滤。
    /// </summary>
    /// <param name="filter">ComboBox 单选筛选维度（<see cref="BedFilter.All"/> 不参与状态过滤）。</param>
    /// <param name="typeFilter">CheckBox 多选床位类型集合（空集合 = 不限类型）。</param>
    /// <param name="statusFilter">CheckBox 多选床位状态集合（空集合 = 不限状态）。</param>
    /// <returns>三个维度都满足的床位集合。</returns>
    private static IReadOnlyList<BedRecord> QueryCombined(
        BedFilter filter,
        IReadOnlySet<BedType> typeFilter,
        IReadOnlySet<BedStatus> statusFilter)
    {
        ArgumentNullException.ThrowIfNull(typeFilter);
        ArgumentNullException.ThrowIfNull(statusFilter);

        BedStatus? filterStatus = filter == BedFilter.All ? null : ToStatusFromFilter(filter);
        bool filterTypeEmpty = typeFilter.Count == 0;
        bool filterStatusEmpty = statusFilter.Count == 0;
        if (filterStatus is null && filterTypeEmpty && filterStatusEmpty)
        {
            return BedCatalog.All;
        }

        List<BedRecord> result = new(BedCatalog.TotalCount);
        foreach (BedRecord record in BedCatalog.All)
        {
            bool typeOk = filterTypeEmpty || typeFilter.Contains(record.Type);
            bool statusOk = true;
            if (filterStatus is not null)
            {
                statusOk = record.Status == filterStatus.Value;
            }

            if (statusOk && statusFilter.Count > 0)
            {
                statusOk = statusFilter.Contains(record.Status);
            }

            if (typeOk && statusOk)
            {
                result.Add(record);
            }
        }

        return result;
    }

    private static BedStatus ToStatusFromFilter(BedFilter filter)
    {
        return filter switch
        {
            BedFilter.Open => BedStatus.Open,
            BedFilter.Occupied => BedStatus.Occupied,
            BedFilter.Locked => BedStatus.Locked,
            BedFilter.Isolated => BedStatus.Isolated,
            _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, "未知的 BedFilter 值。"),
        };
    }
}
