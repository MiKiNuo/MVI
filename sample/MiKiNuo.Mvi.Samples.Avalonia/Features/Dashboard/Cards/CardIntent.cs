using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedCatalog;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.PatientRegistry;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

/// <summary>
/// 表示仪表板卡片的统一意图。
/// 所有 16 张卡共用同一个意图类型集合；reducer 通过 state.PageKey 区分处理。
/// </summary>
public abstract partial record CardIntent : IMviIntent
{
    /// <summary>执行主业务动作。</summary>
    public sealed partial record ExecutePrimaryAction : CardIntent;

    /// <summary>执行辅助业务动作。</summary>
    public sealed partial record ExecuteSecondaryAction : CardIntent;

    /// <summary>应用来自父页面或兄弟卡片的外部更新消息。</summary>
    /// <param name="Message">外部更新消息。</param>
    public sealed partial record ApplyExternalUpdate(string Message) : CardIntent;

    /// <summary>应用来自兄弟入院登记卡片的新入院患者通知。</summary>
    /// <param name="Patient">新入的患者记录（强类型载荷）。</param>
    public sealed partial record ApplyPatientAdmitted(Patient Patient) : CardIntent;

    /// <summary>设置 FormValues 中指定 Key 的值。仅对 Form Card 有效。</summary>
    /// <param name="Key">字段键。</param>
    /// <param name="Value">新值。</param>
    public sealed partial record SetFormField(string Key, string Value) : CardIntent;

    /// <summary>提交 Form Card。仅对 Form Card 有效。</summary>
    public sealed partial record SubmitForm : CardIntent;

    /// <summary>设置床位筛选维度。仅对 BedOverview 卡片有效；其他卡片接收后由 reducer 忽略。</summary>
    /// <param name="Filter">新的筛选维度。</param>
    public sealed partial record SetBedFilter(BedFilter Filter) : CardIntent;

    /// <summary>切换床位类型多选（CheckBox）。仅对 BedOverview 卡片有效；其他卡片 reducer 忽略。</summary>
    /// <param name="BedType">被切换的床位类型。</param>
    /// <param name="IsSelected">true = 加入筛选集合；false = 从集合中移除。</param>
    public sealed partial record ToggleBedType(BedType BedType, bool IsSelected) : CardIntent;

    /// <summary>切换床位状态多选（CheckBox）。仅对 BedOverview 卡片有效；其他卡片 reducer 忽略。</summary>
    /// <param name="BedStatus">被切换的床位状态。</param>
    /// <param name="IsSelected">true = 加入筛选集合；false = 从集合中移除。</param>
    public sealed partial record ToggleBedStatus(BedStatus BedStatus, bool IsSelected) : CardIntent;
}
