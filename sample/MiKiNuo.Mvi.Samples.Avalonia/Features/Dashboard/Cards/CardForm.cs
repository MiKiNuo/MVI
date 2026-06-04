namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

/// <summary>
/// 表示 Form Card 中一个可编辑字符串输入槽的声明（schema）。
/// Key 在 CardDefinition.FormFields 内唯一，且与 FormValueEntry.Key 配套使用。
/// </summary>
public sealed record CardFormField
{
    /// <summary>字段键，用于 FormValues 列表的查找。</summary>
    public string Key { get; init; }

    /// <summary>显示标签。</summary>
    public string Label { get; init; }

    /// <summary>输入框为空时展示的提示文字。</summary>
    public string InputHint { get; init; }

    /// <summary>初始值。</summary>
    public string InitialValue { get; init; }

    /// <summary>是否为提交必填字段。</summary>
    public bool IsRequired { get; init; }

    /// <summary>
    /// 初始化 Form 字段声明。
    /// </summary>
    /// <param name="key">字段键。</param>
    /// <param name="label">显示标签。</param>
    /// <param name="inputHint">输入框为空时展示的提示文字。</param>
    /// <param name="initialValue">初始值。</param>
    /// <param name="isRequired">是否为提交必填字段。</param>
    public CardFormField(string key, string label, string inputHint, string initialValue, bool isRequired)
    {
        Key = key;
        Label = label;
        InputHint = inputHint;
        InitialValue = initialValue;
        IsRequired = isRequired;
    }
}

/// <summary>
/// 表示 Form Card 状态中 FormValues 列表的单个条目。
/// 与 CardFormField 配对使用：Key 引用 CardDefinition.FormFields 中的某项 Key，Value 是当前用户输入。
/// 设计为 record 以支持原生 `with` 语法（详见 .gsd/DECISIONS.md 2026-06-02）。
/// </summary>
/// <param name="Key">字段键。</param>
/// <param name="Value">当前用户输入的字符串值。</param>
public sealed record CardFormValueEntry(string Key, string Value);
