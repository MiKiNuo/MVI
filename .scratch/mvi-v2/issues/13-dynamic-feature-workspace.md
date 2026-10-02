# 13: 动态工作区组合独立子 Feature

**What to build:** Avalonia 工作区可以多开同类编辑器、嵌套子功能并动态移除实例，各子功能通过定向中介者通信且能独立运行。

**Blocked by:** 12 — Avalonia 卸载与重挂载保留 Feature。

**Status:** completed

## Acceptance criteria

- [x] 宿主明确建立所有权关系，分别管理视觉挂载、业务实例和服务范围；不拼接共享可变业务 State。
- [x] 同类型多开、同业务对象多开和嵌套复用均可演示，子 Feature 只依赖契约。
- [x] 运行时添加和移除子实例更新活动成员与确定路由；移除后的旧地址拒绝新消息。
- [x] 子实例独立关闭后，未完成资源释放仍由宿主或所有者跟踪；父级后续清理不会漏掉已脱离活动组合的在途资源。
- [x] 演示协调者明确向多个目标发送消息，并通过公开行为验证实例隔离、独立运行、动态路由及关闭后的资源跟踪。

## Implementation and acceptance evidence

2026-10-02 实现与验证完成，前置基线 `625591a`；架构/并发独立审查由 Root 执行，不实现切片14关闭确认。

- `Feature.InstanceId` 提供唯一实例身份。`Feature.Children` 的 `FeatureOwnership.Add` 返回 `FeatureMember`；单一 owner、无环、关闭后接管拒绝由低频结构锁原子检查。`Active`、`Retired` 返回快照，`Revision` 为实际 Add/活动退出条件版本。
- `FeatureMember.Wire` 只接受该子 Feature 拥有的现有端口，管理明确 Mediator 路由回执。业务实例状态独立，通信只经契约；DI Scope 与视觉宿主不自动形成所有权关系。
- 子直接 Close、成员 Remove、父 Close 统一同一结构退出路径：活动删除、Revision 推进、路由解除、唯一子票据转退休；剩余唯一候选恢复，旧明确地址拒绝。未释放子票据与失败记录由父保留。
- Store.Close 分出纯 CommitClose 与门外 Finish。树锁内先停止父/全部活动后代准入并登记原 Store 票据依赖，树锁外才取消、解绑、释放投影和 Scope；父的原唯一票据等待自身执行及全部活动/既有退休子释放，再释放本体 Scope。
- 业务纯转换期间以线程深度标记拒绝进入生命周期/所有权，避免 Store→tree 反向持锁。普通业务 Store 仍各自独立，结构锁范围与升级条件使用 ponytail 注释明确。
- 可判定自等待沿所属 owner 链扩展到父票据；ExecutionOwner 与 Feature 用 ConditionalWeakTable 关联，不建立全局强实例列表。导航请求跨实例依赖环仍由切片15处理。
- `FeatureOwnershipTests` 用公开 TCS/屏障验证实例/Scope隔离、单owner/无环/关闭接管、动态路由、子直接Close/Remove、退休未合作IO、释放故障、嵌套父依赖等待保护、20轮重复Close竞争、20轮Add/父Close竞争、先停全部子准入再取消回调，以及纯规则结构重入拒绝。
- `--v2-workspace` 真实窗口动态多开两个同类同业务ID编辑器、嵌套完整详情，每个本地 View 经独立 AvaloniaFeatureHost。Root协调者逐端口 Send、详情定向 Post；同业务实例亦可独立宿主运行，窗口所有者手工服务在整树真实退出后释放。

实际验证命令与结果：

1. V2 测试项目与 Avalonia 样例 `dotnet build -c Release --no-restore -p:UseSharedCompilation=false` — 均 exit 0，0 警告、0 错误。
2. V2 可执行程序 `--maximum-parallel-tests 1 --treenode-filter "/*/*/FeatureOwnershipTests/*"` — **9/9**，exit 0。
3. V2 可执行程序 `--maximum-parallel-tests 1` — **263/263**，0 失败、0 跳过，exit 0。
4. `rtk proxy dotnet build MiKiNuo.Mvi.slnx -c Release --no-restore -p:UseSharedCompilation=false -v:minimal` — exit 0，0 错误、5 条既有 v1 MVI0013 警告。
5. `rtk proxy sample/MiKiNuo.Mvi.Samples.Avalonia/bin/Release/net10.0/MiKiNuo.Mvi.Samples.Avalonia.exe --verify-v2-workspace --v2-workspace-result=F:/MiKiNuoProjects/MVI/.scratch/mvi-v2/13-workspace-result.txt` — **PASS v2-workspace**，真实 Windows HWND/UI线程、多开/嵌套/路由恢复/退休scopedIO与父Released均已验证，自动退出，exit 0。
6. `rtk proxy git diff --check` — exit 0；修改与新增C#保持CRLF、UTF-8 BOM。
7. 原有/认证样例测试可执行程序 `--maximum-parallel-tests 1` — **239/239**，0 失败、0 跳过，exit 0。
8. Headless.Consumer 默认state/operation/queue/mediator/post五项自检全部PASS，exit 0。

结果文件 `.scratch/mvi-v2/13-workspace-result.txt`；完整日志 `13-build.log`、`13-sample-build.log`、`13-tests.log`、`13-full-v2.log`、`13-solution-build.log`、`13-workspace.log`、`13-original-tests.log`、`13-headless.log`。Root负责独立审查和提交。

## Independent review corrections

