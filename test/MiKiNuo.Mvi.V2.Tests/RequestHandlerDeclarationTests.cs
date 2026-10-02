using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.V2.Tests;

/// <summary>以独立编译消费者验证强类型请求声明、生成工厂与原声明诊断。</summary>
public sealed class RequestHandlerDeclarationTests
{
    /// <summary>修正验证声明后，合法Task/ValueTask别名和nullable结果连同输入、操作与DI工厂可实际执行。</summary>
    /// <param name="handlerName">合法处理器名称，包括与生成适配参数相同的名称。</param>
    /// <returns>公开生成入口编译执行验证任务。</returns>
    [Test]
    [Arguments("Load")]
    [Arguments("message")]
    [Arguments("operation")]
    public async Task GeneratedFactoryAndPortsRunInAnIndependentCompiledConsumer(string handlerName)
    {
        string source = """
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.Extensions.DependencyInjection;
            using MiKiNuo.Mvi;
            using Op = MiKiNuo.Mvi.Operation<Demo.State>;
            using ResultTask = System.Threading.Tasks.Task<string?>;
            namespace Demo;
            public sealed record Message(int Value);
            public sealed record OtherMessage(int Value);
            public sealed record State { [Input] public int Value { get; init; } }
            public sealed class Scoped : IAsyncDisposable {
                public static int Disposed;
                public ValueTask DisposeAsync() { Interlocked.Increment(ref Disposed); return default; }
            }
            public sealed class Shared;
            public sealed partial class Editor : Feature<State> {
                public Scoped Dependency { get; }
                public Shared Common { get; }
                public Editor(Scoped dependency, Shared common) : base(new()) { Dependency = dependency; Common = common; }
                [RequestHandler(Validate=nameof(Valid),Concurrency=OperationConcurrency.Queue,Capacity=2,CancellationPolicy=RequestCancellationPolicy.Propagate)]
                private async ResultTask Load(Op op, Message message) {
                    await op.UpdateAsync((state,value)=>state with {Value=value},message.Value);
                    return message.Value.ToString();
                }
                private static bool Valid(State state, Message message)=>message.Value>0;
                [RequestHandler] private ValueTask<string?> Query(Op op, OtherMessage message)=>ValueTask.FromResult<string?>(null);
                [Operation] private ValueTask<int> Count(Op op)=>ValueTask.FromResult(op.Snapshot.Value);
            }
            public static class Consumer {
                public static async Task<int> Run() {
                    var services = new ServiceCollection();
                    services.AddScoped<Scoped>(); services.AddSingleton<Shared>();
                    await using var provider=services.BuildServiceProvider(new ServiceProviderOptions {ValidateScopes=true});
                    var first=await Editor.CreateAsync(provider); var second=await Editor.CreateAsync(provider);
                    if(ReferenceEquals(first.Dependency,second.Dependency)||!ReferenceEquals(first.Common,second.Common)) throw new Exception("scope ownership");
                    first.SetValue(3);
                    var mediator=new Mediator(); var load=first.CreateLoadPort();var query=first.CreateQueryPort();
                    using var r=mediator.Register(load); using var q=mediator.Register(query);
                    var reply=await mediator.SendAsync(new Message(7),load);
                    if(reply.OperationResult?.Value!="7"||first.Snapshot.State.Value!=7)throw new Exception("typed handler");
                    var rejected=await mediator.SendAsync(new Message(0),load);
                    if(rejected.OperationResult?.Reason!="ValidationFailed")throw new Exception("validate");
                    using var token=new CancellationTokenSource(); token.Cancel();
                    var canceled=await mediator.SendAsync(new Message(8),load,executionCancellationToken:token.Token);
                    if(canceled.OperationResult?.Kind!=OperationResultKind.Canceled)throw new Exception("propagation");
                    if((await mediator.SendAsync(new OtherMessage(1),query)).OperationResult?.Kind!=OperationResultKind.Completed)throw new Exception("nullable result");
                    if((await first.Count()).Value!=7)throw new Exception("operation");
                    await first.Close().Ticket.Released;await second.Close().Ticket.Released;
                    if(Scoped.Disposed!=2)throw new Exception("release");
                    return 7;
                }
            }
            """;
        source = source.Replace(" Load(", " " + handlerName + "(", StringComparison.Ordinal)
            .Replace("CreateLoadPort()", "Create" + handlerName + "Port()", StringComparison.Ordinal);
        CSharpCompilation invalid = GeneratorTestHost.Compilation(source.Replace("Validate=nameof(Valid)", "Validate=\"Missing\"", StringComparison.Ordinal));
        GeneratorDriver driver = GeneratorTestHost.Driver().RunGenerators(invalid);
        await Assert.That(driver.GetRunResult().Diagnostics.Any(diagnostic => diagnostic.Id == "MVI2012")).IsTrue();
        driver = driver.RunGeneratorsAndUpdateCompilation(GeneratorTestHost.Compilation(source), out Compilation compilation, out _);
        GeneratorDriverRunResult result = driver.GetRunResult();
        await Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.ToString()).ToArray()).IsEmpty();
        await Assert.That(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.ToString()).ToArray()).IsEmpty();
        using MemoryStream assembly = new();
        Microsoft.CodeAnalysis.Emit.EmitResult emitted = compilation.Emit(assembly);
        await Assert.That(emitted.Success).IsTrue();
        assembly.Position = 0;
        Assembly loaded = new AssemblyLoadContext("generated-request-consumer", isCollectible: true).LoadFromStream(assembly);
        Task<int> execution = (Task<int>)loaded.GetType("Demo.Consumer")!.GetMethod("Run")!.Invoke(null, null)!;
        await Assert.That(await execution.WaitAsync(TimeSpan.FromSeconds(15))).IsEqualTo(7);
    }

    /// <summary>非法请求签名、验证、契约重复及策略错误定位原RequestHandler声明。</summary>
    /// <param name="method">被验证的处理方法声明。</param>
    /// <param name="extra">补充成员。</param>
    /// <param name="id">期望诊断编号。</param>
    /// <returns>业务声明诊断位置验证任务。</returns>
    [Test]
    [Arguments("[RequestHandler] public ValueTask<int> Load(Operation<State> op,Message m)=>default;", "", "MVI2011")]
    [Arguments("[RequestHandler] private static ValueTask<int> Load(Operation<State> op,Message m)=>default;", "", "MVI2011")]
    [Arguments("[RequestHandler] private ValueTask<int> Load<T>(Operation<State> op,Message m)=>default;", "", "MVI2011")]
    [Arguments("[RequestHandler] private ValueTask<int> Load(ref Operation<State> op,Message m)=>default;", "", "MVI2011")]
    [Arguments("[RequestHandler] private ValueTask<int> Load(Operation<State> op,Message m=null!)=>default;", "", "MVI2011")]
    [Arguments("[RequestHandler] private ValueTask<int> Load(Operation<State> op,params Message[] m)=>default;", "", "MVI2011")]
    [Arguments("[RequestHandler] private ValueTask<int> Load(Operation<Other> op,Message m)=>default;", "", "MVI2011")]
    [Arguments("[RequestHandler] private Fake.ValueTask<int> Load(Operation<State> op,Message m)=>new();", "", "MVI2011")]
    [Arguments("[RequestHandler] private ValueTask<int> Load(Fake.Operation<State> op,Message m)=>default;", "", "MVI2011")]
    [Arguments("[RequestHandler] private ValueTask<int> Load(Operation<State>? op,Message m)=>default;", "", "MVI2011")]
    [Arguments("[RequestHandler] private ValueTask<int> Load(Operation<State> op,Message? m)=>default;", "", "MVI2011")]
    [Arguments("[RequestHandler] private ValueTask<int> Load(Operation<State> op,System.Span<int> m)=>default;", "", "MVI2011")]
    [Arguments("[RequestHandler] private ValueTask<int> Load(Operation<State> op,HiddenMessage m)=>default;", "private sealed record HiddenMessage;", "MVI2011")]
    [Arguments("[RequestHandler] private ValueTask<int> Load(Operation<State> op,InternalMessage m)=>default;", "internal sealed record InternalMessage;", "MVI2011")]
    [Arguments("[RequestHandler] private ValueTask<HiddenResult> Load(Operation<State> op,Message m)=>default;", "private sealed record HiddenResult;", "MVI2011")]
    [Arguments("[RequestHandler] private ValueTask<int> Load(Operation<State> op,System.Collections.Generic.List<HiddenMessage[]> m)=>default;", "private sealed record HiddenMessage;", "MVI2011")]
    [Arguments("[RequestHandler] private ValueTask<int> Load(Operation<State> op,Hidden.Message m)=>default;", "private sealed class Hidden { public sealed record Message; }", "MVI2011")]
    [Arguments("[RequestHandler(Validate=\"Missing\")] private ValueTask<int> Load(Operation<State> op,Message m)=>default;", "", "MVI2012")]
    [Arguments("[RequestHandler(Validate=\"Valid\")] private ValueTask<int> Load(Operation<State> op,Message m)=>default;", "private bool Valid(State state,Message m)=>true;", "MVI2012")]
    [Arguments("[RequestHandler] private ValueTask<int> Load(Operation<State> op,Message m)=>default;", "[RequestHandler] private Task<int> OtherLoad(Operation<State> op,Message m)=>Task.FromResult(1);", "MVI2013")]
    [Arguments("[RequestHandler] private ValueTask<int> Load(Operation<State> op,Message m)=>default;", "public int CreateLoadPort()=>1;", "MVI2014")]
    [Arguments("[RequestHandler] private ValueTask<int> Load(Operation<State> op,Message m)=>default;", "[Operation] private ValueTask<int> CreateLoadPort(Operation<State> op)=>default;", "MVI2014")]
    [Arguments("[RequestHandler(Concurrency=OperationConcurrency.Queue)] private ValueTask<int> Load(Operation<State> op,Message m)=>default;", "", "MVI2015")]
    [Arguments("[RequestHandler(CancellationPolicy=(RequestCancellationPolicy)9)] private ValueTask<int> Load(Operation<State> op,Message m)=>default;", "", "MVI2015")]
    public async Task InvalidRequestDeclarationsLocateOriginalAttribute(string method, string extra, string id)
    {
        string source = "using System.Threading.Tasks;using MiKiNuo.Mvi;public sealed record State;public sealed record Other;public sealed record Message;"
            + "namespace Fake { public sealed class ValueTask<T>; public sealed class Operation<T>; }"
            + "public sealed partial class Editor():Feature<State>(new()){ " + method + extra + " }";
        (Compilation _, GeneratorDriverRunResult result) = GeneratorTestHost.Run(source);
        Diagnostic diagnostic = result.Diagnostics.First(diagnostic => diagnostic.Id == id);
        await Assert.That(diagnostic.Location.IsInSource).IsTrue();
        string span = source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length);
        await Assert.That(span.StartsWith("RequestHandler", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.GeneratedTrees).IsEmpty();
    }

    /// <summary>内部Feature公开成员的有效边界仍为程序集，允许内部请求和结果契约。</summary>
    /// <returns>内部契约公开输出编译验证任务。</returns>
    [Test]
    public async Task InternalFeaturesCanExposeInternalContracts()
    {
        const string source = """
            using System.Threading.Tasks;using MiKiNuo.Mvi;
            public sealed record State;internal sealed record Message;internal sealed record Reply;
            internal sealed partial class Editor:Feature<State> {
                public Editor():base(new()){}
                [RequestHandler] private ValueTask<Reply> Load(Operation<State> op,Message message)=>new(new Reply());
            }
            """;
        (Compilation output, GeneratorDriverRunResult result) = GeneratorTestHost.Run(source);
        await Assert.That(result.Diagnostics).IsEmpty();
        await Assert.That(output.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)).IsEmpty();
        using MemoryStream assembly = new();
        await Assert.That(output.Emit(assembly).Success).IsTrue();
    }

    /// <summary>假同名属性忽略，多Feature同一契约合法，修正错误声明后可正常编译。</summary>
    /// <returns>真实属性符号及局部契约范围验证任务。</returns>
    [Test]
    public async Task FakeAttributesAreIgnoredAndContractsAreLocalToEachFeature()
    {
        string source = """
            using System.Threading.Tasks;using MiKiNuo.Mvi;
            public sealed record State;public sealed record Message;
            namespace Fake { public sealed class RequestHandlerAttribute:System.Attribute; }
            public sealed partial class One():Feature<State>(new()) {
              [Fake.RequestHandler] public int Bad()=>1;
              [RequestHandler] private ValueTask<int> Load(Operation<State> op,Message m)=>ValueTask.FromResult(1);
            }
            public sealed partial class Two():Feature<State>(new()) {
              [RequestHandler] private Task<int> Load(Operation<State> op,Message m)=>Task.FromResult(2);
            }
            """;
        (Compilation output, GeneratorDriverRunResult result) = GeneratorTestHost.Run(source);
        await Assert.That(result.Diagnostics).IsEmpty();
        await Assert.That(output.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)).IsEmpty();
        (Compilation _, GeneratorDriverRunResult invalid) = GeneratorTestHost.Run(source.Replace("private ValueTask<int> Load", "private static ValueTask<int> Load", StringComparison.Ordinal));
        await Assert.That(invalid.Diagnostics.Any(diagnostic => diagnostic.Id == "MVI2011")).IsTrue();
    }

    /// <summary>工厂名冲突与重复marked构造位置可静态确定，但不猜测未注册依赖。</summary>
    /// <returns>工厂静态诊断边界验证任务。</returns>
    [Test]
    public async Task FactoryConflictsAndPreferredConstructorsAreLocatedWithoutGuessingServices()
    {
        const string source = """
            using System.Threading.Tasks;using MiKiNuo.Mvi;using Microsoft.Extensions.DependencyInjection;
            public sealed record State;public sealed class Dependency;
            public sealed partial class Editor:Feature<State> {
              [ActivatorUtilitiesConstructor] public Editor():base(new()){}
              [ActivatorUtilitiesConstructor] public Editor(Dependency dependency):base(new()){}
            }
            """;
        (Compilation _, GeneratorDriverRunResult duplicate) = GeneratorTestHost.Run(source);
        await Assert.That(duplicate.Diagnostics.Count(diagnostic => diagnostic.Id == "MVI2016" && diagnostic.Location.IsInSource)).IsEqualTo(2);
        (Compilation output, GeneratorDriverRunResult valid) = GeneratorTestHost.Run(source.Replace("[ActivatorUtilitiesConstructor] ", "", StringComparison.Ordinal));
        await Assert.That(valid.Diagnostics).IsEmpty();
        await Assert.That(output.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)).IsEmpty();
        (Compilation _, GeneratorDriverRunResult conflict) = GeneratorTestHost.Run(source.Replace("[ActivatorUtilitiesConstructor] ", "", StringComparison.Ordinal)
            .Replace("public Editor():base(new()){}", "public Editor():base(new()){} public static int CreateAsync()=>1;", StringComparison.Ordinal));
        await Assert.That(conflict.Diagnostics.Any(diagnostic => diagnostic.Id == "MVI2016")).IsTrue();
    }
}
