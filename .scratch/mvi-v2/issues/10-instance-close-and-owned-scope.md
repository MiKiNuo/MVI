# 10: 单实例逻辑关闭与服务范围安全释放

**What to build:** 一个经标准 DI 创建的独立 Feature 可以先逻辑关闭并拒绝新业务，待在途操作真实退出后再释放自己拥有的服务范围。

**Blocked by:** 08 — 独立 Feature 通过 Mediator 定向请求。

**Status:** completed

## Acceptance criteria

- [x] 提供本场景所需的最小标准 IServiceProvider 创建工厂：每次创建独立实例与 Scope，基本创建失败回收本次范围；直接构造可用，外部手工传入服务不归框架释放。
- [x] 逻辑关闭有明确提交点，拒绝新业务和迟到反馈，请求取消在途执行并使目标通信不可再接纳；CloseResult 与独立释放完成信号可区分。
- [x] 以可控在途服务证明 Scope 及其资源在最后一个使用者退出前保持可用，之后只释放一次；未合作执行不会被当作已经结束。
- [x] 释放等待超时或被取消不等于 Released；释放故障有可观察结果；仍在释放的实例保持跟踪。
- [x] 通过公开工厂、请求入口和关闭入口覆盖空闲关闭、在途关闭、迟到写回、不合作 IO、重复关闭及资源归属，不为测试添加私有对象探测接口。

## Implementation and acceptance evidence

2026-10-02 实现与验证完成，依据 ADR 0020、0023；公共生命周期与资源并发修改的独立审查由 Root 进行，审查阶段不扩大切片范围。

- 非泛型 `Feature` 提供异构生命周期共同入口；`Feature<TState>.Close()` 立即提交逻辑关闭，重复调用复用唯一 `CloseResult` 与 `CloseTicket`。`IsClosed` 与 `CloseTicket.Released` 分别观察逻辑关闭和真实释放；`ReleaseResult` 保留释放成功及异常。
- Store 提交门内拒绝新输入、操作和迟到反馈；新操作返回 `Rejected / Closed`，输入与反馈抛出 `FeatureClosedException`。端口停用、投影 Dispose 与取消回调均在门外，已关闭投影的旧回调失效。
- 原子接纳登记所有真实执行，包含 Parallel 全体与 Latest 已失效但尚未退出的旧执行。关闭清空 Queue 等待项且不再启动；仍在执行的工作经过业务方法、全部嵌套 Track、取消回调及 CTS/框架清理后才注销。
- 关闭在门内为取消回调建立工作屏障，门外 `CancelAsync`。取消/终止清理期间仍允许有效所属上下文登记子工作；断开并等已进入的链接令牌回调后再次排空工作，最终才停止 Track 接纳。
- `FeatureFactory.CreateAsync<TFeature>(IServiceProvider)` 使用中央既有的 DI 10.0.11 与 `CreateAsyncScope`。构造选择保持标准 ActivatorUtilities 的无显式参数规则：标记构造优先、最长可解析构造、并列歧义拒绝、默认值和 keyed 依赖；依赖由标准范围提供方解析。每次创建独立范围，不要求把 Feature 注册为 scoped 服务；singleton 属根 provider，直接传入服务不由框架释放。
- 工厂先预留真实目标对象，再通过 [MethodBase.Invoke 的目标对象重载](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.methodbase.invoke?view=net-10.0) 执行所选完整构造函数，并通过 `DoNotWrapExceptions` 保留原构造异常。构造捕获只关联该对象身份，且只在 Feature 基类已经建立 Store 后启用失败关闭；依赖解析、base initializer 和构造体中直接创建的同型外部对象不被误认。构造异常以及构造后已关闭导致的范围绑定失败均先等待真实目标退出再回收本次范围；双故障保留聚合异常。嵌套工厂恢复父构造边界，不自动拥有独立子实例或外部实例。
- 当前自等待保护覆盖所属操作直接读取自身 `CloseTicket.Released`，给出明确错误；导航依赖链保护由切片15负责。未合作工作可能使释放没有时间上界；等待取消和超时不释放其资源。

公开行为验证均使用 TCS 或显式屏障，不使用延时：

- `FeatureLifetimeTests`：空闲/重复关闭、两个 scoped 独立与 singleton 共享、外部服务、关闭后输入/操作/端口拒绝、旧投影回调失效、已接纳请求与已提交状态、不合作 IO、释放等待取消/零超时、关闭后嵌套 Track、取消回调及其工作、Parallel 全体、Latest 旧执行、Queue 等待项不启动和直接自等待保护。
- `FeatureFactoryTests`：基本构造失败、构造已启动工作后失败、已关闭对象绑定失败、构造与回收双故障、真实释放过程及释放异常、嵌套工厂与外部实例归属。
- 切片09测试只将“目标令牌不可取消”的内部假设改为“取消等待后目标令牌未取消”，目标拥有的令牌现在也支持实例关闭；两令牌业务语义继续由完整回归验证。

实际命令与结果：

