namespace MiKiNuo.Mvi.Samples.Avalonia.Features.V2Input;

/// <summary>通过生成输入入口提交表单，附加规则只转换名称输入。</summary>
public sealed partial class InputFormFeature() : Feature<InputFormState>(new())
{
    [OnInput(nameof(InputFormState.Name))]
    private static InputFormState NormalizeName(InputFormState state, string input) => state with { Name = input.Trim() };
}
