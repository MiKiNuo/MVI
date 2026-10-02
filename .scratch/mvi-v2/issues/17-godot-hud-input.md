# 17: Godot HUD 跑通输入与字段投影

**What to build:** 真实 Godot 场景通过 State、Feature 和 View 实现 HUD 输入与显示，按有关字段更新并可承受高频状态变化。

**Blocked by:** 01 — 独立核心消费者跑通纯状态闭环。

**Status:** completed

## Acceptance criteria

- [x] Godot 平台适配及所需生成桥接从强类型输入入口完成 MVI 回写和显示，不依赖 Avalonia 运行时。
- [x] 采用 Godot 原生输入、节点和 UI 调度，View 根据已提交快照投影；不加入跨 Feature 或逐字段框架 R3 订阅。
- [x] 有关字段更新、版本单调和默认展示合并在真实宿主成立；业务输入和一次性行为不被展示合并吞掉。
- [x] 只调整接入真实 Godot 宿主必需的旧目录与构建假设，保留仍有效的现有验证和约定目录布局。
- [x] 提供可运行 HUD 场景及用户声明消费测试；记录真实输入、线程和高频展示行为，Stub 测试不替代真实引擎验收。

## Spec coverage

- 用户故事：US01、US03、US06、US07、US10、US46、US47、US50。
- 实现决策：D02、D04、D05、D07、D10、D25。
- 行为验收：T01、T19、T20。

这些编号引用 MVI v2 实施规格；这里表示本任务负责或贡献的覆盖，完整验收仍以该规格为准。

## Root 最终验收

2026-10-03，Root 已检查真实 HUD 截图、JSON 和平台适配。窄审查的通知途中解绑 P2 已在真实引擎先 RED 再 GREEN；双向回调共享失效状态并在读取/写入前短路，原生目标失效后安全解绑。

固定 Godot 4.6.2 图形进程退出 0；三项重入释放回归均 true，1000 次业务输入/1 次等待展示、主线程/后台线程 2/4、原生按钮与 Actions 各一次、版本单调。消费声明 7/7、完整 v2 332/332、架构 2/2；Debug 0 警告/错误，Release 0 错误及 5 条原 v1 负例警告；格式/差异通过。17 完成，动态多实例与异步生命周期进入 18。

## Implementation

- 新增 `MiKiNuo.Mvi.Godot`，仅引用 Core 与 GodotSharp。`GodotProjection(view)` 在原生主线程连接 `SceneTree.ProcessFrame`，后台投递仅访问托管锁与队列；一帧只处理入帧时已有的有限回调批次，新投递留到下一帧。Core 的 `Coalesce` 合并等待快照，`EveryCommit` 保留中间快照，这与平台的入帧批次边界分别实现。
- `Create(feature.CreateProjection, mode)` 复用生成投影；`BindInput(projection, LineEdit, p => p.PlayerName)` 校验直接可编辑属性，连接原生 TextChanged 和单一本地 PropertyChanged，纠正归一化值并防止回显反馈。View 退出树或释放连接时断开事件、释放所创建的输入和投影、清空旧队列。
- 新增真实 Godot HUD：HudState、HudFeature、HudView，加原生 `Hud.tscn` / `project.godot`。玩家名称走生成输入，计分纯规则逐次处理输入，原生领奖按钮调用生成 OperationCommand，显示按有关字段读取已提交投影。
- Godot 宿主使用 `Godot.NET.Sdk/4.6.1`、net10.0、EnableDynamicLoading，SDK 隐式包通过属性覆盖为 4.6.2。宿主移除本地中央 GodotSharp 版本项以避免 NU1009，未增加重复 PackageReference；解析产物确认 GodotSharp、GodotSharpEditor、Godot.SourceGenerators 均为 4.6.2，MVI 运行时仅 Core / Godot，见 `17-resolved-dependencies.log`。
- 解决方案登记两项目；目录测试只扩展 Godot 样例白名单，保留原专属 BuildTime 禁止规则。用户声明消费测试以真实 Godot 类型 Compile + Emit 证明生成输入和原生连接 API 可消费。
- 窄审查修复：两个方向的捕获回调共用 InputConnection.IsDisposed，Dispose 先失效再解绑；较早的同次 PropertyChanged / TextChanged 处理器释放连接后，后续回调不再触碰控件或回写投影。原生控件已经释放时，解绑通过 GodotObject.IsInstanceValid 安全跳过目标访问。

## Real engine acceptance