1. `rtk proxy dotnet restore MiKiNuo.Mvi.slnx` — exit 0，核心 DI 引用及消费者传递依赖恢复成功。
2. `rtk proxy dotnet build test/MiKiNuo.Mvi.V2.Tests/MiKiNuo.Mvi.V2.Tests.csproj -c Release --no-restore -p:UseSharedCompilation=false` — exit 0，0 警告、0 错误。
3. `rtk proxy test/MiKiNuo.Mvi.V2.Tests/bin/Release/net10.0/MiKiNuo.Mvi.V2.Tests.exe --maximum-parallel-tests 1 --treenode-filter "/*/*/Feature*Tests/*"` — 17/17 通过，0 失败、0 跳过，exit 0。
4. `rtk proxy test/MiKiNuo.Mvi.V2.Tests/bin/Release/net10.0/MiKiNuo.Mvi.V2.Tests.exe --maximum-parallel-tests 1` — 230/230 通过，0 失败、0 跳过，exit 0。
5. `rtk proxy dotnet build MiKiNuo.Mvi.slnx -c Release --no-restore -p:UseSharedCompilation=false -v:minimal` — 最终增量运行 exit 0，0 警告、0 错误。第一次命令受60秒执行预算中断，确认进程结束后重跑成功。
6. `rtk proxy git diff --check` — exit 0；修改与新增的 C# 文件保持 UTF-8 BOM 和 CRLF。

完整证据日志：`.scratch/mvi-v2/10-build.log`、`10-focused-tests.log`、`10-full-v2-tests.log`、`10-solution-restore.log`、`10-solution-build.log`。

## Independent review corrections

2026-10-02 独立审查发现并公开复现两个 P1，保持在切片10范围内修复；以下证据取代首轮测试计数作为最终验收结果。

- 工厂错捕获 RED：`FailedActivationClosesOnlyItsRealTargetAndWaitsForItsWork` 覆盖依赖解析、base initializer、构造体三种同型外部对象来源，前两项失败（外部对象被误关），构造体变体通过，exit 2。准确对象身份关联修复后，三种路径均保留外部实例并等待真实目标工作退出才释放 Scope。
- 外部取消尾部 RED：`InlineExternalCancellationContinuationCannotReleaseCallbackResources` 使用没有异步续体选项的 TCS，在目标令牌回调中内联结束业务等待，回调尾部继续持有资源；真实运行身份被过早清除，exit 2。修复在已取消的终止清理路径使用 `ConfigureAwaitOptions.ForceYielding` 离开回调线程，再注销链接令牌并重新排空尾部登记的 Track 工作。正常完成路径不增加调度。
- 构造兼容回归：与标准 `ActivatorUtilities.CreateInstance` 对比 marked/this 构造链、最长可解析选择、provider 注入、可选参数、nullable enum、keyed 服务与字段初始化；明确拒绝并列候选、多个 marked 和缺失依赖。另证明构造未进入 Feature 基类时可以回收已解析服务而不访问未初始化 Store。
- 修复后 `rtk proxy dotnet build test/MiKiNuo.Mvi.V2.Tests/MiKiNuo.Mvi.V2.Tests.csproj -c Release --no-restore -p:UseSharedCompilation=false` — exit 0，0 警告、0 错误。
- 修复后同一 `--treenode-filter "/*/*/Feature*Tests/*" --maximum-parallel-tests 1` 针对性运行 — **26/26**，0 失败、0 跳过，exit 0。
- 修复后完整 v2 可执行程序 `--maximum-parallel-tests 1` — **239/239**，0 失败、0 跳过，exit 0。
- 修复后 `rtk proxy dotnet build MiKiNuo.Mvi.slnx -c Release --no-restore -p:UseSharedCompilation=false -v:minimal` — exit 0，0 错误，5 条与基线一致的 v1 `MVI0013` 警告。
- 修复后 `rtk proxy test/MiKiNuo.Mvi.Tests/bin/Release/net10.0/MiKiNuo.Mvi.Tests.exe --maximum-parallel-tests 1` — 原有与认证样例测试 **239/239**，0 失败、0 跳过，exit 0。
- 修复后 `rtk proxy test/MiKiNuo.Mvi.Headless.Consumer/bin/Release/net10.0/MiKiNuo.Mvi.Headless.Consumer.exe` — 状态、操作、Queue 与 Mediator 四个公开消费者自检全部 PASS，exit 0。

新增完整日志：`.scratch/mvi-v2/10-review-red-capture.log`、`10-review-red-callback.log`、`10-review-build.log`、`10-review-focused-tests.log`、`10-review-full-v2-tests.log`、`10-review-solution-build.log`、`10-review-original-tests.log`、`10-review-headless.log`。Root 负责这两个修复的增量复核与最终提交。

## Spec coverage

- 用户故事：US14、US21、US27、US37、US40、US41、US42。
- 实现决策：D01、D09、D14、D15、D20、D23、D24、D26。
- 行为验收：T05、T08、T09、T17、T18。

这些编号引用 MVI v2 实施规格；这里表示本任务负责或贡献的覆盖，完整验收仍以该规格为准。

## Root 最终验收

2026-10-02，两个 P1 经公开回归先 RED 再 GREEN；独立增量复核确认准确构造目标关联和外部取消回调退出屏障均已修复，未发现修复引入的实质问题。最终生命周期/工厂 26/26、v2 239/239、原有与认证样例 239/239，合计 478/478，失败和跳过 0；无界面四组自检全部 PASS。Release 解决方案构建 0 错误，5 条已知 v1 夹具 MVI0013 警告；最终差异和格式检查通过。

本票的单实例逻辑关闭、真实退出后释放、标准 DI 创建与失败回收已完成。整树确认和导航跨实例依赖等待保护继续由 14、15 对应的公开行为验收覆盖。
