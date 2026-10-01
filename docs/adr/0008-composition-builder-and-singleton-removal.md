# [MviComposition] 组合构建器完成 ADR-0007 迁移；删除单例对象图

ADR 0007 接受后长期处于迁移中间态：生成容器同时 emit ADR-0004 的单例 Feature 图（含容器级 Mediator 单例）与实例工厂，两套 Mediator 互不相通，`BindSiblingState` 被分析器 MVI0021 封杀却仍在服役；新路径的组合接线（scope 创建、实例身份分配、Register/Bind/Subscribe）全部留给调用方手工完成，样例与测试出现两套装配哲学。

决定：

1. **删除单例路径**：生成容器不再 emit 单例 Feature 对象图、容器级 Mediator 与 `Resolve<IMviStore<...>>` 分支；`MviViewModelBase.BindSiblingState` 及 sibling 订阅管理一并删除，MVI0021 收窄为只拦截外源 Store 直接使用。"天然单例"的 Feature 表达为组合根持有的长生命周期实例。
2. **组合接线声明式化**：新增 `[MviComposition]`（partial 声明类列出成员 Reducer）、`[MviRouteHandler]`（EffectDispatcher 方法声明请求提供方）、`[MviNotificationAcceptor]`（声明通知订阅）。生成器 emit 组合构建器 `CreateXxxCompositionAsync`：创建组合范围、内部分配实例 Guid、创建全部成员实例并完成 Register/Bind/Subscribe 接线；声明类 partial 合并为组合句柄（`IAsyncDisposable`）。
3. **绑定推导沿用 ADR-0006 语义**：同一组合内某请求契约唯一提供方 → 自动 Bind 所有消费方（消费方由 EffectDispatcher 内 `SendAsync` 调用点经语义分析识别）；多提供方 → MVI0022；有消费方无提供方 → MVI0023；接线方法签名/可见性非法 → MVI0024；声明类非 partial → MVI0025；成员不是已发现 Feature → MVI0026。
4. **同步接纳通道**：通知接纳器是同步回调，不能等待异步派发。`IMviIntentSink` 增加 `TryPost`，`MviEffectDispatcherBase` 增加 `TryAcceptIntent`，将本地意图写入 Store 的有界通知队列（与 `MviStore.TryPost` 同一通道），拒绝接纳（Store 关闭/队列满）返回假，由接纳器决定是否抛出。
5. EffectDispatcher 构造签名不变：仍注入 `IMviMediator`；需要发布通知的分发器注入 `MviMediatorEndpoint`（实例工厂将实例独占端点同时缓存为两种类型）。

代价：跨 Feature 通知从"导航时直接派发对方 Store"变为"发布事实 + 订阅方同步接纳"，链路多一跳且摘要类状态更新经通知队列异步落地；这是 ADR-0007 通知语义的固有成本，换取实例隔离与多开能力。
