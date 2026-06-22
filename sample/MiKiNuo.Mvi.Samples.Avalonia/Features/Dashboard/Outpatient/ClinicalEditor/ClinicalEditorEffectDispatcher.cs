using MiKiNuo.Mvi.Application.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalEditor;

/// <summary>
/// 表示门诊病历编辑副作用分发器。
/// </summary>
public sealed class ClinicalEditorEffectDispatcher : IMviEffectDispatcher<ClinicalEditorEffect>
{
    /// <summary>
    /// 分发副作用。
    /// </summary>
    /// <param name="effect">副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步分发过程的任务。</returns>
    public ValueTask DispatchAsync(ClinicalEditorEffect effect, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
