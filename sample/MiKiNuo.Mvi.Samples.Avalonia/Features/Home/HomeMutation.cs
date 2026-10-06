namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Home;
/// <summary>大厅公开信息变化。</summary>
/// <param name="DisplayName">显示名，空串表示清除。</param>
public sealed record HomeMutation(string DisplayName) : IMviMutation<HomeState>;
