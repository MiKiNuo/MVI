# v2 Latest 搜索演示

从仓库根目录构建和运行：

```powershell
rtk proxy dotnet build sample/MiKiNuo.Mvi.Samples.Avalonia/MiKiNuo.Mvi.Samples.Avalonia.csproj -c Release
rtk proxy dotnet run --project sample/MiKiNuo.Mvi.Samples.Avalonia/MiKiNuo.Mvi.Samples.Avalonia.csproj -c Release --no-build -- --v2-search
```

查询框修改后自动搜索，也可点击“重新搜索”。点击“演示 A → B”会等待 A 进入外部服务，再启动 B：B 约 350 毫秒返回，A 的外部等待约 1800 毫秒后退出。服务刻意忽略取消；A 的迟到进度、结果与完成都不能覆盖 B。演示按钮可以重复使用，也能在已有 A 请求运行时重新演示。输入 `fail` 查看业务失败，输入 `fault` 查看执行故障。搜索期间编辑备注，结果提交会保留这些无关输入。

业务代码由 `SearchState`、`SearchFeature` 与 `SearchWindow` 组织。State 上的 `[Input]` 生成输入回写和本地投影；`[Operation(Concurrency = OperationConcurrency.Latest)]` 生成操作入口。View 使用 `AvaloniaProjection.BindInput` 连接原生 TextBox，再根据已提交输入启动生成的 `SearchAsync`；旧投影写回不会重新启动 IO。运行状态来自投影的 `Snapshot.OperationStates`，结果和进度来自同一快照的业务 State。

Latest 仅在通过启动验证时取代同名有效执行，并请求旧执行协作取消。每个执行有唯一身份；旧执行的 `UpdateAsync` 以 `OperationSupersededException` 明确拒绝，真实退出后其调用返回 `Superseded`。被取代的工作仍需真实退出，包括已登记的子工作和取消回调。新执行完成后 Busy 可以结束，即使旧外部工作还在退出；这不表示旧 IO 已撤销。

`Operation.Snapshot` 是通过验证时采样的开始输入；结果转换使用提交时的当前 State。等待期间清空查询会拒绝新启动，但不会重新套用启动条件丢弃已接纳的合法完成。取消和被取代不回滚已经提交的状态或外部 IO。默认并发规则仍是 Reject；本演示仅增加 Latest。

自动验收启动真实 Windows Avalonia Window，操作原生 TextBox 属性及绑定，并以服务屏障控制乱序、故障和进度；不模拟物理键盘。先运行上述 Release 构建，再执行：

```powershell
rtk proxy dotnet run --project sample/MiKiNuo.Mvi.Samples.Avalonia/MiKiNuo.Mvi.Samples.Avalonia.csproj -c Release --no-build -- --verify-v2-search --v2-search-result="$PWD/.scratch/v2-search-05-result.txt"
```

结果默认写入可执行目录的 `v2-search-verification.txt`；`--v2-search-result=<路径>` 可指定已有目录中的文件。成功退出码为 0，报告以 `PASS v2-search` 开头。验收覆盖原生输入、加载/进度/结果/最新错误、A 忽略取消、旧反馈在 B 运行中及完成后失效、无效新尝试保持有效身份、当前状态输入保留、业务失败仍正常完成、通知内新输入遭遇旧投影写回时不重复启动、字段通知过滤、UI 线程、展示版本单调，以及真实演示按钮重复两次。
