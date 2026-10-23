using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MiKiNuo.Mvi.Abstractions.DI;
using MiKiNuo.Mvi.Binding.Disposables;
using MiKiNuo.Mvi.Runtime.DI;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>通过真实平台源码和轻量引擎边界替身验证绑定及调度行为，不替代原生引擎集成测试。</summary>
public sealed class GodotLifecycleRegressionTests
{
    /// <summary>按钮首次刷新失败后，绑定入口清理命令和按钮的订阅。</summary>
    [Test]
    public void BindButton_Should_UnsubscribeWhenInitialRefreshFails()
    {
        RunScenario("""
            var button = new Godot.Button();
            var command = new ProbeCommand();
            var view = new ButtonView { Button = button };
            Exception? observed = null;
            try { view.Bind(command, new UnusedResolver()); }
            catch (Exception exception) { observed = exception; }
            Require(ReferenceEquals(observed, command.Failure), "必须保留首次刷新异常");
            Require(view.ViewModel is null, "失败绑定不能保留 ViewModel");
            command.Fail = false;
            command.Notify();
            button.EmitPressed();
            Require(command.Queries == 1, "失败绑定不能继续响应命令或按钮事件");
            Require(command.Executions == 0, "失败绑定不能执行命令");
            view.Bind(command, new UnusedResolver());
            button.EmitPressed();
            Require(command.Executions == 1, "清理后应能重新绑定");
            view.Unbind();
            int queries = command.Queries;
            command.Notify();
            button.EmitPressed();
            Require(command.Queries == queries, "解绑后不应继续刷新");
            """);
    }

    /// <summary>跨线程投递与显式排空均等待主线程，离树和重新挂载不丢失动作。</summary>
    [Test]
    public void Dispatcher_Should_KeepActionsOnSceneThreadAcrossReattachment()
    {
        RunScenario("""
            var dispatcher = new GodotMviUiDispatcher();
            int owner = Environment.CurrentManagedThreadId;
            var observed = new List<int>();
            void Record(int value)
            {
                Require(Environment.CurrentManagedThreadId == owner, "动作必须在主线程执行");
                observed.Add(value);
            }
            RunOnWorker(() => dispatcher.Post(() => Record(1))).GetAwaiter().GetResult();
            dispatcher.DrainPostedActions();
            Require(observed.Count == 0, "入树前不能执行动作");
            dispatcher.Attach();
            Require(observed.SequenceEqual(new[] { 1 }), "首次就绪应排空待执行动作");
            RunOnWorker(() =>
            {
                dispatcher.Post(() => Record(2));
                dispatcher.DrainPostedActions();
            }).GetAwaiter().GetResult();
            Require(observed.Count == 1, "后台线程不得排空队列");
            dispatcher.Tick();
            Require(observed.SequenceEqual(new[] { 1, 2 }), "下一帧执行后台投递");
            dispatcher.Detach();
            RunOnWorker(() => dispatcher.Post(() => Record(3))).GetAwaiter().GetResult();
            dispatcher.DrainPostedActions();
            Require(observed.Count == 2, "离树后不能执行动作");
            dispatcher.Attach();
            dispatcher.Tick();
            Require(observed.SequenceEqual(new[] { 1, 2, 3 }), "重新挂载不能依赖再次 Ready");
            """);
    }

    /// <summary>排空期间离树保留剩余动作，单个动作异常不阻止后续执行。</summary>
    [Test]
    public void Dispatcher_Should_PreservePendingActionsWhenAnActionDetachesNode()
    {
        RunScenario("""
            var dispatcher = new GodotMviUiDispatcher();
            dispatcher.Attach();
            var observed = new List<int>();
            dispatcher.Post(() => { observed.Add(1); dispatcher.Detach(); });
            dispatcher.Post(() => throw new InvalidOperationException("动作失败"));
            dispatcher.Post(() => observed.Add(2));
            dispatcher.Tick();
            Require(observed.SequenceEqual(new[] { 1 }), "离树时停止排空");
            dispatcher.Attach();
            dispatcher.Tick();
            Require(observed.SequenceEqual(new[] { 1, 2 }), "重新挂载继续执行队列");
            Require(Godot.GD.Errors.Count == 1, "记录失败且继续执行后续动作");
            """);
    }

