using System;

namespace MiKiNuo.Mvi.Platforms.Godot.Composition;

/// <summary>
/// 标记一个 partial Registry 类型需要由源生成器填充 Godot View 注册逻辑。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MviGodotGeneratedRegistryAttribute : Attribute
{
}
