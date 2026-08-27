using Microsoft.CodeAnalysis;
using MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;
using MiKiNuo.Mvi.Tests.TestSupport;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 MviStatePathsGenerator 源生成器的行为测试。
/// 使用 CSharpGeneratorDriver 驱动生成器并验证生成产物。
/// </summary>
public sealed class MviStatePathsGeneratorBehaviorTests
{
    /// <summary>
    /// 验证嵌套 record 状态产出嵌套路径类并可编译。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceNestedPathClassesAsync()
    {
        (GeneratorDriverRunResult runResult, bool emitSuccess) =
            GeneratorTestHost.RunGeneratorAndCompile<MviStatePathsGenerator>(
                StubDefinitions + "\n" + NestedStateSource);

        await Assert.That(emitSuccess).IsTrue();
        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(1);

        string generatedCode = runResult.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("static class DashboardStatePaths");
        await Assert.That(generatedCode).Contains("public static class Machine");
        await Assert.That(generatedCode).Contains("static state => state.Machine.Speed");
        await Assert.That(generatedCode).Contains("\"Machine.Speed\"");
    }

    /// <summary>
    /// 验证集合与可空引用属性作为叶子处理，不展开为分支。
    /// </summary>
    [Test]
    public async Task Generate_Should_TreatCollectionsAndNullableAsLeavesAsync()
    {
        (GeneratorDriverRunResult runResult, bool emitSuccess) =
            GeneratorTestHost.RunGeneratorAndCompile<MviStatePathsGenerator>(
                StubDefinitions + "\n" + LeafBoundaryStateSource);

        await Assert.That(emitSuccess).IsTrue();

        string generatedCode = runResult.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("static state => state.Items");
        await Assert.That(generatedCode).Contains("static state => state.Owner");
        await Assert.That(generatedCode).Contains("static state => state.Machine.Speed");
        await Assert.That(generatedCode.Contains("public static class Items")).IsFalse();
        await Assert.That(generatedCode.Contains("public static class Owner")).IsFalse();
    }

    /// <summary>
    /// 验证状态属性图存在循环引用时报告 MVI0010 诊断。
    /// </summary>
    [Test]
    public async Task Generate_Should_ReportMVI0010_WhenGraphHasCycleAsync()
    {
        GeneratorDriverRunResult runResult =
            GeneratorTestHost.RunGenerator<MviStatePathsGenerator>(
                StubDefinitions + "\n" + CyclicStateSource);

        await Assert.That(runResult.Diagnostics.Any(d => d.Id == "MVI0010")).IsTrue();
    }

    /// <summary>
    /// 验证泛型状态类型跳过生成并报告 MVI0011 诊断。
    /// </summary>
    [Test]
    public async Task Generate_Should_ReportMVI0011_WhenStateIsGenericAsync()
    {
        GeneratorDriverRunResult runResult =
            GeneratorTestHost.RunGenerator<MviStatePathsGenerator>(
                StubDefinitions + "\n" + GenericStateSource);

        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(0);
        await Assert.That(runResult.Diagnostics.Any(d => d.Id == "MVI0011")).IsTrue();
    }

    /// <summary>
    /// 验证未实现 IMviState 的类型不触发生成。
    /// </summary>
    [Test]
    public async Task Generate_Should_NotProduceCode_ForNonStateTypesAsync()
    {
        GeneratorDriverRunResult runResult =
            GeneratorTestHost.RunGenerator<MviStatePathsGenerator>(
                StubDefinitions + "\npublic sealed record PlainData(int Value);");

        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(0);
    }

    /// <summary>
    /// 桩类型定义：共享桩核（MVI 状态契约 + StatePath 运行时）。
    /// </summary>
    private const string StubDefinitions =
        GeneratorTestStubs.StateContracts + "\n" + GeneratorTestStubs.StatePathRuntime;

    /// <summary>
    /// 含嵌套 record 的状态源码。
    /// </summary>
    private const string NestedStateSource = """
        namespace MiKiNuo.Mvi.Tests.Samples
        {
            public sealed record MachineState(double Speed, bool AutoMode);

            public sealed record DashboardState(
                string Title,
                MiKiNuo.Mvi.Tests.Samples.MachineState Machine)
                : MiKiNuo.Mvi.Domain.MVI.State.IMviState;
        }
        """;

    /// <summary>
    /// 含集合与可空引用属性的状态源码。
    /// </summary>
    private const string LeafBoundaryStateSource = """
        namespace MiKiNuo.Mvi.Tests.Samples
        {
            public sealed record MachineState(double Speed);

            public sealed record OwnerState(string Name);

            public sealed record LineState(
                System.Collections.Generic.IReadOnlyList<int> Items,
                MiKiNuo.Mvi.Tests.Samples.OwnerState? Owner,
                MiKiNuo.Mvi.Tests.Samples.MachineState Machine)
                : MiKiNuo.Mvi.Domain.MVI.State.IMviState;
        }
        """;

    /// <summary>
    /// 含循环引用的状态源码。
    /// </summary>
    private const string CyclicStateSource = """
        namespace MiKiNuo.Mvi.Tests.Samples
        {
            public sealed record NodeB(NodeA Child);

            public sealed record NodeA(NodeB Child) : MiKiNuo.Mvi.Domain.MVI.State.IMviState;
        }
        """;

    /// <summary>
    /// 泛型状态源码。
    /// </summary>
    private const string GenericStateSource = """
        namespace MiKiNuo.Mvi.Tests.Samples
        {
            public sealed record GenericState<T>(T Value) : MiKiNuo.Mvi.Domain.MVI.State.IMviState;
        }
        """;
}
