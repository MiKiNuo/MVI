using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using MiKiNuo.Mvi.Generators.Analyzers;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 验证微软编码规范分析器对异步方法命名的判定。
/// </summary>
public sealed class MicrosoftCodingAnalyzerTests
{
    /// <summary>
    /// 验证返回 ValueTask 的受保护释放扩展点同样必须以 Async 结尾。
    /// </summary>
    [Test]
    public async Task DisposeCoreAsyncShouldNotReportMemberNamingAsync()
    {
        const string source = """
            public class AsyncOwner : System.IAsyncDisposable
            {
                public async System.Threading.Tasks.ValueTask DisposeAsync()
                {
                    await DisposeCoreAsync();
                    System.GC.SuppressFinalize(this);
                }

                protected virtual System.Threading.Tasks.ValueTask DisposeCoreAsync()
                {
                    return System.Threading.Tasks.ValueTask.CompletedTask;
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source);

        await Assert.That(diagnostics.Any(diagnostic => diagnostic.Id == "CODE0002")).IsFalse();
    }

    /// <summary>
    /// 验证 DisposeAsyncCore 虽属于常见命名，但不符合本项目“异步方法以 Async 结尾”的规则。
    /// </summary>
    [Test]
    public async Task DisposeAsyncCoreShouldReportMemberNamingAsync()
    {
        const string source = """
            public class AsyncOwner
            {
                protected virtual System.Threading.Tasks.ValueTask DisposeAsyncCore()
                {
                    return System.Threading.Tasks.ValueTask.CompletedTask;
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source);

        await Assert.That(diagnostics.Any(diagnostic => diagnostic.Id == "CODE0002")).IsTrue();
    }

    /// <summary>
    /// 验证普通返回 ValueTask 且未以 Async 结尾的方法仍会产生 CODE0002。
    /// </summary>
    [Test]
    public async Task OrdinaryValueTaskMethodWithoutAsyncSuffixShouldReportMemberNamingAsync()
    {
        const string source = """
            public class Worker
            {
                public System.Threading.Tasks.ValueTask Execute()
                {
                    return System.Threading.Tasks.ValueTask.CompletedTask;
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source);

        await Assert.That(diagnostics.Any(diagnostic => diagnostic.Id == "CODE0002")).IsTrue();
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source)
    {
        Compilation compilation = GeneratorTestHost.CreateCompilation(source);
        CompilationWithAnalyzers analysis = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new MiKiNuoMicrosoftCodingAnalyzer()));

        return await analysis.GetAnalyzerDiagnosticsAsync();
    }
}
