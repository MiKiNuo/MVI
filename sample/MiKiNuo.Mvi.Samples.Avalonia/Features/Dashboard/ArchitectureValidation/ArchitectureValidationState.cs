﻿﻿﻿﻿﻿﻿using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ArchitectureValidation;

/// <summary>
/// 表示架构验证中心状态。
/// <para>
/// 6 个子组件 ViewModel（2 个复用组件 + 4 个指标卡）通过工厂按需解析，
/// 不进入 State——它们在页面生命周期内不变或按 contextName 缓存，
/// 解析路径走 <see cref="ArchitectureValidationViewModel"/> 工厂方法。
/// State 仅保留 3 个派生字段（上下文/状态/日志），子组件自身 MVI 状态由各自 Store 维护。
/// </para>
/// </summary>
/// <param name="ActiveContext">当前业务上下文。</param>
/// <param name="FlowStatus">当前流程状态。</param>
/// <param name="InteractionLog">交互日志。</param>
public sealed record ArchitectureValidationState(
    string ActiveContext,
    string FlowStatus,
    string InteractionLog) : IMviState;
