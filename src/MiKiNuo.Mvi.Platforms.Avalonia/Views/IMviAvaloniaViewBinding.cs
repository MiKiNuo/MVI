namespace MiKiNuo.Mvi.Platforms.Avalonia.Views;

/// <summary>提供不销毁业务实例的视图解绑入口。</summary>
public interface IMviAvaloniaViewBinding
{
    /// <summary>解除视图绑定并释放其订阅。</summary>
    public void Unbind();
}
