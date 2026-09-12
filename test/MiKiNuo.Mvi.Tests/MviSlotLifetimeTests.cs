using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 验证组合设计第五、七节的槽位实例稳定性，完整编译并执行生成代码。
/// </summary>
public sealed class MviSlotLifetimeTests
{
    /// <summary>首次创建失败后父视图模型不保留订阅。</summary>
    [Test]
    public async Task FailedInitialMountDoesNotLeakSubscriptionAsync()
    {
        await Assert.That(Run("FailedInitialMount")).IsTrue();
    }

    /// <summary>延迟调度下旧视图清理先于重新挂载。</summary>
    [Test]
    public async Task DeferredRemountClearsBeforeCreatingAsync()
    {
        await Assert.That(Run("DeferredRemount")).IsTrue();
    }
    /// <summary>相同实例通知保留视图。</summary>
    [Test]
    public async Task SameInstanceKeepsViewAsync()
    {
        await Assert.That(Run("SameInstance")).IsTrue();
    }

    /// <summary>实例替换先解除旧视图绑定，解绑后不再响应通知。</summary>
    [Test]
    public async Task ReplacementUnbindsOldViewAsync()
    {
        await Assert.That(Run("Replacement")).IsTrue();
    }

    /// <summary>Godot 替换仅移除自身节点，并在创建新视图之前解绑旧视图。</summary>
    [Test]
    public async Task GodotReplacementPreservesOtherNodesAsync()
    {
        await Assert.That(Run("GodotReplacement")).IsTrue();
    }

    /// <summary>编译完整生成代码并运行公开场景。</summary>
    /// <param name="scenario">场景名称。</param>
    /// <returns>场景验收结果。</returns>
    private static bool Run(string scenario)
    {
        CSharpCompilation compilation = GeneratorTestHost.CreateCompilation(Source);
        GeneratorDriverRunResult result = CSharpGeneratorDriver.Create(new MviCompositeSlotBindingGenerator()).RunGenerators(compilation).GetRunResult();
        foreach (SyntaxTree tree in result.GeneratedTrees)
        {
            compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(tree.GetText(), new CSharpParseOptions(LanguageVersion.Preview)));
        }