2026-10-03，Windows，`4.6.2.stable.mono.official.71f334935`，OpenGL 3.3.0 Compatibility / NVIDIA GeForce GTX 1060 5GB。使用 Root 已验证官方 SHA512 的本地引擎，真实图形窗口 1000×700。

```powershell
rtk proxy dotnet build sample/MiKiNuo.Mvi.Samples.Godot/MiKiNuo.Mvi.Samples.Godot.csproj -c Debug --no-restore -p:UseSharedCompilation=false
rtk proxy .scratch/mvi-v2/tools/godot-4.6.2/Godot_v4.6.2-stable_mono_win64/Godot_v4.6.2-stable_mono_win64_console.exe --path sample/MiKiNuo.Mvi.Samples.Godot --rendering-method gl_compatibility --resolution 1000x700 -- --self-test --result-path=F:/MiKiNuoProjects/MVI/.scratch/mvi-v2/17-reentrant-green-result.json
```

- Debug 构建输出 `.godot/mono/temp/bin/Debug`，0 警告、0 错误；真实引擎自检退出 0，HUD PASS。
- 输入源为 `Input.ParseInputEvent` 的焦点 LineEdit 键事件和原生 Button 鼠标事件。名称 `r` 归一化为 `R`，重复同值输入也纠正原生编辑值；无关 Telemetry 输入不刷新名称或分数控件。
- 主线程 managed ID 2，后台 ID 4；后台 1000 次计分输入逐次提交 1000 个版本，产生 1 次等待批次展示与 1 次 Score 通知。业务计分为 1000，输入计数为 1000；一次按钮事件完成一次领奖，并在后续帧产生 1 次 Actions 字段通知与原生显示。
- 展示版本为 `[0,1,2,3,1003,1004,1006,1008]`，所有 PropertyChanged 与控件更新断言为主线程。释放连接后排队旧回调和原生输入不能再更新 View 或 State；两次额外输入保留到业务 State，重建连接立即显示最新分数 1005 / 输入计数 1002。
- 等待 `RenderingServer.FramePostDraw` 后保存真实 viewport PNG，已视觉核对名称 R、Score 1005、奖励次数 1，布局完整。
- 帧内解绑 RED 在真实引擎退出 1：较早 PropertyChanged 已释放连接，原捕获回调仍将旧控件更新。GREEN 三项真实指标均为 true：`reentrantPropertyChangedDispose`、`reentrantViewFree`、`reentrantNativeInputDispose`；释放 View 后尾部多播观察仍完成，原生输入同次解绑不回写业务状态。原有线程、1000/1、字段和版本指标保持，无引擎 ERROR / WARNING。Root 已核对这项小修复及实际 JSON。

## Verification

| 验证 | 结果 | 证据 |
| --- | --- | --- |
| 实际 Godot Debug 构建 | 退出 0，0 警告、0 错误 | `17-reentrant-green-build.log` |
| 帧内解绑 RED | Debug 构建成功，真实引擎退出 1，旧捕获回调在解绑后仍更新目标 | `17-reentrant-red-build.log`、`17-reentrant-red.log`、`17-reentrant-red-result.json` |
| 实际图形 GREEN | HUD PASS，三项重入回归通过，退出 0 | `17-reentrant-green.log`、`17-reentrant-green-result.json` |
| 真实截图 | 1000×700，内容与最终状态一致 | `17-reentrant-green-result.png` |
| 投影声明及 Godot 消费 | 7/7 通过 | `17-declaration-tests.log` |
| 全 V2 | 修复后 332/332 通过，0 跳过 | `17-reentrant-full-v2.log` |
| 目录架构 | 2/2 通过 | `17-architecture-tests.log` |
| restore / Release solution | 均退出 0；修复后构建 0 错误，5 个既有 v1 负例 MVI0013 警告 | `17-restore.log`、`17-reentrant-release-build.log` |
| Diff / C# 格式 | 任务范围 diffcheck 退出 0，六个 C# 文件均 BOM / CRLF / 无行尾空白 | `17-diff-check.log`、`17-format-check.log` |

`17-debug-build.log` 保留实际 NU1009 根因；后续构建日志保留原生信号委托类型和 CA2000 的修正证据。初轮 `17-hud-acceptance-*`、`17-full-v2.log`、投影声明 7/7 与目录架构 2/2 证据保留；最终图形、Debug / Release 与全 V2 以 `17-reentrant-*` 为准。源码与验收已完成，待 Root 确认最终票据与门禁；未提交。
