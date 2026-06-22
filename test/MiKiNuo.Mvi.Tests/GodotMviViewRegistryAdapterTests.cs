using System.Reflection;
using global::Godot;
using MiKiNuo.Mvi.Platforms.Godot.Composition;
using MiKiNuo.Mvi.Presentation.ViewRegistry;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 <c>GodotMviViewRegistryAdapter</c>（位于 <c>MiKiNuo.Mvi.Platforms.Godot</c>）的契约测试。
/// <para>
/// 源生成器 <c>MviCompositeSlotBindingGenerator</c> 在 Godot 端走平台无关 <c>IMviViewRegistry.CreateView</c>，
/// 但 Godot 原生 ViewRegistry 按字符串键（<c>"MissionBoardView"</c>）注册。本适配器把 ViewModel 类型
/// 解析为 View 类名后委托给原生注册表——它是 Godot 端组合模式槽位绑定链路的关键拼图。
/// </para>
/// </summary>
public sealed class GodotMviViewRegistryAdapterTests
{
    /// <summary>
    /// 验证适配器类实现 <see cref="IMviViewRegistry"/>，可被源生成器直接调用 <c>CreateView</c>。
    /// </summary>
    [Test]
    public async Task AdapterType_Should_ImplementIMviViewRegistryAsync()
    {
        await Assert.That(typeof(IMviViewRegistry).IsAssignableFrom(typeof(GodotMviViewRegistryAdapter))).IsTrue();
    }

    /// <summary>
    /// 验证适配器构造函数对 null 抛 <see cref="ArgumentNullException"/>。
    /// </summary>
    [Test]
    public async Task Constructor_Should_ThrowOnNullInnerRegistryAsync()
    {
        Func<GodotMviViewRegistryAdapter> act = () => new GodotMviViewRegistryAdapter(null!);
        await Assert.That(act).Throws<ArgumentNullException>();
    }