2026-10-02 公开复现并修复一个 P1 与两个 P2：

- owner 创建失败 RED：调用者 A 的 Operation 等待工厂 F，F 构造接管 A、启动不合作 scoped IO 后抛错；旧工厂因自等待错误直接释放 Scope，资源 DisposeCount 提前为1，exit 2。现在基类精确 Capture 建立 Store 时即绑定本次 Scope并登记构造退出屏障，目标唯一票据持续拥有清理。若失败回收依赖调用者退出，先抛 `FeatureCreationException` 保留原构造异常与 `Cleanup` 票据，允许调用者退出；释放成功或异常可从原票据观察，不绕过守卫等待、不双 Dispose。未进入基类的失败仍直接回收，非依赖失败继续等待并保留原/聚合异常。新增成功及延后释放异常两变体，原切片10失败兼容验收继续通过。
- 实例相等性 RED：两个按业务ID重写 Equals/hash 的不同实例第二 Add 抛 Duplicate key，exit 2。active/retired字典改用 `ReferenceEqualityComparer.Instance`，Membership只在入集合成功后赋值，业务相等实例分别接管、路由、关闭。
- 原生详情 View RED：移除 First后改旧详情TextBox，真实 verifier 抛 ObjectDisposedException、exit 1。窗口持有detailHost字段，移除对应父时同步Unmount/从Panels移除/Dispose。旧详情控件修改不回写、不抛错，Second宿主不受影响；核心没有耦合视觉树。

修复后实际验证：

1. V2测试项目Release构建 — exit 0，0警告、0错误。
2. `--maximum-parallel-tests 1 --treenode-filter "/*/*/Feature*Tests/*"` — **38/38**，exit 0。
3. 完整V2 `--maximum-parallel-tests 1` — **266/266**，0失败、0跳过，exit 0。
4. 解决方案Release构建 — exit 0，0错误、5条既有v1 MVI0013警告。
5. 原有/认证样例测试 — **239/239**，0失败、0跳过，exit 0。
6. 同一真实 `--verify-v2-workspace` 命令 — **PASS**，exit 0，最终 `13-workspace-result.txt` 已更新。

增量日志：`13-review-red-owner.log`、`13-review-red-identity.log`、`13-review-red-ui.log`/`13-review-red-ui-result.txt`、`13-review-build.log`、`13-review-focused.log`、`13-review-full-v2.log`、`13-review-solution-build.log`、`13-review-original-tests.log`、`13-review-workspace.log`。Root负责上述窄修复的增量复核。

## Constructor self-wait guard

2026-10-02 构造退出屏障增加准确同步构造依赖保护：`CloseTicket` 和工厂失败判断共同检查当前 Creation链中的确切已建立Store实例、激活线程ID与尚未退出的构造屏障。本实例及嵌套构造读取尚在构造的祖先 Released 给明确自等待错误；其他后台线程仅继承 AsyncLocal记录时不误判，Operation依赖保护仍保留。

公开构造测试覆盖直接/嵌套两变体、专门后台线程读取Released允许、构造尾部Scope仍未释放、退出后仅释放一次；不以超时或提前释放通过测试。

- V2测试项目Release构建：exit 0，0警告、0错误，`13-constructor-wait-build.log`。
- 工厂定向回归：**18/18**，exit 0，`13-constructor-wait-factory.log`。
- 受影响完整V2：**268/268**，0失败、0跳过，exit 0，`13-constructor-wait-full-v2.log`。
- 原239样例/真实工作区与38项阶段证据保留；本窄核心依赖修复未重复平台全范围。

最后窄增量：构造依赖复用 `FeatureOwnership.DependsOnExecution(owner, constructingFeature.ExecutionOwner)`，同时覆盖正在构造实例本体和其 owner 祖先。新增公开 F同步创建G、G接管F后失败：明确 `FeatureCreationException.Cleanup` 返回，F构造尾部两个Scope仍活，F退出后Cleanup安全完成、两范围各释放一次；没有新增依赖图或15导航逻辑。

- 最终工厂定向 **19/19**，完整V2 **269/269**，均exit 0。
- V2项目Release构建exit 0，0警告、0错误；日志 `13-nested-owner-build.log`、`13-nested-owner-factory.log`、`13-nested-owner-full-v2.log`。

## Spec coverage

- 用户故事：US21、US22、US23、US24、US25、US26、US29、US30、US38、US50。
- 实现决策：D01、D16、D17、D18、D20、D21、D23、D26。
- 行为验收：T08、T11、T12、T15、T17。

这些编号引用 MVI v2 实施规格；这里表示本任务负责或贡献的覆盖，完整验收仍以该规格为准。

## Root 最终验收

2026-10-02，独立审查的 Scope 自依赖提前释放、值相等混淆实例及详情 View 残留连接均已公开 RED/GREEN 修复并增量复核确认。构造退出屏障新增的直接/嵌套构造自等待也已覆盖；最后一处复用现有所有权依赖谓词，由 Root 核对精确激活线程、未退出条件、实例/owner 祖先匹配以及公开嵌套失败回归后收尾。

最新工厂 19/19、完整 v2 269/269、原有与认证样例 239/239（有效两组合计 508/508），全部通过；真实 Windows 工作区结果 `.scratch/mvi-v2/13-workspace-result.txt` PASS。原有完整构建与真实 UI 证据保留，窄修复只运行受影响验证；最终格式/差异检查通过。本票无遗留重要问题，整树业务确认与导航请求依赖链分别进入 14、15。
