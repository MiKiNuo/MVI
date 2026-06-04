using MiKiNuo.Mvi.Domain.MVI.Intent;

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

    /// <summary>设置 FormValues 中指定 Key 的值。仅对 Form Card 有效。</summary>
    /// <param name="Key">字段键。</param>
    /// <param name="Value">新值。</param>
    public sealed partial record SetFormField(string Key, string Value) : CardIntent;

    /// <summary>提交 Form Card。仅对 Form Card 有效。</summary>
    public sealed partial record SubmitForm : CardIntent;
}
