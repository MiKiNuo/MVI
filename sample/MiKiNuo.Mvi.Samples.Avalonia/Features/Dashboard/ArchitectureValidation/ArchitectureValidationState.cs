﻿using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ArchitectureValidation;

/// <summary>
/// 表示架构验证中心状态。
/// <para>
/// 6 个子组件 ViewModel（2 个复用组件 + 4 个指标卡）为静态装配，在页面生命周期内不变，
/// 故不进入 State——它们通过 <see cref="ArchitectureValidationViewModel"/> 构造函数注入。
/// State 仅保留 6 个子组件共享的派生信息与日志，子组件自身 MVI 状态由各自 Store 维护。
/// </para>
/// </summary>
/// <param name="ActiveContext">当前业务上下文。</param>
/// <param name="FlowStatus">当前流程状态。</param>
/// <param name="InteractionLog">交互日志。</param>
public sealed record ArchitectureValidationState(
    string ActiveContext,
    string FlowStatus,
    string InteractionLog) : IMviState;
