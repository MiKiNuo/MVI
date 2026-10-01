using System.Globalization;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.V2Input;

/// <summary>保存普通输入和允许中间编辑值的表单快照。</summary>
public sealed record InputFormState
{
    /// <summary>获取可编辑的显示名称。</summary>
    [Input]
    public string Name { get; init; } = "";

    /// <summary>获取金额的原始编辑文本，允许负号等中间值。</summary>
    [Input]
    public string AmountText { get; init; } = "";

    /// <summary>获取与名称无关的展示计数。</summary>
    [Input]
    public int Other { get; init; }

    /// <summary>获取根据名称计算的只读问候。</summary>
    public string Greeting => "你好，" + Name;

    /// <summary>获取由已提交金额文本计算的只读显示。</summary>
    public string AmountDisplay => decimal.TryParse(AmountText, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount)
        ? amount.ToString("0.00", CultureInfo.InvariantCulture) : "金额尚未完成";
}
