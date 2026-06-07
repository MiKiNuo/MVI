using MiKiNuo.Mvi.Domain.MVI.State;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.PatientRegistry;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

/// <summary>
/// 表示仪表板卡片的统一状态。
/// 涵盖 Simple Card 和 Form Card：Simple Card 的 FormValues 为空集合且 FormFields 视为 null。
/// </summary>
/// <param name="PageKey">该状态归属的卡片 PageKey；reducer 内部路由依赖此字段。</param>
/// <param name="SourceKey">来源键（如 "Inpatient"），用于卡片头部标识。</param>
/// <param name="SourceDisplayName">来源显示名（如 "住院调度"）。</param>
/// <param name="Title">卡片标题。</param>
/// <param name="MainValue">核心指标文本。</param>
/// <param name="StatusText">状态文本。</param>
/// <param name="DetailText">详情文本。</param>
/// <param name="ActionLog">动作日志（最近一次操作的回执）。</param>
/// <param name="PrimaryActionText">主动作按钮文本。</param>
/// <param name="SecondaryActionText">辅助动作按钮文本。</param>
/// <param name="CanPrimaryAction">主动作是否可执行。</param>
/// <param name="CanSecondaryAction">辅助动作是否可执行。</param>
/// <param name="FormErrorMessage">Form 提交错误提示（无错误时为空串）。</param>
/// <param name="FormValues">Form Card 的字段值集合（顺序保留）。Simple Card 传空集合。</param>
/// <param name="RecentAdmittedPatient">最近一次入院登记卡提交后流入本卡片的患者记录（同一 SourceKey 组内共享）。未触发时为 null。</param>
public sealed record CardState(
    PageKey PageKey,
    string SourceKey,
    string SourceDisplayName,
    string Title,
    string MainValue,
    string StatusText,
    string DetailText,
    string ActionLog,
    string PrimaryActionText,
    string SecondaryActionText,
    bool CanPrimaryAction,
    bool CanSecondaryAction,
    string FormErrorMessage,
    IReadOnlyList<CardFormValueEntry> FormValues,
    Patient? RecentAdmittedPatient) : IMviState
{
    /// <summary>
    /// 根据 CardDefinition 创建初始状态。
    /// </summary>
    /// <param name="definition">卡片定义。</param>
    /// <returns>填充初始值的状态。</returns>
    public static CardState FromDefinition(CardDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        CardFormValueEntry[] formValues;
        if (definition.FormFields is null)
        {
            formValues = [];
        }
        else
        {
            formValues = new CardFormValueEntry[definition.FormFields.Count];
            for (int i = 0; i < definition.FormFields.Count; i++)
            {
                CardFormField field = definition.FormFields[i];
                formValues[i] = new CardFormValueEntry(field.Key, field.InitialValue);
            }
        }

        return new CardState(
            definition.Key,
            definition.SourceKey,
            definition.SourceDisplayName,
            definition.Title,
            definition.MainValue,
            definition.StatusText,
            definition.DetailText,
            $"等待 {definition.SourceDisplayName} 触发业务流。",
            definition.PrimaryActionText,
            definition.SecondaryActionText,
            true,
            true,
            string.Empty,
            formValues,
            RecentAdmittedPatient: null);
    }

    /// <summary>
    /// 返回更新某个 FormValues 条目后的新状态；若 Key 不存在则保持不变。
    /// </summary>
    /// <param name="key">字段键。</param>
    /// <param name="value">新值。</param>
    /// <returns>新状态。</returns>
    public CardState WithFormValue(string key, string value)
    {
        ArgumentNullException.ThrowIfNull(key);

        int index = -1;
        for (int i = 0; i < FormValues.Count; i++)
        {
            if (FormValues[i].Key == key)
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            return this;
        }

        CardFormValueEntry[] nextValues = FormValues.ToArray();
        nextValues[index] = new CardFormValueEntry(key, value);
        return this with { FormValues = nextValues };
    }

    /// <summary>
    /// 查找指定 Key 的 FormValues 条目。
    /// </summary>
    /// <param name="key">字段键。</param>
    /// <returns>找到则返回条目；否则返回 null。</returns>
    public CardFormValueEntry? FindFormValue(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        CardFormValueEntry? result = null;
        for (int i = 0; i < FormValues.Count; i++)
        {
            CardFormValueEntry entry = FormValues[i];
            if (entry.Key == key)
            {
                result = entry;
                break;
            }
        }

        return result;
    }
}
