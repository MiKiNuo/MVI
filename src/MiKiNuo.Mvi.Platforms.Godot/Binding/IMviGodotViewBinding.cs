namespace MiKiNuo.Mvi.Platforms.Godot.Binding;

/// <summary>提供不销毁业务实例的 Godot 视图解绑入口。</summary>
public interface IMviGodotViewBinding
{
    /// <summary>解除当前视图绑定。</summary>
    public void Unbind();
}
