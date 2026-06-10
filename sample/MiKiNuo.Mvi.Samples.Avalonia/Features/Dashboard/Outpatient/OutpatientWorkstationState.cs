using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient;

/// <summary>
/// 表示门诊工作站页面状态。
/// <para>
/// 3 个子组件 ViewModel（候诊队列、电子病历编辑、临床提醒）为静态装配，在页面生命周期内
/// 不变，故不进入 State——它们通过 <see cref="OutpatientWorkstationViewModel"/> 构造函数注入。
/// State 仅保留 3 个子组件共享的派生信息与日志，子组件自身 MVI 状态由各自 Store 维护。
/// </para>
/// </summary>
/// <param name="InteractionLog">父子 MVI 与子子 MVI 交互日志。</param>
public sealed record OutpatientWorkstationState(string InteractionLog) : IMviState;
