using Microsoft.CodeAnalysis;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.V2.Tests;

/// <summary>验证操作声明生成强类型入口并精确诊断非法声明。</summary>
public sealed class OperationDeclarationTests
{
    /// <summary>验证两种异步返回类型均生成可编译的取消入口和强类型结果。</summary>
    /// <returns>表示普通消费者编译验证完成的任务。</returns>
    [Test]
    public async Task ConsumerCompilesValueTaskAndTaskOperationsWithStrongResults()
    {
        (Compilation compilation, GeneratorDriverRunResult result) = GeneratorTestHost.Run("""
            using System.Threading;
            using System.Threading.Tasks;
            using MiKiNuo.Mvi;
            namespace Demo;
            public sealed record State { [Input] public string Name { get; init; } = ""; }
            public sealed partial class Editor() : Feature<State>(new()) {
                [Operation(Validate = nameof(CanSubmit), Concurrency = OperationConcurrency.Latest)]
                private ValueTask<int> SubmitAsync(Operation<State> operation) => ValueTask.FromResult(7);
                [Operation(Concurrency = OperationConcurrency.Queue, Capacity = 3)]
                private Task<string?> RefreshAsync(Operation<State> operation) => Task.FromResult<string?>(null);
                private static bool CanSubmit(State state) => state.Name.Length != 0;
            }
            public static class Program {
                public static async Task Run(CancellationToken token) {
                    Editor editor = new();
                    editor.SetName("ready");
                    OperationResult<int> value = await editor.SubmitAsync(token);
                    OperationResult<string?> optional = await editor.RefreshAsync();
                }
            }
            """);
        await Assert.That(result.Diagnostics.IsEmpty).IsTrue();
        await Assert.That(compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(static diagnostic => diagnostic.ToString()).ToArray()).IsEmpty();
        await Assert.That(result.GeneratedTrees.Single().FilePath.EndsWith("Demo.Editor.Inputs.g.cs", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>验证非法操作和验证方法在业务原声明处产生框架诊断。</summary>
    /// <param name="operation">需要验证的操作方法。</param>
    /// <param name="members">附加验证方法或冲突成员。</param>
    /// <param name="id">期望的框架诊断编号。</param>
    /// <returns>表示声明错误定位验证完成的任务。</returns>
    [Test]
    [Arguments("[Operation] public ValueTask<int> SubmitAsync(Operation<State> operation) => default;", "", "MVI2007")]
    [Arguments("[Operation] private static ValueTask<int> SubmitAsync(Operation<State> operation) => default;", "", "MVI2007")]
    [Arguments("[Operation] private ValueTask SubmitAsync(Operation<State> operation) => default;", "", "MVI2007")]
    [Arguments("[Operation] private ValueTask<int> SubmitAsync<T>(Operation<State> operation) => default;", "", "MVI2007")]
    [Arguments("[Operation] private ValueTask<int> SubmitAsync(Operation<Other> operation) => default;", "", "MVI2007")]
    [Arguments("[Operation] private ValueTask<int> SubmitAsync(ref Operation<State> operation) => default;", "", "MVI2007")]
    [Arguments("[Operation] private ValueTask<int> SubmitAsync(Operation<State> operation = null!) => default;", "", "MVI2007")]
    [Arguments("[Operation] private Fake.ValueTask<int> SubmitAsync(Operation<State> operation) => new();", "", "MVI2007")]
    [Arguments("[Operation(Validate = \"Missing\")] private ValueTask<int> SubmitAsync(Operation<State> operation) => default;", "", "MVI2008")]
    [Arguments("[Operation(Validate = \"CanSubmit\")] private ValueTask<int> SubmitAsync(Operation<State> operation) => default;", "private bool CanSubmit(State state) => true;", "MVI2008")]
    [Arguments("[Operation(Validate = \"CanSubmit\")] private ValueTask<int> SubmitAsync(Operation<State> operation) => default;", "private static int CanSubmit(State state) => 1;", "MVI2008")]
    [Arguments("[Operation(Validate = \"CanSubmit\")] private ValueTask<int> SubmitAsync(Operation<State> operation) => default;", "private static bool CanSubmit(Other state) => true;", "MVI2008")]
    [Arguments("[Operation(Validate = \"CanSubmit\")] private ValueTask<int> SubmitAsync(Operation<State> operation) => default;", "private static bool CanSubmit(State state = null!) => true;", "MVI2008")]
    [Arguments("[Operation] private ValueTask<int> SubmitAsync(Operation<State> operation) => default;", "public Task<int> SubmitAsync(System.Threading.CancellationToken token) => Task.FromResult(1);", "MVI2009")]
    [Arguments("[Operation] private ValueTask<int> SetName(Operation<State> operation) => default;", "", "MVI2009")]
    [Arguments("[Operation] private ValueTask<int> Snapshot(Operation<State> operation) => default;", "", "MVI2009")]
    [Arguments("[Operation(Concurrency = OperationConcurrency.Queue)] private ValueTask<int> SubmitAsync(Operation<State> operation) => default;", "", "MVI2010")]
    [Arguments("[Operation(Concurrency = OperationConcurrency.Queue, Capacity = 0)] private ValueTask<int> SubmitAsync(Operation<State> operation) => default;", "", "MVI2010")]
    [Arguments("[Operation(Concurrency = OperationConcurrency.Queue, Capacity = -1)] private ValueTask<int> SubmitAsync(Operation<State> operation) => default;", "", "MVI2010")]
    [Arguments("[Operation(Capacity = 1)] private ValueTask<int> SubmitAsync(Operation<State> operation) => default;", "", "MVI2010")]
    [Arguments("[Operation(Concurrency = (OperationConcurrency)99, Capacity = 1)] private ValueTask<int> SubmitAsync(Operation<State> operation) => default;", "", "MVI2010")]
    [Arguments("[Operation(Concurrency = (OperationConcurrency)99)] private ValueTask<int> SubmitAsync(Operation<State> operation) => default;", "", "MVI2010")]
    public async Task InvalidDeclarationsReceiveLocatedDiagnostics(string operation, string members, string id)
    {
        (Compilation _, GeneratorDriverRunResult result) = GeneratorTestHost.Run("using System.Threading.Tasks; using MiKiNuo.Mvi; "
            + "public sealed record State { [Input] public string Name { get; init; } = \"\"; } public sealed record Other; "
            + "namespace Fake { public sealed class ValueTask<T>; } "
            + "public sealed partial class Editor() : Feature<State>(new()) { " + operation + members + " }");
        await Assert.That(result.Diagnostics.Any(diagnostic => diagnostic.Id == id && diagnostic.Location.IsInSource)).IsTrue();
        await Assert.That(result.GeneratedTrees.Length).IsEqualTo(0);
    }
}
