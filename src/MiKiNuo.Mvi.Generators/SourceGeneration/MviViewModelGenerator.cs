using MiKiNuo.Mvi.Generators.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
namespace MiKiNuo.Mvi.Generators.SourceGeneration;
/// <summary>生成属性缓冲、输入快照、命令和构造函数；不生成业务转发层。</summary>
[Generator]
public sealed class MviViewModelGenerator : IIncrementalGenerator
{
    private const string BaseName = "MiKiNuo.Mvi.Binding.ViewModel.MviViewModelBase`2";
    private const string BindName = "MiKiNuo.Mvi.Abstractions.MVI.Binding.MviBindAttribute";
    private const string CommandName = "MiKiNuo.Mvi.Abstractions.MVI.Binding.MviCommandAttribute";
    private static readonly DiagnosticDescriptor Invalid = new(DiagnosticIdCatalog.MviBindingDeclarationInvalid, "绑定声明无效", "{0}", "MviBinding", DiagnosticSeverity.Error, true);
    /// <summary>登记按类分析的增量管线，输出按内容比较，避免无关更改重复发射。</summary>
    /// <param name="context">生成上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var units = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is ClassDeclarationSyntax c && c.BaseList is not null,
            static (ctx, token) => Build(ctx, token))
            .Where(static unit => unit is not null).Select(static (unit, _) => unit!)
            .WithComparer(UnitComparer.Instance);
        context.RegisterSourceOutput(units, static (output, unit) =>
        {
            foreach (Diagnostic diagnostic in unit.Diagnostics) output.ReportDiagnostic(diagnostic);
            if (unit.Source is not null) output.AddSource(unit.Hint, SourceText.From(unit.Source, Encoding.UTF8));
        });
    }
    private static Unit? Build(GeneratorSyntaxContext ctx, System.Threading.CancellationToken token)
    {
        if (ctx.SemanticModel.GetDeclaredSymbol(ctx.Node, token) is not INamedTypeSymbol type) return null;
        INamedTypeSymbol? definition = ctx.SemanticModel.Compilation.GetTypeByMetadataName(BaseName);
        INamedTypeSymbol? parent = type.BaseType;
        while (parent is not null && !SymbolEqualityComparer.Default.Equals(parent.OriginalDefinition, definition)) parent = parent.BaseType;
        if (parent is null) return null;
        // 只由含继承声明的一份 partial 触发，避免重复 AddSource。
        SyntaxReference? owner = type.DeclaringSyntaxReferences.FirstOrDefault(reference => reference.GetSyntax(token) is ClassDeclarationSyntax c && c.BaseList is not null);
        if (owner is not null && (owner.SyntaxTree != ctx.Node.SyntaxTree || owner.Span != ctx.Node.Span)) return null;
        var diagnostics = new List<Diagnostic>();
        void Error(ISymbol symbol, string message) => diagnostics.Add(Diagnostic.Create(Invalid, symbol.Locations.FirstOrDefault(), message));
        if (type.ContainingType is not null || type.Arity != 0 || type.IsAbstract || type.IsStatic
            || !type.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax(token) is ClassDeclarationSyntax c && c.Modifiers.Any(SyntaxKind.PartialKeyword)))
        { Error(type, "生成式 ViewModel 必须是顶层、非泛型、非抽象 partial class。"); return new Unit(Hint(type), null, diagnostics); }
        ITypeSymbol state = parent.TypeArguments[0], intentBase = parent.TypeArguments[1];
        string S(ITypeSymbol symbol) => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers));
        bool Convertible(ITypeSymbol source, ITypeSymbol target) => ((CSharpCompilation)ctx.SemanticModel.Compilation).ClassifyConversion(source, target).IsImplicit;
        var properties = type.GetMembers().OfType<IPropertySymbol>().ToArray();
        var binds = new List<Bound>(); var commands = new List<Command>();
        foreach (IPropertySymbol p in properties)
        {
            AttributeData? bind = Attribute(p, BindName), command = Attribute(p, CommandName);
            if (bind is null && command is null) continue;
            if (bind is not null && command is not null) { Error(p, "同一个属性不能同时声明绑定与命令。"); continue; }
            if (p.IsStatic || p.IsIndexer || p.GetMethod is null || !p.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax(token) is PropertyDeclarationSyntax syntax && syntax.Modifiers.Any(SyntaxKind.PartialKeyword)))
            { Error(p, "绑定和命令必须声明在实例 partial 属性上。"); continue; }
            if (p.SetMethod?.IsInitOnly == true) { Error(p, "绑定属性不支持 init；使用 get 或 get/set。"); continue; }
            if (bind is null) continue;
            string? source = bind.ConstructorArguments.Length > 0 ? bind.ConstructorArguments[0].Value as string : null;
            bool local = source is null && p.SetMethod?.DeclaredAccessibility == Accessibility.Public;
            bool sensitive = Named(bind, "Sensitive")?.Value is true;
            INamedTypeSymbol? change = Named(bind, "IntentType")?.Value as INamedTypeSymbol;
            IPropertySymbol? sourceProperty = null;
            if (!local)
            {
                source = source ?? p.Name;
                sourceProperty = Members(state).OfType<IPropertySymbol>().FirstOrDefault(candidate => candidate.Name == source && candidate.GetMethod?.DeclaredAccessibility == Accessibility.Public && !candidate.IsStatic);
                if (sourceProperty is null || !Convertible(sourceProperty.Type, p.Type)) Error(p, "状态来源不存在、不可读或类型不兼容：" + source);
                if (p.SetMethod?.DeclaredAccessibility == Accessibility.Public && change is null) Error(p, "可写状态投影必须指定 IntentType；本地输入直接使用 [MviBind]。");
            }
            if (change is not null)
            {
                if (local || p.SetMethod?.DeclaredAccessibility != Accessibility.Public) Error(p, "IntentType 只能用于显式状态来源的可写属性。");
                if (!Convertible(change, intentBase) || !change.InstanceConstructors.Any(c => c.DeclaredAccessibility == Accessibility.Public && c.Parameters.Length == 1 && Convertible(p.Type, c.Parameters[0].Type)))
                    Error(p, "输入 Intent 必须属于当前组件，并有兼容的公开单参数构造函数。");
            }
            if (sensitive && (!local || p.Type.SpecialType != SpecialType.System_String)) Error(p, "Sensitive 只允许用于本地 string 输入，不得进入 State。");
            binds.Add(new Bound(p, local, sensitive, source, change));
        }
        foreach (IPropertySymbol p in properties)
        {
            AttributeData? attr = Attribute(p, CommandName); if (attr is null) continue;
            if (attr.ConstructorArguments.Length == 0 || attr.ConstructorArguments[0].Value is not INamedTypeSymbol target) { Error(p, "缺少 Intent 类型。"); continue; }
            if (!Convertible(target, intentBase) || target.IsAbstract) Error(p, "命令 Intent 必须是本组件意图的可实例化类型。");
            string[] inputs = attr.ConstructorArguments.Length > 1 ? Strings(attr.ConstructorArguments[1]) : Array.Empty<string>();
            var inputProps = new List<IPropertySymbol>();
            foreach (string name in inputs)
            {
                IPropertySymbol? input = Members(type).OfType<IPropertySymbol>().FirstOrDefault(candidate => candidate.Name == name && candidate.GetMethod is not null && !candidate.IsStatic);
                if (input is null) Error(p, "找不到可读快照属性：" + name); else inputProps.Add(input);
            }
            ITypeSymbol? payload = Named(attr, "PayloadType")?.Value as ITypeSymbol;
            IMethodSymbol? ctor = null;
            var ctors = target.InstanceConstructors.Where(c => c.DeclaredAccessibility == Accessibility.Public).ToArray();
            if (inputs.Length != 0)
            {
                if (payload is not null) Error(p, "Inputs 与 PayloadType 不能同时使用。");
                var matching = ctors.Where(c => c.Parameters.Length == inputProps.Count && c.Parameters.All(param => param.RefKind == RefKind.None) && c.Parameters.Select((param, index) => Convertible(inputProps[index].Type, param.Type)).All(v => v)).ToArray();
                if (matching.Length != 1) Error(p, "快照属性没有唯一匹配的 Intent 构造函数。"); else ctor = matching[0];
            }
            else if (payload is not null)
            {
                var matching = ctors.Where(c => c.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals(c.Parameters[0].Type, payload) && c.Parameters[0].RefKind == RefKind.None).ToArray();
                if (matching.Length != 1) Error(p, "PayloadType 没有唯一匹配的 Intent 构造函数。"); else ctor = matching[0];
            }
            else
            {
                ctor = ctors.FirstOrDefault(c => c.Parameters.Length == 0);
                if (ctor is null)
                {
                    var matching = ctors.Where(c => c.Parameters.Length == 1 && c.Parameters[0].RefKind == RefKind.None).ToArray();
                    if (matching.Length != 1) Error(p, "命令需要无参构造、唯一 payload 构造或显式 Inputs。");
                    else { ctor = matching[0]; payload = ctor.Parameters[0].Type; }
                }
            }
            string? can = Named(attr, "CanExecuteProperty")?.Value as string;
            if (!string.IsNullOrWhiteSpace(can))
            {
                ITypeSymbol ownerType = can!.StartsWith("State.", StringComparison.Ordinal) ? state : type;
                string name = can.StartsWith("State.", StringComparison.Ordinal) ? can.Substring(6) : can;
                IPropertySymbol? condition = Members(ownerType).OfType<IPropertySymbol>().FirstOrDefault(candidate => candidate.Name == name && !candidate.IsStatic && candidate.GetMethod is not null);
                if (condition is null || condition.Type.SpecialType != SpecialType.System_Boolean) Error(p, "CanExecuteProperty 必须是可读 bool 属性：" + can);
            }
            var clear = new HashSet<string>(StringComparer.Ordinal);
            foreach (Bound input in binds.Where(b => b.Sensitive && inputs.Contains(b.Property.Name))) clear.Add(input.Property.Name);
            TypedConstant? clearSetting = Named(attr, "ClearAfterCapture");
            if (clearSetting.HasValue) foreach (string name in Strings(clearSetting.Value)) clear.Add(name);
            foreach (string name in clear)
            {
                Bound? input = binds.FirstOrDefault(b => b.Property.Name == name);
                if (input is null || !input.Local || input.Property.Type.SpecialType != SpecialType.System_String || !inputs.Contains(name))
                    Error(p, "清理目标必须是已捕获的本地 string 输入：" + name);
            }
            INamedTypeSymbol? concreteCommand = ctx.SemanticModel.Compilation.GetTypeByMetadataName("MiKiNuo.Mvi.Binding.Command.MviAsyncCommand");
            if (concreteCommand is null || !Convertible(concreteCommand, p.Type)) Error(p, "命令属性类型必须能够接收 MviAsyncCommand。");
            commands.Add(new Command(p, target, inputProps, payload, can, clear.OrderBy(v => v, StringComparer.Ordinal).ToArray()));
        }
        if (diagnostics.Count != 0) return new Unit(Hint(type), null, diagnostics);
        var b = new StringBuilder("// <auto-generated />\n#nullable enable\n");
        if (!type.ContainingNamespace.IsGlobalNamespace) b.Append("namespace ").Append(type.ContainingNamespace.ToDisplayString()).AppendLine(";");
        b.AppendLine("/// <summary>声明式绑定的生成实现。</summary>");
        b.Append(Access(type.DeclaredAccessibility)).Append(" partial class @").Append(type.Name).AppendLine("\n{");
        if (!type.InstanceConstructors.Any(c => !c.IsImplicitlyDeclared))
        {
            b.AppendLine("    /// <summary>连接借用的 Store 与 UI 调度器并初始化生成绑定。</summary>");
            b.AppendLine("    /// <param name=\"store\">组件 Store。</param>\n    /// <param name=\"uiDispatcher\">UI 调度器。</param>");
            b.Append("    public @").Append(type.Name).Append("(global::MiKiNuo.Mvi.Runtime.MVI.Store.IMviStore<").Append(S(state)).Append(", ").Append(S(intentBase)).Append("> store, global::MiKiNuo.Mvi.Binding.Threading.IMviUiDispatcher? uiDispatcher = null) : base(store, uiDispatcher) { InitializeBindings(); }\n");
        }
        foreach (Bound p in binds)
        {
            string initial = p.Property.Type.SpecialType == SpecialType.System_String ? "string.Empty" : "default!";
            b.Append("    private ").Append(S(p.Property.Type)).Append(" __mvi_").Append(p.Property.Name).Append(" = ").Append(initial).AppendLine(";");
            b.AppendLine("    /// <inheritdoc/>");
            b.Append("    ").Append(Access(p.Property.DeclaredAccessibility)).Append(" partial ").Append(S(p.Property.Type)).Append(" @").Append(p.Property.Name).Append(" { get => __mvi_").Append(p.Property.Name).Append(';');
            if (p.Property.SetMethod is not null)
            {
                if (p.Property.SetMethod.DeclaredAccessibility != p.Property.DeclaredAccessibility) b.Append(' ').Append(Access(p.Property.SetMethod.DeclaredAccessibility));
                b.Append(" set { if (SetProperty(ref __mvi_").Append(p.Property.Name).Append(", value, ").Append(Literal(p.Property.Name)).Append(")");
                if (p.Change is not null) b.Append(" && !IsApplyingState");
                b.Append(") { ");
                if (p.Change is not null) b.Append("QueueDispatch(new ").Append(S(p.Change)).Append("(value));");
                b.Append(" } }");
            }
            b.AppendLine(" }");
        }
        foreach (Command c in commands)
        {
            b.Append("    private ").Append(S(c.Property.Type)).Append(" __mvi_").Append(c.Property.Name).AppendLine(" = default!;");
            b.AppendLine("    /// <inheritdoc/>");
            b.Append("    ").Append(Access(c.Property.DeclaredAccessibility)).Append(" partial ").Append(S(c.Property.Type)).Append(" @").Append(c.Property.Name).Append(" { get => __mvi_").Append(c.Property.Name).Append(';');
            if (c.Property.SetMethod is not null)
            { if (c.Property.SetMethod.DeclaredAccessibility != c.Property.DeclaredAccessibility) b.Append(' ').Append(Access(c.Property.SetMethod.DeclaredAccessibility)); b.Append(" set => __mvi_").Append(c.Property.Name).Append(" = value;"); }
            b.AppendLine(" }");
        }
        b.AppendLine("    /// <inheritdoc/>\n    protected override void InitializeGeneratedCommands()\n    {");
        foreach (Command c in commands)
        {
            string can = string.IsNullOrWhiteSpace(c.Can) ? "true" : c.Can!.StartsWith("State.", StringComparison.Ordinal) ? "State.@" + c.Can.Substring(6) : "@" + c.Can;
            b.Append("        __mvi_").Append(c.Property.Name).Append(" = OwnCommand(new global::MiKiNuo.Mvi.Binding.Command.MviAsyncCommand(() => ").Append(can).AppendLine(", (payload, token) =>\n        {");
            for (int i = 0; i < c.Inputs.Count; i++) b.Append("            var input").Append(i).Append(" = @").Append(c.Inputs[i].Name).AppendLine(";");
            string args = string.Join(", ", Enumerable.Range(0, c.Inputs.Count).Select(i => "input" + i));
            if (c.Inputs.Count == 0 && c.Payload is not null)
            {
                bool nullable = c.Payload.IsReferenceType && c.Payload.NullableAnnotation == NullableAnnotation.Annotated || c.Payload.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
                string ptype = S(c.Payload.WithNullableAnnotation(NullableAnnotation.NotAnnotated));
                // Nullable<T> 不能用在类型模式中，改为验证底层装箱类型。
                if (c.Payload is INamedTypeSymbol nt && nt.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T) ptype = S(nt.TypeArguments[0]);
                b.Append("            if (payload is not ").Append(ptype).Append(nullable ? " && payload is not null" : "").AppendLine(") throw new global::System.ArgumentException(\"命令 payload 类型不匹配。\", nameof(payload));");
                args = "(" + S(c.Payload) + ")payload!";
            }
            b.Append("            var intent = new ").Append(S(c.Intent)).Append('(').Append(args).AppendLine(");");
            foreach (string clear in c.Clear) b.Append("            @").Append(clear).AppendLine(" = string.Empty;");
            b.AppendLine("            return DispatchAsync(intent, token);\n        }, UiDispatcher));");
        }
        b.AppendLine("    }\n    /// <inheritdoc/>\n    protected override void ApplyStateCore(" + S(state) + " state)\n    {");
        foreach (Bound p in binds.Where(p => !p.Local))
            b.Append("        SetProperty(ref __mvi_").Append(p.Property.Name).Append(", state.@").Append(p.Source).Append(", ").Append(Literal(p.Property.Name)).AppendLine(");");
        b.AppendLine("    }\n}");
        return new Unit(Hint(type), b.ToString(), diagnostics);
    }
    private static IEnumerable<ISymbol> Members(ITypeSymbol type)
    { for (INamedTypeSymbol? current = type as INamedTypeSymbol; current is not null; current = current.BaseType) foreach (ISymbol member in current.GetMembers()) yield return member; }
    private static AttributeData? Attribute(ISymbol symbol, string name) => symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == name);
    private static TypedConstant? Named(AttributeData attribute, string name)
    { foreach (var item in attribute.NamedArguments) if (item.Key == name) return item.Value; return null; }
    private static string[] Strings(TypedConstant value) => value.Kind == TypedConstantKind.Array && !value.IsNull ? value.Values.Select(v => v.Value as string ?? "").ToArray() : Array.Empty<string>();
    private static string Access(Accessibility access) => access == Accessibility.Public ? "public" : access == Accessibility.Private ? "private" : access == Accessibility.Protected ? "protected" : access == Accessibility.ProtectedOrInternal ? "protected internal" : access == Accessibility.ProtectedAndInternal ? "private protected" : "internal";
    private static string Literal(string value) => SymbolDisplay.FormatLiteral(value, true);
    private static string Hint(INamedTypeSymbol type) => type.ToDisplayString() + ".ViewModel.g.cs";
    private sealed class Bound(IPropertySymbol property, bool local, bool sensitive, string? source, INamedTypeSymbol? change)
    { internal IPropertySymbol Property = property; internal bool Local = local; internal bool Sensitive = sensitive; internal string? Source = source; internal INamedTypeSymbol? Change = change; }
    private sealed class Command(IPropertySymbol property, INamedTypeSymbol intent, List<IPropertySymbol> inputs, ITypeSymbol? payload, string? can, string[] clear)
    { internal IPropertySymbol Property = property; internal INamedTypeSymbol Intent = intent; internal List<IPropertySymbol> Inputs = inputs; internal ITypeSymbol? Payload = payload; internal string? Can = can; internal string[] Clear = clear; }
    private sealed class Unit(string hint, string? source, List<Diagnostic> diagnostics)
    { internal string Hint = hint; internal string? Source = source; internal List<Diagnostic> Diagnostics = diagnostics; }
    private sealed class UnitComparer : IEqualityComparer<Unit>
    {
        internal static readonly UnitComparer Instance = new();
        public bool Equals(Unit? x, Unit? y) => ReferenceEquals(x, y) || x is not null && y is not null && x.Diagnostics.Count == 0 && y.Diagnostics.Count == 0 && x.Hint == y.Hint && x.Source == y.Source;
        public int GetHashCode(Unit obj) => obj.Hint.GetHashCode();
    }
}
