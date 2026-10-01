# 基准项目以三层基线与深度扫描度量 DI 与 MVI 管线

框架此前没有任何性能验证手段：自研编译期 DI 容器（`[DiService]` → `GeneratedMviContainer`，`Resolve(Type)` 为 if-else 线性链）与 MVI 派发管线（`MviStore.DispatchAsync`：锁 → 中间件 → 规约 → 状态发布 → 锁外副作用）的成本水位未知，重构可能引入性能回归而无人察觉。

决定：

1. 新增 `test/MiKiNuo.Mvi.Benchmarks`（BenchmarkDotNet，纯 `BenchmarkSwitcher` 入口）。场景代码（State/Intent/Effect/Reducer/合成服务）是基准与测试共享的普通公共类型；TUnit 冒烟测试放在现有 `MiKiNuo.Mvi.Tests` 并引用基准工程——单 Exe 的入口只能归一家，测试宿主与基准宿主分离，冒烟测试随现有测试流水线运行。
2. DI 用三层基线回答"达到什么水平"：手写 `new`（理论上限）、`Microsoft.Extensions.DependencyInjection`（行业参照）、生成容器（被测对象）。采用深度扫描而非多容器规模扫描：单容器注册 300 个合成服务，按链首/链中/链尾 × 单例/瞬态/作用域扫描解析成本——if-else 链的深度代价才是编译期 DI 的生死题，位置由 `ServiceDescriptors` 真实顺序推导，不依赖源码声明顺序。
3. MVI 用双轨场景：最小场景（1 字段 State，手工装配）测框架底噪；登录复刻场景（镜像真实登录示例、`[MviFeature]` 编译期装配、假认证服务保证确定性）测真实水位与生成装配路径。扫描维度：中间件 0/1/4/8 层、副作用 0/1/4 个、并发 1/2/4/8 线程争抢同一 Store（验证 ADR-0002 锁语义）。Feature 端到端与手工装配对照，可分离"生成装配的额外开销"。
4. 验收口径是版本间回归对比，不设硬性纳秒门槛：绝对数字随环境（CPU/RID/运行时小版本）漂移，门槛无意义；每轮完整结果存档到 `docs/benchmarks/results/`，汇总表与跑法记录在 `docs/benchmarks/README.md`。
5. CI 只跑冒烟档：`--list flat`（秒级，验证基准程序集与分析配置可加载）+ 冒烟测试（验证场景真实可跑）；完整基准本地手动运行，CI 跑不动也不该跑。
6. 场景代码先经 TDD（冒烟测试红 → 场景实现绿），再挂 `[Benchmark]` 壳——测量的正确性先于测量本身。