        using MemoryStream stream = new();
        EmitResult emitted = compilation.Emit(stream);
        if (!emitted.Success)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, emitted.Diagnostics));
        }

        Assembly assembly = System.Reflection.Assembly.Load(stream.ToArray());
        return (bool)assembly.GetType("Fixture.Scenarios")!.GetMethod(scenario)!.Invoke(null, null)!;
    }

    /// <summary>以无引擎平台边界运行生成代码的完整夹具。</summary>
    private const string Source = """
        using System;
        using System.Collections.Generic;
        using System.ComponentModel;
        using MiKiNuo.Mvi.Application.DI;
        using MiKiNuo.Mvi.Presentation.Disposables;
        using MiKiNuo.Mvi.Presentation.ViewRegistry;
        namespace MiKiNuo.Mvi.Presentation.Slot
        {
            [AttributeUsage(AttributeTargets.Field)]
            public sealed class MviSlotAttribute : Attribute
            {
                public MviSlotAttribute(Type type, string factory, params string[] observes) { }
            }
        }
        namespace MiKiNuo.Mvi.Application.DI
        {
            public interface IMviResolver { T Resolve<T>(); }
        }
        namespace MiKiNuo.Mvi.Application.MVI.Threading
        {
            public interface IMviUiDispatcher { void Post(Action action); }
        }
        namespace MiKiNuo.Mvi.Presentation.Disposables
        {
            public sealed class MviDisposableBag : IDisposable
            {
                private readonly List<Action> _actions = new();
                public void Add(Action action) => _actions.Add(action);
                public void Dispose() { foreach (Action action in _actions) action(); _actions.Clear(); }
            }
        }
        namespace MiKiNuo.Mvi.Presentation.ViewRegistry
        {
            public interface IMviViewRegistry { object CreateView(object viewModel); }
        }
        namespace MiKiNuo.Mvi.Platforms.Avalonia.Views
        {
            public interface IMviAvaloniaViewBinding { void Unbind(); }
            public abstract class MviAvaloniaView<TViewModel> where TViewModel : class
            {
                protected virtual void OnBindSlots(TViewModel viewModel, MviDisposableBag bindings, IMviResolver resolver) { }
            }
        }
        namespace MiKiNuo.Mvi.Platforms.Avalonia.Slot
        {
            public sealed class MviSlotHost { public object? Content { get; set; } }
        }
        namespace Godot
        {
            public class GodotObject
            {
                public static bool IsInstanceValid(GodotObject? value) => value is not null;
            }
            public class Node : GodotObject
            {
                private readonly List<Node> _children = new();
                private Node? _parent;
                private bool _queued;
                public Node? GetParent() => _parent;
                public Node[] GetChildren() => _children.ToArray();
                public bool IsQueuedForDeletion() => _queued;
                public void AddChild(Node node) { _children.Add(node); node._parent = this; }
                public void RemoveChild(Node node) { _children.Remove(node); node._parent = null; }
                public void QueueFree() => _queued = true;
            }
            public class Control : Node { }
        }
        namespace MiKiNuo.Mvi.Platforms.Godot.Binding
        {
            public interface IMviGodotViewBinding { void Unbind(); }
            public abstract class GodotMviControlView<TViewModel> where TViewModel : class
            {
                protected virtual void OnBindSlots(TViewModel viewModel, MviDisposableBag bindings, IMviResolver resolver) { }
            }
        }
        namespace Fixture
        {
            public sealed class Parent : INotifyPropertyChanged
            {
                public event PropertyChangedEventHandler? PropertyChanged;
                public int Subscribers => PropertyChanged?.GetInvocationList().Length ?? 0;
                public object? Child { get; set; } = new object();
                public object? GetChild() => Child;
                public void Notify() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Child"));
            }
            public sealed class ChildView : Godot.Control, MiKiNuo.Mvi.Platforms.Avalonia.Views.IMviAvaloniaViewBinding, MiKiNuo.Mvi.Platforms.Godot.Binding.IMviGodotViewBinding
            {
                public bool Bound { get; private set; } = true;
                public void Unbind() => Bound = false;
            }
            public sealed class Registry : IMviViewRegistry, IMviResolver, MiKiNuo.Mvi.Application.MVI.Threading.IMviUiDispatcher
            {
                public int Created { get; private set; }
                public ChildView? Last { get; private set; }
                public bool ReplacedWhileBound { get; private set; }
                public bool FailCreation { get; set; }
                public bool Deferred { get; set; }
                private readonly Queue<Action> _queue = new();
                public object CreateView(object vm) { if (FailCreation) throw new InvalidOperationException("创建失败"); ReplacedWhileBound |= Last?.Bound == true; Created++; return Last = new ChildView(); }
                public T Resolve<T>() => (T)(object)this;
                public void Post(Action action) { if (Deferred) _queue.Enqueue(action); else action(); }
                public void Drain() { while (_queue.Count > 0) _queue.Dequeue()(); }
            }
            public partial class ParentView : MiKiNuo.Mvi.Platforms.Avalonia.Views.MviAvaloniaView<Parent>
            {
                [MiKiNuo.Mvi.Presentation.Slot.MviSlot(typeof(ChildView), "GetChild", "Child")]
                public MiKiNuo.Mvi.Platforms.Avalonia.Slot.MviSlotHost Slot = new();
                public void Bind(Parent parent, MviDisposableBag bag, IMviResolver resolver) => OnBindSlots(parent, bag, resolver);
            }
            public static class Scenarios
            {
                public static bool FailedInitialMount()
                {
                    Parent parent = new(); Registry registry = new() { FailCreation = true }; ParentView view = new();
                    using MviDisposableBag bag = new();
                    try { view.Bind(parent, bag, registry); } catch (InvalidOperationException) { }
                    bag.Dispose();
                    return parent.Subscribers == 0;
                }
                public static bool DeferredRemount()
                {
                    Parent parent = new(); Registry registry = new() { Deferred = true }; GodotParentView view = new();
                    using MviDisposableBag first = new(); view.Bind(parent, first, registry); registry.Drain();
                    first.Dispose();
                    using MviDisposableBag second = new(); view.Bind(parent, second, registry); registry.Drain();
                    return !registry.ReplacedWhileBound && view.Slot.GetChildren().Length == 1;
                }
                public static bool GodotReplacement()
                {
                    Parent parent = new(); Registry registry = new(); GodotParentView view = new();
                    Godot.Node decoration = new(); view.Slot.AddChild(decoration);
                    using MviDisposableBag bag = new(); view.Bind(parent, bag, registry);
                    ChildView original = registry.Last!;
                    parent.Notify();
                    if (registry.Created != 1) return false;
                    parent.Child = new object(); parent.Notify();
                    ChildView replacement = registry.Last!;
                    bool replaced = !original.Bound && original.GetParent() is null && original.IsQueuedForDeletion()
                        && !registry.ReplacedWhileBound && replacement.GetParent() == view.Slot && view.Slot.GetChildren().Length == 2;
                    parent.Child = null; parent.Notify();
                    return replaced && !replacement.Bound && replacement.GetParent() is null
                        && view.Slot.GetChildren().Length == 1 && decoration.GetParent() == view.Slot && !decoration.IsQueuedForDeletion();
                }
                public static bool Replacement()
                {
                    Parent parent = new(); Registry registry = new(); ParentView view = new();
                    using MviDisposableBag bag = new(); view.Bind(parent, bag, registry);
                    ChildView original = (ChildView)view.Slot.Content!;
                    parent.Child = new object(); parent.Notify();
                    ChildView replacement = (ChildView)view.Slot.Content!;
                    bool replaced = !original.Bound && replacement.Bound && !registry.ReplacedWhileBound;
                    bag.Dispose(); parent.Notify();
                    return replaced && !replacement.Bound && view.Slot.Content is null && registry.Created == 2;
                }
                public static bool SameInstance()
                {
                    Parent parent = new(); Registry registry = new(); ParentView view = new();
                    using MviDisposableBag bag = new(); view.Bind(parent, bag, registry);
                    object? original = view.Slot.Content; parent.Notify();
                    return ReferenceEquals(original, view.Slot.Content) && registry.Created == 1;
                }
            }
            public partial class GodotParentView : MiKiNuo.Mvi.Platforms.Godot.Binding.GodotMviControlView<Parent>
            {
                [MiKiNuo.Mvi.Presentation.Slot.MviSlot(typeof(ChildView), "GetChild", "Child")]
                public Godot.Control Slot = new();
                public void Bind(Parent parent, MviDisposableBag bag, IMviResolver resolver) => OnBindSlots(parent, bag, resolver);
            }
        }
        """;
}
