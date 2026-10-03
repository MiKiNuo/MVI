# 19: 三包独立 NuGet 消费验证

**What to build:** 全新消费者只引用核心或一个平台入口包即可编译运行；双平台消费正确去重，应用无需直接引用框架源码项目。

**Blocked by:** 04 — Avalonia 认证表单迁入 v2；16 — 创建失败和生成错误定位业务声明；18 — Godot 异步子 View 与生命周期闭环。

**Status:** completed

## Acceptance criteria

- [x] 产出核心、Avalonia、Godot 三个包，核心无 GUI 运行依赖，平台入口转递核心与所需编译资产。
- [x] 生成器独立编译并作为核心包内唯一 analyzer 资产提供，不发布第四个生成器包，不进入应用运行时依赖；单平台、双平台场景均无重复生成或类型冲突。
- [x] 干净消费者仅安装本地产物，分别运行已完成的 Avalonia 认证表单、Godot 异步子 View 和无 GUI 核心调用，验证输入、异步操作与标准 DI 工厂。
- [x] 双平台消费者证明依赖和资产去重，外部消费者运行已完成的框架误用诊断矩阵；不强制团队文档风格。应用自行提供平台宿主与主题，不隐式捆绑另一个 UI 平台。
- [x] 形成可重复的本地打包与消费验收入口，后续集成必须用当时的候选源码重建产物，不能复用旧包冒充新版本验证。

## Spec coverage

- 用户故事：US02、US03、US04、US05、US41、US44、US45、US52、US53。
- 实现决策：D02、D03、D04、D05、D26、D27、D29。
- 行为验收：T18、T19、T21。

这些编号引用 MVI v2 实施规格；这里表示本任务负责或贡献的覆盖，完整验收仍以该规格为准。

## 实现与验证证据（2026-10-03）

实现只调整三个 v2 项目的打包配置，新增 `scripts/verify-package-consumers.ps1` 与 `test/MiKiNuo.Mvi.PackageConsumers/` 中的消费者源、诊断矩阵和说明；没有修改已完成 Core 协议、生成器行为、默认 App 分支或旧发布工作流。

- Core 开启打包，通过 `ReferenceOutputAssembly=false / PrivateAssets=all` 的生成器项目引用保证 fresh 构建；唯一 DLL 位于 `analyzers/dotnet/cs/MiKiNuo.Mvi.Generators.dll`。没有 generator runtime DLL、第四个包或旧分层包依赖。
- 实际独立 Avalonia-only probe 已证明 NuGet 自动转递 Core 内的 analyzer，生成 `SetCount` 并编译为 0 警告、0 错误；因此没有新增 buildTransitive 桥接文件。
- 每次完整入口使用唯一版本、仓库外新目录和空 `NUGET_PACKAGES`，逐项目 `Rebuild` 后只打三个包。NuGet source mapping 将 `MiKiNuo.Mvi*` 限制到本次 feed，消费者没有源码 ProjectReference，也不继承仓库 props、targets 或 editorconfig。结束时再次校验候选源码哈希。
- Root 最终核对指出验收脚本不应把外层工具的 RTK 规则带给开发机/CI。入口现直接调用 dotnet、git 和原生引擎，内部 RTK 引用为 0；要求 PowerShell 7.2，GUI 保持 Hidden/日志，超时通过 `Process.Kill(true)` / `WaitForExit()` 回收本次启动的完整进程树。以下最终候选已在窄修复后完整 fresh 重跑。
- 英文无 XML 的普通 Core/Dual 源实际调用生成输入、纯规则、验证与慢 IO、当前状态反馈、生成标准 DI 工厂、独立 Scope 和释放；同时运行名为 `message` / `operation` 的合法请求 handler，覆盖切片 16 的参数 shadow 修复。
- Avalonia 宿主自行提供 Desktop / FluentTheme，并复用现有三个认证 Feature、原生控件和可控验收服务。仅在外部 staging 的 `HttpAuthService.cs` 副本去除旧 DI using / 属性两行；仓库共享源保持原样，切片 20 正式退役旧声明后可删除该 staging 步骤。
- Godot 宿主自行提供 SDK、AssemblyName、场景与主题。只复制样例顶层源码和场景，不复制 `.godot/bin/obj`；实际使用 Godot.NET.Sdk 4.6.1、GodotSharp / SourceGenerators 4.6.2 与固定原生 mono 引擎 4.6.2。

执行命令：

```powershell
rtk proxy pwsh -NoProfile -File scripts/verify-package-consumers.ps1
rtk proxy pwsh -NoProfile -File .scratch/mvi-v2/19-missing-generator.ps1
rtk proxy git diff --check
```

完整入口 exit=0，34 个原始构建/运行命令中 14 个成功、20 个非法声明构建按预期 exit=1。三个源码项目 Rebuild 和四个消费者编译均为 0 警告、0 错误；Core/Dual 运行、真实 Windows Avalonia、真实 Godot 组合场景全部 exit=0。缺失生成器检查暂时移开 DLL 后 pack exit=1 / `NU5019`，定位 `MiKiNuo.Mvi.Generators.dll`，最后按原 SHA-256 恢复；没有 Exists 条件静默漏资产。Diff check 和 BOM/CRLF/PowerShell parser 检查通过。

通过候选（所有哈希为 SHA-256）：

