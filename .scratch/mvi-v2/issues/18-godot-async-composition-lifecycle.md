# 18: Godot 异步子 View 与生命周期闭环

**What to build:** Godot 场景可以挂载多个独立子 Feature，运行异步操作并展示 Loading，通过中介者更新目标，卸载和关闭保持节点与资源归属正确。

**Blocked by:** 13 — 动态工作区组合独立子 Feature；17 — Godot HUD 跑通输入与字段投影。

**Status:** completed

## Acceptance criteria

- [x] 复用已有核心 Operation 与组合协议，真实 Godot View 能调用操作并显示运行、业务失败和故障状态。
- [x] 同类型子实例可多开并通过确定契约路由通信，不写跨子 View 订阅代码，也不把两个实例状态串用。
- [x] 卸载只释放所属绑定，重新挂载恢复当前状态；旧回调失效，稳定挂载不重复创建；移除节点不影响无关节点。
- [x] 逻辑关闭拒绝新业务和迟到写回，仍在途的工作结束后才释放其资源；操作等待不依赖渲染完成。
- [x] 用真实引擎场景覆盖慢 IO、动态子实例、实例替换、卸载重挂载、定向消息和在途关闭，并保留相关自动化行为验证。

## Spec coverage

- 用户故事：US03、US11、US13、US14、US15、US21、US22、US24、US26、US31、US32、US37、US38、US40、US47。
- 实现决策：D01、D12、D14、D16、D17、D18、D21、D23、D24、D25。
- 行为验收：T04、T05、T08、T11、T12、T15、T17、T20。

这些编号引用 MVI v2 实施规格；这里表示本任务负责或贡献的覆盖，完整验收仍以该规格为准。

## Implementation

- 新增托管 `GodotFeatureHost`，持有原生容器、现有 Feature 和本地 View 工厂。稳定 Mount 复用，实际创建与挂载采用原生 deferred 阶段及 revision 失效校验；方法返回时工厂可尚未执行。容器退出只移除并 QueueFree 自有 View，保留 Feature、工厂及其他节点；重入从最新 Snapshot 创建投影。Unmount / Dispose 不关闭业务实例。
- 新增 `Composition.tscn`、`CompositionView`、`EditorView` 和独立编辑器 / scoped 服务。View 通过 17 的 GodotProjection 连接生成输入及操作；分别展示 Load 和 HandleLoad 的已提交运行事实，业务失败显示为正常 Completed 的失败值，非预期故障独立显示。
- 两个同类型、同业务对象 ID 的实例拥有不同 InstanceId、State、服务范围和端口。宿主使用 Core Children.Add / FeatureMember.Wire / Remove 建立所有权及明确 Mediator 路由；替换、退休实例和实际释放完全复用 Core，未修改 Core 或 17 的投影适配。
- 场景具有原生加载、定向发送、替换、隐藏与显示按钮。完成等待不要求绘制；卸载保留业务实例和消息处理能力，重挂与容器退出重入恢复当前状态；窗口退出经逻辑关闭和 Released 后释放根提供方。
- 新增公开宿主 Compile + Emit 消费验证。未引入跨 Feature 或逐字段 R3 订阅，也未扩展发布、默认入口或旧平台对象图。

## Real engine acceptance

2026-10-03，使用切片 17 已验证的 `4.6.2.stable.mono.official.71f334935`，Windows / OpenGL 3.3 Compatibility / GTX 1060 5GB，真实窗口与截图 1000×700。

```powershell
rtk proxy dotnet build sample/MiKiNuo.Mvi.Samples.Godot/MiKiNuo.Mvi.Samples.Godot.csproj -c Debug --no-restore -p:UseSharedCompilation=false
rtk proxy .scratch/mvi-v2/tools/godot-4.6.2/Godot_v4.6.2-stable_mono_win64/Godot_v4.6.2-stable_mono_win64_console.exe --path sample/MiKiNuo.Mvi.Samples.Godot res://Composition.tscn --rendering-method gl_compatibility --resolution 1000x700 -- --composition-self-test --result-path=F:/MiKiNuoProjects/MVI/.scratch/mvi-v2/18-composition-result.json
```

