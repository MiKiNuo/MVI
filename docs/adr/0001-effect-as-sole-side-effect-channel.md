# Effect 是唯一副作用通道，删除 IntentHandler

框架曾同时存在两套异步机制：IntentHandler（执行业务异步、返回后续 Intent）与 Effect → EffectDispatcher（一次性平台动作）。双轨导致职责边界模糊、文档无法自洽，且 IntentHandler 运行在 Store 派发锁内会冻结同 Store 的其他交互。

决定：删除 IntentHandler。所有异步统一为 Effect——Reducer 声明 Effect（数据快照随 Effect 携带），EffectDispatcher 执行，需要回流时显式 Dispatch 新 Intent，回流 Intent 走完整 Middleware 管线。Reducer 因此成为唯一决策点，"是否发起异步调用"可在 Reducer 单元测试中断言。
