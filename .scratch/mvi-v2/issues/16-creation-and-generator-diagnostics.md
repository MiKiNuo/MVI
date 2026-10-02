# 16: 创建失败和生成错误定位业务声明

**What to build:** 消费者能够用直接构造或标准 DI 创建 Feature；错误依赖和非法业务声明给可定位的失败，失败后不遗留本次创建的资源。

**Blocked by:** 10 — 单实例逻辑关闭与服务范围安全释放。

**Status:** completed

## Acceptance criteria

- [x] 在已有工厂成功路径上补齐缺少服务、构造异常和创建中途失败的清理，外部服务不重复释放；每次工厂创建仍独立。
- [x] 合法 Input、Operation、消息处理和构造声明可编译并调用；错误签名、重复处理器、成员冲突和可静态确定的歧义定位原始声明。
- [x] 类型判断采用实际符号关系，不能把同名伪类型或不支持的方法签名当成有效声明。
- [x] 增量生成只处理有关候选，无关变更不触发全量无关生成；消费者默认诊断聚焦框架误用，不强制仓库内部文档语言或层次命名。
- [x] 独立编译消费者在修正错误后可实际运行；使用现有生成器测试方法验证公开输出与诊断，不固化内部类布局或大量整份生成文本。

## Spec coverage

- 用户故事：US01、US41、US42、US43、US44、US45、US55。
- 实现决策：D02、D04、D26、D27。
- 行为验收：T18、T19。

这些编号引用 MVI v2 实施规格；这里表示本任务负责或贡献的覆盖，完整验收仍以该规格为准。

## Root 最终验收

2026-10-03，独立声明协议审查的两个 P2 已通过公开 RED/GREEN 修复并窄增量复核，无遗留实质问题。合法 handler 名称不再被适配 lambda 遮蔽；不可公开的消息/响应契约定位原声明，同时允许 internal Feature 的合法内部契约。

最终声明/编译执行 30/30、工厂 19/19、增量 2/2、完整 v2 331/331，独立 Headless 五组 PASS。Release 解决方案构建 0 错误，5 条已有 v1 负例警告；格式和差异检查通过。生成入口、失败清理委托及增量输出范围均有实际消费者证据。

## Implementation

- 新增 `[RequestHandler]`：声明 private 实例 `Task<TResult>` / `ValueTask<TResult>(Operation<State>, TMessage)`，生成 `Create{MethodName}Port()`；验证、并发容量、并行上限和取消策略复用既有 `CreateRequestPort` 入口，端口由宿主明确接线。
- 为具体 Feature 生成 `static ValueTask<TFeature> CreateAsync(IServiceProvider)`，直接适配 `FeatureFactory`；沿用切片 10/13 的独立 scope、构造失败清理和在途工作退出后释放，不新增生命周期实现。
- `MVI2011`–`MVI2016` 定位处理签名、验证、单 Feature 重复契约、生成成员冲突、策略以及工厂或重复标记构造声明；类型检查使用真实 Roslyn 符号，ref struct 消息及不能在生成入口暴露的契约在原属性处拒绝，多 Feature 相同契约仍合法。
- 沿用属性候选、`FeatureEmission` 比较器和现有增量管线。公开消费者先修正无效 Validate，再通过 Compile + Emit + AssemblyLoad 实际调用生成输入、操作、端口和 DI 工厂；独立 Headless 的详情请求也使用生成端口。
- 独立审查修复：Task 适配显式调用 `this.@处理器`，避免合法 `message` / `operation` 方法名被 lambda 参数遮蔽；请求与响应按生成方法的有效可访问性递归校验包含类型、泛型实参及数组元素，internal Feature 仍可使用 internal 契约。

## Verification

2026-10-03，Release，TUnit 串行 `--maximum-parallel-tests 1`：

| 验证 | 实际结果 | 证据 |
| --- | --- | --- |
| ref struct 消息 RED | 构建成功；22 项中 1 项失败，原声明缺少 MVI2011 | `16-ref-like-red-build.log`、`16-ref-like-red.log` |
| 审查边界 RED | 构建成功；30 项中 7 项失败，2 个 CS0149、5 个原属性 MVI2011 缺失 | `16-review-red-build.log`、`16-review-red.log` |
| 请求声明 GREEN 与公开消费者 | 30/30 通过；无效 Validate 修正后同 driver 编译、发射并执行 Consumer.Run，包含 Load/message/operation 名称及 internal 契约正例 | `16-review-green-build.log`、`16-review-green.log` |
| 工厂失败及生命周期回归 | 19/19 通过；歧义、缺依赖、基类前异常和在途工作失败经生成 CreateAsync 验证 | `16-factory-tests.log` |
| 增量范围 | 2/2 通过；无关变化缓存复用，单 Feature 配置修改仅影响有关输出 | `16-final-incremental.log` |
| 全 V2 | 331/331 通过，0 跳过 | `16-review-full-v2.log` |
| 独立 Headless 消费者 | 状态、操作、队列、Mediator、投递 5 个 PASS，退出 0 | `16-review-headless.log` |
| 解决方案构建 | `dotnet build MiKiNuo.Mvi.slnx -c Release --no-restore -p:UseSharedCompilation=false`，退出 0、0 错误，5 个未改动 v1 测试负例的 MVI0013 警告 | `16-review-solution-build.log` |
| 最终 Diff 与格式 | 任务范围 `git diff --check` 退出 0；六个改动 C# 文件均为 UTF-8 BOM / CRLF，无行尾空白 | `16-diff-check.log`、`16-format-check.log` |

首次解决方案命令超过 55 秒输出捕获窗口，保留 `16-solution-build.log`；等待原进程结束后 `16-final-solution-build.log` 确认构建成功。审查修复后的最终门禁以上表 `16-review-*` 为准。5 个 MVI0013 警告来自未改动的 `MviDispatcherSyncAcceptanceTests`（Ping/FollowUp/Slow）及 `MviStoreTests`（Ping/Slow）；V2 针对性构建为 0 警告、0 错误。实现待 Root 窄增量复核，未提交。
