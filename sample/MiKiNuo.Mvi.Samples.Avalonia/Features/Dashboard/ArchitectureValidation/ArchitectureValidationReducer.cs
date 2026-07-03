using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ArchitectureValidation;

/// <summary>
/// 表示架构验证中心规约器。
/// </summary>
public sealed partial class ArchitectureValidationReducer
    : MviReducerBase<ArchitectureValidationState, ArchitectureValidationIntent, UnitEffect>
{
}
