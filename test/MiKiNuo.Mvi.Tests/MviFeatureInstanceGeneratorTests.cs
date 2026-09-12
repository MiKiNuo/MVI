using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 验证生成实例工厂的运行时隔离及旧解析兼容。
/// </summary>
public sealed class MviFeatureInstanceGeneratorTests
{
    /// <summary>
    /// 验证同类型实例独立且旧容器解析仍复用同一对象。
    /// </summary>
    [Test]
    public async Task GeneratedFactory_Should_IsolateInstancesAsync()
    {
        string feature = MviFeatureContainerGeneratorTests.FeatureSource
            .Replace("MviReduceResult.State<TestState, TestEffect>(state)", "MviReduceResult.State<TestState, TestEffect>(state with { Count = state.Count + 1 })")
            .Replace("protected override void ApplyStateCore(TestState state)", "public int Count => Store.CurrentState.Count; public System.Threading.Tasks.ValueTask Increment() => Store.DispatchAsync(new TestIntent.Increment()); protected override void ApplyStateCore(TestState state)");
        string source = feature + """

            public static class InstanceProbe
            {
                public static async System.Threading.Tasks.Task<bool> Run()
                {
                    using MiKiNuo.Mvi.Application.MVI.Mediator.MviCompositionScope scope = new();
                    MviGeneratorTestAssembly.Composition.GeneratedMviContainer container = new();
                    await using MiKiNuo.Mvi.Application.MVI.Composition.MviFeatureInstance<FeatureTest.TestViewModel> first =
                        await container.CreateTestInstanceAsync(scope.CreateEndpoint(System.Guid.NewGuid()));
                    await using MiKiNuo.Mvi.Application.MVI.Composition.MviFeatureInstance<FeatureTest.TestViewModel> second =
                        await container.CreateTestInstanceAsync(scope.CreateEndpoint(System.Guid.NewGuid()), new FeatureTest.TestState(40));
                    await first.ViewModel.Increment();
                    return first.ViewModel.Count == 1 && second.ViewModel.Count == 40
                        && first.Id != second.Id
                        && !object.ReferenceEquals(first.ViewModel, second.ViewModel)
                        && object.ReferenceEquals(container.Resolve<FeatureTest.TestViewModel>(), container.Resolve<FeatureTest.TestViewModel>());
                }
            }
            """;
        await Assert.That(await RunProbeAsync(source)).IsTrue();
    }

    /// <summary>实例工厂拒绝通过应用单例间接捕获另一份 Store。</summary>
    [Test]
    public async Task InstanceRejectsSingletonCapturingStoreAsync()
    {
        string source = MviFeatureContainerGeneratorTests.FeatureSource.Replace(
            "IMviUiDispatcher? uiDispatcher = null)", "SingletonCapture capture, IMviUiDispatcher? uiDispatcher = null)") + """
            namespace FeatureTest
            {
                [MiKiNuo.Mvi.Domain.DI.DiService(MiKiNuo.Mvi.Domain.DI.ServiceLifetime.Singleton)]
                public sealed class SingletonCapture
                {
                    public SingletonCapture(MiKiNuo.Mvi.Application.MVI.Store.IMviStore<TestState, TestIntent, TestEffect> store) { }
                }
            }
            public static class InstanceProbe
            {
                public static async System.Threading.Tasks.Task<bool> Run()
                {
                    using MiKiNuo.Mvi.Application.MVI.Mediator.MviCompositionScope scope = new();
                    MviGeneratorTestAssembly.Composition.GeneratedMviContainer container = new();
                    try
                    {
                        await using MiKiNuo.Mvi.Application.MVI.Composition.MviFeatureInstance<FeatureTest.TestViewModel> instance =
                            await container.CreateTestInstanceAsync(scope.CreateEndpoint(System.Guid.NewGuid()));
                        return false;
                    }
                    catch (System.InvalidOperationException error) { return error.Message.Contains("单例"); }
                }
            }
            """;
        await Assert.That(await RunProbeAsync(source)).IsTrue();
    }

    /// <summary>编译生成对象图并执行公开验收入口。</summary>
    /// <param name="source">完整测试源码。</param>
    /// <returns>运行时验收结果。</returns>
    private static async Task<bool> RunProbeAsync(string source)
    {
        CSharpCompilation compilation = GeneratorTestHost.CreateCompilation(source,
            MviFeatureContainerGeneratorTests.GetFrameworkReferences());
        GeneratorDriverRunResult result = CSharpGeneratorDriver.Create(new MviDiContainerGenerator())
            .RunGenerators(compilation).GetRunResult();
        CSharpParseOptions options = new(LanguageVersion.Preview);
        compilation = compilation.AddSyntaxTrees(result.GeneratedTrees.Select(tree =>
            CSharpSyntaxTree.ParseText(tree.GetText(), options)));
        using MemoryStream stream = new();
        Microsoft.CodeAnalysis.Emit.EmitResult emitted = compilation.Emit(stream);
        await Assert.That(emitted.Success).IsTrue().Because(string.Join("\n", emitted.Diagnostics));
        Assembly assembly = System.Reflection.Assembly.Load(stream.ToArray());
        MethodInfo method = assembly.GetType("InstanceProbe")!.GetMethod("Run")!;
        return await (Task<bool>)method.Invoke(null, null)!;
    }
}
