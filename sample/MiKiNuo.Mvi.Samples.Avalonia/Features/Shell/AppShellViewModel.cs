namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;
/// <summary>壳直接暴露只读 State，无须重复声明页面属性。</summary>
public sealed partial class AppShellViewModel : MviViewModelBase<AppShellState, AppShellIntent> { }
