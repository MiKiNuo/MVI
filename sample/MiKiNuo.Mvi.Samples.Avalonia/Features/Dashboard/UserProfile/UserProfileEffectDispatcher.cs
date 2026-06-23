using MiKiNuo.Mvi.Application.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.UserProfile;

/// <summary>
/// 表示用户信息组件副作用分发器。
/// </summary>
public sealed class UserProfileEffectDispatcher : IMviEffectDispatcher<UserProfileEffect>
{
    /// <summary>
    /// 分发副作用。
    /// </summary>
    /// <param name="effect">副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步分发过程的任务。</returns>
    public ValueTask DispatchAsync(UserProfileEffect effect, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("用户资料当前无副作用需要派发。");
    }
}
