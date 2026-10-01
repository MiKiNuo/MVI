# [MviFeature] 显式装配 MVI 对象图；跨 Feature 协调只走 Mediator

仓库曾并存三套 DI 风格：框架通用 `[DiService]` 生成器（几乎无人使用）、示例专属硬编码生成器、Godot 示例纯手写组合根。"编译期 DI"承诺未兑现。同时 Godot 大厅出现兄弟 Store 网状直连（Player↔Mission↔HeroRoster↔Inventory），违背框架初衷。

决定：

1. 在 Reducer 上标注 `[MviFeature]`，生成器从基类泛型参数推导 State/Intent/Effect 三件套，按类型签名发现匹配的 EffectDispatcher 与 ViewModel，emit 进 `GeneratedMviContainer`（Store 单例、ViewModel 单例、构造参数从容器解析）。拒绝命名约定发现——真实项目命名纪律不可靠，显式特性换编译期确定性。
2. 跨 Feature 协调只允许 Mediator Request/Response；同 Feature 集群内允许 `StoreReference`/`BindSiblingState` 直连（游戏大厅等紧耦合场景的现实需求），边界由 Analyzer 约束。

---

**后续状态（2026-09-18）**：本决策的第 1 点中"Feature 对象图固定单例"与第 2 点中"StoreReference/BindSiblingState 直连"已被 ADR 0007 取代并**完成实施**：生成容器不再 emit 单例 Feature 图与容器级 Mediator，`BindSiblingState` 已从 `MviViewModelBase` 删除；Feature 创建统一经实例工厂与 `[MviComposition]` 组合构建器（见 ADR 0008）。`[MviFeature]` 显式特性装配的部分继续有效。
