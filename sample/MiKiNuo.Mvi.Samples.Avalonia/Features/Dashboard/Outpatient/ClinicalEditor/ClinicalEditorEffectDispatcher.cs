using MiKiNuo.Mvi.Application.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalEditor;

/// <summary>
/// 表示门诊病历编辑副作用分发器。
/// </summary>
public sealed class ClinicalEditorEffectDispatcher : IMviEffectDispatcher<ClinicalEditorEffect>
{
    /// <inheritdoc />
    public ValueTask DispatchAsync(ClinicalEditorEffect effect, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
