using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MiKiNuo.Mvi.Samples.Avalonia.BuildTime.SourceGeneration;

public sealed partial class AvaloniaSampleDiContainerGenerator
{
    /// <summary>
    /// 生成 <c>CreateWithCore</c> 方法：按类型与参数个数生成 if 分支，避免反射。
    /// </summary>
    private static void AppendCreateWithCore(StringBuilder builder, GenerationModel model)
    {
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// 按构造参数即时构造并返回服务实例的核心实现。");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    private object CreateWithCore(Type serviceType, object[] args)");
        builder.AppendLine("    {");
        builder.AppendLine("        ArgumentNullException.ThrowIfNull(serviceType);");
        builder.AppendLine("        ArgumentNullException.ThrowIfNull(args);");
        builder.AppendLine();

        List<INamedTypeSymbol> types = new List<INamedTypeSymbol>();
        foreach (FeatureInfo feature in model.Features)
        {
            types.Add(feature.ViewModelType);
        }

        if (model.SampleGreetingViewModelType is not null)
        {
            types.Add(model.SampleGreetingViewModelType);
        }

        foreach (INamedTypeSymbol type in types)
        {
            AppendCreateWithCase(builder, type);
        }

        builder.AppendLine("        throw new InvalidOperationException($\"未找到匹配 {args.Length} 个参数的构造函数：{serviceType.FullName}\");");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    /// <summary>
    /// 为单个类型生成 0 参数或 1 个 string 参数的构造分支。
    /// </summary>
    private static void AppendCreateWithCase(StringBuilder builder, INamedTypeSymbol type)
    {
        string typeName = Format(type);

        bool hasParameterless = type.Constructors.Any(static ctor =>
            ctor.DeclaredAccessibility == Accessibility.Public && ctor.Parameters.Length == 0);
        if (hasParameterless)
        {
            builder.Append("        if (serviceType == typeof(").Append(typeName).AppendLine(") && args.Length == 0)");
            builder.AppendLine("        {");
            builder.Append("            return new ").Append(typeName).AppendLine("();");
            builder.AppendLine("        }");
        }

        bool hasStringCtor = type.Constructors.Any(static ctor =>
            ctor.DeclaredAccessibility == Accessibility.Public
            && ctor.Parameters.Length == 1
            && ctor.Parameters[0].Type.SpecialType == SpecialType.System_String);
        if (hasStringCtor)
        {
            builder.Append("        if (serviceType == typeof(").Append(typeName).AppendLine(") && args.Length == 1 && args[0] is string a0)");
            builder.AppendLine("        {");
            builder.Append("            return new ").Append(typeName).AppendLine("(a0);");
            builder.AppendLine("        }");
        }
    }

    private static void AppendNavigateToDashboard(StringBuilder builder, FeatureInfo shell, FeatureInfo? dashboard)
    {
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// 导航到 Dashboard。");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <param name=\"displayName\">显示名称。</param>");
        builder.AppendLine("    /// <param name=\"cancellationToken\">取消标记。</param>");
        builder.AppendLine("    /// <returns>表示异步导航过程的任务。</returns>");
        builder.AppendLine("    public async ValueTask NavigateToDashboardAsync(string displayName, CancellationToken cancellationToken = default)");
        builder.AppendLine("    {");
        if (dashboard is not null)
        {
            builder.Append("        ").Append(dashboard.ViewModelTypeName)
                .AppendLine(" dashboardViewModel = CreateDashboardViewModel(displayName);");
            builder.Append("        ").Append(shell.ViewModelTypeName)
                .AppendLine(" shellViewModel = ResolveShellViewModel();");
            builder.AppendLine("        await shellViewModel.ShowPageAsync(\"Dashboard\", \"HIS/EMR 组合式 Dashboard\").ConfigureAwait(false);");
        }
        else
        {
            builder.Append("        ").Append(shell.ViewModelTypeName)
                .AppendLine(" shellViewModel = ResolveShellViewModel();");
            builder.AppendLine("        await shellViewModel.ShowPageAsync(displayName, displayName).ConfigureAwait(false);");
        }

        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void AppendMediator(StringBuilder builder, FeatureInfo? dashboard, FeatureInfo? clinicalEditor, FeatureInfo? clinicalReminder, FeatureInfo? businessPage)
    {
        string ns = "global::MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Mediator.";

        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// 发送请求并返回响应。");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <typeparam name=\"TRequest\">请求类型。</typeparam>");
        builder.AppendLine("    /// <typeparam name=\"TResponse\">响应类型。</typeparam>");
        builder.AppendLine("    /// <param name=\"request\">请求对象。</param>");
        builder.AppendLine("    /// <param name=\"cancellationToken\">取消标记。</param>");
        builder.AppendLine("    /// <returns>响应对象。</returns>");
        builder.AppendLine("    public async ValueTask<TResponse> SendAsync<TRequest, TResponse>(");
        builder.AppendLine("        TRequest request,");
        builder.AppendLine("        CancellationToken cancellationToken = default)");
        builder.AppendLine("        where TRequest : notnull");
        builder.AppendLine("    {");
        builder.AppendLine("        ArgumentNullException.ThrowIfNull(request);");
        builder.AppendLine();
        EmitMediatorRequestBranches(builder, ns, dashboard, clinicalEditor, clinicalReminder, businessPage);
        builder.AppendLine();
        builder.AppendLine("        throw new InvalidOperationException($\"未注册中介者路由：{typeof(TRequest).FullName} -> {typeof(TResponse).FullName}\");");
        builder.AppendLine("    }");
        builder.AppendLine();

        if (dashboard is not null)
        {
            AppendNavigateDashboardPageAsync(builder, dashboard, ns);
        }
    }

    /// <summary>生成 Mediator 的 3 个请求类型 if 分支。</summary>
    private static void EmitMediatorRequestBranches(
        StringBuilder builder,
        string ns,
        FeatureInfo? dashboard,
        FeatureInfo? clinicalEditor,
        FeatureInfo? clinicalReminder,
        FeatureInfo? businessPage)
    {
        builder.Append("        if (request is ").Append(ns).AppendLine("NavigateDashboardPageRequest navigateRequest)");
        builder.AppendLine("        {");
        builder.AppendLine("            string pageKey = navigateRequest.PageKey;");
        if (dashboard is not null)
        {
            builder.AppendLine("            return (TResponse)(object)await NavigateDashboardPageAsync(pageKey, cancellationToken).ConfigureAwait(false);");
        }
        else
        {
            builder.Append("            return (TResponse)(object)new ").Append(ns).AppendLine("DashboardNavigationResponse(pageKey, true);");
        }

        builder.AppendLine("        }");
        builder.AppendLine();
        builder.Append("        if (request is ").Append(ns).AppendLine("OpenPatientEncounterRequest encounterRequest)");
        builder.AppendLine("        {");
        builder.AppendLine("            string patientName = encounterRequest.PatientName;");
        if (clinicalEditor is not null)
        {
            builder.Append("            if (_clinicalEditorStore is not null) await _clinicalEditorStore.DispatchAsync(new ")
                .Append(clinicalEditor.IntentTypeName)
                .AppendLine(".LoadPatient(patientName), cancellationToken).ConfigureAwait(false);");
        }
        if (clinicalReminder is not null)
        {
            builder.Append("            if (_clinicalReminderStore is not null) await _clinicalReminderStore.DispatchAsync(new ")
                .Append(clinicalReminder.IntentTypeName)
                .AppendLine(".LoadPatient(patientName), cancellationToken).ConfigureAwait(false);");
        }
        builder.Append("            return (TResponse)(object)new ").Append(ns).AppendLine("PatientEncounterResponse(patientName, true);");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.Append("        if (request is ").Append(ns).AppendLine("DashboardComponentInteractionRequest interactionRequest)");
        builder.AppendLine("        {");
        builder.AppendLine("            string source = interactionRequest.SourceComponent;");
        builder.AppendLine("            string action = interactionRequest.ActionKey;");
        if (businessPage is not null)
        {
            builder.Append("            if (_businessCompositePageStore is not null) await _businessCompositePageStore.DispatchAsync(new ")
                .Append(businessPage.IntentTypeName)
                .AppendLine(".AppendInteractionLog($\"子组件 {source} 触发 {action}\"), cancellationToken).ConfigureAwait(false);");
        }
        builder.Append("            return (TResponse)(object)new ").Append(ns).AppendLine("DashboardComponentInteractionResponse(source + \":\" + action, true);");
        builder.AppendLine("        }");
    }

    /// <summary>
    /// 生成 <c>NavigateDashboardPageAsync</c> 方法，返回具体响应类型。
    /// </summary>
    private static void AppendNavigateDashboardPageAsync(StringBuilder builder, FeatureInfo dashboard, string ns)
    {
        builder.Append("    private async ValueTask<").Append(ns).AppendLine("DashboardNavigationResponse> NavigateDashboardPageAsync(");
        builder.AppendLine("        string pageKey,");
        builder.AppendLine("        CancellationToken cancellationToken)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (_dashboardStore is null)");
        builder.AppendLine("        {");
        builder.AppendLine("            CreateDashboardViewModel(string.Empty);");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        (string resolvedPageKey, string title, string description) = ResolveDashboardPageMetadata(pageKey);");
        builder.Append("        await _dashboardStore!.DispatchAsync(new ")
            .Append(dashboard.IntentTypeName)
            .AppendLine(".ShowPage(resolvedPageKey, title, description), cancellationToken).ConfigureAwait(false);");
        builder.Append("        return new ").Append(ns).AppendLine("DashboardNavigationResponse(title, true);");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void AppendResolveCore(
        StringBuilder builder,
        GenerationModel model,
        string containerName,
        FeatureInfo login,
        FeatureInfo shell,
        FeatureInfo? dashboard)
    {
        builder.AppendLine("    private object ResolveCore(Type serviceType)");
        builder.AppendLine("    {");

        EmitResolveCoreServiceBranches(builder, model);
        EmitResolveCoreFeatureBranches(builder, model, login, shell, dashboard);

        builder.AppendLine();
        builder.AppendLine("        throw new InvalidOperationException($\"未注册服务：{serviceType.FullName}\");");
        builder.AppendLine("    }");
        builder.AppendLine();
        EmitGetSingletonHelper(builder);
    }

    /// <summary>生成基础设施服务的 ResolveCore 分支。</summary>
    private static void EmitResolveCoreServiceBranches(StringBuilder builder, GenerationModel model)
    {
        if (model.AuthService is not null)
        {
            builder.Append("        if (serviceType == typeof(").Append(model.AuthService.ServiceTypeName).AppendLine("))");
            builder.AppendLine("        {");
            builder.Append("            return GetSingleton(serviceType, static () => new ")
                .Append(model.AuthService.ImplementationTypeName).AppendLine("());");
            builder.AppendLine("        }");
            builder.AppendLine();
        }

        if (model.LoginNavigationService is not null)
        {
            builder.Append("        if (serviceType == typeof(").Append(Format(model.LoginNavigationService)).AppendLine("))");
            builder.AppendLine("        {");
            builder.AppendLine("            return this;");
            builder.AppendLine("        }");
            builder.AppendLine();
        }

        builder.AppendLine("        if (serviceType == typeof(IMviMediator))");
        builder.AppendLine("        {");
        builder.AppendLine("            return this;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        if (serviceType == typeof(IMviDiagnosticSink))");
        builder.AppendLine("        {");
        builder.AppendLine("            return this;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        if (serviceType == typeof(IMviViewRegistry))");
        builder.AppendLine("        {");
        builder.AppendLine("            return this;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        if (serviceType == typeof(IMviUiDispatcher))");
        builder.AppendLine("        {");
        // AvaloniaMviUiDispatcher 来自 MiKiNuo.Mvi.Platforms.Avalonia 程序集，
        // 该程序集不在本生成器的 Compilation 内可枚举类型范围（[DiService] 跨程序集不可见），
        // 因此显式硬编码以保证示例在 Avalonia 平台上能将状态变更安全地 marshal 到 UI 线程。
        // 若 MviCommandBase 回退到 MviInlineUiDispatcher，Avalonia 的 Button.CanExecuteChanged
        // 处理器会在 R3 订阅线程上访问 button.Command 触发 VerifyAccess 异常。
        // 复用 ResolveUiDispatcher() 缓存的实例，确保容器外部 Resolve<IMviUiDispatcher>()
        // 与 ViewModel 构造函数注入拿到的是同一对象。
        builder.AppendLine("            return GetSingleton(serviceType, ResolveUiDispatcher);");
        builder.AppendLine("        }");
        builder.AppendLine();
    }

    /// <summary>生成功能特性的 ResolveCore 分支。</summary>
    private static void EmitResolveCoreFeatureBranches(
        StringBuilder builder,
        GenerationModel model,
        FeatureInfo login,
        FeatureInfo shell,
        FeatureInfo? dashboard)
    {
        builder.Append("        if (serviceType == typeof(").Append(shell.ViewModelTypeName).AppendLine("))");
        builder.AppendLine("        {");
        builder.AppendLine("            return ResolveShellViewModel();");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.Append("        if (serviceType == typeof(").Append(login.ViewModelTypeName).AppendLine("))");
        builder.AppendLine("        {");
        builder.Append("            return GetSingleton(serviceType, () => new ").Append(login.ViewModelTypeName)
            .AppendLine("(ResolveLoginStore(), ResolveUiDispatcher()));");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.Append("        if (serviceType == typeof(").Append(StoreType(login)).AppendLine("))");
        builder.AppendLine("        {");
        builder.AppendLine("            return ResolveLoginStore();");
        builder.AppendLine("        }");

        if (dashboard is not null)
        {
            builder.AppendLine();
            builder.Append("        if (serviceType == typeof(").Append(dashboard.ViewModelTypeName).AppendLine("))");
            builder.AppendLine("        {");
            builder.AppendLine("            return CreateDashboardViewModel(string.Empty);");
            builder.AppendLine("        }");
            if (model.DashboardPageFactoryTypeName is not null)
            {
                builder.AppendLine();
                builder.Append("        if (serviceType == typeof(").Append(model.DashboardPageFactoryTypeName).AppendLine("))");
                builder.AppendLine("        {");
                builder.AppendLine("            return this;");
                builder.AppendLine("        }");
            }
        }

        if (model.ShellPageFactoryTypeName is not null)
        {
            builder.AppendLine();
            builder.Append("        if (serviceType == typeof(").Append(model.ShellPageFactoryTypeName).AppendLine("))");
            builder.AppendLine("        {");
            builder.AppendLine("            return this;");
            builder.AppendLine("        }");
        }
    }

    /// <summary>生成 GetSingleton 辅助方法。</summary>
    private static void EmitGetSingletonHelper(StringBuilder builder)
    {
        builder.AppendLine("    private object GetSingleton(Type serviceType, Func<object> factory)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (_singletons.TryGetValue(serviceType, out object? instance))");
        builder.AppendLine("        {");
        builder.AppendLine("            return instance;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        object created = factory();");
        builder.AppendLine("        _singletons[serviceType] = created;");
        builder.AppendLine("        return created;");
        builder.AppendLine("    }");
        builder.AppendLine();
    }
}
