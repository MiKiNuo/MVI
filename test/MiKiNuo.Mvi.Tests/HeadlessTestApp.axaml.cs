using Avalonia;
using Avalonia.Markup.Xaml;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 Avalonia headless 测试专用应用。
/// <para>
/// 仅注册 <see cref="Avalonia.Themes.Fluent.FluentTheme"/>：headless 平台不需要 UI 主题，
/// 但部分控件（ComboBox、TextBox、Button）的默认模板会引用 <c>ResourceInclude</c>，
/// 没有主题会抛 <c>ResourceNotFoundException</c>。
/// </para>
/// <para>
/// 这里不引用 <c>MiKiNuo.Mvi.Samples.Avalonia.App</c>，因为该 <see cref="global::Avalonia.Application"/> 子类
/// 会重写 <c>OnFrameworkInitializationCompleted</c> 实例化 <c>SampleCompositionRoot</c> 并打开登录窗；
/// headless 阶段不需要这些副作用。
/// </para>
/// </summary>
public sealed class HeadlessTestApp : global::Avalonia.Application
{
    /// <summary>
    /// 初始化应用程序。
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
