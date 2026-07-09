using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MiKiNuo.Mvi.Samples.Avalonia.BuildTime.SourceGeneration;

public sealed partial class AvaloniaSampleDiContainerGenerator
{
    private static void AppendStoreHelpers(StringBuilder builder, GenerationModel model, FeatureInfo login, FeatureInfo shell)
    {
        builder.Append("    private ").Append(shell.ViewModelTypeName).AppendLine(" ResolveShellViewModel()");
        builder.AppendLine("    {");
        builder.Append("        return (").Append(shell.ViewModelTypeName).Append(")GetSingleton(typeof(")
            .Append(shell.ViewModelTypeName).AppendLine("), CreateShellViewModel);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.Append("    private ").Append(shell.ViewModelTypeName).AppendLine(" CreateShellViewModel()");
        builder.AppendLine("    {");
        builder.Append("        ").Append(StoreType(shell)).Append(" store = ");
        EmitStoreConstructionHeader(builder, shell);
        builder.Append("            ").Append(shell.StateTypeName).AppendLine(".Initial,");
        EmitStoreReducerArgs(builder, shell);
        builder.Append("            ").Append(CreateDispatcherExpression(shell)).AppendLine(");");
        builder.Append("        return new ").Append(shell.ViewModelTypeName).AppendLine("(store, this, ResolveUiDispatcher());");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.Append("    private ").Append(StoreType(login)).AppendLine(" ResolveLoginStore()");
        builder.AppendLine("    {");
        builder.Append("        return (").Append(StoreType(login)).Append(")GetSingleton(typeof(")
            .Append(StoreType(login)).AppendLine("), CreateLoginStore);");
        builder.AppendLine("    }");
        builder.AppendLine();
        AppendLoginStoreFactory(builder, model, login, "    ");
    }

    private static void AppendLoginStoreFactory(StringBuilder builder, GenerationModel model, FeatureInfo login, string indent)
    {
        builder.Append(indent).Append("private ").Append(StoreType(login)).Append(" CreateLoginStore()");
        builder.Append(indent).AppendLine("{");
        // 服务契约可能位于与 ViewModel 不同的命名空间（如 IAuthService 下沉到 Shared 项目），
        // 因此必须使用源生成器已发现的真实类型全名，而非从 ViewModel 命名空间推导。
        string loginNavigationServiceTypeName = model.LoginNavigationService is not null
            ? Format(model.LoginNavigationService)
            : "global::" + login.ViewModelType.ContainingNamespace.ToDisplayString() + ".ILoginNavigationService";
        string authServiceTypeName = model.AuthService is not null
            ? model.AuthService.ServiceTypeName
            : "global::" + login.ViewModelType.ContainingNamespace.ToDisplayString() + ".IAuthService";
        // 经典 MVI 模式：IntentHandler 持有 AuthService 执行异步登录，EffectDispatcher 仅持有导航服务。
        builder.Append(indent).Append("    ").Append(login.DispatcherTypeName).Append(" dispatcher = new(");
        builder.Append("Resolve<").Append(loginNavigationServiceTypeName).Append(">());").AppendLine();
        builder.Append(indent).Append("    return new MviStore<").Append(login.StateTypeName).Append(", ")
            .Append(login.IntentTypeName).Append(", ").Append(login.EffectTypeName).AppendLine(">(");
        builder.Append(indent).Append("        ").Append(login.StateTypeName).AppendLine(".Initial,");
        builder.Append(indent).Append("        new ").Append(login.IntentHandlerTypeName).Append("(");
        builder.Append("Resolve<").Append(authServiceTypeName).Append(">()),");
        builder.Append(indent).Append("        new ").Append(login.ReducerTypeName).AppendLine("(),");
        builder.Append(indent).AppendLine("        dispatcher);");
        builder.Append(indent).AppendLine("}");
        builder.AppendLine();
    }

    private static void AppendDashboardHelpers(
        StringBuilder builder,
        GenerationModel model,
        FeatureInfo? dashboard)
    {
        if (dashboard is null)
        {
            return;
        }

        FeatureInfo? dashboardMenu = model.GetFeature("DashboardMenu");

        EmitCreateDashboardViewModel(builder, dashboard);
        AppendKnownFeatureFactory(builder, dashboardMenu, "CreateDashboardMenuViewModel", dashboardMenu?.StateTypeName + ".Initial");
        EmitCreateHeaderViewModel(builder, model);
        EmitCreateOutpatientWorkstationViewModel(builder, model);
        EmitDashboardKnownFactories(builder, model);
        EmitDashboardCompositeFactories(builder, model);
    }

    /// <summary>生成 CreateDashboardViewModel 方法。</summary>
    private static void EmitCreateDashboardViewModel(StringBuilder builder, FeatureInfo dashboard)
    {
        builder.Append("    private ").Append(dashboard.ViewModelTypeName).AppendLine(" CreateDashboardViewModel(string displayName)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (_dashboardViewModel is not null)");
        builder.AppendLine("        {");
        builder.AppendLine("            return _dashboardViewModel;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        global::MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Menu.DashboardMenuViewModel menuViewModel = CreateDashboardMenuViewModel();");
        builder.AppendLine("        global::MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Header.HeaderViewModel menuHeaderViewModel = CreateHeaderViewModel(displayName);");
        // 父 VM 不再直接持有 Menu/Header 子 VM 引用；改用 IDashboardChromeFactory 工厂封装 2 个 chrome 子 VM，
        // 由父 VM 在 View 按需解析时通过工厂方法获取，避免"VM-in-VM"反模式。
        builder.AppendLine("        global::MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.DashboardChromeFactory chromeFactory = new(menuViewModel, displayName => CreateHeaderViewModel(displayName));");
        builder.Append("        _dashboardStore = ");
        EmitStoreConstructionHeader(builder, dashboard);
        builder.Append("            new ").Append(dashboard.StateTypeName).AppendLine("(");
        builder.AppendLine("                displayName,");
        builder.AppendLine("                \"门诊工作站\",");
        builder.AppendLine("                \"门诊工作站\",");
        builder.AppendLine("                \"通过源生成组合根创建的 Dashboard 初始页面。\"),");
        EmitStoreReducerArgs(builder, dashboard);
        builder.Append("            ").Append(CreateDispatcherExpression(dashboard)).AppendLine(");");
        builder.Append("        _dashboardViewModel = new ").Append(dashboard.ViewModelTypeName)
            .AppendLine("(store: _dashboardStore, chromeFactory: chromeFactory, pageFactory: this, uiDispatcher: ResolveUiDispatcher());");
        builder.AppendLine("        return _dashboardViewModel;");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    /// <summary>生成 CreateHeaderViewModel 方法。</summary>
    private static void EmitCreateHeaderViewModel(StringBuilder builder, GenerationModel model)
    {
        FeatureInfo? header = model.GetFeature("Header");
        if (header is null)
        {
            return;
        }

        builder.Append("    private ").Append(header.ViewModelTypeName).AppendLine(" CreateHeaderViewModel(string displayName)");
        builder.AppendLine("    {");
        builder.Append("        ").Append(StoreType(header)).Append(" store = ");
        EmitStoreConstructionHeader(builder, header);
        builder.Append("            new ").Append(header.StateTypeName)
            .AppendLine("(\"HIS/EMR 组合式 Dashboard\", $\"欢迎，{displayName}\"),");
        EmitStoreReducerArgs(builder, header);
        builder.Append("            ").Append(CreateDispatcherExpression(header)).AppendLine(");");
        builder.Append("        return new ").Append(header.ViewModelTypeName).AppendLine("(store, ResolveUiDispatcher());");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    /// <summary>生成 CreateOutpatientWorkstationViewModel 方法。</summary>
    private static void EmitCreateOutpatientWorkstationViewModel(StringBuilder builder, GenerationModel model)
    {
        FeatureInfo? outpatient = model.GetFeature("OutpatientWorkstation");
        if (outpatient is null)
        {
            return;
        }

        FeatureInfo? clinicalEditor = model.GetFeature("ClinicalEditor");
        FeatureInfo? clinicalReminder = model.GetFeature("ClinicalReminder");

        builder.Append("    private ").Append(outpatient.ViewModelTypeName).AppendLine(" CreateOutpatientWorkstationViewModel()");
        builder.AppendLine("    {");
        builder.Append("        global::MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.PatientQueue.PatientQueueViewModel queueViewModel = CreatePatientQueueViewModel();").AppendLine();
        EmitOutpatientClinicalStores(builder, clinicalEditor, clinicalReminder);
        builder.Append("        ").Append(StoreType(outpatient)).Append(" store = ");
        EmitStoreConstructionHeader(builder, outpatient);
        builder.Append("            new ").Append(outpatient.StateTypeName)
            .AppendLine("(\"等待子组件交互。\"),");
        EmitStoreReducerArgs(builder, outpatient);
        builder.Append("            ").Append(CreateDispatcherExpression(outpatient)).AppendLine(");");
        // 父 VM 不再直接持有子 VM 引用；改用 IOutpatientSubPanelFactory 工厂封装 3 个子 VM，
        // 由父 VM 在 View 按需解析时通过工厂方法获取，避免"VM-in-VM"反模式。
        builder.AppendLine("        global::MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.OutpatientSubPanelFactory subPanelFactory = new(queueViewModel, editorViewModel, reminderViewModel);");
        builder.Append("        return new ").Append(outpatient.ViewModelTypeName)
            .AppendLine("(store, subPanelFactory, ResolveUiDispatcher());");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    /// <summary>生成门诊工作站的临床编辑/提醒 Store 分支。</summary>
    private static void EmitOutpatientClinicalStores(
        StringBuilder builder,
        FeatureInfo? clinicalEditor,
        FeatureInfo? clinicalReminder)
    {
        if (clinicalEditor is not null)
        {
            builder.Append("        ").Append(StoreType(clinicalEditor)).Append(" editorStore = ");
            EmitStoreConstructionHeader(builder, clinicalEditor);
            builder.Append("            ").Append(clinicalEditor.StateTypeName).AppendLine(".Initial,");
            EmitStoreReducerArgs(builder, clinicalEditor);
            builder.Append("            ").Append(CreateDispatcherExpression(clinicalEditor)).AppendLine(");");
            builder.AppendLine("        _clinicalEditorStore = editorStore;");
            builder.Append("        global::MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalEditor.ClinicalEditorViewModel editorViewModel = new ")
                .Append(clinicalEditor.ViewModelTypeName).AppendLine("(editorStore, ResolveUiDispatcher());");
        }
        else
        {
            builder.AppendLine("        global::MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalEditor.ClinicalEditorViewModel editorViewModel = CreateClinicalEditorViewModel();");
        }
        if (clinicalReminder is not null)
        {
            builder.Append("        ").Append(StoreType(clinicalReminder)).Append(" reminderStore = ");
            EmitStoreConstructionHeader(builder, clinicalReminder);
            builder.Append("            ").Append(clinicalReminder.StateTypeName).AppendLine(".Initial,");
            EmitStoreReducerArgs(builder, clinicalReminder);
            builder.Append("            ").Append(CreateDispatcherExpression(clinicalReminder)).AppendLine(");");
            builder.AppendLine("        _clinicalReminderStore = reminderStore;");
            builder.Append("        global::MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalReminder.ClinicalReminderViewModel reminderViewModel = new ")
                .Append(clinicalReminder.ViewModelTypeName).AppendLine("(reminderStore, ResolveUiDispatcher());");
        }
        else
        {
            builder.AppendLine("        global::MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalReminder.ClinicalReminderViewModel reminderViewModel = CreateClinicalReminderViewModel();");
        }
    }

    /// <summary>生成 Dashboard 已知特性工厂方法。</summary>
    private static void EmitDashboardKnownFactories(StringBuilder builder, GenerationModel model)
    {
        FeatureInfo? patientQueue = model.GetFeature("PatientQueue");
        FeatureInfo? clinicalEditor = model.GetFeature("ClinicalEditor");
        FeatureInfo? clinicalReminder = model.GetFeature("ClinicalReminder");
        FeatureInfo? bedOverview = model.GetFeature("BedOverview");
        FeatureInfo? admissionCoordinator = model.GetFeature("AdmissionCoordinator");
        FeatureInfo? nursingTaskBoard = model.GetFeature("NursingTaskBoard");
        FeatureInfo? wardRiskPanel = model.GetFeature("WardRiskPanel");
        FeatureInfo? labOrderComposer = model.GetFeature("LabOrderComposer");
        FeatureInfo? specimenTracker = model.GetFeature("SpecimenTracker");
        FeatureInfo? criticalValueMonitor = model.GetFeature("CriticalValueMonitor");
        FeatureInfo? labTurnaroundBoard = model.GetFeature("LabTurnaroundBoard");
        FeatureInfo? prescriptionReviewBoard = model.GetFeature("PrescriptionReviewBoard");
        FeatureInfo? drugStockMonitor = model.GetFeature("DrugStockMonitor");
        FeatureInfo? replenishmentPlanner = model.GetFeature("ReplenishmentPlanner");
        FeatureInfo? medicationSafetyPanel = model.GetFeature("MedicationSafetyPanel");
        FeatureInfo? qualityKpiBoard = model.GetFeature("QualityKpiBoard");
        FeatureInfo? medicalRecordAuditBoard = model.GetFeature("MedicalRecordAuditBoard");
        FeatureInfo? riskEventBoard = model.GetFeature("RiskEventBoard");
        FeatureInfo? rectificationTracker = model.GetFeature("RectificationTracker");

        AppendKnownFeatureFactory(builder, patientQueue, "CreatePatientQueueViewModel", patientQueue?.StateTypeName + ".Initial");
        if (clinicalEditor is null)
        {
            AppendKnownFeatureFactory(builder, clinicalEditor, "CreateClinicalEditorViewModel", clinicalEditor?.StateTypeName + ".Initial");
        }
        if (clinicalReminder is null)
        {
            AppendKnownFeatureFactory(builder, clinicalReminder, "CreateClinicalReminderViewModel", clinicalReminder?.StateTypeName + ".Initial");
        }
        AppendKnownFeatureFactory(builder, bedOverview, "CreateBedOverviewViewModel", bedOverview?.StateTypeName + ".Initial");
        AppendKnownFeatureFactory(builder, admissionCoordinator, "CreateAdmissionCoordinatorViewModel", admissionCoordinator?.StateTypeName + ".Initial");
        AppendKnownFeatureFactory(builder, nursingTaskBoard, "CreateNursingTaskBoardViewModel", nursingTaskBoard?.StateTypeName + ".Initial");
        AppendKnownFeatureFactory(builder, wardRiskPanel, "CreateWardRiskPanelViewModel", wardRiskPanel?.StateTypeName + ".Initial");
        AppendKnownFeatureFactory(builder, labOrderComposer, "CreateLabOrderComposerViewModel", labOrderComposer?.StateTypeName + ".Initial");
        AppendKnownFeatureFactory(builder, specimenTracker, "CreateSpecimenTrackerViewModel", specimenTracker?.StateTypeName + ".Initial");
        AppendKnownFeatureFactory(builder, criticalValueMonitor, "CreateCriticalValueMonitorViewModel", criticalValueMonitor?.StateTypeName + ".Initial");
        AppendKnownFeatureFactory(builder, labTurnaroundBoard, "CreateLabTurnaroundBoardViewModel", labTurnaroundBoard?.StateTypeName + ".Initial");
        AppendKnownFeatureFactory(builder, prescriptionReviewBoard, "CreatePrescriptionReviewBoardViewModel", prescriptionReviewBoard?.StateTypeName + ".Initial");
        AppendKnownFeatureFactory(builder, drugStockMonitor, "CreateDrugStockMonitorViewModel", drugStockMonitor?.StateTypeName + ".Initial");
        AppendKnownFeatureFactory(builder, replenishmentPlanner, "CreateReplenishmentPlannerViewModel", replenishmentPlanner?.StateTypeName + ".Initial");
        AppendKnownFeatureFactory(builder, medicationSafetyPanel, "CreateMedicationSafetyPanelViewModel", medicationSafetyPanel?.StateTypeName + ".Initial");
        AppendKnownFeatureFactory(builder, qualityKpiBoard, "CreateQualityKpiBoardViewModel", qualityKpiBoard?.StateTypeName + ".Initial");
        AppendKnownFeatureFactory(builder, medicalRecordAuditBoard, "CreateMedicalRecordAuditBoardViewModel", medicalRecordAuditBoard?.StateTypeName + ".Initial");
        AppendKnownFeatureFactory(builder, riskEventBoard, "CreateRiskEventBoardViewModel", riskEventBoard?.StateTypeName + ".Initial");
        AppendKnownFeatureFactory(builder, rectificationTracker, "CreateRectificationTrackerViewModel", rectificationTracker?.StateTypeName + ".Initial");
    }

    /// <summary>生成 Dashboard 组合页面工厂方法。</summary>
    private static void EmitDashboardCompositeFactories(StringBuilder builder, GenerationModel model)
    {
        FeatureInfo? businessPage = model.GetFeature("BusinessCompositePage");
        FeatureInfo? patientSearch = model.GetFeature("PatientSearch");
        FeatureInfo? auditTimeline = model.GetFeature("AuditTimeline");
        FeatureInfo? architectureValidation = model.GetFeature("ArchitectureValidation");

        AppendBusinessPageFactory(builder, businessPage);
        AppendPageKeyFeatureFactory(builder, patientSearch, "CreatePatientSearchViewModel");
        AppendPageKeyFeatureFactory(builder, auditTimeline, "CreateAuditTimelineViewModel");
        AppendArchitectureValidationFactory(builder, architectureValidation, patientSearch, auditTimeline);
    }

    private static void AppendDashboardPageFactory(
        StringBuilder builder,
        GenerationModel model,
        FeatureInfo? dashboard)
    {
        if (dashboard is null)
        {
            return;
        }

        FeatureInfo? outpatient = model.GetFeature("OutpatientWorkstation");
        FeatureInfo? architectureValidation = model.GetFeature("ArchitectureValidation");

        EmitCreatePageSwitch(builder, outpatient, architectureValidation);
        EmitResolvePageMetadataSwitch(builder, outpatient, architectureValidation);
    }

    private static void EmitCreatePageSwitch(
        StringBuilder builder,
        FeatureInfo? outpatient,
        FeatureInfo? architectureValidation)
    {
        // IDashboardPageFactory.CreatePage：把 menuKey 解析为具体的顶层页面 ViewModel。
        // 容器直接实现该接口，避免父 VM 长期持有子 VM 引用造成的"VM-in-VM"反模式。
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// 根据页面键创建顶层页面 ViewModel。");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <param name=\"pageKey\">页面键。</param>");
        builder.AppendLine("    /// <returns>页面 ViewModel；未识别时返回 null。</returns>");
        builder.AppendLine("    public object? CreatePage(string pageKey)");
        builder.AppendLine("    {");
        builder.AppendLine("        ArgumentNullException.ThrowIfNull(pageKey);");
        builder.AppendLine("        switch (pageKey)");
        builder.AppendLine("        {");
        if (outpatient is not null)
        {
            builder.AppendLine("            case \"门诊工作站\":");
            builder.AppendLine("                return CreateOutpatientWorkstationViewModel();");
        }

        AppendBusinessPageCreateCase(builder, "住院床位", "床位总览 → 入院协调 → 护理任务 → 病区风险，4 节点 Z 形数据流。", "Inpatient");
        AppendBusinessPageCreateCase(builder, "检验医嘱", "医嘱开立 → 标本流转 → 危急值 → TAT 监控，4 节点 Z 形数据流。", "Lab");
        AppendBusinessPageCreateCase(builder, "药房库存", "处方审核 → 库存水位 → 补货计划 → 用药拦截，4 节点 Z 形数据流。", "Pharmacy");
        AppendBusinessPageCreateCase(builder, "运营质控", "院级 KPI → 病历抽查 → 风险分级 → 整改闭环，4 节点 Z 形数据流。", "Quality");
        if (architectureValidation is not null)
        {
            builder.AppendLine("            case \"架构验证中心\":");
            builder.AppendLine("                return CreateArchitectureValidationViewModel();");
        }

        builder.AppendLine("            default:");
        if (outpatient is not null)
        {
            builder.AppendLine("                return CreateOutpatientWorkstationViewModel();");
        }
        else
        {
            builder.AppendLine("                return null;");
        }

        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void EmitResolvePageMetadataSwitch(
        StringBuilder builder,
        FeatureInfo? outpatient,
        FeatureInfo? architectureValidation)
    {
        // ResolveDashboardPageMetadata：把 menuKey 解析为 (pageKey, title, description) 元组，
        // 仅供 Mediator 在派发 ShowPage Intent 时更新 Dashboard 状态使用。
        builder.AppendLine("    private (string ResolvedPageKey, string Title, string Description) ResolveDashboardPageMetadata(string pageKey)");
        builder.AppendLine("    {");
        builder.AppendLine("        ArgumentNullException.ThrowIfNull(pageKey);");
        builder.AppendLine("        switch (pageKey)");
        builder.AppendLine("        {");
        if (outpatient is not null)
        {
            builder.AppendLine("            case \"门诊工作站\":");
            builder.AppendLine("                return (\"门诊工作站\", \"门诊工作站\", \"门诊医生工作站组合页面。\");");
        }

        EmitBusinessPageMetadataCases(builder);
        if (architectureValidation is not null)
        {
            builder.AppendLine("            case \"架构验证中心\":");
            builder.AppendLine("                return (\"架构验证中心\", \"架构验证中心\", \"验证组合模式、复用特性、中介者和事件绑定的复杂页面。\");");
        }

        builder.AppendLine("            default:");
        if (outpatient is not null)
        {
            builder.AppendLine("                return (\"门诊工作站\", \"门诊工作站\", \"门诊医生工作站组合页面。\");");
        }
        else
        {
            builder.AppendLine("                return (pageKey, pageKey, \"未注册 Dashboard 页面。\");");
        }

        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void EmitBusinessPageMetadataCases(StringBuilder builder)
    {
        AppendBusinessPageCase(
            builder,
            "住院床位",
            "住院床位 · 数据流",
            "床位总览 → 入院协调 → 护理任务 → 病区风险，4 节点 Z 形数据流。");
        AppendBusinessPageCase(
            builder,
            "检验医嘱",
            "检验医嘱 · 数据流",
            "医嘱开立 → 标本流转 → 危急值 → TAT 监控，4 节点 Z 形数据流。");
        AppendBusinessPageCase(
            builder,
            "药房库存",
            "药房库存 · 数据流",
            "处方审核 → 库存水位 → 补货计划 → 用药拦截，4 节点 Z 形数据流。");
        AppendBusinessPageCase(
            builder,
            "运营质控",
            "运营质控 · 数据流",
            "院级 KPI → 病历抽查 → 风险分级 → 整改闭环，4 节点 Z 形数据流。");
    }

    private static void AppendBusinessPageCreateCase(StringBuilder builder, string menuKey, string summary, string layout)
    {
        builder.Append("            case \"").Append(menuKey).AppendLine("\":");
        builder.Append("                return CreateBusinessCompositePageViewModel(\"")
            .Append(menuKey)
            .Append(" · 数据流\", \"")
            .Append(summary)
            .Append("\", \"").Append(layout)
            .AppendLine("\");");
    }

    /// <summary>
    /// 在容器中追加 <c>IShellPageFactory.CreatePage(string)</c> 实现。
    /// <para>
    /// 该工厂把 <c>ShellPageKeys</c> 判别器解析为具体顶层页面 ViewModel，
    /// 容器直接实现接口避免在 <c>AppShellState</c> 中嵌入页面 VM 引用。
    /// Login 走容器自管的 <c>Resolve&lt;LoginViewModel&gt;</c>；
    /// EventBindingWorkbench 由组合根在构造期间通过 <c>SetEventBindingWorkbenchViewModel</c> 注入，
    /// 因为它的子组件 Store/Mediator 装配在源生成器枚举范围之外；
    /// Dashboard 走容器自管的 <c>CreateDashboardViewModel(string.Empty)</c>（首次构造时由登录导航服务带 displayName 预先创建并缓存）。
    /// </para>
    /// </summary>
    private static void AppendShellPageFactory(
        StringBuilder builder,
        GenerationModel model,
        FeatureInfo login,
        FeatureInfo? dashboard)
    {
        if (model.ShellPageFactoryTypeName is null)
        {
            return;
        }

        builder.AppendLine("    private object? _eventBindingWorkbenchViewModel;");
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// 由组合根注入事件绑定 Workbench 顶层页面 ViewModel。");
        builder.AppendLine("    /// 容器自身不构造该 VM，因其 3 个子组件 Store/Mediator 的装配逻辑在源生成器枚举范围之外。");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public void SetEventBindingWorkbenchViewModel(object viewModel)");
        builder.AppendLine("    {");
        builder.AppendLine("        ArgumentNullException.ThrowIfNull(viewModel);");
        builder.AppendLine("        _eventBindingWorkbenchViewModel = viewModel;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.Append("    /// <inheritdoc cref=\"").Append(model.ShellPageFactoryTypeName).AppendLine("\" />");
        builder.Append("    object? ").Append(model.ShellPageFactoryTypeName).AppendLine(".CreatePage(string pageKey)");
        builder.AppendLine("    {");
        builder.AppendLine("        ArgumentNullException.ThrowIfNull(pageKey);");
        builder.AppendLine("        switch (pageKey)");
        builder.AppendLine("        {");
        builder.AppendLine("            case \"Login\":");
        builder.Append("                return (").Append(login.ViewModelTypeName).AppendLine(")Resolve(typeof(").Append(login.ViewModelTypeName).AppendLine("));");
        builder.AppendLine("            case \"EventBindingWorkbench\":");
        builder.AppendLine("                return _eventBindingWorkbenchViewModel;");
        if (dashboard is not null)
        {
            // Dashboard 在登录流程中已由 ILoginNavigationService.NavigateToDashboardAsync(displayName)
            // 通过 CreateDashboardViewModel(displayName) 预先创建并缓存到 _dashboardViewModel 字段。
            // 此处用空 displayName 兜底（仅在有人未走登录流程就 ShowPageAsync("Dashboard") 时触发），
            // CreateDashboardViewModel 内部已检查缓存，会返回已构造的实例。
            builder.AppendLine("            case \"Dashboard\":");
            builder.Append("                return CreateDashboardViewModel(string.Empty);");
        }

        builder.AppendLine("            default:");
        builder.AppendLine("                return null;");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void AppendBusinessPageCase(
        StringBuilder builder,
        string menuKey,
        string title,
        string summary)
    {
        builder.Append("            case \"").Append(menuKey).AppendLine("\":");
        builder.Append("                return (\"").Append(menuKey).Append("\", \"").Append(title).Append("\", \"")
            .Append(summary).AppendLine("\");");
    }

    private static void AppendBusinessPageFactory(StringBuilder builder, FeatureInfo? feature)
    {
        if (feature is null)
        {
            return;
        }

        builder.Append("    private ").Append(feature.ViewModelTypeName).AppendLine(" CreateBusinessCompositePageViewModel(");
        builder.AppendLine("        string title,");
        builder.AppendLine("        string summary,");
        builder.AppendLine("        string layout)");
        builder.AppendLine("    {");
        builder.Append("        ").Append(StoreType(feature)).Append(" store = ");
        EmitStoreConstructionHeader(builder, feature);
        builder.Append("            new ").Append(feature.StateTypeName).AppendLine("(");
        builder.AppendLine("                title,");
        builder.AppendLine("                summary,");
        builder.AppendLine("                layout,");
        builder.AppendLine("                \"等待子组件交互。\",");
        builder.AppendLine("                \"等待子组件交互。\",");
        builder.AppendLine("                \"等待子组件交互。\"),");
        EmitStoreReducerArgs(builder, feature);
        builder.Append("            ").Append(CreateDispatcherExpression(feature)).AppendLine(");");
        builder.AppendLine("        _businessCompositePageStore = store;");
        builder.Append("        return new ").Append(feature.ViewModelTypeName).AppendLine("(store, ResolveCardStoreFactory(), ResolveUiDispatcher());");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    /// <summary>
    /// 生成按需构造的 <c>CardStoreFactory</c> 解析器，缓存为单例。
    /// 工厂仅依赖 <c>IMviMediator</c>，因此通过 <c>this</c> 满足依赖。
    /// </summary>
    /// <param name="builder">字符串构建器。</param>
    /// <param name="model">生成模型。</param>
    private static void AppendCardStoreFactoryResolver(StringBuilder builder, GenerationModel model)
    {
        if (model.CardStoreFactory is null)
        {
            return;
        }

        builder.Append("    private ").Append(model.CardStoreFactory.TypeName).AppendLine(" ResolveCardStoreFactory()");
        builder.AppendLine("    {");
        builder.AppendLine("        if (_cardStoreFactory is not null)");
        builder.AppendLine("        {");
        builder.AppendLine("            return _cardStoreFactory;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.Append("        _cardStoreFactory = new ").Append(model.CardStoreFactory.TypeName)
            .Append("(this, (global::MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.PatientRegistry.IMviPatientRegistry)GetSingleton(typeof(global::MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.PatientRegistry.IMviPatientRegistry), ")
            .Append("static () => new global::MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.PatientRegistry.InMemoryPatientRegistry()));").AppendLine();
        builder.AppendLine("        return _cardStoreFactory;");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void AppendPageKeyFeatureFactory(
        StringBuilder builder,
        FeatureInfo? feature,
        string methodName)
    {
        if (feature is null || !feature.HasStringCreateInitial)
        {
            return;
        }

        builder.Append("    private ").Append(feature.ViewModelTypeName).Append(' ').Append(methodName)
            .AppendLine("(string pageKey)");
        builder.AppendLine("    {");
        builder.Append("        ").Append(StoreType(feature)).Append(" store = ");
        EmitStoreConstructionHeader(builder, feature);
        builder.Append("            ").Append(feature.StateTypeName).AppendLine(".CreateInitial(pageKey),");
        EmitStoreReducerArgs(builder, feature);
        builder.Append("            ").Append(CreateDispatcherExpression(feature)).AppendLine(");");
        builder.Append("        return new ").Append(feature.ViewModelTypeName).AppendLine("(store, ResolveUiDispatcher());");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void AppendArchitectureValidationFactory(
        StringBuilder builder,
        FeatureInfo? feature,
        FeatureInfo? patientSearch,
        FeatureInfo? auditTimeline)
    {
        if (feature is null
            || patientSearch is null
            || auditTimeline is null)
        {
            return;
        }

        // 4 张指标卡通过 CardStoreFactory.GetViewModel(PageKey.X) 解析，
        // 与业务页 16 张卡片共享同一 store/VM 实例（含完整 14 参 CardState），
        // 因此源代码生成器不再独立构造 5 参简化版 CardState——之前 AppendMetricCardFactory
        // 调用 `new CardState(title, value, status, detail, true)` 在 CardState 构造已扩展为
        // 14 参后会导致整页编译失败、菜单项路由被默认回退到门诊工作站，
        // 这就是"门诊工作站和架构验证中心两个菜单进去界面完全一样"的根本原因。
        builder.Append("    private ").Append(feature.ViewModelTypeName).AppendLine(" CreateArchitectureValidationViewModel()");
        builder.AppendLine("    {");
        // 父 VM 不再直接持有 6 个子 VM 引用；
        // 2 个复用组件（患者检索 / 审计时间线）通过 IArchitectureValidationPanelFactory 工厂解析，
        // 4 张指标卡通过 CardStoreFactory 解析（与业务页 16 张卡片共享同一 store/VM 实例）。
        builder.Append("        ").Append(patientSearch.ViewModelTypeName).AppendLine(" patientSearchViewModel = CreatePatientSearchViewModel(\"架构验证中心\");");
        builder.Append("        ").Append(auditTimeline.ViewModelTypeName).AppendLine(" auditTimelineViewModel = CreateAuditTimelineViewModel(\"架构验证中心\");");
        builder.AppendLine("        global::MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ArchitectureValidation.ArchitectureValidationPanelFactory panelFactory = new(contextName => CreatePatientSearchViewModel(contextName), contextName => CreateAuditTimelineViewModel(contextName));");
        builder.Append("        ").Append(StoreType(feature)).Append(" store = ");
        EmitStoreConstructionHeader(builder, feature);
        builder.Append("            new ").Append(feature.StateTypeName).AppendLine("(),");
        EmitStoreReducerArgs(builder, feature);
        builder.Append("            ").Append(CreateDispatcherExpression(feature)).AppendLine(");");
        builder.Append("        return new ").Append(feature.ViewModelTypeName).AppendLine("(store, panelFactory, ResolveCardStoreFactory(), ResolveUiDispatcher());");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void AppendKnownFeatureFactory(
        StringBuilder builder,
        FeatureInfo? feature,
        string methodName,
        string? stateExpression)
    {
        if (feature is null || stateExpression is null)
        {
            return;
        }

        builder.Append("    private ").Append(feature.ViewModelTypeName).Append(' ').Append(methodName).AppendLine("()");
        builder.AppendLine("    {");
        builder.Append("        ").Append(StoreType(feature)).Append(" store = ");
        EmitStoreConstructionHeader(builder, feature);
        builder.Append("            ").Append(stateExpression).AppendLine(",");
        EmitStoreReducerArgs(builder, feature);
        builder.Append("            ").Append(CreateDispatcherExpression(feature)).AppendLine(");");
        builder.Append("        return new ").Append(feature.ViewModelTypeName).AppendLine("(store, ResolveUiDispatcher());");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void AppendScope(StringBuilder builder, string containerName, FeatureInfo login)
    {
        EmitScopeClassHeader(builder, containerName);
        EmitScopeResolveMethods(builder, login);
        EmitScopeLifecycleMethods(builder, login);
    }

    private static void EmitScopeClassHeader(StringBuilder builder, string containerName)
    {
        builder.Append("    private sealed class ").Append(containerName).AppendLine("Scope : IMviScope");
        builder.AppendLine("    {");
        builder.Append("        private readonly ").Append(containerName).AppendLine(" _container;");
        builder.AppendLine("        private readonly Dictionary<Type, object> _scoped = new();");
        builder.AppendLine();
        builder.Append("        public ").Append(containerName).Append("Scope(").Append(containerName).AppendLine(" container)");
        builder.AppendLine("        {");
        builder.AppendLine("            _container = container;");
        builder.AppendLine("        }");
        builder.AppendLine();
    }

    private static void EmitScopeResolveMethods(StringBuilder builder, FeatureInfo login)
    {
        builder.AppendLine("        public TService Resolve<TService>()");
        builder.AppendLine("            where TService : notnull");
        builder.AppendLine("        {");
        builder.AppendLine("            return (TService)Resolve(typeof(TService));");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        public object Resolve(Type serviceType)");
        builder.AppendLine("        {");
        builder.AppendLine("            ArgumentNullException.ThrowIfNull(serviceType);");
        builder.Append("            if (serviceType == typeof(").Append(StoreType(login)).AppendLine("))");
        builder.AppendLine("            {");
        builder.AppendLine("                return ResolveLoginStore();");
        builder.AppendLine("            }");
        builder.AppendLine();
        builder.Append("            if (serviceType == typeof(").Append(login.ViewModelTypeName).AppendLine("))");
        builder.AppendLine("            {");
        builder.AppendLine("                return GetScoped(serviceType, () => new " + login.ViewModelTypeName + "(ResolveLoginStore()));");
        builder.AppendLine("            }");
        builder.AppendLine();
        builder.AppendLine("            return _container.Resolve(serviceType);");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        public TService CreateWith<TService>(params object[] args)");
        builder.AppendLine("            where TService : notnull");
        builder.AppendLine("        {");
        builder.AppendLine("            return _container.CreateWith<TService>(args);");
        builder.AppendLine("        }");
        builder.AppendLine();
    }

    private static void EmitScopeLifecycleMethods(StringBuilder builder, FeatureInfo login)
    {
        builder.AppendLine("        public void Dispose()");
        builder.AppendLine("        {");
        builder.AppendLine("            foreach (object instance in _scoped.Values)");
        builder.AppendLine("            {");
        builder.AppendLine("                if (instance is IDisposable disposable)");
        builder.AppendLine("            {");
        builder.AppendLine("                disposable.Dispose();");
        builder.AppendLine("            }");
        builder.AppendLine("            }");
        builder.AppendLine();
        builder.AppendLine("            _scoped.Clear();");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.Append("        private ").Append(StoreType(login)).AppendLine(" ResolveLoginStore()");
        builder.AppendLine("        {");
        builder.Append("            return (").Append(StoreType(login)).Append(")GetScoped(typeof(")
            .Append(StoreType(login)).AppendLine("), () => _container.CreateLoginStore());");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        private object GetScoped(Type serviceType, Func<object> factory)");
        builder.AppendLine("        {");
        builder.AppendLine("            if (_scoped.TryGetValue(serviceType, out object? instance))");
        builder.AppendLine("            {");
        builder.AppendLine("                return instance;");
        builder.AppendLine("            }");
        builder.AppendLine();
        builder.AppendLine("            object created = factory();");
        builder.AppendLine("            _scoped[serviceType] = created;");
        builder.AppendLine("            return created;");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
    }

    private static void AppendDescriptor(
        StringBuilder builder,
        string? serviceType,
        string? implementationType,
        string lifetime)
    {
        if (serviceType is null || implementationType is null)
        {
            return;
        }

        builder.Append("            new(typeof(").Append(serviceType).Append("), typeof(")
            .Append(implementationType).Append("), ServiceLifetime.").Append(lifetime).AppendLine("),");
    }

    private static string CreateDispatcherExpression(FeatureInfo feature)
    {
        // UnitEffect 表示无副作用，统一返回 NullEffectDispatcher 单例。
        if (feature.IsUnitEffect)
        {
            return "global::MiKiNuo.Mvi.Application.MVI.Effect.NullEffectDispatcher.Instance";
        }

        switch (feature.DispatcherConstructorKind)
        {
            case DispatcherConstructorKind.Mediator:
                return "new " + feature.DispatcherTypeName + "(this)";
            case DispatcherConstructorKind.Parameterless:
                return "new " + feature.DispatcherTypeName + "()";
            default:
                return "new " + feature.DispatcherTypeName + "()";
        }
    }

    private static string ReducerInterfaceType(FeatureInfo feature)
    {
        return "global::MiKiNuo.Mvi.Application.MVI.Reducer.IMviReducer<"
            + feature.StateTypeName + ", "
            + feature.IntentTypeName + ", "
            + feature.EffectTypeName + ">";
    }

    /// <summary>发射 Store 构造头部（类型名与左括号）。</summary>
    private static void EmitStoreConstructionHeader(StringBuilder builder, FeatureInfo feature)
    {
        builder.Append("new MviStore<")
            .Append(feature.StateTypeName).Append(", ")
            .Append(feature.IntentTypeName).Append(", ")
            .Append(feature.EffectTypeName).AppendLine(">(");
    }

    /// <summary>发射 Store 意图处理器与规约器实参。</summary>
    private static void EmitStoreReducerArgs(StringBuilder builder, FeatureInfo feature)
    {
        builder.Append("            new ").Append(feature.IntentHandlerTypeName).AppendLine("(),");
        builder.Append("            new ").Append(feature.ReducerTypeName).AppendLine("(),");
    }

    private static string StoreType(FeatureInfo feature)
    {
        return "global::MiKiNuo.Mvi.Application.MVI.Store.IMviStore<"
            + feature.StateTypeName + ", "
            + feature.IntentTypeName + ", "
            + feature.EffectTypeName + ">";
    }

    private static string Format(ITypeSymbol symbol)
    {
        return symbol.ToDisplayString(TypeFormat);
    }

    private static string RemoveSuffix(string value, string suffix)
    {
        return value.EndsWith(suffix, StringComparison.Ordinal)
            ? value.Substring(0, value.Length - suffix.Length)
            : value;
    }

    private static string GetCompositionPrefix(string? assemblyName)
    {
        if (assemblyName is not null
            && assemblyName.IndexOf(".Samples.", StringComparison.Ordinal) >= 0)
        {
            return "Sample";
        }

        return "Mvi";
    }
}
