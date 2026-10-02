namespace MiKiNuo.Mvi.Samples.Godot;

/// <summary>通过生成输入与操作入口处理 HUD 编辑、计分及领奖。</summary>
public sealed partial class HudFeature() : Feature<HudState>(new())
{
    [OnInput(nameof(HudState.PlayerName))]
    private static HudState NormalizeName(HudState state, string value)
        => state with { PlayerName = value.Trim().ToUpperInvariant() };

    [OnInput(nameof(HudState.Delta))]
    private static HudState Accumulate(HudState state, int value)
        => state with { Delta = value, Score = state.Score + value, AcceptedInputs = state.AcceptedInputs + 1 };

    [Operation(Validate = nameof(CanClaim))]
    private async ValueTask<int> Claim(Operation<HudState> operation)
    {
        await operation.UpdateAsync(static (state, _) => state with { Actions = state.Actions + 1 }, 0);
        return operation.Snapshot.Actions + 1;
    }

    private static bool CanClaim(HudState state) => state.PlayerName.Length != 0;
}