    private static void RunScenario(string body)
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

        string platform = Path.Combine(directory.FullName, "src", "MiKiNuo.Mvi.Platforms.Godot");
        string[] files =
        [
            "Binding/GodotMviControlView.cs", "Binding/IMviGodotBindable.cs",
            "Binding/IMviGodotViewBinding.cs", "Threading/GodotMviUiDispatcher.cs",
        ];
        List<SyntaxTree> trees = files.Select(file =>
            CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(platform, file)))).ToList<SyntaxTree>();
        trees.Add(CSharpSyntaxTree.ParseText(EngineBoundary));
        trees.Add(CSharpSyntaxTree.ParseText("""
            using MiKiNuo.Mvi.Platforms.Godot.Threading;
            /// <summary>运行公开入口场景。</summary>
            public static class Scenario
            {
                /// <summary>执行回归断言。</summary>
                public static void Run()
                {
            """ + body + """
                }
                private static void Require(bool condition, string message)
                {
                    if (!condition) throw new InvalidOperationException(message);
                }
                private static Task RunOnWorker(Action action)
                    => Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
            """));

        string[] runtime = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        IEnumerable<string> references = runtime.Where(path =>
                Path.GetFileName(path).StartsWith("System.", StringComparison.Ordinal) ||
                Path.GetFileName(path) is "mscorlib.dll" or "netstandard.dll")
            .Concat([typeof(DiServiceAttribute).Assembly.Location, typeof(MviDisposableBag).Assembly.Location,
                typeof(IMviResolver).Assembly.Location]);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "GodotBoundary_" + Guid.NewGuid().ToString("N"), trees,
            references.Distinct().Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        using MemoryStream image = new();
        var result = compilation.Emit(image);
        if (!result.Success)
            throw new InvalidOperationException(string.Join(Environment.NewLine, result.Diagnostics));
        image.Position = 0;
        AssemblyLoadContext context = new("GodotBoundary", isCollectible: true);
        try
        {
            Assembly assembly = context.LoadFromStream(image);
            Action run = assembly.GetType("Scenario")!.GetMethod("Run")!.CreateDelegate<Action>();
            run();
        }
        finally { context.Unload(); }
    }

    // 仅替换外部 Godot 边界；MVI 调度、绑定和 DisposableBag 使用真实实现。
    private const string EngineBoundary = """
        global using System;
        global using System.Collections.Generic;
        global using System.Linq;
        global using System.Threading;
        global using System.Threading.Tasks;
        using MiKiNuo.Mvi.Binding.Command;
        using MiKiNuo.Mvi.Binding.Disposables;
        using MiKiNuo.Mvi.Platforms.Godot.Binding;
        using MiKiNuo.Mvi.Runtime.DI;

        namespace Godot
        {
            /// <summary>模拟主线程场景树边界。</summary>
            public class Node
            {
                private readonly int _owner = Environment.CurrentManagedThreadId;
                private bool _inside;
                private bool _ready;
                private bool _process;
                /// <summary>处理线程组。</summary>
                public enum ProcessThreadGroupEnum { Inherit, MainThread, SubThread }
                /// <summary>节点处理线程组。</summary>
                public ProcessThreadGroupEnum ProcessThreadGroup { get; set; }
                /// <summary>节点名称。</summary>
                public string Name { get; set; } = "Probe";
                /// <summary>进入场景树。</summary>
                public virtual void _EnterTree() { }
                /// <summary>首次就绪。</summary>
                public virtual void _Ready() { }
                /// <summary>离开场景树。</summary>
                public virtual void _ExitTree() { }
                /// <summary>处理一帧。</summary>
                public virtual void _Process(double delta) { }
                /// <summary>查询树状态。</summary>
                public bool IsInsideTree() { CheckThread(); return _inside; }
                /// <summary>设置处理状态。</summary>
                public void SetProcess(bool enabled) { CheckThread(); _process = enabled; }
                /// <summary>模拟挂载，重新入树不重复就绪。</summary>
                public void Attach()
                {
                    CheckThread(); _inside = true; _EnterTree();
                    if (!_ready) { _ready = true; _Ready(); }
                }
                /// <summary>模拟离树，退出回调期间原生节点仍在树中。</summary>
                public void Detach() { CheckThread(); _ExitTree(); _inside = false; }
                /// <summary>由主线程处理一帧。</summary>
                public void Tick() { CheckThread(); if (_inside && _process) _Process(0); }
                private void CheckThread()
                {
                    if (_owner != Environment.CurrentManagedThreadId)
                        throw new InvalidOperationException("后台线程访问了 Godot 节点");
                }
            }
            /// <summary>控件边界。</summary>
            public class Control : Node { }
            /// <summary>按钮边界。</summary>
            public class Button : Control
            {
                /// <summary>按钮禁用状态。</summary>
                public bool Disabled { get; set; }
                /// <summary>点击事件。</summary>
                public event Action? Pressed;
                /// <summary>模拟点击。</summary>
                public void EmitPressed() => Pressed?.Invoke();
            }
            /// <summary>引擎错误输出边界。</summary>
            public static class GD
            {
                /// <summary>捕获输出的错误。</summary>
                public static List<string> Errors { get; } = new();
                /// <summary>记录动作故障。</summary>
                public static void PushError(string message) => Errors.Add(message);
            }
        }
        /// <summary>按钮绑定视图。</summary>
        public sealed class ButtonView : GodotMviControlView<ProbeCommand>
        {
            /// <summary>目标按钮。</summary>
            public Godot.Button Button { get; set; } = null!;
            /// <summary>绑定参数工厂。</summary>
            public Func<object?>? ParameterFactory { get; set; }
            /// <summary>绑定按钮。</summary>
            protected override void OnBind(ProbeCommand viewModel, MviDisposableBag bindings)
                => BindButton(Button, viewModel, bindings, ParameterFactory);
        }
        /// <summary>可触发首次刷新异常的命令。</summary>
        public sealed class ProbeCommand : IMviCommand
        {
            /// <summary>首次刷新异常。</summary>
            public Exception Failure { get; } = new InvalidOperationException("首次刷新失败");
            /// <summary>是否抛出刷新异常。</summary>
            public bool Fail { get; set; } = true;
            /// <summary>可执行性查询次数。</summary>
            public int Queries { get; private set; }
            /// <summary>执行次数。</summary>
            public int Executions { get; private set; }
            /// <summary>可执行性变化事件。</summary>
            public event EventHandler? CanExecuteChanged;
            /// <summary>查询是否可以执行。</summary>
            public bool CanExecute(object? parameter) { Queries++; if (Fail) throw Failure; return true; }
            /// <summary>执行命令。</summary>
            public void Execute(object? parameter) => Executions++;
            /// <summary>发送可执行性变化通知。</summary>
            public void Notify() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>本场景不使用的解析器。</summary>
        public sealed class UnusedResolver : IMviResolver
        {
            /// <summary>不解析服务。</summary>
            public TService Resolve<TService>() where TService : notnull => throw new NotSupportedException();
            /// <summary>不构造服务。</summary>
            public TService CreateWith<TService>(params object[] args) where TService : notnull => throw new NotSupportedException();
            /// <summary>不解析服务。</summary>
            public object Resolve(Type type) => throw new NotSupportedException();
            /// <summary>不创建作用域。</summary>
            public IMviScope CreateScope() => throw new NotSupportedException();
        }
        """;
}
