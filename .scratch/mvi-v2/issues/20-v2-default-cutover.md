# 20: 切换唯一 v2 默认协议并退役旧实现

**What to build:** 新默认分支的示例、构建、测试和打包全部使用完整 v2 协议，删除已无调用者的旧运行对象图；v1 仍可从归档恢复。

**Blocked by:** 05 — Latest 搜索拒绝全部迟到反馈；06 — 有界 Queue 顺序处理业务操作；07 — 有界 Parallel 独立完成并行业务；11 — 定向 Post 明确接纳与后续失败；15 — 导航关闭发起者时解除等待环；19 — 三包独立 NuGet 消费验证。

**Status:** completed

## Acceptance criteria

- [x] 确认所有迁移分支的调用者已转入 v2 后执行 contract：移除被替代的 v1 对象图、生成容器、角色样板和过时约束，不添加长期兼容层。
- [x] main 与既有归档标签保持可恢复，工作继续在现有 v2 默认分支；不重复建立归档或再次切换仓库设置。
- [x] 正式、预览和本地流程使用相同三包边界；新分支构建与 CI 只保留一套默认运行协议，消费者诊断与仓库内部规范分开。
- [x] 在集成后的同一候选上运行适用的 T01—T21，包括 Queue、Parallel、Post 与关闭、关闭后的路由、整组确认、两平台和重新打包的独立消费者。
- [x] 同步规格、领域词汇、UML、数据流、示例说明和工程指南；提交功能与产物验收证据，性能达标结论由后续固定负载任务提供。

## Spec coverage

- 用户故事：US01、US02、US03、US05、US18、US19、US28、US33、US34、US35、US38、US39、US40、US45、US51、US52、US53、US54、US55。
- 实现决策：D03、D04、D05、D17、D22、D23、D24、D27、D29。
- 行为验收：T01、T02、T03、T04、T05、T06、T07、T08、T09、T10、T11、T12、T13、T14、T15、T16、T17、T18、T19、T20、T21。

这些编号引用 MVI v2 实施规格；这里表示本任务负责或贡献的覆盖，完整验收仍以该规格为准。

## 集成交付与验收

2026-10-03：默认入口采用 v2 认证，六个 v1 源项目、旧容器/角色样板、旧样例与旧 benchmark 已退役。保留的认证与目录测试迁入 v2 测试工程，当前源工程为 Core、Generators、Avalonia、Godot 四个。Core 实现未修改。当前分支仍为 `codex/mvi-v2`，归档 `archive/v1-2026-10-01` 仍指向 `ed77eb38ecba77a62fac2c0cbbbb35321a540d03`；未修改 main、标签或远程设置。

本地、正式、预览与 CI 复用 `scripts/pack-local.ps1` → `scripts/verify-package-consumers.ps1`。普通托管 CI 使用明确的 `NonGraphical` 范围，记录 `PASS_NON_GRAPHICAL / nativeUi=false`；本机完整入口实际运行两个原生 GUI，记录 `PASS / full-native-ui / nativeUi=true`。两种范围均已实际运行。

- 完整入口：`rtk proxy pwsh -NoProfile -File scripts/pack-local.ps1 -Version 2.0.0+cutover20.build.7`，exit 0，Release 0 警告/0 错误，TUnit 串行 **358/358**，失败/跳过均 0；三包、四外部消费者、两个真实 UI、20 个 SARIF 原声明定位全部通过。
- CI 范围：`rtk proxy pwsh -NoProfile -File scripts/pack-local.ps1 -Version 2.0.0-cutover20-ci.final -NonGraphical`，exit 0，**358/358**，四消费者编译/Core与Dual运行/20诊断通过，真实 UI 明确未运行。
- 两轮候选源 SHA-256 均为 `DDB5095A2EBFF9E1BE92F02C6EB5FA646DF34FC819577E86E8482FBF2167AE9B`，114 个输入在 Root 复核时 **0 变化**。完整结果位于 `C:/Users/luoji/AppData/Local/Temp/mvi-package-consumers-1f154ff5fd4d46219aa22d66f0935426/result.json`；CI 范围结果位于 `C:/Users/luoji/AppData/Local/Temp/mvi-package-consumers-be6dffa7b0434062a615818720e496a5/result.json`。交付包与证据同步保存在 `artifacts/packages/<version>/`。
- 原生入口复验：`.scratch/mvi-v2/20-verify-platforms.ps1` 依次运行 Input、Search、Auth、Remount、Workspace、Close、Navigation 七个 Avalonia verifier，并在本次外部 NuGet Godot 消费者上运行 HUD。八项均 exit 0 / PASS；汇总 `.scratch/mvi-v2/20-platform-results.json`，运行日志 `.scratch/mvi-v2/20-platforms-final.log`。
- 文档/图示：README、词汇、工程指南、当前架构及七份 Mermaid 已同步；14 个 SVG/PNG 产物均生成并逐图检查，Root 核对 `.scratch/mvi-v2/20-diagram-exports.json` 中 SHA-256 **0 不匹配**。交互 JSON/HTML 与既有回执字节未改，独立审查确认其闭环仍有效。

