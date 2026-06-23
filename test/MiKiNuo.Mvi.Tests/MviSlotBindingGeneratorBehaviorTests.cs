using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
    /// 验证含 <c>[MviSlot]</c> 字段的 Avalonia View 触发生成器产出槽位挂载代码。
    /// </summary>
    [Test]
    public async Task Generate_OnBindSlots_Should_ProduceSlotMountingCodeAsync()
    {
        IIncrementalGenerator generator = CreateSlotBindingGenerator();
        CSharpCompilation compilation = CreateTestCompilation(SlotSourceWithoutObserves);
        GeneratorDriverRunResult result = RunGenerator(generator, compilation);

        await Assert.That(result.GeneratedTrees.Length).IsGreaterThan(0);

        string generatedCode = result.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("OnBindSlots");
        await Assert.That(generatedCode).Contains("_childSlot");
        await Assert.That(generatedCode).Contains(".Content = view");
    }

    /// <summary>
    /// 验证 <c>[MviSlot]</c> 特性指定 <c>Observes</c> 时生成代码包含属性变更订阅。
    /// </summary>
    [Test]
    public async Task Generate_OnBindSlots_Should_SubscribePropertyChangedForObservedPropertiesAsync()
    {
        IIncrementalGenerator generator = CreateSlotBindingGenerator();
        CSharpCompilation compilation = CreateTestCompilation(SlotSourceWithObserves);
        GeneratorDriverRunResult result = RunGenerator(generator, compilation);

        await Assert.That(result.GeneratedTrees.Length).IsGreaterThan(0);

        string generatedCode = result.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("PropertyChanged");
        await Assert.That(generatedCode).Contains("SelectedTab");
    }

    /// <summary>
    /// 驱动生成器并返回运行结果。
    /// </summary>
    /// <param name="generator">增量源生成器实例。</param>
    /// <param name="compilation">测试用编译对象。</param>
    /// <returns>生成器运行结果。</returns>
    private static GeneratorDriverRunResult RunGenerator(IIncrementalGenerator generator, CSharpCompilation compilation)
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        return driver.RunGenerators(compilation).GetRunResult();
    }

    /// <summary>
    /// 加载 Infrastructure 程序集并创建槽位绑定生成器实例。
    /// </summary>
    private static IIncrementalGenerator CreateSlotBindingGenerator()
    {
        string root = FindRepositoryRoot();
        string infrastructurePath = Path.Combine(
            root,
            "src",
            "MiKiNuo.Mvi.Infrastructure",
            "bin",
            "Release",
            "netstandard2.0",
            "MiKiNuo.Mvi.Infrastructure.dll");

        if (!File.Exists(infrastructurePath))
        {
            infrastructurePath = Path.Combine(
                root,
                "src",
                "MiKiNuo.Mvi.Infrastructure",
                "bin",
                "Debug",
                "netstandard2.0",
                "MiKiNuo.Mvi.Infrastructure.dll");
        }

        System.Reflection.Assembly assembly = System.Reflection.Assembly.LoadFrom(infrastructurePath);
        Type generatorType = assembly.GetType(
            "MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration.MviCompositeSlotBindingGenerator")
            ?? throw new InvalidOperationException("未找到槽位绑定生成器类型。");

        object? instance = Activator.CreateInstance(generatorType);
        return (IIncrementalGenerator)(instance ?? throw new InvalidOperationException("无法创建生成器实例。"));
    }

    /// <summary>
    /// 创建包含桩类型与测试 View 的 <c>CSharpCompilation</c>。
    /// </summary>
    private static CSharpCompilation CreateTestCompilation(string testSource)
    {
        string fullSource = StubDefinitions + "\n" + testSource;

        CSharpParseOptions parseOptions = new(LanguageVersion.Preview);
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(fullSource, parseOptions);

        List<MetadataReference> references = new()
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(EventArgs).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.ComponentModel.INotifyPropertyChanged).Assembly.Location),
        };

        // 补充 System.Runtime 中的核心程序集引用
        System.Reflection.Assembly coreAssembly = typeof(object).Assembly;
        string? coreDir = Path.GetDirectoryName(coreAssembly.Location);
        if (coreDir is not null)
        {
            string[] runtimeAssemblies = new[]
            {
                "System.Runtime.dll",
                "System.ComponentModel.Primitives.dll",
                "System.Collections.dll",
                "System.Linq.dll",
            };

            foreach (string dllName in runtimeAssemblies)
            {
                string path = Path.Combine(coreDir, dllName);
                if (File.Exists(path))
                {
                    references.Add(MetadataReference.CreateFromFile(path));
                }
            }
        }

        CSharpCompilationOptions options = new(
            OutputKind.DynamicallyLinkedLibrary,
            nullableContextOptions: NullableContextOptions.Enable);

        return CSharpCompilation.Create(
            "MviSlotBindingBehaviorTestAssembly",
            new[] { syntaxTree },
            references,
            options);
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
