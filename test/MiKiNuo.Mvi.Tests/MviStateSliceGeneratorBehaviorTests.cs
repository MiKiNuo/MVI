using Microsoft.CodeAnalysis;
using MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;
using MiKiNuo.Mvi.Tests.TestSupport;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 MviStateSliceGenerator 源生成器的行为测试。
/// 使用 CSharpGeneratorDriver 驱动生成器并验证生成产物。
/// </summary>
public sealed class MviStateSliceGeneratorBehaviorTests
{
    /// <summary>
    /// 验证切片构造参数解析为状态属性路径并产出可编译代码。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceSlicePathAsync()
    {
        (GeneratorDriverRunResult runResult, bool emitSuccess) =
            GeneratorTestHost.RunGeneratorAndCompile<MviStateSliceGenerator>(
                StubDefinitions + "\n" + StateSource + "\n" + SliceSource);

        await Assert.That(emitSuccess).IsTrue();
        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(1);

        string generatedCode = runResult.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("static class DashboardStateSlices");
        await Assert.That(generatedCode).Contains("MachinePanel");
        await Assert.That(generatedCode).Contains("state.Machine.Speed");
        await Assert.That(generatedCode).Contains("state.Machine.AutoMode");
    }

    /// <summary>
    /// 验证切片构造参数无法解析时报告 MVI0012 诊断。
    /// </summary>
    [Test]
    public async Task Generate_Should_ReportMVI0012_WhenParameterUnresolvedAsync()
    {
        GeneratorDriverRunResult runResult =
            GeneratorTestHost.RunGenerator<MviStateSliceGenerator>(
                StubDefinitions + "\n" + StateSource + "\n" + UnresolvedSliceSource);

        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(0);
        await Assert.That(runResult.Diagnostics.Any(d => d.Id == "MVI0012")).IsTrue();
    }

    /// <summary>
    /// 验证切片构造参数匹配多条路径时报告 MVI0013 诊断。
    /// </summary>
    [Test]
    public async Task Generate_Should_ReportMVI0013_WhenParameterAmbiguousAsync()
    {
        GeneratorDriverRunResult runResult =
            GeneratorTestHost.RunGenerator<MviStateSliceGenerator>(
                StubDefinitions + "\n" + AmbiguousStateSource + "\n" + AmbiguousSliceSource);

        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(0);
        await Assert.That(runResult.Diagnostics.Any(d => d.Id == "MVI0013")).IsTrue();
    }

    /// <summary>
    /// 验证缺少位置参数主构造函数的切片报告 MVI0014 诊断。
    /// </summary>
    [Test]
    public async Task Generate_Should_ReportMVI0014_WhenSliceHasNoPrimaryConstructorAsync()
    {
        GeneratorDriverRunResult runResult =
            GeneratorTestHost.RunGenerator<MviStateSliceGenerator>(
                StubDefinitions + "\n" + StateSource + "\n" + InvalidSliceSource);

        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(0);
        await Assert.That(runResult.Diagnostics.Any(d => d.Id == "MVI0014")).IsTrue();
    }

    /// <summary>
    /// 桩类型定义：共享桩核（MVI 状态契约 + StatePath 运行时 + 切片特性）。
    /// </summary>
    private const string StubDefinitions =
        GeneratorTestStubs.StateContracts + "\n" + GeneratorTestStubs.StatePathRuntime;

    /// <summary>
    /// 含嵌套 record 的状态源码。
    /// </summary>
    private const string StateSource = """
        namespace MiKiNuo.Mvi.Tests.Samples
        {
            public sealed record MachineState(double Speed, bool AutoMode);

            public sealed record TaskState(int Progress);

            public sealed record DashboardState(
                string Title,
                MiKiNuo.Mvi.Tests.Samples.MachineState Machine,
                MiKiNuo.Mvi.Tests.Samples.TaskState Task)
                : MiKiNuo.Mvi.Domain.MVI.State.IMviState;
        }
        """;

    /// <summary>
    /// 正常切片源码。
    /// </summary>
    private const string SliceSource = """
        namespace MiKiNuo.Mvi.Tests.Samples
        {
            [MiKiNuo.Mvi.Domain.MVI.State.MviStateSlice(typeof(DashboardState))]
            public sealed record MachinePanelState(double Speed, bool AutoMode);
        }
        """;

    /// <summary>
    /// 含无法解析参数的切片源码。
    /// </summary>
    private const string UnresolvedSliceSource = """
        namespace MiKiNuo.Mvi.Tests.Samples
        {
            [MiKiNuo.Mvi.Domain.MVI.State.MviStateSlice(typeof(DashboardState))]
            public sealed record BrokenPanelState(double NotExist);
        }
        """;

    /// <summary>
    /// 含同名叶子的状态源码。
    /// </summary>
    private const string AmbiguousStateSource = """
        namespace MiKiNuo.Mvi.Tests.Samples
        {
            public sealed record MachineState(double Speed);

            public sealed record TaskState(double Speed);

            public sealed record AmbiguousState(
                MiKiNuo.Mvi.Tests.Samples.MachineState Machine,
                MiKiNuo.Mvi.Tests.Samples.TaskState Task)
                : MiKiNuo.Mvi.Domain.MVI.State.IMviState;
        }
        """;

    /// <summary>
    /// 歧义切片源码。
    /// </summary>
    private const string AmbiguousSliceSource = """
        namespace MiKiNuo.Mvi.Tests.Samples
        {
            [MiKiNuo.Mvi.Domain.MVI.State.MviStateSlice(typeof(AmbiguousState))]
            public sealed record SpeedPanelState(double Speed);
        }
        """;

    /// <summary>
    /// 无位置参数主构造函数的切片源码。
    /// </summary>
    private const string InvalidSliceSource = """
        namespace MiKiNuo.Mvi.Tests.Samples
        {
            [MiKiNuo.Mvi.Domain.MVI.State.MviStateSlice(typeof(DashboardState))]
            public sealed record EmptyPanelState
            {
                public double Speed { get; init; }
            }
        }
        """;
}