- 基线 HEAD：`8a728368d8501f648ceff5f9bc89631152f1c92e`。
- 唯一包版本：`2.0.0-consumer19.20261002235803.run2514bd947ebd42398245794f1777445f`。
- 64 个候选输入文件合成哈希：`0F67F3C5B14AEBCD93B8AFDD8C0F70EE7C758F24C08D0733A1FC09E4E5142062`。
- 全部外部证据目录：`C:/Users/luoji/AppData/Local/Temp/mvi-package-consumers-2514bd947ebd42398245794f1777445f/`。
- 仓库摘要日志：`.scratch/mvi-v2/19-consumers-final.log`；早期宿主配置失败保留于 attempt1/2，attempt3 为 RTK 依赖修正前的通过记录，最终以 final 为准。
- 缺失 DLL 原始日志：`.scratch/mvi-v2/19-missing-generator.log`；该检查在前一个候选中完成并恢复 DLL 哈希 `781BE3146286523B81FFC57FC9E98A9DA2D86FA46EE736844029DA18C9CB7888`，三 csproj 打包边界之后没有变化。

| 包 | nupkg SHA-256 | 编译/运行资产边界 |
| --- | --- | --- |
| MiKiNuo.Mvi | `8DA3DB866EECB79F1BFFFDD8CEA8B808755184DAA89CFDD515C4987262F2D33A` | 一个 runtime DLL 与唯一 analyzer；只依赖标准 DI，无 GUI |
| MiKiNuo.Mvi.Avalonia | `9B82A0A373C06E6B70EDDBFC919D83D14A887AE51C64C4F1246D700095C5ED9F` | 一个平台 DLL；依赖本次 Core 与 Avalonia 12.0.2；无 Godot |
| MiKiNuo.Mvi.Godot | `B2FA74012D9D2544145849EE5A25207B8B10F27B296F8F99C12BF8C458CA84D8` | 一个平台 DLL；依赖本次 Core 与 GodotSharp 4.6.2；无 Avalonia |

`candidate.json` 保存各文件哈希；`packages.json` 保存包路径、哈希、完整 ZIP 资产与 nuspec；`commands.json` 保存精确参数、退出码和原始日志路径；`assets.json` 保存每个消费者的 analyzer、生成文件、引用和 runtime 依赖。

| 干净消费者 | 直接 PackageReference | MVI analyzer 数 | MVI 生成文件数 | 框架程序集引用数 | 实际结果 |
| --- | --- | --- | --- | --- | --- |
| Core | MiKiNuo.Mvi | 1 | 1 | 1 | 输入/Operation/生成 DI 工厂、独立 Scope 与释放 PASS；runtime deps 无 GUI/生成器/CodeAnalysis |
| Avalonia | MiKiNuo.Mvi.Avalonia | 1 | 3 | 2 | 真实平台句柄、三个表单、原生输入/按钮、验证/Busy/继续编辑/完成/业务失败/取消/故障/UI 线程/释放连接 PASS |
| Godot | MiKiNuo.Mvi.Godot | 1 | 3 | 2 | Ready 挂载、慢 IO/结果/故障、定向目标、卸载重挂、容器退出重入、替换、自有节点、in-flight close/晚反馈拒绝/真实退出后释放 PASS |
| Dual | MiKiNuo.Mvi.Avalonia + MiKiNuo.Mvi.Godot | 1 | 1 | 3 | 两平台公开资产实际编译，Core/生成器/生成类型均无重复；同一 Core/DI/Operation 场景运行 PASS |

`avalonia-result.txt/.png` 与 `godot-result.json/.png` 保存真实运行结果和截图；两张截图已人工查看。Godot JSON 中 `passed=true`、`createdViews=6`，全部生命周期布尔项为 true；引擎 stdout/stderr 无 `ERROR:` 或 `WARNING:`。SDK 实际为 `11.0.100-rc.1.26425.128`，目标 net10.0，原生引擎版本由运行日志记录。

外部真实编译诊断矩阵 20/20：每行均核对 compiler SARIF 的 `Declaration.cs` 原声明行列和期望行号，不能以 CS 错误或生成文件位置代替框架错误。完整源和 SARIF 位于同名消费者子目录，汇总为 `diagnostic-results.json`。

| case | ID | 原声明 line:column |
| --- | --- | --- |
| nonpartial | MVI2001 | 4:21 |
| mutable-state | MVI2002 | 4:14 |
| invalid-input | MVI2003 | 4:2 |
| invalid-rule | MVI2004 | 5:2 |
| ambiguous-rules | MVI2005 | 6:2 |
| input-conflict | MVI2006 | 5:13 |
| invalid-operation | MVI2007 | 5:2 |
| invalid-validation | MVI2008 | 5:2 |
| operation-conflict | MVI2009 | 5:2 |
| invalid-concurrency | MVI2010 | 5:2 |
| invalid-request | MVI2011 | 6:2 |
| invalid-request-validation | MVI2012 | 6:2 |
| duplicate-request | MVI2013 | 7:2 |
| request-entry-conflict | MVI2014 | 6:2 |
| invalid-request-policy | MVI2015 | 6:2 |
| factory-conflict | MVI2016 | 5:19 |
| preferred-constructors | MVI2016 | 6:2, 8:2 |
| nonpublic-message | MVI2011 | 6:2 |
| nonpublic-result | MVI2011 | 7:2 |
| ref-like-message | MVI2011 | 5:2 |

没有提交或远程发布；PROGRESS/TICKETS、用户 `.gitignore`、v1 默认入口与正式/preview 统一流程由 Root/切片 20 继续处理。