- 原生加载及定向发送按钮由 `Input.ParseInputEvent` 的鼠标事件驱动；本地慢 IO 显示 Loading、保留期间的新编辑并提交结果。业务拒绝为 `Completed/BusinessFailure`，服务故障为 Faulted，定向慢 IO / fault 明确显示在选定目标，另一实例业务状态不被串用；多候选无目标时返回 AmbiguousTarget。
- 主线程暂未进入下一帧时，同步等待 Core 操作已正常完成并提交 State，旧 View 版本仍未更新；随后帧显示已完成响应，证明等待不依赖渲染。
- 稳定 Mount 不再创建 View。卸载后原生旧输入与排队旧回调失效；隐藏期间定向消息仍完成，重挂恢复最新输入及响应。容器原生退出时只移除自有 View，装饰 Label 保留；重入重建唯一当前投影。
- 替换创建独立 scoped 实例，退休旧实例释放一次、旧端口 TargetUnavailable，其他实例和无关节点保留。完整轮受控创建 6 个 View。
- 不合作 IO 忽略取消继续持有资源。子实例逻辑关闭后拒绝输入、新操作及旧地址；父仍保留退休子释放票据，资源未提前 Dispose。真正 IO 返回尾部仍可使用资源，晚反馈被取消拒绝，State 不被覆盖；最后父票据成功，两个当前子 scope 各释放一次。
- FramePostDraw 后保存真实 viewport PNG，截图显示两个独立编辑器、定向 Fault 及本地 Loading，Root 与 Worker 已核对布局完整可读。

## Native Ready regression

窄审查发现子 Ready 期间父 Container 正在遍历孩子，直接 AddChild 会原生报错但不抛 C# 异常。真实 RED 输出 `Parent node is busy setting up children`，结果退出 1，View 无 parent。最小修复为所有 Mount 请求通过 deferred + revision 创建，在完成 AddChild 后验证实际 parent，并回收未被接纳节点。GREEN 只创建 1 个 View、parent 正确、投影可用、两个装饰 / Ready child 保留，退出 0，无意外原生 ERROR / WARNING。Root 已核对修复范围和完整 GREEN。

## Verification

| 验证 | 结果 | 证据 |
| --- | --- | --- |
| Host 首步 Debug | 0 警告、0 错误 | `18-host-build.log` |
| Ready RED | Debug 编译成功；真实引擎退出 1，原生 parent busy 和错误挂载回归 | `18-ready-red-build.log`、`18-ready-red.log`、`18-ready-red-result.json` |
| Ready GREEN | 编译及真实引擎退出 0，正确唯一挂载且装饰节点保留 | `18-ready-green-build.log`、`18-ready-green.log`、`18-ready-green-result.json` |
| 完整 Debug | 退出 0，0 警告、0 错误 | `18-debug-acceptance-build.log` |
| 完整真实组合 | 全项 PASS，退出 0，无意外 ERROR / WARNING | `18-composition-run.log`、`18-composition-result.json` |
| 真实截图 | 1000×700，Loading / Fault / 两独立实例完整可读 | `18-composition-result.png` |
| 公开宿主及投影声明消费 | 8/8 通过 | `18-declaration-tests.log` |
| 全 V2 | 333/333 通过，0 跳过 | `18-full-v2.log` |
| Release solution | 退出 0，0 错误，5 个既有 v1 负例 MVI0013 警告 | `18-solution-build.log` |
| Diff / 格式 | 任务范围 diffcheck 退出 0，六个 C# 文件均 BOM / CRLF / 无行尾空白 | `18-diff-check.log`、`18-format-check.log` |

实现与真实验收完成，待 Root 确认最终门禁与票据；未提交。
