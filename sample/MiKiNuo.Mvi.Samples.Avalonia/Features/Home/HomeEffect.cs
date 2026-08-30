using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Home;

/// <summary>
/// 表示主页副作用。
/// </summary>
public abstract partial record HomeEffect : IMviEffect
{
    /// <summary>
    /// 表示跳转到登录页副作用。
    /// </summary>
    public sealed partial record ShowLoginPage : HomeEffect;
}
