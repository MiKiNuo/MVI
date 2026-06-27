using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示活动日志意图。
/// </summary>
public abstract partial record ActivityLogIntent : IMviIntent
{
    /// <summary>
    /// 表示追加日志条目。
    /// </summary>
    public sealed partial record AppendEntry : ActivityLogIntent
    {
        /// <summary>
        /// 初始化追加日志条目。
        /// </summary>
        /// <param name="message">日志消息。</param>
        public AppendEntry(string message)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(message);
            Message = message;
        }

        /// <summary>获取日志消息。</summary>
        public string Message { get; init; }
    }
}
