using Microsoft.CodeAnalysis;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.V2.Tests;

/// <summary>验证用户声明能够生成可编辑和只读的本地投影。</summary>
public sealed class ProjectionDeclarationTests
{
    /// <summary>Godot 用户只声明 State、Feature 并使用生成输入与投影即可连接原生控件。</summary>
    /// <returns>真实 Godot 类型的公开消费编译验证任务。</returns>
    [Test]
    public async Task GodotConsumerUsesGeneratedInputsAndNativeProjectionConnection()
    {
        const string source = """
            using System;using Godot;using MiKiNuo.Mvi;using MiKiNuo.Mvi.Platforms.Godot;
            public sealed record State { [Input] public string Name {get;init;} = ""; }
            public sealed partial class Hud():Feature<State>(new());
            public static class Consumer {
                public static IDisposable Connect(Node view,LineEdit edit) {
                    Hud feature = new();feature.SetName("pilot");
                    GodotProjection connection = new(view);
                    Hud.Projection projection = connection.Create(feature.CreateProjection);
                    connection.BindInput(projection,edit,p=>p.Name);
                    return connection;
                }
            }
            """;
        (Compilation output, GeneratorDriverRunResult result) = GeneratorTestHost.Run(source);
        await Assert.That(result.Diagnostics).IsEmpty();
        await Assert.That(output.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.ToString()).ToArray()).IsEmpty();
        using MemoryStream assembly = new();
        await Assert.That(output.Emit(assembly).Success).IsTrue();
    }

    /// <summary>验证保留名称冲突定位用户声明而非生成代码。</summary>
    /// <param name="member">与投影冲突的用户成员。</param>
    /// <param name="onFeature">是否在功能中声明该成员。</param>
    /// <returns>表示诊断验证完成的任务。</returns>
    [Test]
    [Arguments("public string Snapshot => \"\";", false)]
    [Arguments("public string PropertyChanged => \"\";", false)]
    [Arguments("public string Projection => \"\";", false)]
    [Arguments("public void CreateProjection() { }", true)]
    [Arguments("public class Projection { }", true)]
    public async Task ProjectionMemberConflictsPointAtUserDeclaration(string member, bool onFeature)
    {
        (Compilation _, GeneratorDriverRunResult result) = GeneratorTestHost.Run("using MiKiNuo.Mvi; public sealed record State { "
            + (onFeature ? "" : member) + " } public sealed partial class Editor() : Feature<State>(new()) { "
            + (onFeature ? member : "") + " }");
        await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Id == "MVI2006" && diagnostic.Location.IsInSource)).IsTrue();
        await Assert.That(result.GeneratedTrees.Length).IsEqualTo(0);
    }

    /// <summary>验证绑定回写调用同一输入规则且显示来自提交快照。</summary>
    /// <returns>表示消费验证完成的任务。</returns>
    [Test]
    public async Task ConsumerUsesGeneratedProjectionWithoutWritingViewModel()
    {
        (Compilation compilation, GeneratorDriverRunResult result) = GeneratorTestHost.Run("""
            using MiKiNuo.Mvi;
            public sealed record State {
                [Input] public string Text { get; init; } = "";
                public string Label => "Value:" + Text;
            }
            public sealed partial class Editor() : Feature<State>(new()) {
                [OnInput(nameof(State.Text))]
                private static State Normalize(State state, string value) => state with { Text = value.Trim() };
            }
            public static class Consumer {
                public static string Run() {
                    Editor feature = new();
                    using Editor.Projection view = feature.CreateProjection(action => action());
                    view.Text = "  intermediate-  ";
                    return view.Label + ":" + view.Snapshot.Version + ":" + feature.Snapshot.State.Text;
                }
            }
            """);
        await Assert.That(result.Diagnostics.IsEmpty).IsTrue();
        Diagnostic[] errors = compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        await Assert.That(string.Join("\n", errors.Select(static diagnostic => diagnostic.ToString()))).IsEqualTo("");
        using MemoryStream output = new();
        await Assert.That(compilation.Emit(output).Success).IsTrue();
        System.Reflection.Assembly consumer = System.Reflection.Assembly.Load(output.ToArray());
        await Assert.That(consumer.GetType("Consumer")!.GetMethod("Run")!.Invoke(null, null)).IsEqualTo("Value:intermediate-:1:intermediate-");
        await Assert.That(consumer.GetType("Editor+Projection")!.GetProperty("Label")!.CanWrite).IsFalse();
    }
}
