namespace MiKiNuo.Mvi.Samples.Godot;

/// <summary>保存 HUD 的独立不可变业务状态。</summary>
public sealed record HudState
{
    /// <summary>获取允许中间编辑并由纯规则归一化的玩家名称。</summary>
    [Input]
    public string PlayerName { get; init; } = "PILOT";
    /// <summary>获取最近一次计分输入。</summary>
    [Input]
    public int Delta { get; init; }
    /// <summary>获取逐次输入累计的分数。</summary>
    public int Score { get; init; }
    /// <summary>获取已处理的计分输入数量。</summary>
    public int AcceptedInputs { get; init; }
    /// <summary>获取不参与 HUD 字段展示的遥测值。</summary>
    [Input]
    public int Telemetry { get; init; }
    /// <summary>获取实际完成的领奖操作数量。</summary>
    public int Actions { get; init; }
}
