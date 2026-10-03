# MVI v2 剩余切片执行记录

用户授权：2026-10-02，当前任务已完成到 08，09—22 按直接前置逐个自主完成。

工作区：`F:\MiKiNuoProjects\MVI`，沿用当前 `codex/mvi-v2` 分支。保留原 `.gitignore`、任务规格、合并备份及其他本地内容。正式验收依据为对应票据与 `docs/mvi-next/SPEC.md`。

| 切片 | 状态 | 当前证据或下一步 |
| --- | --- | --- |
| 01—08 | 已合入 | HEAD 起点 `3716b4f`；04、06 已同步完成状态及交付证据；无界面四组消费者自检 PASS |
| 09 | 已完成 | 提交 `cc0bbac`；显式端口取消策略与独立执行令牌；Mediator 定向 45/45、最终 v2 213/213，Release 构建 0 错误；验收及命令见 09 票据 |
| 10 | 已完成 | 提交 `5cec7c7`；两 P1 先 RED 后修复，增量复核无遗留；定向 26/26，原有/样例及 v2 合计 478/478，四组消费者 PASS，Release 0 错误 |
| 11 | 已完成 | 提交 `4a5a930`；每实例共享有界入口及关联结果；端口停用与入箱原子排序，14/14、全 v2 253/253、Headless 五组 PASS，独立复核无遗留 |
| 12 | 已完成 | 提交 `625591a`；Host 与既有 Feature 表单复用；真实 Windows remount PASS（含闭实例禁止重连），两项目493/493，Root核对归属无遗留 |
| 13 | 已完成 | 提交 `39dc57c`；独立所有权/路由退出/retired跟踪；review修复含Ctor Scope依赖、引用身份、详情卸载、Ctor直接/嵌套自等待；最新v2 269/269，原/样例239/239，真实Workspace PASS |
| 14 | 已完成 | 提交 `b6b7cea`；整树全模型锁确认条件重验；确认IO真实退出跟踪；3 P2 RED/GREEN及增量review无遗留；13/13、v2 282/282、原/样例239/239，真实Close PASS |
| 15 | 已完成 | 提交 `1f54ee1`；精确frame/活动请求链/同步fresh资源依赖；review修复及普通async事件边寿命回归完成；18/18、最新v2 300/300、真实Navigation PASS |
| 16 | 已完成 | 提交 `0168886`；RequestHandler/生成工厂与端口，MVI2011–2016；review两P2修复，30/30声明、19/19工厂、2/2增量、全v2 331/331、Headless5 PASS |
| 17 | 已完成 | 提交 `2cb6cbb`；真实4.6.2 HUD PASS；1000输入/1等待display、线程/版本/一次按钮；通知途中释放P2已RED/GREEN；全v2 332/332、声明7/7、架构2/2 |
| 18 | 已完成 | 提交 `8a72836`；独立review的Ready挂载P2已真实RED/GREEN；完整Godot组合自检exit0，无ERROR/WARNING；定向Loading/Fault、重挂/替换/在途关闭均PASS；全v2 333/333、声明8/8；Root已核对修复、真实JSON、截图和最终Diff |
| 19 | 已完成 | 提交 `11b5cbd`；三包fresh Rebuild/pack、四外部消费、两原生UI、20/20 SARIF定位PASS；generator唯一Core analyzer且不入runtime；最终入口无内部RTK依赖、owned进程树超时终止；Root已核对配置/完整脚本/最终result |
| 20 | 已完成 | 旧图退役/测试迁入/默认认证/统一pack；Release 0警告0错、358/358；完整fresh三包/四消费/两GUI/20诊断PASS，同源CI子集PASS_NON_GRAPHICAL；两个P2已RED/GREEN及独立增量关闭；7个Avalonia入口+包Godot HUD全部exit0；114输入0变化、14图示哈希一致 |
| 21 | 下一项 | 派发分配、多实例与资源成本及数值预算；20功能候选已可用于固定负载 |
| 22 | 待执行 | 双平台真实 UI 与生成编译成本及数值预算 |

