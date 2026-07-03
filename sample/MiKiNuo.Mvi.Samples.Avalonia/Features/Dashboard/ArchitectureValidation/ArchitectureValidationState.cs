﻿using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ArchitectureValidation;

/// <summary>
/// 表示架构验证中心状态。
/// <para>
/// 演示页无运行时状态：6 个子组件 ViewModel 通过工厂按需解析，
/// 自身 MVI 状态由各自 Store 维护。
/// </para>
/// </summary>
public sealed record ArchitectureValidationState() : IMviState;
