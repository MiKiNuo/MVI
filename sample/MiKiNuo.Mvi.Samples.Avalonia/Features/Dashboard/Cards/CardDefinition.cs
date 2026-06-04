namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

/// <summary>
/// 表示一张仪表板卡片的所有可配置数据。
/// 通过 DashboardCardRegistry 注册，reducer 内部按 PageKey 查找。
/// </summary>
/// <param name="Key">卡片 PageKey。</param>
/// <param name="SourceKey">Mediator 路由中的"源组件"分类（住院床位 / 检验医嘱 / 处方 / 风险等）。</param>
/// <param name="SourceDisplayName">卡片在 Mediator 日志中显示的中文名称。</param>
/// <param name="Title">卡片标题。</param>
/// <param name="MainValue">核心指标文本。</param>
/// <param name="StatusText">初始状态文本。</param>
/// <param name="DetailText">初始详情文本。</param>
/// <param name="PrimaryActionText">主动作按钮文本。</param>
/// <param name="SecondaryActionText">辅助动作按钮文本。</param>
/// <param name="FormFields">Form 字段声明集合。null 或空集合表示 Simple Card。</param>
/// <param name="RequiredFormFields">提交必填字段 Key 集合（FormCard 专用）。</param>
/// <param name="Validator">Form 验证函数；输入当前 FormValues，输出 (CanSubmit, StatusText, ActionLog)。null 表示 Simple Card。</param>
public sealed record CardDefinition(
    PageKey Key,
    string SourceKey,
    string SourceDisplayName,
    string Title,
    string MainValue,
    string StatusText,
    string DetailText,
    string PrimaryActionText,
    string SecondaryActionText,
    IReadOnlyList<CardFormField>? FormFields,
    IReadOnlyList<string>? RequiredFormFields,
    Func<IReadOnlyList<CardFormValueEntry>, (bool CanSubmit, string StatusText, string ActionLog)>? Validator)
{
    /// <summary>
    /// 判断当前卡片是否为 Form Card。
    /// </summary>
    public bool IsFormCard => FormFields is { Count: > 0 };
}
