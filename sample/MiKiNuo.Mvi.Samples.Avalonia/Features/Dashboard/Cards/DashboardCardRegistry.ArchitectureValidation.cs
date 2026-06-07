namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

/// <summary>
/// 仪表板卡片注册表的架构验证中心分部。
/// 负责中间件、复用组件、中介者、副作用 4 张指标卡的 <see cref="CardDefinition"/> 注册。
/// 4 张卡复用业务页同款 <see cref="CardViewModel"/> 与 <see cref="CardView"/>，由
/// <c>CardStoreFactory</c> 在 <c>SampleGeneratedContainer</c> 中按 PageKey 解析；架构验证页面
/// 通过 <c>CardStoreFactory.GetViewModel</c> 拉取同一份 store/VM 实例，
/// 保证患者检索 → 指标卡 → 审计时间线三方在 Mediator 上观察到的"架构验证中心"链路与业务页完全同构。
/// </summary>
public static partial class DashboardCardRegistry
{
    private static partial void AddArchitectureValidationDefinitions(Dictionary<PageKey, CardDefinition> dict)
    {
        ArgumentNullException.ThrowIfNull(dict);

        dict[PageKey.MiddlewareMetric] = new CardDefinition(
            Key: PageKey.MiddlewareMetric,
            SourceKey: "架构验证中心",
            SourceDisplayName: "中间件指标 MVI",
            Title: "中间件指标 MVI",
            MainValue: "2",
            StatusText: "Active",
            DetailText: "日志与性能中间件已接入 SampleGeneratedContainer 组合根。",
            PrimaryActionText: "重放中间件链路",
            SecondaryActionText: "查看中间件事件",
            FormFields: null,
            RequiredFormFields: null,
            Validator: null);

        dict[PageKey.ReuseMetric] = new CardDefinition(
            Key: PageKey.ReuseMetric,
            SourceKey: "架构验证中心",
            SourceDisplayName: "复用指标 MVI",
            Title: "复用指标 MVI",
            MainValue: "2",
            StatusText: "Reusable",
            DetailText: "患者检索与审计时间线可在架构验证中心、住院床位、检验医嘱三个菜单间复用。",
            PrimaryActionText: "在住院页复用检索",
            SecondaryActionText: "在检验页复用检索",
            FormFields: null,
            RequiredFormFields: null,
            Validator: null);

        dict[PageKey.MediatorMetric] = new CardDefinition(
            Key: PageKey.MediatorMetric,
            SourceKey: "架构验证中心",
            SourceDisplayName: "中介者指标 MVI",
            Title: "中介者指标 MVI",
            MainValue: "1",
            StatusText: "Routing",
            DetailText: "父子组件通过 Request/Response 中介者协作，Mediator 路由请求自动派发到订阅者。",
            PrimaryActionText: "派发 Mediator 请求",
            SecondaryActionText: "查看订阅者清单",
            FormFields: null,
            RequiredFormFields: null,
            Validator: null);

        dict[PageKey.EffectMetric] = new CardDefinition(
            Key: PageKey.EffectMetric,
            SourceKey: "架构验证中心",
            SourceDisplayName: "副作用指标 MVI",
            Title: "副作用指标 MVI",
            MainValue: "多路",
            StatusText: "Observed",
            DetailText: "组件副作用通过 EffectDispatcher 进入业务流，可在审计时间线实时回放。",
            PrimaryActionText: "回放副作用事件",
            SecondaryActionText: "查看审计时间线",
            FormFields: null,
            RequiredFormFields: null,
            Validator: null);
    }
}