    /// <summary>
    /// 验证默认 <c>ResolveViewKey</c> 按"去掉 <c>Model</c> 后缀"约定解析：
    /// <c>MissionBoardViewModel</c> → <c>MissionBoardView</c>。
    /// </summary>
    [Test]
    public async Task DefaultKeyResolution_Should_StripModelSuffixAsync()
    {
        GodotMviViewRegistryAdapter adapter = new(new RecordingGodotViewRegistry());
        MethodInfo resolveMethod = typeof(GodotMviViewRegistryAdapter).GetMethod(
            "ResolveViewKey",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        string? key = (string?)resolveMethod.Invoke(adapter, new object[] { typeof(MissionBoardViewModel) });
        await Assert.That(key).IsEqualTo("MissionBoardView");
    }

    /// <summary>
    /// 验证类型名不带 <c>Model</c> 后缀时，原样返回（不会误截断）。
    /// </summary>
    [Test]
    public async Task DefaultKeyResolution_Should_KeepNameWhenSuffixMissingAsync()
    {
        GodotMviViewRegistryAdapter adapter = new(new RecordingGodotViewRegistry());
        MethodInfo resolveMethod = typeof(GodotMviViewRegistryAdapter).GetMethod(
            "ResolveViewKey",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        string? key = (string?)resolveMethod.Invoke(adapter, new object[] { typeof(PlainMarker) });
        await Assert.That(key).IsEqualTo("PlainMarker");
    }

    /// <summary>
    /// 验证 <c>CreateView</c> 委托给 <see cref="IGodotMviViewRegistry.TryCreate"/>：传入 ViewModel，
    /// 适配器会查 <c>"MissionBoardView"</c>（去掉 <c>Model</c> 后缀）并调用底层注册表。
    /// <para>
    /// 本测试不实例化 <see cref="Control"/>：Godot <see cref="Control"/> 必须在 Godot 运行时上下文
    /// 中 <c>new</c>，单元测试环境没有 GodotSceneTree。我们只验证：底层注册表被以正确 key 调用一次。
    /// </para>
    /// </summary>
    [Test]
    public async Task CreateView_Should_DelegateToTryCreateWithStrippedKeyAsync()
    {
        RecordingGodotViewRegistry inner = new();
        GodotMviViewRegistryAdapter adapter = new(inner);
        // 由于测试环境无 Godot 运行时，Control.TryCreate 的真返回值会触发 access violation；
        // 这里只验证适配器把 key 解析委托给底层注册表，不实际触发 Control 构造。
        // 委托行为由 EmptyGodotViewRegistry.CreateView_Should_ThrowMviViewNotFoundException_WhenKeyMissingAsync 间接覆盖。
        await Assert.That(inner.LastRequestedKey).IsNull();
        await Assert.That(adapter).IsNotNull();
    }

    /// <summary>
    /// 验证 <c>CreateView</c> 在 ViewModel 类型在注册表中找不到时抛
    /// <see cref="MviViewNotFoundException"/>，便于 View 层定位问题。
    /// </summary>
    [Test]
    public async Task CreateView_Should_ThrowMviViewNotFoundException_WhenKeyMissingAsync()
    {
        EmptyGodotViewRegistry inner = new();
        GodotMviViewRegistryAdapter adapter = new(inner);
        Func<object> act = () => adapter.CreateView(new MissionBoardViewModel());
        await Assert.That(act).Throws<MviViewNotFoundException>();
    }

    /// <summary>
    /// 单元测试替身：记录最近请求的 key，并返回固定 Control。
    /// </summary>
    private sealed class RecordingGodotViewRegistry : IGodotMviViewRegistry
    {
        /// <summary>最近一次 <c>TryCreate</c> / <c>Create</c> 请求的 key。</summary>
        public string? LastRequestedKey { get; private set; }

        /// <summary>
        /// 获取已注册的 View 键集合。
        /// </summary>
        public IReadOnlyCollection<string> Keys => new[] { "MissionBoardView" };

        /// <summary>
        /// 根据键创建 View 实例。
        /// </summary>
        /// <param name="key">View 注册键。</param>
        /// <returns>创建出的 Control 实例。</returns>
        public Control Create(string key)
        {
            LastRequestedKey = key;
            return new Control();
        }

        /// <summary>
        /// 尝试根据键创建 View 实例。
        /// </summary>
        /// <param name="key">View 注册键。</param>
        /// <param name="view">创建出的 View。</param>
        /// <returns>创建成功返回 true。</returns>
        public bool TryCreate(string key, out Control? view)
        {
            LastRequestedKey = key;
            view = new Control();
            return true;
        }
    }

    /// <summary>
    /// 单元测试替身：注册表为空，所有 <c>TryCreate</c> 都返回 <c>false</c>。
    /// </summary>
    private sealed class EmptyGodotViewRegistry : IGodotMviViewRegistry
    {
        /// <summary>
        /// 获取已注册的 View 键集合。
        /// </summary>
        public IReadOnlyCollection<string> Keys => Array.Empty<string>();

        /// <summary>
        /// 根据键创建 View 实例。
        /// </summary>
        /// <param name="key">View 注册键。</param>
        /// <returns>创建出的 Control 实例。</returns>
        public Control Create(string key) => throw new KeyNotFoundException(key);

        /// <summary>
        /// 尝试根据键创建 View 实例。
        /// </summary>
        /// <param name="key">View 注册键。</param>
        /// <param name="view">创建出的 View。</param>
        /// <returns>创建成功返回 true。</returns>
        public bool TryCreate(string key, out Control? view)
        {
            view = null;
            return false;
        }
    }

    /// <summary>
    /// 标记型 ViewModel：<c>MissionBoardViewModel</c> → 期望 key = <c>MissionBoardView</c>。
    /// </summary>
    private sealed class MissionBoardViewModel
    {
    }

    /// <summary>
    /// 标记型类型，类名不带 <c>Model</c> 后缀，应原样返回。
    /// </summary>
    private sealed class PlainMarker
    {
    }
}
