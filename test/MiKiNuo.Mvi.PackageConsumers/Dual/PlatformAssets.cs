namespace PackageConsumer;

public static class PlatformAssets
{
    public static Type Avalonia => typeof(global::MiKiNuo.Mvi.Platforms.Avalonia.AvaloniaFeatureHost);
    public static Type Godot => typeof(global::MiKiNuo.Mvi.Platforms.Godot.GodotFeatureHost);
}
