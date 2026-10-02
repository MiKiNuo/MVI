# 12: Avalonia 卸载与重挂载保留 Feature

**What to build:** Avalonia 页面卸载时释放自己的展示连接，但仍被宿主拥有的 Feature 继续存在；重新挂载恢复最新状态。

**Blocked by:** 02 — Avalonia 表单输入与状态投影闭环；10 — 单实例逻辑关闭与服务范围安全释放。

**Status:** completed

## Acceptance criteria

- [x] View 卸载不等同于 Feature 关闭；保留实例时 State 与有效操作继续遵守运行协议。
- [x] 卸载释放本地输入和投影连接，旧 View 已排队回调失效；新 View 不被旧版本或旧连接更新。
- [x] 同一实例稳定挂载不重复创建 View 或连接；替换实例时只切换对应连接并正确显示新实例状态。
- [x] Feature 逻辑关闭后 View 可卸载，Scope 仍按执行退出条件释放；UI 不等待释放完成才能结束交互。
- [x] 真实 Avalonia 宿主与可控调度测试覆盖重复挂载、卸载期间状态变化、重挂载、实例替换和关闭中的卸载。

## Implementation and acceptance evidence

2026-10-02 实现与真实 Windows Avalonia 验证完成，基线 `4a5a930`；Root 负责最终验收与提交。

- `AvaloniaFeatureHost : ContentControl, IDisposable` 提供强类型 `Mount(existingFeature, viewFactory)`、`Unmount` 与 `Dispose`。只拥有可释放 View 的本地连接，不关闭或等待 Feature；所有入口使用 UI 线程检查，StyleKey 使用原生 ContentControl 主题。
- 同实例且 Content 仍是自有 View 时稳定复用，不重复 factory/投影。不同实例先成功构造新 View 再替换，新工厂失败保留原合法挂载；同实例失去自有 Content 后先释放旧投影再重建，遵守唯一活动投影规则。
- 视觉 Detach 释放 View 及输入连接，保留实例与工厂；再次 Attach 在没有外部 Content 时创建最新投影。显式 Unmount 忘记挂载意图；Dispose 永久结束宿主。只清 ReferenceEquals 的自有 Content，不移除调用方后来设置的无关内容。
- Login/Register/ResetPassword 表单增加接收已有 Feature 的构造入口，原 service 构造转发复用；没有引入 v1 ViewModel/Bindings/对象图。
- `--v2-remount` 展示窗口显式持有登录 Feature，按钮只卸载/重建本地 View，窗口关闭才逻辑关闭实例。`--verify-v2-remount` 复用 Opened→验收→写报告→Shutdown 自动链。
- 可控 Queue<Action> 投影回归验证卸载后旧回调无效、新投影从当前快照开始、实例替换保持隔离；真实原生窗口覆盖稳定 factory 一次、旧 TextBox 断开、卸载时 IO 继续、最新输入/结果/Busy 重挂载、替换旧回调、工厂失败、视觉 Detach/Attach、无关 Content、三个表单复用以及同实例失去 Content 后重建。
- 真实标准 DI scoped 不合作服务证明 Feature.Close 后 View 立即 Unmount、Released 尚未完成且服务未释放；IO 尾部仍能使用资源，真实退出后 Scope 只释放一次。

实际验证命令与结果：

1. `rtk proxy dotnet build sample/MiKiNuo.Mvi.Samples.Avalonia/MiKiNuo.Mvi.Samples.Avalonia.csproj -c Release --no-restore -p:UseSharedCompilation=false` — exit 0，0 警告、0 错误。
2. `rtk proxy dotnet build test/MiKiNuo.Mvi.V2.Tests/MiKiNuo.Mvi.V2.Tests.csproj -c Release --no-restore -p:UseSharedCompilation=false` — exit 0，0 警告、0 错误。
3. V2 可执行程序 `--maximum-parallel-tests 1 --treenode-filter "/*/*/ProjectionTests/*"` — **9/9**，exit 0。
4. V2 可执行程序 `--maximum-parallel-tests 1` — **254/254**，0 失败、0 跳过，exit 0。
5. `rtk proxy dotnet build test/MiKiNuo.Mvi.Tests/MiKiNuo.Mvi.Tests.csproj -c Release --no-restore -p:UseSharedCompilation=false` — exit 0，0 错误、5 条既有 v1 MVI0013 警告。
6. 原有/认证样例测试可执行程序 `--maximum-parallel-tests 1` — **239/239**，0 失败、0 跳过，exit 0。
7. `rtk proxy sample/MiKiNuo.Mvi.Samples.Avalonia/bin/Release/net10.0/MiKiNuo.Mvi.Samples.Avalonia.exe --verify-v2-remount --v2-remount-result=F:/MiKiNuoProjects/MVI/.scratch/mvi-v2/12-remount-result.txt` — **PASS v2-remount**，真实 Windows HWND/UI线程/原生模板与非零表单布局均已确认，程序自动退出，exit 0。

结果文件：`.scratch/mvi-v2/12-remount-result.txt`。完整日志：`12-sample-build.log`、`12-v2-build.log`、`12-projection.log`、`12-full-v2.log`、`12-original-build.log`、`12-original-tests.log`、`12-remount.log`。可手动运行同一 exe 的 `--v2-remount` 查看演示，无需网络服务。

## Closed instance during visual detach

2026-10-02 Root 核对后的窄生命周期补充：自动视觉 Attach 先检查保留实例 `IsClosed`，已关闭时 Unmount 清除本地挂载意图，不调用工厂、不重新建投影、不关闭实例或等待 Released，也保留调用方无关 Content。

真实 `--verify-v2-remount` 增加 Detached→owner.Close→Attached：工厂次数保持不变、Host.Content 为空、原生 Attach 不抛异常，原 Feature 的释放票据正常完成。

- 样例/平台针对性 Release 构建 — exit 0，0 警告、0 错误，日志 `12-closed-attach-build.log`。
- 同一真实 Windows verifier 命令 — **PASS v2-remount**，exit 0，最终结果文件仍为 `12-remount-result.txt`，日志 `12-closed-attach-verifier.log`。
- `rtk proxy git diff --check` — exit 0；两个修改的 C# 文件保持 CRLF、UTF-8 BOM。
- 既有可控投影9/9、V2 254/254和原样例239/239证据有效，本窄平台边界未重复全量运行。

## Spec coverage

- 用户故事：US15、US31、US32、US37、US46、US50。
- 实现决策：D21、D23、D25。
- 行为验收：T12、T17、T20。

这些编号引用 MVI v2 实施规格；这里表示本任务负责或贡献的覆盖，完整验收仍以该规格为准。

## Root 最终验收

2026-10-02，Root 核对宿主实现与实际 Windows PASS 报告，确认全部卸载/释放路径仅处理本地 View，没有关闭业务实例或等待 Scope 释放。另补 Detached → owner.Close → Attached 回归，自动重连不会调用已关闭实例的 View 工厂，也不误清无关 Content；最终真实验收再次 PASS。

有效证据：投影 9/9、完整 v2 254/254、原有与认证样例 239/239（合计 493/493）；增量样例/平台构建 0 警告、0 错误；最终 `--verify-v2-remount` 退出码 0、`.scratch/mvi-v2/12-remount-result.txt` PASS；差异/格式检查通过。无需重复完整测试或增加独立审查。
