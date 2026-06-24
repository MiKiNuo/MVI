using MiKiNuo.Mvi.Domain.MVI.Mutation;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient;

/// <summary>
/// 表示门诊工作站页面变更。
/// </summary>
public abstract record OutpatientWorkstationMutation : IMviMutation<OutpatientWorkstationState>;
