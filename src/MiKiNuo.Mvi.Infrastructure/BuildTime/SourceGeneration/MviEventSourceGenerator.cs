using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;

/// <summary>
/// 表示控件事件源扩展源生成器。
/// 全量扫描编译中的 Avalonia / Godot 控件类型，为每个控件的每个 public 事件生成
/// <c>ToEventSource().EventName</c> 链式扩展方法，返回 <c>IEventSource&lt;TEvent&gt;</c>。
/// </summary>
/// <remarks>
/// 对标 ReactiveUI 的 <c>ReactiveMarbles.ObservableEvents.SourceGenerator</c>，无白名单。
/// 生成器通过检测 compilation 中是否存在 Avalonia / Godot 类型来决定生成哪种扩展：
/// <list type="bullet">
/// <item>检测到 <c>Avalonia.Controls.Control</c> → 扫描所有继承自该类型的控件；</item>
/// <item>检测到 <c>Godot.Control</c> → 扫描所有继承自该类型的控件。</item>
/// </list>
/// 生成的代码注入到引用 Infrastructure 作为 Analyzer 的项目中，无反射，编译期类型安全。
/// </remarks>
[Generator]
public sealed class MviEventSourceGenerator : IIncrementalGenerator
{
    /// <summary>
    /// 初始化源生成器注册编译回调。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<SourceControlEvents> sourceControls = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is TypeDeclarationSyntax,
                static (syntaxContext, cancellationToken) =>
                    CollectSourceControlEvents(syntaxContext, cancellationToken))
            .Where(static control => control is not null)
            .Select(static (control, _) => control!);

        IncrementalValueProvider<Compilation> referencedCompilation = context.CompilationProvider
            .WithComparer(MetadataReferenceCompilationComparer.Instance);

        context.RegisterSourceOutput(
            sourceControls.Collect().Combine(referencedCompilation),
            Execute);
    }

    /// <summary>
    /// 收集单个源码控件类型的事件。
    /// </summary>
    private static SourceControlEvents? CollectSourceControlEvents(
        GeneratorSyntaxContext context,
        System.Threading.CancellationToken cancellationToken)
    {
        if (context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken) is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        return TypeScanner.ScanSourceControlEvents(typeSymbol);
    }

    /// <summary>
    /// 执行生成逻辑：合并源码控件与引用程序集控件的事件。
    /// </summary>
    private static void Execute(
        SourceProductionContext context,
        (ImmutableArray<SourceControlEvents> Left, Compilation Right) input)
    {
        List<EventDefinition> avaloniaEvents = new();
        List<EventDefinition> godotEvents = new();
        foreach (SourceControlEvents sourceControl in input.Left)
        {
            List<EventDefinition> target = sourceControl.Platform == ControlPlatform.Avalonia
                ? avaloniaEvents
                : godotEvents;
            target.AddRange(sourceControl.Events);
        }

        Compilation compilation = input.Right;
        INamedTypeSymbol? avaloniaControlType = compilation.GetTypeByMetadataName("Avalonia.Controls.Control");
        INamedTypeSymbol? godotControlType = compilation.GetTypeByMetadataName("Godot.Control");

        if (avaloniaControlType is not null)
        {
            avaloniaEvents.AddRange(TypeScanner.ScanReferencedControlEvents(compilation, avaloniaControlType));
        }

        if (godotControlType is not null)
        {
            godotEvents.AddRange(TypeScanner.ScanReferencedControlEvents(compilation, godotControlType));
        }

        if (avaloniaEvents.Count > 0)
        {
            string source = Emission.EmitExtensions(
                avaloniaEvents,
                "MiKiNuo.Mvi.Platforms.Avalonia.Events",
                "MviAvaloniaEventSourceExtensions");
            context.AddSource(
                "MviAvaloniaEventSourceExtensions.g.cs",
                SourceText.From(source, Encoding.UTF8));
        }

        if (godotEvents.Count > 0)
        {
            string source = Emission.EmitExtensions(
                godotEvents,
                "MiKiNuo.Mvi.Platforms.Godot.Binding",
                "MviGodotEventSourceExtensions");
            context.AddSource(
                "MviGodotEventSourceExtensions.g.cs",
                SourceText.From(source, Encoding.UTF8));
        }
    }

    internal enum ControlPlatform
    {
        Avalonia,
        Godot,
    }

    internal sealed class SourceControlEvents(
        ControlPlatform platform,
        IReadOnlyList<EventDefinition> events)
    {
        /// <summary>获取控件平台。</summary>
        public ControlPlatform Platform { get; } = platform;

        /// <summary>获取控件事件。</summary>
        public IReadOnlyList<EventDefinition> Events { get; } = events;
    }

    private sealed class MetadataReferenceCompilationComparer : IEqualityComparer<Compilation>
    {
        public static MetadataReferenceCompilationComparer Instance { get; } = new();

        public bool Equals(Compilation? x, Compilation? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null
                || y is null
                || !string.Equals(x.AssemblyName, y.AssemblyName, System.StringComparison.Ordinal)
                || x.ExternalReferences.Length != y.ExternalReferences.Length)
            {
                return false;
            }

            for (int i = 0; i < x.ExternalReferences.Length; i++)
            {
                if (!object.Equals(x.ExternalReferences[i], y.ExternalReferences[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(Compilation obj)
        {
            int hash = obj.AssemblyName?.GetHashCode() ?? 0;
            foreach (MetadataReference reference in obj.ExternalReferences)
            {
                hash = unchecked((hash * 397) ^ reference.GetHashCode());
            }

            return hash;
        }
    }

    /// <summary>
    /// 表示控件事件定义。
    /// </summary>
    internal sealed class EventDefinition
    {
        /// <summary>
        /// 初始化事件定义。
        /// </summary>
        public EventDefinition(
            string controlFullName,
            string controlShortName,
            string eventName,
            string eventArgsFullName,
            string delegateFullName,
            int delegateParameterCount)
        {
            ControlFullName = controlFullName;
            ControlShortName = controlShortName;
            EventName = eventName;
            EventArgsFullName = eventArgsFullName;
            DelegateFullName = delegateFullName;
            DelegateParameterCount = delegateParameterCount;
        }

        /// <summary>获取控件全限定名。</summary>
        public string ControlFullName { get; }

        /// <summary>获取控件短名。</summary>
        public string ControlShortName { get; }

        /// <summary>获取事件名。</summary>
        public string EventName { get; }

        /// <summary>获取事件参数全限定名。</summary>
        public string EventArgsFullName { get; }

        /// <summary>获取委托类型全限定名。</summary>
        public string DelegateFullName { get; }

        /// <summary>获取委托参数数量。</summary>
        public int DelegateParameterCount { get; }
    }

    /// <summary>
    /// 表示类型扫描器：递归遍历编译中的所有类型，过滤出控件类型并提取事件。
    /// </summary>
    internal static class TypeScanner
    {
        /// <summary>
        /// 扫描单个源码类型，若为平台控件则提取其 public 事件。
        /// </summary>
        /// <param name="type">源码类型。</param>
        /// <returns>源码控件事件；不是平台控件时返回 <c>null</c>。</returns>
        public static SourceControlEvents? ScanSourceControlEvents(INamedTypeSymbol type)
        {
            if (type.TypeKind != TypeKind.Class
                || type.IsGenericType
                || !IsPublicAccessible(type))
            {
                return null;
            }

            ControlPlatform? platform = DeterminePlatform(type);
            if (platform is null)
            {
                return null;
            }

            List<EventDefinition> events = new();
            AddEvents(type, events);
            return events.Count == 0 ? null : new SourceControlEvents(platform.Value, events);
        }

        /// <summary>
        /// 扫描引用程序集中所有继承自指定控件基类的类型，提取其 public 事件。
        /// </summary>
        /// <param name="compilation">当前编译。</param>
        /// <param name="controlBaseType">控件基类型符号。</param>
        /// <returns>事件定义列表。</returns>
        public static List<EventDefinition> ScanReferencedControlEvents(
            Compilation compilation,
            INamedTypeSymbol controlBaseType)
        {
            List<EventDefinition> events = new();
            HashSet<string> seenTypes = new();

            foreach (MetadataReference reference in compilation.ExternalReferences)
            {
                if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly)
                {
                    continue;
                }

                foreach (INamedTypeSymbol type in EnumerateAllTypes(assembly.GlobalNamespace))
                {
                    ProcessType(type, controlBaseType, seenTypes, events);
                }
            }

            return events;
        }

        private static ControlPlatform? DeterminePlatform(INamedTypeSymbol type)
        {
            for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
            {
                string metadataName = current.OriginalDefinition.ToDisplayString();
                if (string.Equals(metadataName, "Avalonia.Controls.Control", System.StringComparison.Ordinal))
                {
                    return ControlPlatform.Avalonia;
                }

                if (string.Equals(metadataName, "Godot.Control", System.StringComparison.Ordinal))
                {
                    return ControlPlatform.Godot;
                }
            }

            return null;
        }

        /// <summary>
        /// 处理单个类型：若继承自控件基类则提取其事件。
        /// </summary>
        private static void ProcessType(
            INamedTypeSymbol type,
            INamedTypeSymbol controlBaseType,
            HashSet<string> seenTypes,
            List<EventDefinition> events)
        {
            if (type.TypeKind != TypeKind.Class)
            {
                return;
            }

            if (type.IsGenericType)
            {
                return;
            }

            if (!IsPublicAccessible(type))
            {
                return;
            }

            if (!InheritsFrom(type, controlBaseType))
            {
                return;
            }

            string controlFullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (!seenTypes.Add(controlFullName))
            {
                return;
            }

            AddEvents(type, events);
        }

        private static void AddEvents(INamedTypeSymbol type, List<EventDefinition> events)
        {
            foreach (IEventSymbol evt in EnumerateEvents(type))
            {
                EventDefinition? definition = EventExtractor.Extract(evt, type);
                if (definition is not null)
                {
                    events.Add(definition);
                }
            }
        }

        /// <summary>
        /// 判断类型及其所有外层类型是否均为 public 可访问。
        /// </summary>
        private static bool IsPublicAccessible(INamedTypeSymbol type)
        {
            INamedTypeSymbol? current = type;
            while (current is not null)
            {
                if (current.DeclaredAccessibility != Accessibility.Public)
                {
                    return false;
                }

                current = current.ContainingType;
            }

            return true;
        }

        /// <summary>
        /// 递归遍历命名空间中的所有类型。
        /// </summary>
        /// <param name="namespaceSymbol">命名空间符号。</param>
        /// <returns>类型序列。</returns>
        private static IEnumerable<INamedTypeSymbol> EnumerateAllTypes(INamespaceSymbol namespaceSymbol)
        {
            foreach (INamespaceSymbol childNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                foreach (INamedTypeSymbol type in EnumerateAllTypes(childNamespace))
                {
                    yield return type;
                }
            }

            foreach (INamedTypeSymbol type in namespaceSymbol.GetTypeMembers())
            {
                yield return type;

                foreach (INamedTypeSymbol nested in EnumerateNestedTypes(type))
                {
                    yield return nested;
                }
            }
        }

        /// <summary>
        /// 递归遍历嵌套类型。
        /// </summary>
        private static IEnumerable<INamedTypeSymbol> EnumerateNestedTypes(INamedTypeSymbol type)
        {
            foreach (INamedTypeSymbol nested in type.GetTypeMembers())
            {
                yield return nested;

                foreach (INamedTypeSymbol deeper in EnumerateNestedTypes(nested))
                {
                    yield return deeper;
                }
            }
        }

        /// <summary>
        /// 枚举类型及其基类链上的所有 public 事件。
        /// </summary>
        /// <param name="type">起始类型符号。</param>
        /// <returns>事件符号序列。</returns>
        private static IEnumerable<IEventSymbol> EnumerateEvents(INamedTypeSymbol type)
        {
            INamedTypeSymbol? current = type;
            HashSet<string> seenEvents = new();

            while (current is not null)
            {
                foreach (ISymbol member in current.GetMembers())
                {
                    if (member is not IEventSymbol evt)
                    {
                        continue;
                    }

                    if (evt.IsStatic)
                    {
                        continue;
                    }

                    if (evt.DeclaredAccessibility != Accessibility.Public)
                    {
                        continue;
                    }

                    if (!seenEvents.Add(evt.Name))
                    {
                        continue;
                    }

                    yield return evt;
                }

                current = current.BaseType;
            }
        }

        /// <summary>
        /// 判断类型是否继承自指定基类型（包含类型本身）。
        /// </summary>
        private static bool InheritsFrom(INamedTypeSymbol type, INamedTypeSymbol baseType)
        {
            INamedTypeSymbol? current = type;
            while (current is not null)
            {
                if (SymbolEqualityComparer.Default.Equals(current, baseType))
                {
                    return true;
                }

                current = current.BaseType;
            }

            return false;
        }
    }

    /// <summary>
    /// 表示事件提取器：从事件符号提取事件定义，通用解析委托类型。
    /// </summary>
    internal static class EventExtractor
    {
        /// <summary>
        /// 从事件符号提取事件定义。
        /// </summary>
        /// <param name="evt">事件符号。</param>
        /// <param name="containingType">包含类型。</param>
        /// <returns>事件定义，若无法解析则返回 null。</returns>
        public static EventDefinition? Extract(IEventSymbol evt, INamedTypeSymbol containingType)
        {
            if (evt.Type is not INamedTypeSymbol delegateType)
            {
                return null;
            }

            IMethodSymbol? invokeMethod = delegateType.DelegateInvokeMethod;
            if (invokeMethod is null)
            {
                return null;
            }

            if (!invokeMethod.ReturnsVoid)
            {
                return null;
            }

            int paramCount = invokeMethod.Parameters.Length;
            string eventArgsFullName = ResolveEventArgsFullName(invokeMethod, paramCount);
            string delegateFullName = delegateType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string controlFullName = containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string controlShortName = containingType.Name;

            return new EventDefinition(
                controlFullName,
                controlShortName,
                evt.Name,
                eventArgsFullName,
                delegateFullName,
                paramCount);
        }

        /// <summary>
        /// 解析事件参数全限定名。
        /// </summary>
        private static string ResolveEventArgsFullName(IMethodSymbol invokeMethod, int paramCount)
        {
            if (paramCount == 0)
            {
                return "global::System.EventArgs";
            }

            IParameterSymbol lastParam = invokeMethod.Parameters[paramCount - 1];
            return lastParam.Type.ToDisplayString(GeneratorSyntaxHelpers.FullyQualifiedNullableFormat);
        }
    }

    /// <summary>
    /// 表示代码发射阶段：根据事件定义列表生成扩展方法。
    /// </summary>
    internal static class Emission
    {
        /// <summary>
        /// 生成事件源扩展代码。
        /// </summary>
        /// <param name="events">事件定义列表。</param>
        /// <param name="targetNamespace">目标命名空间。</param>
        /// <param name="extensionsClassName">扩展方法类名。</param>
        /// <returns>生成的源代码。</returns>
        public static string EmitExtensions(
            List<EventDefinition> events,
            string targetNamespace,
            string extensionsClassName)
        {
            StringBuilder sb = new();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("// 由 MviEventSourceGenerator 自动生成，请勿手动修改。");
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine("using MiKiNuo.Mvi.Application.MVI.EventBinding;");
            sb.AppendLine();
            sb.AppendLine($"namespace {targetNamespace};");
            sb.AppendLine();
            sb.AppendLine("/// <summary>");
            sb.AppendLine($"/// 表示控件事件源扩展方法入口。");
            sb.AppendLine("/// </summary>");
            sb.AppendLine($"internal static class {extensionsClassName}");
            sb.AppendLine("{");

            HashSet<string> emittedControls = new();
            foreach (EventDefinition evt in events)
            {
                if (emittedControls.Add(evt.ControlShortName))
                {
                    EmitToEventSourceExtension(sb, evt);
                }
            }

            sb.AppendLine("}");
            sb.AppendLine();

            IEnumerable<IGrouping<string, EventDefinition>> groupedByControl = events.GroupBy(e => e.ControlShortName);
            foreach (IGrouping<string, EventDefinition> group in groupedByControl)
            {
                EmitAdapterClass(sb, group);
            }

            return sb.ToString();
        }

        /// <summary>
        /// 发射 ToEventSource 扩展方法。
        /// </summary>
        private static void EmitToEventSourceExtension(StringBuilder sb, EventDefinition evt)
        {
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// 把 {evt.ControlShortName} 包装为事件源适配器。");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    /// <param name=\"control\">{evt.ControlShortName} 控件。</param>");
            sb.AppendLine($"    /// <returns>{evt.ControlShortName} 事件源适配器。</returns>");
            sb.AppendLine($"    internal static {evt.ControlShortName}EventSourceAdapter ToEventSource(this {evt.ControlFullName} control)");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        ArgumentNullException.ThrowIfNull(control);");
            sb.AppendLine($"        return new {evt.ControlShortName}EventSourceAdapter(control);");
            sb.AppendLine($"    }}");
            sb.AppendLine();
        }

        /// <summary>
        /// 发射事件源适配器类。
        /// </summary>
        private static void EmitAdapterClass(StringBuilder sb, IGrouping<string, EventDefinition> group)
        {
            EventDefinition first = group.First();
            sb.AppendLine($"/// <summary>");
            sb.AppendLine($"/// 表示 {first.ControlShortName} 的事件源适配器。");
            sb.AppendLine($"/// </summary>");
            sb.AppendLine($"internal sealed class {first.ControlShortName}EventSourceAdapter");
            sb.AppendLine($"{{");
            sb.AppendLine($"    private readonly {first.ControlFullName} _control;");
            sb.AppendLine();
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// 初始化 {first.ControlShortName} 事件源适配器。");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    /// <param name=\"control\">{first.ControlShortName} 控件。</param>");
            sb.AppendLine($"    internal {first.ControlShortName}EventSourceAdapter({first.ControlFullName} control)");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        _control = control;");
            sb.AppendLine($"    }}");
            sb.AppendLine();

            foreach (EventDefinition evt in group)
            {
                EmitEventProperty(sb, evt);
            }

            sb.AppendLine($"}}");
            sb.AppendLine();
        }

        /// <summary>
        /// 发射事件属性。
        /// </summary>
        private static void EmitEventProperty(StringBuilder sb, EventDefinition evt)
        {
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// 获取 {evt.EventName} 事件源。");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public IEventSource<{evt.EventArgsFullName}> {evt.EventName}");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        get");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            return new DelegateEventSource<{evt.EventArgsFullName}>(handler =>");
            sb.AppendLine($"            {{");
            EmitAdapterLambda(sb, evt);
            sb.AppendLine($"                _control.{evt.EventName} += adapter;");
            sb.AppendLine($"                return new ActionDisposable(() => _control.{evt.EventName} -= adapter);");
            sb.AppendLine($"            }});");
            sb.AppendLine($"        }}");
            sb.AppendLine($"    }}");
            sb.AppendLine();
        }

        /// <summary>
        /// 发射适配器 lambda 表达式，通用处理任意参数数量的委托。
        /// </summary>
        private static void EmitAdapterLambda(StringBuilder sb, EventDefinition evt)
        {
            int paramCount = evt.DelegateParameterCount;
            string lambdaParams = BuildLambdaParameters(paramCount);
            string lambdaBody = BuildLambdaBody(paramCount);

            sb.AppendLine($"                {evt.DelegateFullName} adapter = {lambdaParams} => handler({lambdaBody});");
        }

        /// <summary>
        /// 构建委托 lambda 参数列表。
        /// </summary>
        private static string BuildLambdaParameters(int paramCount)
        {
            if (paramCount == 0)
            {
                return "()";
            }

            List<string> parameters = new();
            for (int i = 0; i < paramCount; i++)
            {
                parameters.Add(i == paramCount - 1 ? "e" : "_");
            }

            return $"({string.Join(", ", parameters)})";
        }

        /// <summary>
        /// 构建委托 lambda 传给 handler 的表达式。
        /// </summary>
        private static string BuildLambdaBody(int paramCount)
        {
            if (paramCount == 0)
            {
                return "global::System.EventArgs.Empty";
            }

            return "e";
        }
    }
}
