using System;
using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Godot.Features.AppShell;

/// <summary>
/// 表示游戏应用壳 MVI 副作用。
/// </summary>
public abstract partial record AppShellEffect : IMviEffect
{
    /// <summary>
    /// 表示写入应用壳日志的副作用。
    /// </summary>
    public sealed partial record Trace : AppShellEffect
    {
        /// <summary>
        /// 初始化写入应用壳日志的副作用。
        /// </summary>
        /// <param name="text">日志文本。</param>
        public Trace(string text)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(text);
            Text = text;
        }

        /// <summary>
        /// 获取日志文本。
        /// </summary>
        public string Text { get; init; }
    }
}
