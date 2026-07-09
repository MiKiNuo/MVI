using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 <c>MviCompositeSlotBindingGenerator</c> 源生成器的契约测试。
/// <para>
/// 组合模式槽位源生成器扫描 partial class 上标有 <c>[MviSlot]</c> 的字段，
/// 在 partial class 内 <c>override</c> <c>OnBindSlots(viewModel, bindings, resolver)</c> 钩子，
/// emit <c>viewModel.{Factory}()</c> → <c>registry.CreateView(childVm)</c> → 写入槽位的绑定管线。
/// </para>
/// <para>
/// 平台差异由"槽位字段类型"承担：Avalonia 的 <c>MviSlotHost</c>（ContentControl）使用
/// <c>slot.Content = view</c>；Godot 的 <c>Control</c> 使用
/// <c>Clear + AddChild</c>。生成器依据字段符号类型 emit 对应片段。
/// </para>
/// </summary>
public sealed class MviCompositeSlotBindingGeneratorTests
{
    /// <summary>
    /// 验证生成器 emit 的 OnBindSlots override 同时支持 Avalonia（<c>slot.Content = view</c>）
    /// 与 Godot（<c>Clear + AddChild</c>）两种槽位字段挂载语义。
    /// </summary>
    [Test]
    public async Task Emission_Should_HandleBothAvaloniaAndGodotSlotMountingAsync()
    {
        string root = FindRepositoryRoot();
        string generatorPath = Path.Combine(
            root,
            "src",
            "MiKiNuo.Mvi.Infrastructure",
            "BuildTime",
            "SourceGeneration",
            "MviCompositeSlotBindingGenerator.cs");

        string content = await File.ReadAllTextAsync(generatorPath);

        // Avalonia: MviSlotHost 的 ContentControl.Content 赋值
        await Assert.That(content).Contains("MviSlotHost");
        await Assert.That(content).Contains(".Content = ");

        // Godot: Control 节点 Clear + AddChild
        await Assert.That(content).Contains("GetChildren");
        await Assert.That(content).Contains("AddChild");
    }

    /// <summary>
    /// 验证 Godot 槽位挂载片段不再错误地引用 Avalonia 的 <c>MviSlotHost</c> 元数据名，
    /// 且正确把 <c>object</c> 类型的 view 强转为 <c>Node</c> 之后才能调 <c>Control.AddChild</c>。
    /// <para>
    /// Avalonia 走 <c>ContentControl.Content = view</c>；Godot 走 <c>Control.AddChild(view)</c>。
    /// IMviViewRegistry.CreateView 返回 <c>object</c>，Godot 路径必须显式强转 <c>(Node)view</c>：
    /// </para>
    /// <list type="bullet">
    /// <item>修复前 1：生成器在 Godot 路径写出 <c>(MiKiNuo.Mvi.Platforms.Avalonia.Slot.MviSlotHost)view</c>，编译必失败（找不到类型）。</item>
    /// <item>修复前 2：直接 <c>slot.AddChild(view)</c> 触发 CS1503（object 无法隐式转 Node）。</item>
    /// </list>
    /// </summary>
    [Test]
    public async Task Emission_Should_NotCastGodotViewToAvaloniaSlotHostAsync()
    {
        string root = FindRepositoryRoot();
        string generatorPath = Path.Combine(
            root,
            "src",
            "MiKiNuo.Mvi.Infrastructure",
            "BuildTime",
            "SourceGeneration",
            "MviCompositeSlotBindingGenerator.cs");

        string content = await File.ReadAllTextAsync(generatorPath);

        // 1) 全文不应再出现把 view 强转为 Avalonia MviSlotHost 的字面量
        await Assert.That(content).DoesNotContain("(MiKiNuo.Mvi.Platforms.Avalonia.Slot.MviSlotHost)view");

        // 2) 更精准：MountSlotExpression 函数体的 Godot 分支不应再拼接 AvaloniaSlotHostMetadataName
        int mountIndex = content.IndexOf("private static string MountSlotExpression", StringComparison.Ordinal);
        await Assert.That(mountIndex).IsGreaterThanOrEqualTo(0);
        int mountEnd = content.IndexOf("private static string ClearSlotExpression", mountIndex, StringComparison.Ordinal);
        await Assert.That(mountEnd).IsGreaterThan(mountIndex);
        string mountBody = content.Substring(mountIndex, mountEnd - mountIndex);
        int godotBranchStart = mountBody.IndexOf("SlotPlatform.Godot", StringComparison.Ordinal);
        int godotBranchEnd = mountBody.IndexOf('_', godotBranchStart);
        await Assert.That(godotBranchStart).IsGreaterThanOrEqualTo(0);
        await Assert.That(godotBranchEnd).IsGreaterThan(godotBranchStart);
        string godotBranch = mountBody.Substring(godotBranchStart, godotBranchEnd - godotBranchStart);
        await Assert.That(godotBranch).DoesNotContain("AvaloniaSlotHostMetadataName");
        await Assert.That(godotBranch).Contains("AddChild");

        // 3) Godot 分支必须包含 (Node)view 强转，否则 Godot 编译会因为 object 无法转 Node 失败
        await Assert.That(godotBranch).Contains("(Node)view");
    }

    /// <summary>
    /// 验证生成器在同一个项目里扫描到多个含 <c>[MviSlot]</c> 字段的 partial class 时，
    /// <b>每一个</b>类都拿到独立的 <c>OnBindSlots</c> override emit，而不是只 emit 字典序首个。
    /// <para>
    /// 之前 <c>MviCompositeSlotBindingGenerator.Initialize</c> 内部把
    /// <c>grouped.First()</c> 作为唯一目标类型，导致项目里
    /// <c>DashboardView</c> / <c>OutpatientWorkstationView</c> 同时有
    /// <c>[MviSlot]</c> 字段时，只有 <c>DashboardView</c> 拿到 override，
    /// 门诊工作站右侧组合页（候诊队列 / 电子病历 / 临床提醒）永远为空白。
    /// </para>
    /// </summary>
    [Test]
    public async Task Emission_Should_EmitOnBindSlots_ForEveryClassWithSlotsAsync()
    {
        string root = FindRepositoryRoot();
        string generatorPath = Path.Combine(
            root,
            "src",
            "MiKiNuo.Mvi.Infrastructure",
            "BuildTime",
            "SourceGeneration",
            "MviCompositeSlotBindingGenerator.cs");

        string content = await File.ReadAllTextAsync(generatorPath);

        // 关键契约：必须按 ContainingType 分组循环 emit，output 回调内含 foreach (KeyValuePair)。
        await Assert.That(content).Contains("foreach (KeyValuePair<INamedTypeSymbol");
        await Assert.That(content).Contains("productionContext.AddSource(model.HintName");

        // 反向兜底：BuildGenerationModel 现在是按"传入的 slotsForClass"逐个 emit，
        // 不应在内部再做"取 First()"这种"只取首个"的选择。
        int buildIndex = content.IndexOf("public static SlotGenerationModel? BuildGenerationModel", StringComparison.Ordinal);
        await Assert.That(buildIndex).IsGreaterThanOrEqualTo(0);
        int buildEnd = content.IndexOf("private static INamedTypeSymbol? ResolveViewModelType", buildIndex, StringComparison.Ordinal);
        await Assert.That(buildEnd).IsGreaterThan(buildIndex);
        string buildBody = content.Substring(buildIndex, buildEnd - buildIndex);
        await Assert.That(buildBody).DoesNotContain(".First()");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MiKiNuo.Mvi.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException("未找到解决方案根目录。");
        }

        return directory.FullName;
    }
}
