# Store 派发语义：锁只护 Reduce，Effect 在锁外派发

旧实现中 SemaphoreSlim 派发门横跨整个异步 IntentHandler，慢请求（如登录 HTTP）会阻塞同 Store 的全部后续 Intent（包括打字输入），且 IntentHandler 产出的后续 Intent 绕过 Middleware 直进 Reducer，追踪链断裂。

决定：派发锁只保护同步的 Middleware → Reduce → State 发布（原子、全序）；Effect 在锁释放后按序派发；Effect 回流的新 Intent 作为普通派发重新排队，中间件全程可见。并发防护交给 Reducer Guard（如 `IsBusy` 时不再产出 `PerformLogin` Effect），这是既有机制而非新增概念。
