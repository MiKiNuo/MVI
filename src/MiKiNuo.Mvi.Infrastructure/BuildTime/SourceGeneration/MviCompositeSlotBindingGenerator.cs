using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;

/// <summary>
/// 表示组合模式槽位绑定源生成器。
/// 扫描标有 <c>[MviSlot]</c> 特性的字段所在的 partial class，emit <c>OnBindSlots</c> 虚方法 override：
/// <list type="number">
/// <item>解析父 ViewModel 上 <c>Factory</c> 指定的子 ViewModel 工厂方法；</item>
/// <item>用 <c>IMviResolver.Resolve&lt;IMviViewRegistry&gt;</c> 拿到 <c>IMviViewRegistry</c>；</item>
/// <item>在 <c>Observes</c> 列出的属性变化时重新解析子 VM 并写入槽位控件；</item>
/// <item>依据字段类型 emit 平台相关挂载片段：Avalonia <c>MviSlotHost</c> 走 <c>Content =</c>，
///       Godot <c>Control</c> 走 <c>Clear + AddChild</c>。</item>
/// </list>
/// <para>
/// 与 <c>MviAvaloniaView&lt;T&gt;</c> / <c>GodotMviControlView&lt;T&gt;</c> 的虚方法钩子共同组成
/// "声明式槽位 + 编译器安全 emit" 管道：用户只声明 [MviSlot]，源生成器负责接线。
/// </para>
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class MviCompositeSlotBindingGenerator : IIncrementalGenerator
{
    private const string SlotAttributeMetadataName = "MiKiNuo.Mvi.Presentation.Slot.MviSlotAttribute";
    private const string AvaloniaViewBaseMetadataName = "MiKiNuo.Mvi.Platforms.Avalonia.Views.MviAvaloniaView`1";
    private const string GodotViewBaseMetadataName = "MiKiNuo.Mvi.Platforms.Godot.Binding.GodotMviControlView`1";
    private const string AvaloniaViewBaseFullyQualifiedName = "MiKiNuo.Mvi.Platforms.Avalonia.Views.MviAvaloniaView<TViewModel>";
    private const string GodotViewBaseFullyQualifiedName = "MiKiNuo.Mvi.Platforms.Godot.Binding.GodotMviControlView<TViewModel>";
    private const string ViewRegistryMetadataName = "MiKiNuo.Mvi.Presentation.ViewRegistry.IMviViewRegistry";
    private const string DisposableBagMetadataName = "MiKiNuo.Mvi.Presentation.Disposables.MviDisposableBag";
    private const string ResolverMetadataName = "MiKiNuo.Mvi.Application.DI.IMviResolver";

    /// <summary>
    /// 初始化源生成器注册槽位分析。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<SlotFieldModel> slots = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                SlotAttributeMetadataName,
                static (node, _) => node is VariableDeclaratorSyntax,
                static (syntaxContext, cancellationToken) => Analysis.CollectSlotField(syntaxContext, cancellationToken))
            .Where(static model => model is not null)
            .Select(static (model, _) => model!);

        IncrementalValueProvider<ImmutableArray<SlotFieldModel>> groupedSlots = slots.Collect();

        context.RegisterSourceOutput(groupedSlots.Combine(context.CompilationProvider), static (productionContext, source) =>
        {
            // 必须按 [MviSlot] 字段所在的 partial class 聚合后逐个 emit：
            // 之前只取 grouped.First() 会让"多个含 [MviSlot] 的 View"项目里只有
            // 字典序首个 View 拿到 OnBindSlots override，其余 View 的槽位全部空着。
            // 这里对每个含 [MviSlot] 的 View 单独调用 AddSource，互不影响。
            Dictionary<INamedTypeSymbol, List<SlotFieldModel>> grouped = new(SymbolEqualityComparer.Default);
            foreach (SlotFieldModel slot in source.Left)
            {
                if (!grouped.TryGetValue(slot.ContainingType, out List<SlotFieldModel>? list))
                {
                    list = new List<SlotFieldModel>();
                    grouped.Add(slot.ContainingType, list);
                }

                list.Add(slot);
            }

            foreach (KeyValuePair<INamedTypeSymbol, List<SlotFieldModel>> group in grouped)
            {
                productionContext.CancellationToken.ThrowIfCancellationRequested();
                SlotGenerationModel? model = Analysis.BuildGenerationModel(group.Value, source.Right, productionContext.CancellationToken);
                if (model is null)
                {
                    continue;
                }

                string generatedSource = Emission.Generate(model);
                productionContext.AddSource(model.HintName, SourceText.From(generatedSource, Encoding.UTF8));
            }
        });
    }

    /// <summary>
    /// 表示 <see cref="MviCompositeSlotBindingGenerator"/> 的分析阶段：
    /// 解析 [MviSlot] 字段、所属 View 类型、ViewModel 类型，决定槽位挂载平台（Avalonia / Godot）。
    /// </summary>
    internal static class Analysis
    {
        private static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.FullyQualifiedFormat
            .WithMiscellaneousOptions(
                SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
                | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        /// <summary>
        /// 收集单个 [MviSlot] 字段的元数据；返回 null 表示应跳过。
        /// </summary>
        public static SlotFieldModel? CollectSlotField(
            GeneratorAttributeSyntaxContext syntaxContext,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // ForAttributeWithMetadataName 对标在字段上的特性返回的 TargetNode 是 VariableDeclaratorSyntax
            //（字段声明 `int a, b;` 里每个变量名都是独立的 VariableDeclaratorSyntax），
            // 而非 FieldDeclarationSyntax。必须向上找到 FieldDeclarationSyntax 才能拿到 partial 修饰符。
            if (syntaxContext.TargetNode is not VariableDeclaratorSyntax variableDeclarator)
            {
                return null;
            }

            FieldDeclarationSyntax? fieldDeclaration = variableDeclarator.FirstAncestorOrSelf<FieldDeclarationSyntax>();
            if (fieldDeclaration is null)
            {
                return null;
            }

            if (syntaxContext.TargetSymbol is not IFieldSymbol fieldSymbol)
            {
                return null;
            }

            INamedTypeSymbol? containingType = fieldSymbol.ContainingType;
            if (containingType is null)
            {
                return null;
            }

            if (!containingType.DeclaredAccessibility.HasFlag(Accessibility.Public))
            {
                return null;
            }

            // [MviSlot] 只允许 partial class 上（源生成器要 emit override），
            // 非 partial class 跳过——避免误 emit 到 sealed 静态类等无意义位置。
            ClassDeclarationSyntax? classDeclaration = fieldDeclaration.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (classDeclaration is null)
            {
                return null;
            }

            if (!classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                return null;
            }

            SlotPlatform platform = DeterminePlatform(containingType);
            if (platform == SlotPlatform.Unknown)
            {
                return null;
            }

            string? factoryName = null;
            IReadOnlyList<string> observes = Array.Empty<string>();

            foreach (AttributeData attribute in syntaxContext.Attributes)
            {
                if (!string.Equals(attribute.AttributeClass?.ToDisplayString(), SlotAttributeMetadataName, System.StringComparison.Ordinal))
                {
                    continue;
                }

                // 构造函数形参：(Type childViewType, string? factory, params string[] observes)
                // 第一个参数为子 View 类型（保留供将来校验），第二参数为 factory 名称，后续为 observes 名称数组。
                if (attribute.ConstructorArguments.Length >= 2)
                {
                    string? candidate = attribute.ConstructorArguments[1].Value as string;
                    if (!string.IsNullOrWhiteSpace(candidate))
                    {
                        factoryName = candidate;
                    }
                }

                if (attribute.ConstructorArguments.Length >= 3)
                {
                    TypedConstant observesArg = attribute.ConstructorArguments[2];
                    if (observesArg.Kind == TypedConstantKind.Array)
                    {
                        List<string> names = new(observesArg.Values.Length);
                        foreach (TypedConstant value in observesArg.Values)
                        {
                            if (value.Value is not string name || string.IsNullOrWhiteSpace(name))
                            {
                                continue;
                            }

                            if (!names.Contains(name, System.StringComparer.Ordinal))
                            {
                                names.Add(name);
                            }
                        }

                        observes = names;
                    }
                }
            }

            if (factoryName is null)
            {
                return null;
            }

            return new SlotFieldModel(
                containingType,
                fieldSymbol.Name,
                FormatFieldType(fieldSymbol.Type),
                factoryName,
                observes);
        }

        private static string FormatFieldType(ITypeSymbol typeSymbol)
        {
            return typeSymbol.ToDisplayString(TypeFormat);
        }

        /// <summary>
        /// 为单个含 <c>[MviSlot]</c> 字段的 View 构建生成模型。
        /// <para>
        /// 调用方需在传入前按 <see cref="SlotFieldModel.ContainingType"/> 自行聚合；
        /// 本方法只处理"同类型多个槽位字段"的单次 emit。
        /// </para>
        /// </summary>
        public static SlotGenerationModel? BuildGenerationModel(
            List<SlotFieldModel> slotsForClass,
            Compilation compilation,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (slotsForClass.Count == 0)
            {
                return null;
            }

            // 同一 partial class 上的所有 [MviSlot] 字段共享一个 ContainingType。
            INamedTypeSymbol containingType = slotsForClass[0].ContainingType;

            SlotPlatform platform = DeterminePlatform(containingType);
            if (platform == SlotPlatform.Unknown)
            {
                return null;
            }

            INamedTypeSymbol? viewModelType = ResolveViewModelType(containingType, platform, compilation, cancellationToken);
            if (viewModelType is null)
            {
                return null;
            }

            string viewModelTypeName = viewModelType.ToDisplayString(TypeFormat);

            // 校验所有 [MviSlot] 字段的 Factory 方法在父 VM 上确实存在。
            List<SlotFieldModel> validatedSlots = new(slotsForClass.Count);
            foreach (SlotFieldModel slot in slotsForClass)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (FindFactoryMethod(viewModelType, slot.FactoryName, compilation, cancellationToken) is not null)
                {
                    validatedSlots.Add(slot);
                }
            }

            if (validatedSlots.Count == 0)
            {
                return null;
            }

            string namespaceName = containingType.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : containingType.ContainingNamespace.ToDisplayString();

            string hintName = containingType.ToDisplayString(TypeFormat)
                .Replace("global::", string.Empty)
                .Replace('.', '_') + ".MviSlot.g.cs";

            return new SlotGenerationModel(
                namespaceName,
                GetAccessibilityText(containingType.DeclaredAccessibility),
                containingType.Name,
                platform,
                viewModelTypeName,
                viewModelType.ToDisplayString(TypeFormat),
                validatedSlots,
                hintName);
        }

        private static INamedTypeSymbol? ResolveViewModelType(
            INamedTypeSymbol viewType,
            SlotPlatform platform,
            Compilation compilation,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string expectedFullyQualified = platform switch
            {
                SlotPlatform.Avalonia => AvaloniaViewBaseFullyQualifiedName,
                SlotPlatform.Godot => GodotViewBaseFullyQualifiedName,
                _ => string.Empty,
            };

            for (INamedTypeSymbol? current = viewType.BaseType; current is not null; current = current.BaseType)
            {
                // ToDisplayString() 默认格式对泛型返回 Name<TParam>（如 MviAvaloniaView<TViewModel>）
                if (string.Equals(current.OriginalDefinition.ToDisplayString(), expectedFullyQualified, System.StringComparison.Ordinal)
                    && current.TypeArguments.Length == 1)
                {
                    return (INamedTypeSymbol)current.TypeArguments[0];
                }
            }

            return null;
        }

        private static IMethodSymbol? FindFactoryMethod(
            INamedTypeSymbol viewModelType,
            string methodName,
            Compilation compilation,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (ISymbol member in viewModelType.GetMembers(methodName))
            {
                if (member is IMethodSymbol method
                    && method.DeclaredAccessibility == Accessibility.Public
                    && method.Parameters.Length == 0)
                {
                    return method;
                }
            }

            return null;
        }

        private static SlotPlatform DeterminePlatform(INamedTypeSymbol viewType)
        {
            for (INamedTypeSymbol? current = viewType.BaseType; current is not null; current = current.BaseType)
            {
                INamedTypeSymbol original = current.OriginalDefinition;
                // ToDisplayString() 默认格式对泛型返回 Name<TParam>（如 MviAvaloniaView<TViewModel>）
                string fullyQualified = original.ToDisplayString();

                if (string.Equals(fullyQualified, AvaloniaViewBaseFullyQualifiedName, System.StringComparison.Ordinal))
                {
                    return SlotPlatform.Avalonia;
                }

                if (string.Equals(fullyQualified, GodotViewBaseFullyQualifiedName, System.StringComparison.Ordinal))
                {
                    return SlotPlatform.Godot;
                }
            }

            return SlotPlatform.Unknown;
        }

        private static string GetAccessibilityText(Accessibility accessibility)
        {
            return accessibility switch
            {
                Accessibility.Public => "public",
                Accessibility.Internal => "internal",
                Accessibility.Private => "private",
                Accessibility.Protected => "protected",
                Accessibility.ProtectedAndInternal => "private protected",
                Accessibility.ProtectedOrInternal => "protected internal",
                _ => "internal",
            };
        }
    }

    /// <summary>
    /// 表示 <see cref="MviCompositeSlotBindingGenerator"/> 的代码发射阶段：
    /// 根据 <see cref="SlotGenerationModel"/> 生成 <c>OnBindSlots</c> override 源码。
    /// </summary>
    internal static class Emission
    {
        /// <summary>
        /// 为单个 View 生成 <c>OnBindSlots</c> override 源码。
        /// </summary>
        public static string Generate(SlotGenerationModel model)
        {
            StringBuilder builder = new();

            builder.AppendLine("// <auto-generated />");
            builder.AppendLine("#nullable enable");
            builder.AppendLine();
            builder.AppendLine("using System;");
            builder.AppendLine("using System.ComponentModel;");
            builder.AppendLine("using MiKiNuo.Mvi.Application.DI;");
            builder.AppendLine("using MiKiNuo.Mvi.Presentation.Disposables;");
            builder.AppendLine("using MiKiNuo.Mvi.Presentation.ViewRegistry;");
            if (model.Platform == SlotPlatform.Godot)
            {
                builder.AppendLine("using Godot;");
            }

            builder.AppendLine();

            if (!string.IsNullOrEmpty(model.NamespaceName))
            {
                builder.Append("namespace ").Append(model.NamespaceName).AppendLine(";");
                builder.AppendLine();
            }

            builder.Append(model.Accessibility)
                .Append(" partial class ")
                .Append(model.ClassName)
                .AppendLine();
            builder.AppendLine("{");

            // 1. IMviViewRegistry 缓存字段
            builder.AppendLine("    private IMviViewRegistry? __mviSlotViewRegistry;");
            builder.AppendLine();

            // 2. OnBindSlots override
            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// 由 MviCompositeSlotBindingGenerator 自动生成：");
            builder.AppendLine("    /// 遍历所有 [MviSlot] 字段，按需解析子 ViewModel → 创建子 View → 挂载到槽位。");
            builder.AppendLine("    /// 父 ViewModel 上 <c>Observes</c> 列出的属性变化时，会重新触发对应槽位的解析与挂载。");
            builder.AppendLine("    /// </summary>");
            builder.Append("    protected override void OnBindSlots(")
                .Append(model.ViewModelTypeName)
                .AppendLine(" viewModel, MviDisposableBag bindings, IMviResolver resolver)");
            builder.AppendLine("    {");
            builder.AppendLine("        ArgumentNullException.ThrowIfNull(viewModel);");
            builder.AppendLine("        ArgumentNullException.ThrowIfNull(bindings);");
            builder.AppendLine("        ArgumentNullException.ThrowIfNull(resolver);");
            builder.AppendLine();
            builder.AppendLine("        __mviSlotViewRegistry ??= resolver.Resolve<IMviViewRegistry>();");
            builder.AppendLine("        IMviViewRegistry registry = __mviSlotViewRegistry;");
            builder.AppendLine();

            foreach (SlotFieldModel slot in model.Slots)
            {
                EmitSlotBinding(builder, model, slot);
                builder.AppendLine();
            }

            builder.AppendLine("    }");
            builder.AppendLine("}");

            return builder.ToString();
        }

        private static void EmitSlotBinding(StringBuilder builder, SlotGenerationModel model, SlotFieldModel slot)
        {
            string slotExpression = "this." + slot.FieldName;
            string rebindMethodName = "__mviRebind_" + slot.FieldName;
            string handlerLocalName = "__mviHandler_" + slot.FieldName;
            string factoryCall = "viewModel." + slot.FactoryName + "()";

            builder.AppendLine("    // ---- Slot: " + slot.FieldName + " ----");
            builder.AppendLine();

            // 局部函数：重新解析子 ViewModel → 创建子 View → 挂载到槽位
            builder.Append("    void ").Append(rebindMethodName).AppendLine("()");
            builder.AppendLine("    {");
            // 槽位字段在 View 构造完成后保证非 null（构造函数负责 FindControl 并赋值），
            // 但字段声明为可空以支持 InitializeComponent 前的默认状态，此处用 ! 抑制空警告。
            builder.AppendLine("        object? childViewModel = " + factoryCall + ";");
            builder.Append("        if (childViewModel is null) { ").Append(ClearSlotExpression(slotExpression + "!", model.Platform)).AppendLine("; return; }");
            builder.AppendLine("        object view = registry.CreateView(childViewModel);");
            builder.Append("        ").Append(MountSlotExpression(slotExpression + "!", model.Platform)).AppendLine(";");
            builder.AppendLine("    }");
            builder.AppendLine();

            if (slot.Observes.Count == 0)
            {
                // 一次性绑定：直接在 OnBindSlots 末尾调用一次 rebind，不订阅 PropertyChanged。
                builder.Append("    ").Append(rebindMethodName).AppendLine("();");
                return;
            }

            // 订阅 PropertyChanged：当 Observes 列出的属性变化时重新触发 rebind。
            // 使用局部变量保存委托引用，便于后续取消订阅。
            builder.Append("    PropertyChangedEventHandler ").Append(handlerLocalName)
                .Append(" = (object? sender, PropertyChangedEventArgs args) =>");
            builder.AppendLine("    {");
            builder.AppendLine("        if (!string.IsNullOrEmpty(args.PropertyName))");
            builder.AppendLine("        {");
            foreach (string propertyName in slot.Observes)
            {
                builder.Append("            if (string.Equals(args.PropertyName, \"")
                    .Append(Escape(propertyName))
                    .Append("\", StringComparison.Ordinal)) { ")
                    .Append(rebindMethodName)
                    .AppendLine("(); return; }");
            }

            builder.AppendLine("            return; // 属性名不在 Observes 列表中，忽略");
            builder.AppendLine("        }");
            builder.Append("        ").Append(rebindMethodName).AppendLine("();");
            builder.AppendLine("    };");
            builder.AppendLine("    viewModel.PropertyChanged += " + handlerLocalName + ";");
            builder.Append("    ").Append(rebindMethodName).AppendLine("();");
            builder.Append("    bindings.Add(() => viewModel.PropertyChanged -= ").Append(handlerLocalName).AppendLine(");");
        }

        private static string MountSlotExpression(string slotExpression, SlotPlatform platform)
        {
            return platform switch
            {
                // Avalonia MviSlotHost.Content 形参是 object，无需强转。
                SlotPlatform.Avalonia => slotExpression + ".Content = view",
                // Godot 端 view 由 IMviViewRegistry（GodotMviViewRegistryAdapter）返回，运行时类型是 Control/Node；
                // IMviViewRegistry.CreateView 的返回类型是 object，需强转为 Node 才能调 Control.AddChild。
                // 之前裸调 slot.AddChild(view) 会触发 CS1503，Godot 编译必失败。
                SlotPlatform.Godot => slotExpression + ".AddChild((Node)view)",
                _ => slotExpression + ".Content = view",
            };
        }

        private static string ClearSlotExpression(string slotExpression, SlotPlatform platform)
        {
            return platform switch
            {
                SlotPlatform.Avalonia => slotExpression + ".Content = null",
                SlotPlatform.Godot => "foreach (Node __n in " + slotExpression + ".GetChildren()) __n.QueueFree()",
                _ => slotExpression + ".Content = null",
            };
        }

        private static string Escape(string text)
        {
            return text.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }

    /// <summary>
    /// 表示一个 [MviSlot] 字段的元数据。
    /// </summary>
    internal sealed class SlotFieldModel
    {
        public SlotFieldModel(
            INamedTypeSymbol containingType,
            string fieldName,
            string fieldTypeName,
            string factoryName,
            IReadOnlyList<string> observes)
        {
            ContainingType = containingType;
            FieldName = fieldName;
            FieldTypeName = fieldTypeName;
            FactoryName = factoryName;
            Observes = observes;
        }

        public INamedTypeSymbol ContainingType { get; }

        public string FieldName { get; }

        public string FieldTypeName { get; }

        public string FactoryName { get; }

        public IReadOnlyList<string> Observes { get; }
    }

    /// <summary>
    /// 表示一个 View 的完整槽位绑定生成模型。
    /// </summary>
    internal sealed class SlotGenerationModel
    {
        public SlotGenerationModel(
            string namespaceName,
            string accessibility,
            string className,
            SlotPlatform platform,
            string viewModelTypeName,
            string viewModelFullTypeName,
            IReadOnlyList<SlotFieldModel> slots,
            string hintName)
        {
            NamespaceName = namespaceName;
            Accessibility = accessibility;
            ClassName = className;
            Platform = platform;
            ViewModelTypeName = viewModelTypeName;
            ViewModelFullTypeName = viewModelFullTypeName;
            Slots = slots;
            HintName = hintName;
        }

        public string NamespaceName { get; }

        public string Accessibility { get; }

        public string ClassName { get; }

        public SlotPlatform Platform { get; }

        public string ViewModelTypeName { get; }

        public string ViewModelFullTypeName { get; }

        public IReadOnlyList<SlotFieldModel> Slots { get; }

        public string HintName { get; }
    }

    /// <summary>
    /// 表示槽位 View 所属平台：Avalonia / Godot / 未识别。
    /// </summary>
    internal enum SlotPlatform
    {
        Unknown = 0,
        Avalonia = 1,
        Godot = 2,
    }
}
