# 05: Latest 搜索拒绝全部迟到反馈

**What to build:** Avalonia 搜索框连续触发 A、B 请求时，B 的结果保持有效，即使 A 忽略取消且最后返回，也不能覆盖新结果或加载状态。

**Blocked by:** 02 — Avalonia 表单输入与状态投影闭环；03 — 异步 Operation 的验证、反馈与完成契约。

**Status:** done

## Acceptance criteria

- [x] 同一操作接纳新 generation 与启动快照采样原子完成，并请求取消旧操作；不同操作保持各自身份与有效性。
- [x] 结果、进度与完成反馈在提交点核对 generation 和生命周期，旧反馈均被拒绝，不能清除新操作 Busy。
- [x] 失效 UpdateAsync 返回明确的被取代或失效结果，不能作为正常成功继续推进；已经发生的外部 IO 不被宣称撤销。
- [x] 结果转换基于提交时的当前业务 State，保留 IO 等待期间无关的新输入；框架不重新套用启动条件丢弃合法完成。
- [x] 真实搜索 View 可演示新旧结果乱序；可控时序测试覆盖忽略取消、旧进度、旧完成、故障和并发输入。

## Spec coverage

- 用户故事：US12、US14、US17、US20、US46。
- 实现决策：D08、D09、D11、D12、D13、D15、D25。
- 行为验收：T03、T04、T05、T07、T20。

这些编号引用 MVI v2 实施规格；这里表示本任务负责或贡献的覆盖，完整验收仍以该规格为准。

## 验证证据

2026-10-02，分支 `codex/latest-search-05`。为避免与任务 04、06、07、08 的共享 checkout 写入重叠，任务 05 在独立工作树完成；本任务不修改其他任务的实现。

- `MiKiNuo.Mvi.slnx` 完整 Release restore/build 通过，0 错误；保留原 v1 测试的 5 条 `MVI0013` 警告。
- 全部 TUnit 测试串行通过：v2 122/122，v1 216/216，合计 338/338，无失败或跳过。Latest 专项包含 19 个可控时序用例。
- 真实 Windows Avalonia `--verify-v2-search` 和原 `--verify-v2-input` 均 PASS；覆盖原生输入、旧进度/结果/完成/故障、当前状态编辑、Busy/错误绑定、字段过滤、UI 线程、单调展示版本和重复两次的 A/B 乱序演示。
- 独立 `code-review` 复核：Standards 0 项发现，Spec 0 项发现；13 个 C# 文件符合 UTF-8 BOM 与 CRLF，暂存 Diff 检查通过。

运行与验收命令见 [搜索样例说明](../../../sample/MiKiNuo.Mvi.Samples.Avalonia/Features/V2Search/README.md)。操作的并发策略按声明固定；实例关闭、大页面/HUD 性能及 Queue/Parallel 仍由对应后续任务验收。

## 合并到默认开发分支

2026-10-02，任务 05 合入 `codex/mvi-v2`，保留已合入的任务 08 Mediator 请求端口。`Feature.cs` 的单处冲突组合为带默认 Reject 的操作入口与原请求端口方法，没有增加业务行为。

整合后的完整 Release 构建通过（0 错误，原 v1 测试 5 条 MVI0013 警告）；全部 TUnit 测试 357/357 通过（v2 141/141、v1 216/216），无失败或跳过。独立消费者的 state、operation、mediator 三组自检及真实 Windows 搜索窗口验收均 PASS。合并前的重叠草稿保存在 Git stash `4f838209093a1485028ade73b6d9594578a3be85`，原 `.gitignore` 编辑已恢复且不纳入合并提交。