执行规则：一个连续 Writer 完成每个切片的实现与针对性验证；Root 核对行为、最终 Diff 和重要风险后更新状态。共享构建、GUI 与性能负载串行运行。不能以构建通过代替真实平台、独立包消费或性能验收。全部切片满足要求后停止并将 Goal 标记为完成。

## 当前基线

- Release 解决方案构建成功，0 错误；5 条 `MVI0013` 警告来自已有 v1 测试夹具。
- 两个 Release 测试可执行文件以 `--maximum-parallel-tests 1` 串行执行：原有与样例测试项目 239/239、v2 测试项目 194/194，合计 433/433；失败、跳过、取消均为 0。
- 结果位于 `TestResults/baseline-goal/` 的两份 TUnit HTML 报告。
- 首次 context-mode 基线构建达到 60 秒工具超时，重试构建在 31.81 秒结束；没有把超时结果计为构建通过。

## 后续 Godot 环境准备（只读核实）

- 本机既有引擎：`D:\Program Files\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe`，实际 `--version` 为 `4.7.1.stable.mono.official.a13da4feb`。未修改该安装；固定 4.6.2 尚未发现。
- 已安装 .NET SDK 10.0.401、runtime 10.0.12；另有 .NET 11 RC，测量和宿主构建须记录实际 SDK。Godot 4.6.2 官方 GodotSharp 目标 net8.0，但支持更高消费者 TargetFramework；继续保持 net10.0。
- 固定基线官方 [4.6.2 Windows x64 .NET ZIP](https://github.com/godotengine/godot-builds/releases/download/4.6.2-stable/Godot_v4.6.2-stable_mono_win64.zip) 与 [SHA512](https://github.com/godotengine/godot-builds/releases/download/4.6.2-stable/SHA512-SUMS.txt) 已核实。到 17 再按需要获取，4.7.1 不替代固定基线验收。
- 真实图形自检使用原生 `Input.ParseInputEvent`、主线程 Control/SceneTree、窗口渲染及 `FramePostDraw` 后截图；无图形 headless 仅覆盖逻辑。[CLI](https://docs.godotengine.org/en/4.6/tutorials/editor/command_line_tutorial.html)、[线程](https://docs.godotengine.org/en/4.6/tutorials/performance/thread_safe_apis.html)、[输入](https://docs.godotengine.org/en/4.6/classes/class_input.html#class-input-method-parse-input-event)。
- Godot.NET.Sdk 的隐式 GodotSharp/GodotSharpEditor/SourceGenerators 版本须与中央 4.6.2 核对；不能混用后据编译通过宣称真实平台成立。

2026-10-03 SDK 实际核实：本机 NuGet 缓存 `godot.net.sdk/4.6.1/Sdk/SdkPackageVersions.props` 设定 `PackageVersion_GodotSharp` 与 `PackageVersion_Godot_SourceGenerators` 为 4.6.1。保留仓库指定 SDK 4.6.1 时，可在宿主正文将两属性明确设为 4.6.2，使隐式 GodotSharp/GodotSharpEditor/SourceGenerators 一致；不要重复显式添加这些包。Debug 实际输出为宿主 `.godot/mono/temp/bin/Debug/`，没有 net10.0 子目录。平台库仍使用 Microsoft.NET.Sdk，新游戏宿主才使用 Godot.NET.Sdk；net10.0/EnableDynamicLoading=true。

固定 ZIP 官方 SHA512：`4b99c9a34e585c64a3838f34b0e67e4e029c978ff1ac25d357be2449bd5fafafac068f2a37c0c16a00b771683259cf867bfd027cc1d9814dab631f4516382efd`。官方 release 记录 109645645 bytes，SHA256 `7c530d4b45d9f7aadd788b85a4d9dbdb5f2a239a72999afd7d29fda506fbedb6`。

固定引擎已下载、SHA512 全量校验并运行 `--version`：`.scratch/mvi-v2/tools/godot-4.6.2/Godot_v4.6.2-stable_mono_win64/Godot_v4.6.2-stable_mono_win64_console.exe`，实际版本 **4.6.2.stable.mono.official.71f334935**。可重跑 `.scratch/mvi-v2/get-godot.ps1` 验证/准备；本机宿主的 `Get-FileHash` 不可用，脚本采用标准 .NET SHA512 和 ZipFile，下载/校验/版本探测最终 exit 0。

17 已由 Root 验收并提交 `2cb6cbb`。托管 `GodotProjection(view)` 在主线程连接 SceneTree.ProcessFrame；后台仅入 ConcurrentQueue，每帧有限批次，TreeExiting/Dispose 清队列并释放投影/信号连接。实际 Core 调度为 `Action<Action>`，不要新增 IUiScheduler。真实 HUD 自检已覆盖原生输入、后台密集提交、字段/线程/版本、合并及一次按钮行为；通知过程中重入解绑/释放的 P2 已真实 RED/GREEN。18 已完成并提交 `8a72836`；独立只读审查 `/root/godot18_review` 的 Ready 挂载 P2 已真实 RED/GREEN，Root 已核对完整组合 JSON 与截图，无须重跑这一轮。19 交给干净上下文的唯一 Writer `/root/package_consumers19`。

## 当前修复与后继边界

切片 10 的两个 P1 已经公开回归 RED/GREEN 并独立复核解决：标准 DI 参数服务工厂先创建同型外部实例的误捕获，外部取消回调内联完成业务时的提前退出。准确对象激活和取消路径 ForceYielding 已通过实际测试；不要重新做这一轮完整调查。

工厂真实目标关联可使用 .NET 10 官方支持的预分配对象加 `MethodBase.Invoke(target, args)` 完整构造，不能靠 first/last 类型匹配；构造选择及失败时未完成基类初始化仍须明确处理。取消清理必须先离开回调线程、真实注销并重新排空 Track，再移除执行跟踪。

切片 11 已完成：每实例 Post FIFO 的容量包含所有未终结 Post；强类型 RequestPort 与显式目标，原回执 ID 对应 OperationResult。端口门内仅实际入箱，消费者调度门外；Close/RegisterPort 门外停用，只有 port → Store 顺序。独立及增量审查均无遗留。

切片 12 调查已完成：v2 Avalonia 仅有 AvaloniaProjection/BindInput，没有 Slot。可增加 ContentControl 宿主，显式 Mount(existingFeature, viewFactory)/Unmount；相同实例稳定复用，detach 仅释放 IDisposable View，本体继续运行，reattach 新 View 从最新快照创建。三个 V2Auth Form 已 IDisposable，但当前构造自己 new Feature，需要接收既有 Feature 的入口。复用 Program/App 的 --verify-v2-* 真实 Windows verifier 与 Queue<Action> 投影测试。

切片 15 还须覆盖失效发起操作的后续框架消息发送有效性（SPEC D15）以及导航依赖链的释放自等待保护；不要把直接 Released getter 的本实例保护误认为已完成所有依赖环验收。

## 接续状态

切片 15 上述要求现已实现：精确执行 frame 检查生命期/Latest generation/取消与故障；Send 的活动等待边在正常响应、WaitCanceled、失败时解除，断开的中段不会错误关联上游。fresh 展示仍可发新消息，但同步调用栈的释放依赖持续到实际 scope 返回，普通 async 回调随后不误拒。

上轮 Goal 曾返回 `blocked`；2026-10-03 续接时已现场核实为 `active`，当前 HEAD 为 `1f54ee1`，源码工作区没有未提交改动。16 正在执行，17—22 待完成；不重复 09—15 已验证工作。

16 只读定位已完成：新增消息 handler 声明及生成强类型端口/工厂；现 FeatureGenerator 已有增量 emission comparer/FeatureEmission tracking，不需重写整条管线。复用现有 GeneratorTestHost 的 Emit/公开 Consumer.Run、IncrementalGenerationTests；Factory失败/所有权矩阵已有10/13证据，只增加生成入口集成缺口。保持真实Roslyn符号判定，非法签名/重复/冲突定位原声明，默认消费者无仓库文档/层次限制。17 的固定Godot环境与官方下载/校验信息见本记录前文。20前不提前删除v1；21/22必须在完整候选固定负载串行测量。

19/20 的只读发布定位已完成：Core 当前 IsPackable=false，Avalonia 默认可打包但没有 generator 资产；Generators netstandard2.0/IsPackable=false，不应成为运行时依赖。19 将 generator DLL 唯一嵌入 Core analyzers/dotnet/cs，编译期 ProjectReference 保证 fresh pack 前有资产，缺失应失败；如真实平台转递不自动到达，再由 Core 唯一 buildTransitive 接入并去重。旧平台各嵌旧 Infrastructure 资产及 Exists 条件不可照搬。独立消费者必须在仓库外隔离目录、只引用本次重建的本地 NuGet 包，验证 Core/Avalonia/Godot/双平台四种路径及真实场景，不继承仓库 props/规范。旧 pack-local 仅列四 v1 包，正式/预览各列六包；20 统一三包清单与消费门禁，随后退役无调用者的 v1。现有 Headless 是项目引用，仅可复用业务源码，不是包消费证明。

19/20 补充边界：新 `MiKiNuo.Mvi.Avalonia` 项目的公开 namespace 仍为 `MiKiNuo.Mvi.Platforms.Avalonia`，不能仅凭 using 判定旧依赖。真实旧入口在 `App.axaml.cs` 的默认 `SampleCompositionRoot` 分支、`MainWindow`、`Composition/` 与旧 Feature 目录；共享 `Features/Auth/HttpAuthService.cs` 仍有旧 `DiService` 属性。19 的独立认证消费者应复用已完成表单与可控服务，20 再清理默认旧入口和旧 DI 声明。`test/MiKiNuo.Mvi.Tests/V2AuthFlowTests.cs` 是混合旧测试工程中需保留迁移的 v2 测试，不能随旧工程直接丢弃。

21/22 只读准备完成：固定 v1 tag `archive/v1-2026-10-01` 对应 `ed77eb38ecba77a62fac2c0cbbbb35321a540d03`，树中含完整 Benchmarks、runtime、Infrastructure 及中央配置。20 后可从固定提交独立恢复测量，不需在 v2 保留兼容实现；必须保留归档 Benchmarks 的 `Directory.Build.props`，否则 BDN 自动工程可能受 XML/warnings-as-errors 影响。现有 Reducer/ConcurrentDispatch/StoreEffectDispatch/MediatorSend/DiScopeAndFactory 基准可复用固定负载，但没有完整 v2 四轴、逐级 retained memory/连接残留或真实 GUI/编译计时。21/22 须在20候选上串行预热/重复测量，保存原始样本、源码与包hash；先以固定基线建立并记录预算，再评价 v2，不得用历史时间或烟测代替。跨 Feature 观察者为0，稳定泄漏/错写/丢事件/取消误判直接失败。

20 退役边界只读定位完成：默认 App 的 v1 链为 `App.axaml.cs` → `SampleCompositionRoot`/`AppComposition` → `MainWindow`；旧 `Features/{Login,Register,ResetPassword,Shell,Home,CompositionDemo}` 可退役，v2目录不依赖它们。保留 `Features/Auth/{IAuthService,HttpAuthService,AuthResult,AuthValidation}`；只去掉 HttpAuthService 的旧 DiService/Domain.DI 声明，V2Auth/AuthFormsWindow 已负责自有服务释放，无需新DI包装。旧测试工程保留迁移 `V2AuthFlowTests` 和 `ArchitectureDirectoryTests`，其他测试及TestSupport绑定v1/旧generator/旧benchmark，不能为保留它们维持旧运行图；默认测试/样例改名不是硬要求。

20 直接失败点：CI 仍运行旧 Benchmarks `--list flat`；solution和旧测试csproj引用六旧src/benchmark，Directory.Build.targets 注入旧Infrastructure。移除这些连线并保留 `.editorconfig` 的明确类型/访问符/预定义类型等仓库标准，只清除退役DOC/ARCH/CODE诊断条目。build.ps1/sh目前仅调用稳定solution，无需再造一套入口。文档同步范围：根 README/CONTEXT/AGENTS、docs/mvi-next/SPEC.md 和 ARCHITECTURE.md 当前状态、V2Auth README 的旧共用中间件描述、docs/mvi-next/diagrams/README及uml-types/uml-components/mvi-dataflow 的mmd与SVG/PNG；交互JSON改变时同步HTML和已有校验。diagram中FeatureDefinition/FeatureRuntime/EffectRunner等设计名称要对照真实实现。历史ADR保留历史，设计讨论仅更新入口状态；20不提前宣称21/22性能达标。

19 最终实证：Core 的 analyzers/dotnet/cs 自动转递，不需 buildTransitive；四消费者、两个真实GUI、MVI2001—MVI2016的20个SARIF原声明定位全部通过，并覆盖handler命名遮蔽/非公开契约/ref-like消息。缺生成器的pack明确NU5019失败，原DLL已按SHA恢复。Root发现验收脚本内部RTK依赖不适合外部/CI，Writer已改为标准工具直接启动，保留外层RTK；GUI超时Kill(true)清理本次进程树，入口要求pwsh7.2。最终全量fresh入口exit0，证据目录 `C:/Users/luoji/AppData/Local/Temp/mvi-package-consumers-2514bd947ebd42398245794f1777445f`，result.json为PASS（带UTF8 BOM，Node读取去BOM），64输入候选SHA256 `0F67F3C5B14AEBCD93B8AFDD8C0F70EE7C758F24C08D0733A1FC09E4E5142062`，末尾核对0变化。认证staging临时副本仍移除旧Domain.DI using/DiService两行，20正式清理源后删这个过渡步骤。fixtures在test/MiKiNuo.Mvi.PackageConsumers，入口scripts/verify-package-consumers.ps1；20必须用当时源码fresh验证，不能复用19产物。

20 写入已交给干净上下文 `/root/cutover20`，已提供上述删除/保留/文档清单、全部真实环境与19入口。Root已告知用户采用 writing-for-agents 并读 `C:/Users/luoji/.agents/skills/writing-for-agents/SKILL.md`，仅应用于工程指导同步。Worker独占构建/GUI并在实现稳定后请求Root启动高风险退役的只读独立审查；不要在其写入中并行改共享文件。Root仍独占本PROGRESS和TICKETS总状态，未完成21/22前Goal保持active。

20 CI图形边界已由只读 researcher `/root/ci_graphics20` 核实并告知Writer：GitHub普通windows-latest只保证CPU/RAM等标准硬件，未承诺真实窗口GPU验收；这不等于已证明GUI必失败。Godot4.6.2的opengl3_angle是官方支持入口，但未核实强制WARP保证，不引入未经验证驱动；headless禁用渲染且RenderingServer多处dummy，不能冒充FramePostDraw/Viewport图像验收。默认CI做build/tests/四消费者编译+Core/Dual运行+诊断资产等非图形门禁；若入口增加CI明确NonGraphical模式，结果须记录scope和未运行GUI，默认本地入口仍完整两GUI；同候选本机真实验收是20必须执行的门禁。不推送或虚构云端job结果。依据：https://docs.github.com/en/actions/reference/runners/github-hosted-runners#supported-runners-and-hardware-resources 、https://docs.godotengine.org/en/4.6/classes/class_renderingserver.html#description 、https://github.com/godotengine/godot/blob/4.6.2-stable/platform/windows/gl_manager_windows_angle.cpp#L44-L52 。
