using System.Threading;
using System.Threading.Tasks;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Common;

/// <summary>
/// 表示跨 MVI 组件的游戏壳导航协调器。
/// </summary>
public interface IGameShellNavigator
{
    /// <summary>
    /// 打开游戏大厅并传递玩家资料。
    /// </summary>
    /// <param name="profile">玩家资料。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步操作。</returns>
    public ValueTask OpenLobbyAsync(PlayerProfile profile, CancellationToken cancellationToken);

    /// <summary>
    /// 返回登录界面。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步操作。</returns>
    public ValueTask ReturnToLoginAsync(CancellationToken cancellationToken);
}