## T01—T21 证据映射

以下测试组均包含在本次 358/358 串行运行中；原生与包消费结果使用上述同候选源。

| 条目 | 当前权威证据 |
| --- | --- |
| T01 | StateLoopTests、DeclarationTests、ProjectionTests；真实 Input 与 HUD |
| T02 | OperationTests、OperationCommandTests、V2AuthFlowTests；真实 Auth |
| T03 | OperationTests.ValidationAndStartingInputAreAtomicAgainstConcurrentEditing 的启动验证/快照竞争 |
| T04 | OperationTests、V2AuthFlowTests；真实 Auth 与 Godot Composition 慢 IO |
| T05 | OperationTests、OperationProjectionTests；真实 Auth/Composition 的成功、业务失败、故障、取消及完成先于绘制 |
| T06 | OperationQueueTests、OperationParallelTests、MediatorOperationPolicyTests |
| T07 | LatestOperationTests；真实 Search |
| T08 | MediatorRequestTests、FeatureOwnershipTests；真实 Workspace/Composition 同型隔离 |
| T09 | MediatorPostTests、MediatorOperationPolicyTests |
| T10 | MediatorRequestTests 的等待取消/执行取消矩阵 |
| T11 | FeatureOwnershipTests、MediatorRequestTests；Workspace/Composition 明确端口与多实例 |
| T12 | ProjectionTests、ProjectionDeclarationTests；真实 Remount/Workspace/Composition |
| T13 | CloseConfirmationTests；真实 Close |
| T14 | CloseConfirmationTests；真实 Close 的条件变化重验 |
| T15 | FeatureOwnershipTests、FeatureLifetimeTests；真实 Workspace/Composition 退休子跟踪 |
| T16 | NavigationRequestTests；真实 Navigation |
| T17 | FeatureLifetimeTests、FeatureFactoryTests；真实 Workspace/Composition 不合作 IO 与真实退出后释放 |
| T18 | FeatureFactoryTests；外部 Core/Dual 标准 DI 创建、隔离及释放 |
| T19 | DeclarationTests、OperationDeclarationTests、RequestHandlerDeclarationTests、IncrementalGenerationTests；20 外部编译诊断与合法 shadow handler |
| T20 | ProjectionTests；真实 Input/Search/Workspace/HUD 的线程、版本、合并与业务计数 |
| T21 | 本次 fresh 三包、Core/Avalonia/Godot/Dual 四消费者；唯一 Core analyzer、无运行时 generator/Roslyn、无源码引用 |

## 独立审查与修复

`cutover20_review` 独立只读审查发现两个 P2，均完成实际 RED/GREEN 与增量复核：

1. 输出目录混入旧/第四包：导出前拒绝清单之外 `.nupkg`，保留原文件，拒绝发生前不复制本次三包；干净目录导出恰好三包且字节一致。`.scratch/mvi-v2/20-output-boundary-{red,green}.log` 与实际 AST 分支测试 `test/MiKiNuo.Mvi.PackageConsumers/verify-output-boundary.ps1` 为证据。
2. 合法 NuGet 版本规范化：实际 `2.0.0+cutover20.build.7` RED 的 pack 成功但旧文件名识别失败；现使用 SDK 自带 `NuGet.Versioning` 标准规则，身份/文件名/资产比较采用 `2.0.0`，build/pack 保留原输入。三种规范化用例及该完整 metadata 版本全部 GREEN，见 `.scratch/mvi-v2/20-metadata-red.log`、`.scratch/mvi-v2/20-boundaries-green.log` 与最终完整入口。

增量审查确认两 P2 可关闭、未引入新的实质问题，导出三个包 SHA-256 与验证集合一致。性能目标仍由 21、22 固定负载测量与数值预算证明，本票不宣称性能已达标。
