using System.ComponentModel;
using System.Runtime.CompilerServices;
using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;
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
    private bool _isFormCard;

    /// <summary>
    /// 初始化仪表板卡片 ViewModel。
    /// </summary>
    /// <param name="store">状态存储。</param>
    public CardViewModel(IMviStore<CardState, CardIntent, CardEffect> store)
        : base(store)
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

        if (IsFormCard != isFormCard)
        {
            IsFormCard = isFormCard;
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
}
