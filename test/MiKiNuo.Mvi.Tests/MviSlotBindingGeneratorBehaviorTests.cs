using Microsoft.CodeAnalysis;
using MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 <c>MviCompositeSlotBindingGenerator</c> 源生成器的行为测试。
/// 使用 <c>CSharpGeneratorDriver</c> 驱动生成器并验证生成产物。
/// </summary>
public sealed class MviSlotBindingGeneratorBehaviorTests
{
    /// <summary>
    /// 验证含 <c>[MviSlot]</c> 字段的 Avalonia View
    /// 触发生成器产出槽位挂载代码。
    /// </summary>
    /// <remarks>
    /// 使用生成树检查而非编译验证:槽位绑定生成代码
    /// 依赖完整的 MviAvaloniaView/MviDisposableBag/IMviResolver
    /// 运行时接口,桩定义无法完整模拟,编译验证不可行。
    /// </remarks>
    [Test]
    public async Task Generate_OnBindSlots_Should_ProduceSlotMountingCodeAsync()
    {
        GeneratorDriverRunResult result =
            GeneratorTestHost.RunGenerator<MviCompositeSlotBindingGenerator>(
                StubDefinitions + "\n" + SlotSourceWithoutObserves);

        await Assert.That(result.GeneratedTrees.Length).IsEqualTo(1);
        string generatedCode = result.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("OnBindSlots");
        await Assert.That(generatedCode).Contains("_childSlot");
    }

    /// <summary>
    /// 验证 <c>[MviSlot]</c> 特性指定 <c>Observes</c> 时
    /// 生成属性变更订阅代码。
    /// </summary>
    /// <remarks>
    /// 使用生成树检查而非编译验证:同上,
    /// 桩定义无法完整模拟运行时接口。
    /// </remarks>
    [Test]
    public async Task Generate_OnBindSlots_Should_ProducePropertyChangedSubscriptionAsync()
    {
        GeneratorDriverRunResult result =
            GeneratorTestHost.RunGenerator<MviCompositeSlotBindingGenerator>(
                StubDefinitions + "\n" + SlotSourceWithObserves);

        await Assert.That(result.GeneratedTrees.Length).IsEqualTo(1);
        string generatedCode = result.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("PropertyChanged");
        await Assert.That(generatedCode).Contains("SelectedTab");
    }

    /// <summary>
    /// 桩类型定义：模拟 MVI 与 Avalonia 框架的关键类型，让生成器能正常分析。
    /// </summary>
    private const string StubDefinitions = """
        // 桩类型定义：模拟 MVI 框架关键类型，仅供源生成器行为测试使用。

        namespace MiKiNuo.Mvi.Presentation.Slot
        {
            [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
            public sealed class MviSlotAttribute : System.Attribute
            {
                public MviSlotAttribute(System.Type childViewType, string? factory, params string[] observes) { }
            }
        }

        namespace MiKiNuo.Mvi.Platforms.Avalonia.Views
        {
            public abstract class MviAvaloniaView<TViewModel> where TViewModel : class
            {
            }
        }

        namespace MiKiNuo.Mvi.Platforms.Avalonia.Slot
        {
            public sealed class MviSlotHost
            {
                public object? Content { get; set; }
            }
        }

        namespace MiKiNuo.Mvi.Presentation.Disposables
        {
            public sealed class MviDisposableBag
            {
            }
        }

        namespace MiKiNuo.Mvi.Application.DI
        {
            public interface IMviResolver
            {
            }
        }

        namespace MiKiNuo.Mvi.Presentation.ViewRegistry
        {
            public interface IMviViewRegistry
            {
                object CreateView(object viewModel);
            }
        }
        """;

    /// <summary>
    /// 测试源代码：含 <c>[MviSlot]</c> 字段但不观察任何属性的 Avalonia View。
    /// </summary>
    private const string SlotSourceWithoutObserves = """
        namespace TestApp
        {
            public class TestViewModel
            {
                public object? CreateChildViewModel() => new object();
            }

            public partial class TestView : MiKiNuo.Mvi.Platforms.Avalonia.Views.MviAvaloniaView<TestViewModel>
            {
                [MiKiNuo.Mvi.Presentation.Slot.MviSlot(typeof(object), "CreateChildViewModel")]
                public MiKiNuo.Mvi.Platforms.Avalonia.Slot.MviSlotHost? _childSlot;
            }
        }
        """;

    /// <summary>
    /// 测试源代码：含 <c>[MviSlot]</c> 字段并观察 <c>SelectedTab</c> 属性的 Avalonia View。
    /// </summary>
    private const string SlotSourceWithObserves = """
        namespace TestApp
        {
            public class TestViewModel : System.ComponentModel.INotifyPropertyChanged
            {
                public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

                public string? SelectedTab { get; set; }

                public object? CreateChildViewModel() => new object();
            }

            public partial class TestView : MiKiNuo.Mvi.Platforms.Avalonia.Views.MviAvaloniaView<TestViewModel>
            {
                [MiKiNuo.Mvi.Presentation.Slot.MviSlot(typeof(object), "CreateChildViewModel", "SelectedTab")]
                public MiKiNuo.Mvi.Platforms.Avalonia.Slot.MviSlotHost? _childSlot;
            }
        }
        """;
}
