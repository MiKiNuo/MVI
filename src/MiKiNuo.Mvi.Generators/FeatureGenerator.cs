using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MiKiNuo.Mvi.Generators;

/// <summary>为功能声明生成强类型状态输入入口。</summary>
[Generator]
public sealed class FeatureGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor ImmutableState = new("MVI2002", "状态必须深层不可变",
        "状态成员 '{0}' 必须深层不可变：仅支持不可变标量、源码声明的密封记录与只读记录结构及已支持的不可变集合", "Mvi", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor InvalidFeature = new("MVI2001", "功能声明无效",
        "功能 '{0}' 必须是顶层、非泛型的 partial 类，并直接继承 Feature<TState>", "Mvi", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor InvalidInput = new("MVI2003", "输入声明无效",
        "输入 '{0}' 必须是 State 的 public 实例属性，具有 public get 与 public init", "Mvi", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor InvalidRule = new("MVI2004", "输入规则签名无效",
        "规则 '{0}' 必须引用一个 Input 属性，并声明为 private static State(State, 属性类型)，参数不能按引用传递、可选或为 params", "Mvi", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor AmbiguousRule = new("MVI2005", "输入规则有歧义",
        "输入 '{0}' 只能声明一个 OnInput 规则", "Mvi", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor MemberConflict = new("MVI2006", "生成入口与现有成员冲突",
        "生成成员 '{0}' 与功能成员或本地投影保留成员冲突", "Mvi", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor InvalidOperation = new("MVI2007", "操作声明无效",
        "操作 '{0}' 必须是 private 实例方法，接受唯一的 Operation<State> 参数并返回 Task<TResult> 或 ValueTask<TResult>，不能为泛型、按引用、可选或 params 声明", "Mvi", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor InvalidValidation = new("MVI2008", "启动验证签名无效",
        "操作 '{0}' 的 Validate 必须引用唯一的 private static bool(State) 纯方法，参数不能按引用、可选或为 params", "Mvi", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor OperationConflict = new("MVI2009", "操作生成入口冲突",
        "操作入口 '{0}' 与功能现有成员或生成输入入口冲突", "Mvi", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor InvalidConcurrency = new("MVI2010", "操作并发配置无效",
        "操作 '{0}' 的并发策略必须是 Reject、Latest、Queue 或 Parallel；Queue 必须声明正数 Capacity，Parallel 必须声明正数 MaxConcurrency，其他策略对应配置必须为零", "Mvi", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor InvalidHandler = new("MVI2011", "请求处理声明无效",
        "请求处理 '{0}' 必须是 private 实例异步方法，接受 Operation<State> 和非空消息参数，不支持泛型、按引用、可选、params 或 ref struct 消息；消息和结果类型必须能在生成入口暴露", "Mvi", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor InvalidHandlerValidation = new("MVI2012", "请求验证无效",
        "请求处理 '{0}' 的 Validate 必须引用唯一的 private static bool(State, Message) 纯方法", "Mvi", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor DuplicateHandler = new("MVI2013", "请求契约重复",
        "请求处理 '{0}' 与本功能的其他处理方法重复提供同一请求与结果契约", "Mvi", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor HandlerConflict = new("MVI2014", "请求端口生成入口冲突",
        "请求端口入口 '{0}' 与已有或其他生成成员冲突", "Mvi", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor InvalidHandlerPolicy = new("MVI2015", "请求端口策略无效",
        "请求处理 '{0}' 的并发界限或取消策略无效", "Mvi", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor FactoryConflict = new("MVI2016", "生成工厂声明冲突",
        "功能 '{0}' 的 CreateAsync 入口冲突或存在多个标记的公开构造函数", "Mvi", DiagnosticSeverity.Error, true);
    /// <summary>注册功能声明的增量生成管线。</summary>
    /// <param name="context">当前增量生成器上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<FeatureModel?> models = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
            static (syntax, _) => CreateModel(syntax)).WithTrackingName("FeatureModels");

        context.RegisterSourceOutput(models, static (production, model) =>
        {
            if (model is not null)
            {
                foreach (Diagnostic diagnostic in model.Diagnostics)
                {
                    production.ReportDiagnostic(diagnostic);
                }
            }
        });

        context.RegisterSourceOutput(models.WithComparer(FeatureModelComparer.Instance).WithTrackingName("FeatureEmission"),
            static (production, model) =>
            {
                if (model is not null && model.Diagnostics.IsEmpty)
                {
                    production.AddSource(model.HintName, SourceText.From(model.Source, Encoding.UTF8));
                }
            });
    }

    private static FeatureModel? CreateModel(GeneratorSyntaxContext context)
    {
        ClassDeclarationSyntax declaration = (ClassDeclarationSyntax)context.Node;
        INamedTypeSymbol? feature = context.SemanticModel.GetDeclaredSymbol(declaration);
        INamedTypeSymbol? featureBase = context.SemanticModel.Compilation.GetTypeByMetadataName("MiKiNuo.Mvi.Feature`1");
        if (feature is null || feature.BaseType is null || featureBase is null
            || !SymbolEqualityComparer.Default.Equals(feature.BaseType.OriginalDefinition, featureBase))
        {
            return null;
        }

        SyntaxReference canonical = feature.DeclaringSyntaxReferences.First(static reference =>
            reference.GetSyntax() is ClassDeclarationSyntax { BaseList: not null });
        if (canonical.SyntaxTree != declaration.SyntaxTree || canonical.Span != declaration.Span)
        {
            return null;
        }

        string hintName = feature.ToDisplayString() + ".Inputs.g.cs";
        ImmutableArray<Diagnostic>.Builder diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        if (feature.ContainingType is not null || feature.Arity != 0
            || feature.DeclaringSyntaxReferences.Any(static reference => reference.GetSyntax() is not ClassDeclarationSyntax syntax
                || !syntax.Modifiers.Any(SyntaxKind.PartialKeyword)))
        {
            diagnostics.Add(Diagnostic.Create(InvalidFeature, declaration.Identifier.GetLocation(), feature.Name));
            return new FeatureModel(hintName, string.Empty, diagnostics.ToImmutable());
        }

        ITypeSymbol state = feature.BaseType.TypeArguments[0];
        Compilation compilation = context.SemanticModel.Compilation;
        INamedTypeSymbol? inputAttribute = compilation.GetTypeByMetadataName("MiKiNuo.Mvi.InputAttribute");
        INamedTypeSymbol? ruleAttribute = compilation.GetTypeByMetadataName("MiKiNuo.Mvi.OnInputAttribute");
        Dictionary<string, IPropertySymbol> inputs = new(StringComparer.Ordinal);
        if (state is INamedTypeSymbol namedState)
        {
            HashSet<string> names = new(StringComparer.Ordinal);
            for (INamedTypeSymbol? current = namedState; current is not null; current = current.BaseType)
            {
                foreach (ISymbol member in current.GetMembers())
                {
                    AttributeData? attribute = FindAttribute(member, inputAttribute);
                    if (!names.Add(member.Name) || attribute is null)
                    {
                        continue;
                    }

                    if (member is not IPropertySymbol property || property.IsStatic || property.IsIndexer
                        || property.DeclaredAccessibility != Accessibility.Public
                        || property.GetMethod?.DeclaredAccessibility != Accessibility.Public
                        || property.SetMethod?.DeclaredAccessibility != Accessibility.Public || !property.SetMethod.IsInitOnly)
                    {
                        diagnostics.Add(Diagnostic.Create(InvalidInput, AttributeLocation(attribute, member), member.Name));
                        continue;
                    }

                    inputs.Add(property.Name, property);
                }
            }
        }

        if (!StateShapeValidation.IsImmutable(state, compilation,
            new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default), out ISymbol invalid))
        {
            diagnostics.Add(Diagnostic.Create(ImmutableState, invalid.Locations.FirstOrDefault(static location => location.IsInSource)
                ?? declaration.Identifier.GetLocation(), invalid.Name));
        }

        Dictionary<string, IMethodSymbol> rules = new(StringComparer.Ordinal);
        INamedTypeSymbol? operationAttribute = compilation.GetTypeByMetadataName("MiKiNuo.Mvi.OperationAttribute");
        IMethodSymbol[] operations = feature.GetMembers().OfType<IMethodSymbol>()
            .Where(method => FindAttribute(method, operationAttribute) is not null).ToArray();
        INamedTypeSymbol? operationType = compilation.GetTypeByMetadataName("MiKiNuo.Mvi.Operation`1");
        INamedTypeSymbol? taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        INamedTypeSymbol? valueTaskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
        INamedTypeSymbol? handlerAttribute = compilation.GetTypeByMetadataName("MiKiNuo.Mvi.RequestHandlerAttribute");
        IMethodSymbol[] handlers = feature.GetMembers().OfType<IMethodSymbol>()
            .Where(method => FindAttribute(method, handlerAttribute) is not null).ToArray();
        List<(ITypeSymbol Message, ITypeSymbol Result)> contracts = [];
        foreach (IMethodSymbol handler in handlers)
        {
            AttributeData attribute = FindAttribute(handler, handlerAttribute)!;
            int concurrency = NamedInt(attribute, "Concurrency");
            int capacity = NamedInt(attribute, "Capacity");
            int maxConcurrency = NamedInt(attribute, "MaxConcurrency");
            if (concurrency is < 0 or > 3 || (concurrency == 2 ? capacity <= 0 : capacity != 0)
                || (concurrency == 3 ? maxConcurrency <= 0 : maxConcurrency != 0)
                || NamedInt(attribute, "CancellationPolicy") is < 0 or > 1)
            {
                diagnostics.Add(Diagnostic.Create(InvalidHandlerPolicy, AttributeLocation(attribute, handler), handler.Name));
            }

            if (!HasOrdinarySignature(handler) || handler.IsStatic || handler.IsAbstract || handler.IsExtern
                || handler.Parameters.Length != 2 || handler.Parameters[0].Type is not INamedTypeSymbol parameter
                || !SymbolEqualityComparer.Default.Equals(parameter.OriginalDefinition, operationType)
                || !SymbolEqualityComparer.IncludeNullability.Equals(parameter.TypeArguments[0], state)
                || parameter.NullableAnnotation == NullableAnnotation.Annotated
                || handler.Parameters[1].Type.NullableAnnotation == NullableAnnotation.Annotated
                || handler.Parameters[1].Type.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer
                || handler.Parameters[1].Type is INamedTypeSymbol { IsRefLikeType: true }
                || handler.ReturnType is not INamedTypeSymbol result
                || !(SymbolEqualityComparer.Default.Equals(result.OriginalDefinition, taskType)
                    || SymbolEqualityComparer.Default.Equals(result.OriginalDefinition, valueTaskType))
                || !IsAccessibleContract(handler.Parameters[1].Type, feature.DeclaredAccessibility == Accessibility.Public)
                || !IsAccessibleContract(result.TypeArguments[0], feature.DeclaredAccessibility == Accessibility.Public))
            {
                diagnostics.Add(Diagnostic.Create(InvalidHandler, AttributeLocation(attribute, handler), handler.Name));
                continue;
            }

            ITypeSymbol message = handler.Parameters[1].Type;
            ITypeSymbol response = result.TypeArguments[0];
            if (contracts.Any(contract => SymbolEqualityComparer.Default.Equals(contract.Message, message)
                && SymbolEqualityComparer.Default.Equals(contract.Result, response)))
            {
                diagnostics.Add(Diagnostic.Create(DuplicateHandler, AttributeLocation(attribute, handler), handler.Name));
            }
            contracts.Add((message, response));
            string? validate = NamedString(attribute, "Validate");
            if (validate is not null && feature.GetMembers(validate).OfType<IMethodSymbol>().Count(method =>
                HasOrdinarySignature(method) && method.IsStatic && method.ReturnType.SpecialType == SpecialType.System_Boolean
                && method.Parameters.Length == 2 && SymbolEqualityComparer.IncludeNullability.Equals(method.Parameters[0].Type, state)
                && SymbolEqualityComparer.IncludeNullability.Equals(method.Parameters[1].Type, message)) != 1)
            {
                diagnostics.Add(Diagnostic.Create(InvalidHandlerValidation, AttributeLocation(attribute, handler), handler.Name));
            }

            string name = "Create" + handler.Name + "Port";
            if (feature.Name == name || feature.GetMembers(name).Length != 0
                || inputs.Values.Any(property => "Set" + property.Name == name)
                || operations.Any(operation => operation.Name == name)
                || handlers.Count(other => other.Name == handler.Name) > 1)
            {
                diagnostics.Add(Diagnostic.Create(HandlerConflict, AttributeLocation(attribute, handler), name));
            }
        }

        if (!feature.IsAbstract)
        {
            ISymbol? conflict = feature.GetMembers("CreateAsync").FirstOrDefault();
            if (conflict is not null || feature.Name == "CreateAsync" || operations.Any(operation => operation.Name == "CreateAsync"))
            {
                diagnostics.Add(Diagnostic.Create(FactoryConflict, conflict?.Locations.FirstOrDefault() ?? declaration.Identifier.GetLocation(), feature.Name));
            }
            INamedTypeSymbol? preferred = compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructorAttribute");
            IMethodSymbol[] marked = feature.InstanceConstructors.Where(constructor => constructor.DeclaredAccessibility == Accessibility.Public
                && FindAttribute(constructor, preferred) is not null).ToArray();
            if (marked.Length > 1)
            {
                foreach (IMethodSymbol constructor in marked)
                    diagnostics.Add(Diagnostic.Create(FactoryConflict, AttributeLocation(FindAttribute(constructor, preferred)!, constructor), feature.Name));
            }
        }
        foreach (IMethodSymbol operation in operations)
        {
            AttributeData attribute = FindAttribute(operation, operationAttribute)!;
            int concurrency = attribute.NamedArguments.FirstOrDefault(static pair => pair.Key == "Concurrency").Value.Value is int strategy ? strategy : 0;
            int capacity = attribute.NamedArguments.FirstOrDefault(static pair => pair.Key == "Capacity").Value.Value is int limit ? limit : 0;
            int maxConcurrency = (int?)attribute.NamedArguments.FirstOrDefault(static pair => pair.Key == "MaxConcurrency").Value.Value ?? 0;
            if (concurrency is < 0 or > 3
                || (concurrency == 2 ? capacity <= 0 : capacity != 0)
                || (concurrency == 3 ? maxConcurrency <= 0 : maxConcurrency != 0))
            {
                diagnostics.Add(Diagnostic.Create(InvalidConcurrency, AttributeLocation(attribute, operation), operation.Name));
            }

            if (!HasOrdinarySignature(operation) || operation.IsStatic || operation.IsAbstract || operation.IsExtern
                || operation.Parameters.Length != 1 || operation.Parameters[0].Type is not INamedTypeSymbol parameter
                || !SymbolEqualityComparer.Default.Equals(parameter.OriginalDefinition, operationType)
                || !SymbolEqualityComparer.IncludeNullability.Equals(parameter.TypeArguments[0], state)
                || parameter.NullableAnnotation == NullableAnnotation.Annotated
                || operation.ReturnType is not INamedTypeSymbol result
                || !(SymbolEqualityComparer.Default.Equals(result.OriginalDefinition, taskType)
                    || SymbolEqualityComparer.Default.Equals(result.OriginalDefinition, valueTaskType)))
            {
                diagnostics.Add(Diagnostic.Create(InvalidOperation, AttributeLocation(attribute, operation), operation.Name));
                continue;
            }

            string? validationName = attribute.NamedArguments.FirstOrDefault(static pair => pair.Key == "Validate").Value.Value as string;
            if (validationName is not null && feature.GetMembers(validationName).OfType<IMethodSymbol>().Count(method =>
                HasOrdinarySignature(method) && method.IsStatic && method.ReturnType.SpecialType == SpecialType.System_Boolean
                && method.Parameters.Length == 1 && SymbolEqualityComparer.IncludeNullability.Equals(method.Parameters[0].Type, state)) != 1)
            {
                diagnostics.Add(Diagnostic.Create(InvalidValidation, AttributeLocation(attribute, operation), operation.Name));
            }

            if (feature.Name == operation.Name || feature.BaseType.GetMembers(operation.Name).Length != 0
                || inputs.Values.Any(property => "Set" + property.Name == operation.Name)
                || feature.GetMembers(operation.Name).Any(member => !SymbolEqualityComparer.Default.Equals(member, operation)))
            {
                diagnostics.Add(Diagnostic.Create(OperationConflict, AttributeLocation(attribute, operation), operation.Name));
            }
        }
        foreach (IMethodSymbol method in feature.GetMembers().OfType<IMethodSymbol>())
        {
            foreach (AttributeData attribute in method.GetAttributes().Where(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, ruleAttribute)))
            {
                string? propertyName = attribute.ConstructorArguments.Length == 1 ? attribute.ConstructorArguments[0].Value as string : null;
                if (propertyName is null || !inputs.TryGetValue(propertyName, out IPropertySymbol? property)
                    || !IsValidRule(method, state, property.Type))
                {
                    diagnostics.Add(Diagnostic.Create(InvalidRule, AttributeLocation(attribute, method), method.Name));
                }
                else if (rules.ContainsKey(propertyName))
                {
                    diagnostics.Add(Diagnostic.Create(AmbiguousRule, AttributeLocation(attribute, method), propertyName));
                }
                else
                {
                    rules.Add(propertyName, method);
                }
            }
        }

        foreach (IPropertySymbol property in inputs.Values)
        {
            string entryName = "Set" + property.Name;
            if (feature.Name == entryName)
            {
                diagnostics.Add(Diagnostic.Create(MemberConflict, declaration.Identifier.GetLocation(), entryName));
            }
            else if (feature.GetMembers(entryName).Length != 0)
            {
                diagnostics.Add(Diagnostic.Create(MemberConflict, feature.GetMembers(entryName)[0].Locations[0], entryName));
            }
        }

        Dictionary<string, IPropertySymbol> properties = new(StringComparer.Ordinal);
        if (state is INamedTypeSymbol projectionState)
        {
            for (INamedTypeSymbol? current = projectionState; current is not null; current = current.BaseType)
            {
                foreach (IPropertySymbol property in current.GetMembers().OfType<IPropertySymbol>())
                {
                    if (!property.IsStatic && !property.IsIndexer && property.DeclaredAccessibility == Accessibility.Public
                        && property.GetMethod?.DeclaredAccessibility == Accessibility.Public && !properties.ContainsKey(property.Name))
                    {
                        properties.Add(property.Name, property);
                    }
                }
            }
        }

        foreach (string name in new[] { "CreateProjection", "Projection" })
        {
            ISymbol? conflict = feature.GetMembers(name).FirstOrDefault();
            if (conflict is not null || feature.Name == name)
            {
                diagnostics.Add(Diagnostic.Create(MemberConflict, conflict?.Locations[0] ?? declaration.Identifier.GetLocation(), name));
            }
        }

        foreach (IPropertySymbol property in properties.Values)
        {
            if (new[] { "Snapshot", "PropertyChanged", "Dispose", "Projection", "OnSnapshotChanged", "NotifyPropertyChanged",
                "InitializeProjection", "EnsureActive", "SetProjectionInput", "TakeInputFeedback", "__feature" }.Contains(property.Name, StringComparer.Ordinal))
            {
                diagnostics.Add(Diagnostic.Create(MemberConflict, property.Locations[0], property.Name));
            }
        }

        foreach (IMethodSymbol operation in operations)
        {
            if (properties.ContainsKey(operation.Name + "Command"))
            {
                diagnostics.Add(Diagnostic.Create(OperationConflict, operation.Locations[0], operation.Name + "Command"));
            }
        }

        if (diagnostics.Count != 0)
        {
            return new FeatureModel(hintName, string.Empty, diagnostics.ToImmutable());
        }

        StringBuilder source = new("// <auto-generated/>\n#nullable enable\n");
        if (!feature.ContainingNamespace.IsGlobalNamespace)
        {
            source.Append("namespace ").Append(feature.ContainingNamespace.ToDisplayString()).Append(";\n");
        }

        source.Append(feature.DeclaredAccessibility == Accessibility.Public ? "public" : "internal")
            .Append(" partial class @").Append(feature.Name).Append("\n{\n");
        if (!feature.IsAbstract)
        {
            string concrete = feature.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            source.Append("    /// <summary>使用标准 DI 为本功能创建独立实例与服务范围。</summary>\n")
                .Append("    /// <param name=\"services\">宿主拥有的根服务提供方。</param>\n")
                .Append("    /// <returns>拥有独立范围的功能实例，失败清理复用框架工厂。</returns>\n")
                .Append("    public static global::System.Threading.Tasks.ValueTask<").Append(concrete)
                .Append("> CreateAsync(global::System.IServiceProvider services) => global::MiKiNuo.Mvi.FeatureFactory.CreateAsync<")
                .Append(concrete).Append(">(services);\n");
        }
        foreach (IMethodSymbol handler in handlers.OrderBy(static method => method.Name, StringComparer.Ordinal))
        {
            AttributeData attribute = FindAttribute(handler, handlerAttribute)!;
            string message = TypeName(handler.Parameters[1].Type);
            string response = TypeName(((INamedTypeSymbol)handler.ReturnType).TypeArguments[0]);
            string? validate = NamedString(attribute, "Validate");
            source.Append("    /// <summary>创建复用统一操作入口的强类型请求端口。</summary>\n")
                .Append("    /// <returns>由宿主明确接线的独立实例端口。</returns>\n")
                .Append("    public global::MiKiNuo.Mvi.RequestPort<").Append(message).Append(", ").Append(response)
                .Append("> Create").Append(handler.Name).Append("Port() => base.CreateRequestPort<")
                .Append(message).Append(", ").Append(response).Append(">(\"").Append(handler.Name).Append("\", ")
                .Append(validate is null ? "null" : "@" + validate).Append(", ");
            if (SymbolEqualityComparer.Default.Equals(((INamedTypeSymbol)handler.ReturnType).OriginalDefinition, taskType))
                source.Append("(operation, message) => new global::System.Threading.Tasks.ValueTask<").Append(response).Append(">(this.@").Append(handler.Name).Append("(operation, message))");
            else source.Append('@').Append(handler.Name);
            source.Append(", (global::MiKiNuo.Mvi.OperationConcurrency)").Append(NamedInt(attribute, "Concurrency"))
                .Append(", capacity: ").Append(NamedInt(attribute, "Capacity")).Append(", maxConcurrency: ").Append(NamedInt(attribute, "MaxConcurrency"))
                .Append(", cancellationPolicy: (global::MiKiNuo.Mvi.RequestCancellationPolicy)").Append(NamedInt(attribute, "CancellationPolicy")).Append(");\n");
        }
        foreach (IPropertySymbol property in inputs.Values.OrderBy(static property => property.Name, StringComparer.Ordinal))
        {
            rules.TryGetValue(property.Name, out IMethodSymbol? rule);
            string valueType = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
                SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier));
            source.Append("    /// <summary>将输入提交到实例状态。</summary>\n    /// <param name=\"value\">输入值。</param>\n")
                .Append("    public void Set").Append(property.Name).Append('(').Append(valueType).Append(" value)\n")
                .Append("        => base.DispatchInput(value, static (state, input) => ");
            source.Append(rule is null ? "state with { @" + property.Name + " = input }" : "@" + rule.Name + "(state, input)")
                .Append(");\n");
        }

        string stateType = state.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        source.Append("    /// <summary>创建该功能唯一活动 View 的强类型本地投影，释放后可重新连接。</summary>\n")
            .Append("    /// <param name=\"schedule\">安排到所属 UI 线程的原生调度入口。</param>\n")
            .Append("    /// <param name=\"mode\">等待展示的合并方式。</param>\n")
            .Append("    /// <returns>供该 View 原生绑定使用的投影。</returns>\n")
            .Append("    public Projection CreateProjection(global::System.Action<global::System.Action> schedule, global::MiKiNuo.Mvi.ProjectionMode mode = global::MiKiNuo.Mvi.ProjectionMode.Coalesce) => new(this, schedule, mode);\n")
            .Append("    /// <summary>生成的强类型本地绑定，不供功能间业务通信使用。</summary>\n")
            .Append("    public sealed class Projection : global::MiKiNuo.Mvi.FeatureProjection<").Append(stateType).Append(">\n    {\n")
            .Append("        private readonly @").Append(feature.Name).Append(" __feature;\n")
            .Append("        internal Projection(@").Append(feature.Name).Append(" feature, global::System.Action<global::System.Action> schedule, global::MiKiNuo.Mvi.ProjectionMode mode) : base(feature, schedule, mode)\n")
            .Append("        {\n            __feature = feature;\n");
        foreach (IMethodSymbol operation in operations.OrderBy(static method => method.Name, StringComparer.Ordinal))
        {
            AttributeData attribute = FindAttribute(operation, operationAttribute)!;
            string? validate = attribute.NamedArguments.FirstOrDefault(static pair => pair.Key == "Validate").Value.Value as string;
            int concurrency = (int?)attribute.NamedArguments.FirstOrDefault(static pair => pair.Key == "Concurrency").Value.Value ?? 0;
            int capacity = (int?)attribute.NamedArguments.FirstOrDefault(static pair => pair.Key == "Capacity").Value.Value ?? 0;
            int maxConcurrency = (int?)attribute.NamedArguments.FirstOrDefault(static pair => pair.Key == "MaxConcurrency").Value.Value ?? 0;
            source.Append("            @").Append(operation.Name).Append("Command = CreateOperationCommand(\"")
                .Append(operation.Name).Append("\", ").Append(validate is null ? "null" : "@" + validate)
                .Append(", () => __feature.@").Append(operation.Name)
                .Append("(), (global::MiKiNuo.Mvi.OperationConcurrency)").Append(concurrency)
                .Append(", capacity: ").Append(capacity).Append(", maxConcurrency: ").Append(maxConcurrency).Append(");\n");
        }

        source.Append("            InitializeProjection();\n        }\n");
        foreach (IMethodSymbol operation in operations.OrderBy(static method => method.Name, StringComparer.Ordinal))
        {
            string resultType = ((INamedTypeSymbol)operation.ReturnType).TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
                SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier));
            source.Append("        /// <summary>通过统一验证入口执行业务操作的原生命令。</summary>\n")
                .Append("        public global::MiKiNuo.Mvi.OperationCommand<").Append(resultType).Append("> @")
                .Append(operation.Name).Append("Command { get; }\n");
        }
        foreach (IPropertySymbol property in properties.Values.OrderBy(static property => property.Name, StringComparer.Ordinal))
        {
            string valueType = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
                SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier));
            source.Append("        /// <summary>读取已展示快照中的属性").Append(property.Name).Append("。</summary>\n")
                .Append("        public ").Append(valueType).Append(" @").Append(property.Name).Append("\n        {\n")
                .Append("            get => Snapshot.State.@").Append(property.Name).Append(";\n");
            if (inputs.ContainsKey(property.Name))
            {
                source.Append("            set => SetProjectionInput(nameof(@").Append(property.Name).Append("), value, __feature, static (feature, input) => feature.Set").Append(property.Name).Append("(input));\n");
            }

            source.Append("        }\n");
        }

        source.Append("        /// <summary>按有关字段比较两个已提交状态。</summary>\n")
            .Append("        /// <param name=\"previous\">上次展示状态。</param>\n        /// <param name=\"current\">当前展示状态。</param>\n")
            .Append("        protected override void OnSnapshotChanged(").Append(stateType).Append(" previous, ").Append(stateType).Append(" current)\n        {\n");
        foreach (IPropertySymbol property in properties.Values.OrderBy(static property => property.Name, StringComparer.Ordinal))
        {
            string valueType = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
                SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier));
            source.Append("            if (");
            if (inputs.ContainsKey(property.Name))
            {
                source.Append("TakeInputFeedback(nameof(@").Append(property.Name).Append(")) || ");
            }

            source.Append("!global::System.Collections.Generic.EqualityComparer<").Append(valueType)
                .Append(">.Default.Equals(previous.@").Append(property.Name).Append(", current.@").Append(property.Name).Append("))\n")
                .Append("                NotifyPropertyChanged(nameof(@").Append(property.Name).Append("));\n");
        }

        source.Append("        }\n    }\n");

        foreach (IMethodSymbol operation in operations.OrderBy(static method => method.Name, StringComparer.Ordinal))
        {
            AttributeData attribute = FindAttribute(operation, operationAttribute)!;
            string? validate = attribute.NamedArguments.FirstOrDefault(static pair => pair.Key == "Validate").Value.Value as string;
            string resultType = ((INamedTypeSymbol)operation.ReturnType).TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
                SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier));
            source.Append("    /// <summary>启动业务操作并等待有关状态提交。</summary>\n")
                .Append("    /// <param name=\"cancellationToken\">执行的协作取消令牌。</param>\n")
                .Append("    /// <returns>操作执行结果与强类型业务返回值。</returns>\n")
                .Append("    public global::System.Threading.Tasks.Task<global::MiKiNuo.Mvi.OperationResult<").Append(resultType)
                .Append(">> @").Append(operation.Name).Append("(global::System.Threading.CancellationToken cancellationToken = default)\n")
                .Append("        => base.DispatchOperation<").Append(resultType).Append(">(\"").Append(operation.Name).Append("\", ")
                .Append(validate is null ? "null" : "@" + validate).Append(", ");
            if (SymbolEqualityComparer.Default.Equals(((INamedTypeSymbol)operation.ReturnType).OriginalDefinition, taskType))
            {
                source.Append("operation => new global::System.Threading.Tasks.ValueTask<").Append(resultType)
                    .Append(">(@").Append(operation.Name).Append("(operation))");
            }
            else
            {
                source.Append('@').Append(operation.Name);
            }

            int concurrency = attribute.NamedArguments.FirstOrDefault(static pair => pair.Key == "Concurrency").Value.Value is int strategy ? strategy : 0;
            int capacity = attribute.NamedArguments.FirstOrDefault(static pair => pair.Key == "Capacity").Value.Value is int limit ? limit : 0;
            int maxConcurrency = (int?)attribute.NamedArguments.FirstOrDefault(static pair => pair.Key == "MaxConcurrency").Value.Value ?? 0;
            source.Append(", cancellationToken, (global::MiKiNuo.Mvi.OperationConcurrency)").Append(concurrency)
                .Append(", capacity: ").Append(capacity).Append(", maxConcurrency: ").Append(maxConcurrency).Append(");\n");
        }

        source.Append("}\n");
        return new FeatureModel(hintName, source.ToString(), []);
    }

    private static AttributeData? FindAttribute(ISymbol symbol, INamedTypeSymbol? attributeType)
        => symbol.GetAttributes().FirstOrDefault(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType));

    private static int NamedInt(AttributeData attribute, string name)
        => (int?)attribute.NamedArguments.FirstOrDefault(pair => pair.Key == name).Value.Value ?? 0;
    private static string? NamedString(AttributeData attribute, string name)
        => attribute.NamedArguments.FirstOrDefault(pair => pair.Key == name).Value.Value as string;
    private static string TypeName(ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
        SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier));

    private static bool IsAccessibleContract(ITypeSymbol type, bool requirePublic)
    {
        if (type is IArrayTypeSymbol array) return IsAccessibleContract(array.ElementType, requirePublic);
        if (type is not INamedTypeSymbol named) return true;
        if (named.DeclaredAccessibility != Accessibility.Public
            && (requirePublic || named.DeclaredAccessibility is not (Accessibility.Internal or Accessibility.ProtectedOrInternal)))
        {
            return false;
        }

        return (named.ContainingType is null || IsAccessibleContract(named.ContainingType, requirePublic))
            && named.TypeArguments.All(argument => IsAccessibleContract(argument, requirePublic));
    }

    private static Location AttributeLocation(AttributeData attribute, ISymbol symbol)
        => attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? symbol.Locations[0];

    private static bool IsValidRule(IMethodSymbol method, ITypeSymbol state, ITypeSymbol value)
        => method.IsStatic && method.DeclaredAccessibility == Accessibility.Private && method.Arity == 0
            && !method.ReturnsByRef && !method.ReturnsByRefReadonly
            && SymbolEqualityComparer.IncludeNullability.Equals(method.ReturnType, state)
            && method.Parameters.Length == 2
            && method.Parameters.All(static parameter => parameter.RefKind == RefKind.None && !parameter.IsOptional && !parameter.IsParams)
            && SymbolEqualityComparer.IncludeNullability.Equals(method.Parameters[0].Type, state)
            && SymbolEqualityComparer.IncludeNullability.Equals(method.Parameters[1].Type, value);

    private sealed class FeatureModel(string hintName, string source, ImmutableArray<Diagnostic> diagnostics)
    {
        internal string HintName { get; } = hintName;
        internal string Source { get; } = source;
        internal ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;
    }

    private static bool HasOrdinarySignature(IMethodSymbol method)
        => method.MethodKind == MethodKind.Ordinary && method.DeclaredAccessibility == Accessibility.Private && method.Arity == 0
            && !method.ReturnsByRef && !method.ReturnsByRefReadonly
            && method.Parameters.All(static parameter => parameter.RefKind == RefKind.None && !parameter.IsOptional && !parameter.IsParams);

    private sealed class FeatureModelComparer : IEqualityComparer<FeatureModel?>
    {
        internal static readonly FeatureModelComparer Instance = new();

        public bool Equals(FeatureModel? x, FeatureModel? y)
            => x?.HintName == y?.HintName && x?.Source == y?.Source;

        public int GetHashCode(FeatureModel? obj) => obj?.Source.GetHashCode() ?? 0;
    }
}
