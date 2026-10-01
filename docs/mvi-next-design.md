# MVI 下一版本设计讨论

状态：Q1～Q23 基础决策已接受，组合通信以用户最新修订 ADR 0025 为准，完整设计等待整体确认。更新日期：2026-10-01。

## 已明确的目标

- 按全新版本重新讨论架构，不以保留当前 API 或类型划分为设计前提。
- 降低业务开发者的使用成本，使代码组织和开发体验更简洁。
- 复杂业务 UI 与游戏/实时交互 UI 同等纳入新版本目标。
- 重复业务声明、输入绑定、创建组合、生成器调试四类使用负担都需要解决。
- 保留完整、独立、可复用的 Feature 与组合模式，进一步设计组合、通信和生命周期。
- 同时支持纯状态转换和直观的异步业务流程，两种写法共用一个运行内核。
- 普通业务默认只编写 State、Feature 与 View，支持同类型 Feature 的多个独立实例及运行时动态组合。
- 每个 Feature 的业务状态坚持不可变快照；异步操作按实例、按操作配置并发策略，默认拒绝同操作重复启动。
- 框架自动维护操作的运行、取消与异常状态，业务结果由业务转换表达，View 选择加载与错误的表现。
- View 挂载与业务实例生命周期独立：视图卸载释放平台本地绑定，实例由逻辑所有者管理；显式关闭时停止接纳新业务、请求取消并拒绝迟到写回。
- 输入字段采用一次声明与统一验证入口；操作调用区分执行结果与业务结果，正常返回保证已接纳状态变化已提交。
- 业务操作默认后台执行，平台绑定按字段关注数据并合并待展示更新；业务输入、定向消息与一次性行为维持各自处理契约。
- 不同 View 和子 View 的业务通信统一通过 Mediator 定向投递；唯一目标可自动绑定，多候选实例由宿主明确选择。
- 组合不实现观察者发布订阅：移除 Subscribe/Publish、通知主题与订阅者列表；View 本地展示不为每个字段创建框架 R3 订阅。
- 关闭包含未保存编辑的组合时先确认整个所有权范围，任一拒绝则保持可用，确认条件失效时重新验证。
- 交互响应、多实例内存与更新成本、派发吞吐与分配、生成器与编译性能都纳入验收，不以改善其中一项代替其余项目。
- NuGet 采用一个核心包加 Avalonia、Godot 两个平台适配包；单平台用户显式安装一个入口包。
- 本阶段讨论和记录设计，确认整体共识后再进入实现。

## 版本与分支

- `main` 保留现有架构作为 v1，固定归档提交为 `ed77eb38ecba77a62fac2c0cbbbb35321a540d03`。
- 归档标签为 `archive/v1-2026-10-01`，使用归档命名，不作为 NuGet 发布版本标签。
- 新架构在 `codex/mvi-v2` 进行设计与开发，并设置为仓库默认分支。
- 当前源码是 v1 基线；本阶段提交设计、版本指引和分支 CI 入口。
- 相关设计与 ADR 纳入新分支版本控制。参见 [ADR 0014](adr/0014-v1-archive-and-v2-default-branch.md)。

## 当前版本的证据

- 登录业务的核心声明分散在 State、Intent、Reducer、Effect、EffectDispatcher 和 ViewModel 六个类型中，另有视图和中间件。参见 [Login 目录](../sample/MiKiNuo.Mvi.Samples.Avalonia/Features/Login/)。
- 用户名修改需要关联 State 属性、ChangeUserName Intent、Reducer 方法及 ViewModel 的 MviBind 声明；密码输入也有同样的关联。
- 登录流程是 Submit Intent → Reducer 声明 PerformLogin Effect → EffectDispatcher 调用服务 → Succeeded/Failed Intent 回流 → Reducer 更新状态。
- 当前组合装配已通过源生成器自动化，不能将旧版本的手工 Register/Bind/Subscribe 当作当前版本的主要成本。参见 [ADR 0008](adr/0008-composition-builder-and-singleton-removal.md)。
- 独立 Feature 实例和 Mediator 通信是当前版本的设计选择；本轮已确认保留组合和独立性，继续设计其易用性与扩展方式。参见 [ADR 0007](adr/0007-recursive-mvi-composition.md)。
- 尚未对本次改造运行新的性能测量，不根据类型数量或源码长度推断运行速度。

## 当前 NuGet 结构

- 当前 src 下六个项目均声明 IsPackable，正式与预览发布配置均列出六个包；这是配置事实，不代表本轮验证了线上发布结果。
- Domain、Application、Presentation 是独立运行时包，两个平台包通过项目引用建立传递依赖。
- 单平台使用者已经可以只显式引用对应的一个 MVI 平台包；应用宿主与主题等平台依赖仍由应用项目负责。
- 两个平台包都将 Infrastructure.dll 作为 analyzer 资产嵌入，因此正常打包后不要求使用者另装 Infrastructure 包。两个 Include 都有文件存在条件。
- 五个运行时项目当前都使用 net10.0，Infrastructure 使用 netstandard2.0；平台项目分别依赖 Avalonia 与 GodotSharp，现有配置没有按消费者平台选择依赖的条件。
- 本地 pack 脚本只列出四个基础包，正式与预览发布列出六包；本轮仅作为设计背景记录，未修改脚本。

源码依据：[Avalonia 项目](../src/MiKiNuo.Mvi.Platforms.Avalonia/MiKiNuo.Mvi.Platforms.Avalonia.csproj)、[Godot 项目](../src/MiKiNuo.Mvi.Platforms.Godot/MiKiNuo.Mvi.Platforms.Godot.csproj)、[正式发布](../.github/workflows/nuget-release.yml)、[本地 pack](../scripts/pack-local.ps1)。

NuGet 官方说明：[传递依赖解析](https://learn.microsoft.com/en-us/nuget/concepts/dependency-resolution)、[程序集与包的关系](https://learn.microsoft.com/en-us/nuget/create-packages/select-assemblies-referenced-by-projects)、[Analyzer 包资产](https://learn.microsoft.com/en-us/nuget/guides/analyzers-conventions)。

## 第二轮已接受的架构方向

### 一个运行内核与两种业务表达

纯状态转换方法根据输入计算新 State；异步业务方法表达 IO 流程，并通过受控状态提交入口更新 State。两种表达共用实例身份、状态提交、平台展示、错误报告和生命周期规则。IO 等待期间不持有状态提交门。

统一内核指统一实现和执行协议。每个 Feature 实例仍拥有自己的状态、在途操作和生命周期，不将所有 Feature 合并为一个全局 Store 或全局操作队列。参见 [ADR 0010](adr/0010-vnext-unified-runtime-and-feature-authoring.md)。

已接受的默认用户代码是 State + Feature + View。Feature 可包含纯转换方法与异步操作方法；普通业务不要求另写 Intent、Effect、Dispatcher、ViewModel 和生成容器声明。高级场景是否提供显式消息类型扩展点，后续再决定。

以下展示输入声明与业务表达语法；特性、方法及返回类型名称均为候选 API。运行状态、并发与完成结果的方向已接受，具体类型和取消契约继续讨论；此片段不作为完整登录实现。

```csharp
public sealed record LoginState
{
    [Input]
    public string UserName { get; init; } = "";

    [Input]
    public string Password { get; init; } = "";

    public string? DisplayName { get; init; }

    public string? ErrorMessage { get; init; }
}

public sealed partial class LoginFeature(IAuthService authService)
    : Feature<LoginState>(new())
{
    [OnInput(nameof(LoginState.UserName))]
    private static LoginState ChangeUserName(LoginState state, string value)
        => state with { UserName = value, ErrorMessage = null };

    [Operation(Validate = nameof(CanSubmit))]
    private async ValueTask<AuthResult> SubmitAsync(Operation<LoginState> operation)
    {
        LoginState input = operation.Snapshot;
        AuthResult result = await authService.LoginAsync(
            input.UserName,
            input.Password,
            operation.CancellationToken);

        await operation.UpdateAsync(ApplyLoginResult, result);
        return result;
    }

    private static bool CanSubmit(LoginState state)
        => !string.IsNullOrWhiteSpace(state.UserName)
            && !string.IsNullOrWhiteSpace(state.Password);

    private static LoginState ApplyLoginResult(LoginState state, AuthResult result)
        => result.IsSuccess && result.DisplayName is not null
            ? state with { DisplayName = result.DisplayName, ErrorMessage = null }
            : state with
            {
                DisplayName = null,
                ErrorMessage = result.ErrorMessage ?? "登录失败。",
            };
}
```

这里的关键语义是：IO 使用操作开始时的输入快照；ApplyLoginResult 接收提交时的当前状态，避免用整个旧快照覆盖其他已完成的编辑。是否仍允许该操作写回，由操作有效性与实例生命周期规则决定，不能仅靠当前状态参数解决迟到结果问题。

异步流程可复用纯转换方法，操作身份与有效性仍由同一内核管理。UpdateAsync 的具体返回值、取消行为及错误契约尚未确定，示例不表示这些接口已经存在。

示例中 Password 的基本回写由输入声明生成；UserName 因需要清理业务错误而提供额外纯转换。Submit 的验证在所有调用入口执行，业务方法返回 AuthResult，框架同时提供执行结果与操作运行状态。

### 组合按实例组织

已接受支持同类型 Feature 的多个独立实例、运行时动态增减子实例和嵌套组合；业务通信经所属范围内的 Mediator。持有子实例用于挂载和资源管理，与跨 Feature 的业务通信分别规定。静态声明可作为便捷装配方式，实例与通信范围的具体规则仍需明确。参见 [ADR 0011](adr/0011-vnext-dynamic-instance-composition.md)。

### 核心与平台适配的发布边界

已接受一个核心包加两个平台适配包的发布结构；表中包名作为当前设计名称。参见 [ADR 0012](adr/0012-vnext-core-and-platform-package-topology.md)。

| 包 | 职责 | 使用入口 |
| --- | --- | --- |
| MiKiNuo.Mvi | 平台无关运行时；生成器作为编译期资产随包提供 | 无界面宿主或纯业务测试 |
| MiKiNuo.Mvi.Avalonia | Avalonia 绑定、视图与生命周期适配，依赖核心包 | Avalonia 用户显式安装此包 |
| MiKiNuo.Mvi.Godot | Godot 绑定、节点与生命周期适配，依赖核心包 | Godot 用户显式安装此包 |

Domain、Application、Presentation 可作为核心内部的职责边界，不再默认逐层发布 NuGet 包。生成器仍需独立编译，作为 analyzer 资产发布；其程序集不属于应用运行时依赖。源码项目数量、程序集数量和 NuGet 数量分别决定。

平台入口依赖核心包时，必须明确编译期资产的转递规则，并在仓库外的独立消费项目验证仅安装平台包即可生成代码。NuGet 的 PrivateAssets 默认包含 analyzers，不能把 analyzer 转递视为普通运行时依赖的自动结果。参见 [PackageReference 资产控制](https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files#controlling-dependency-assets)。

本次采用核心与平台适配分别发布的方案，以保留平台依赖边界并集中平台无关能力。

## 第三轮执行规则

Q10～Q12 已接受，见 [ADR 0013](adr/0013-vnext-state-and-operation-semantics.md)。Q13 已接受，见 [ADR 0015](adr/0015-vnext-view-and-feature-lifetimes.md)。

| 问题 | 状态 | 规则 | 要检验的场景 |
| --- | --- | --- | --- |
| Q10 状态模型 | 已接受 | 每个 Feature 独立持有不可变业务状态快照；纯转换基于当前快照计算新状态 | 高频小状态更新、大列表与多个独立编辑器；不复制整个组合树 |
| Q11 操作并发 | 已接受 | 按实例、按操作设策略；默认拒绝同操作重复启动，搜索支持 Latest，排队与并行按需声明 | 重复登录点击；旧搜索晚于新搜索完成；不同操作并行期间仍可提交输入 |
| Q12 操作运行状态 | 已接受 | 框架维护每个操作的运行、取消与异常状态，View 选择加载和错误表现；业务结果仍由业务转换表达 | 少写 IsBusy/try-finally；失败或取消后结束忙碌；后台刷新与提交显示不同加载状态 |
| Q13 视图与实例生命周期 | 已接受 | View 卸载释放平台本地绑定；业务实例由逻辑所有者管理，显式关闭时停止接纳、请求取消并拒绝迟到写回 | Tab 切换后恢复同实例；关闭子编辑器后从组合移除；父组合关闭所有拥有的子实例 |

不可变快照是状态内容的约束，使用 record 声明本身不能保证其嵌套集合或对象不可变；状态表示与诊断规则需落实这个边界。

业务方法返回成功调用结果，不等于业务已经成功。例如接口返回 AuthResult.IsSuccess=false，属于业务结果；业务转换决定如何展示，不能仅因方法正常返回而把业务标记成功。

Latest 策略需要在提交状态时核对操作有效性。请求取消并不能保证外部任务立即结束，旧请求即使返回结果也不得覆盖新请求；参数与业务对象变化造成的其他结果冲突，需要明确业务条件。取消、错误、操作完成和结果是否已提交是不同的可观察事实。

## 第四轮已接受的规则

Q14～Q18 已接受，记录在 ADR 0016～0019。以下规则定义默认业务体验，特性、方法和返回类型名称仍可调整。

### Q14 输入与业务验证（ADR 0016）

- State 字段默认只读；可编辑字段只标注一次，框架生成属性、回写与通知适配，基本赋值不要求再写修改 Intent 或转换方法。
- 有额外业务规则时，在 Feature 中声明纯转换或验证方法；业务验证经统一入口执行，覆盖 UI、程序调用和跨 Feature 请求。
- 编辑阶段允许业务模型可表达的中间值，展示验证反馈；操作启动前检查业务条件，不符合时不执行 IO。
- CanExecute 提供 UI 反馈，操作启动时仍按当前状态验证；不能仅依赖按钮禁用来阻止无效业务。

### Q15 操作的完成结果（ADR 0017）

- await 操作调用返回时，业务方法已经结束，该操作发出的已接纳状态变化已经提交；等待入队成功与等待完整处理完成分别定义。
- 调用结果区分 Completed、Rejected、Canceled、Superseded、Faulted 等执行结果；业务方法可返回强类型业务值，正常结束不代替业务成功判断。
- UI 命令由框架适配这套结果，普通使用者不必为每个按钮重复处理运行状态。
- UI 绑定通知与绘制独立调度，await 不以实际绘制完成为条件。
- 已提交的状态和已发生的外部操作不会因后续取消自动回滚，调用者不能把一次异步操作当作跨时间的原子事务。

### Q16 执行调度与 UI 更新（ADR 0018）

- 业务操作默认在后台执行，涉及 UI 的服务通过平台调度入口执行；平台无关业务代码采用同一默认规则。
- 业务状态提交保持有序，变更通过直接展示调度与诊断表达；平台绑定按字段投影，不提供业务 State.Subscribe。
- 默认合并等待中的 UI 更新，使视图展示最新值，避免重复刷新无关字段；提供明确选择中间展示状态的方式。
- 合并 UI 展示不丢弃业务输入、中介者定向消息或一次性 UI 行为；提示、导航等行为不以瞬时 State 布尔开关代替投递契约。
- 高频 UI 场景按实际平台负载检验调度延迟、通知次数和分配成本。

### Q17 组合通信的默认路由（ADR 0019，订阅部分由 ADR 0025 替代）

- Feature 依赖定向消息契约，宿主负责目标地址与接线；不要求子 Feature 依赖父级或兄弟的具体类型。
- 请求在明确通信范围内有唯一提供方时可自动绑定；出现多个候选实例时由宿主显式选目标，能够静态确定的歧义给编译诊断，动态歧义给明确运行结果。
- 所有消息明确目标，框架不实现观察者订阅；跨范围通信由宿主显式接线，多目标由组合协调者明确分派。
- 同类型实例通过实例身份或契约端口寻址，业务对象 ID 不自动充当实例地址。
- 独立宿主可以提供同一契约的实现或替代服务，保持业务功能的独立运行能力。

### Q18 关闭确认与未保存编辑（ADR 0019）

- 请求关闭与正式进入关闭阶段分别定义；关闭确认阶段检查整个所有权范围是否允许关闭。
- 任一子实例拒绝关闭时，组合保持可用；在最终确认前不提前取消其他子实例的 IO 或释放资源。
- 确认后停止接纳新业务与迟到写回，再请求取消、解绑并释放资源。
- 校验期间状态或成员变化使确认条件失效时，必须重新验证；在途操作的资源释放和关闭完成含义需据此继续细化。
- 普通实例可采用默认允许关闭；需要保护未保存数据时由业务定义确认规则。

## 最后一轮已接受的基础决策

Q19～Q23 已接受，记录在 ADR 0020～0024。

### Q19 依赖注入与生成器职责

- 支持标准 IServiceProvider 和直接构造，不再以框架自制通用 DI 容器作为默认前提。
- 生成器生成 Feature 工厂、输入与命令适配、强类型分派及契约接线描述，采用属性候选与增量管线。
- DI 工厂为每个 Feature 实例建立独立服务范围；singleton 服务按应用注册规则共享，外部直接传入的服务由其创建方管理。
- 构造依赖在创建时解析，状态转换和常规操作热路径不重复执行 DI 查找。
- DI Scope 不自动形成 Feature 所有权树，父子关系、在途执行和释放由 Feature 生命周期管理。
- 默认错误诊断聚焦框架误用并定位原始声明；团队命名与文档风格规则分别配置。

依据：[DI 生命周期与释放指南](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines)、[CreateAsyncScope](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.serviceproviderserviceextensions.createasyncscope)、[增量生成源码输出](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.incrementalgeneratorinitializationcontext.registersourceoutput)。

### Q20 定向消息接纳与处理完成（原通知模型已由 ADR 0025 替代）

- SendAsync 等待确定目标处理完成；Post/TryPost 只取得确定目标实例收件箱的接纳结果。
- 定向消息复用实例已有收件箱、验证和执行入口，关闭或队列已满等拒绝原因可见；后续失败通过结果、日志或明确错误处理入口报告。
- Post 接纳不代表启动校验已经通过；实际处理时按当前状态验证，不能由接纳推断业务执行成功。
- 不实现发布订阅、订阅者列表或逐订阅者队列。需要更新多个子功能时，组合协调者对明确目标发送。

### Q21 请求等待取消与目标执行

- 调用者在请求接纳前取消时，请求不应启动。
- 目标已接纳后，停止调用者的等待与终止目标执行分别定义；取消等待不证明目标已结束或外部效果已撤销。
- 默认目标拥有已接纳请求的执行；允许契约显式选择传播取消，查询等场景可声明协作取消策略。
- 请求发起方关闭不会自动撤销目标已提交的状态或已执行的副作用。
- 被取消的等待不是正常完成响应；执行结果与等待结果分开记录，调用者可观察拒绝、等待取消与目标故障。

依据：[协作取消](https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads)、[Task.WaitAsync](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.waitasync?view=net-10.0)。

### Q22 逻辑关闭与资源释放完成

- 请求关闭先按 Q18 确认；逻辑关闭后拒绝新业务与迟到写回，并移除活动成员与通信绑定。
- UI 可在逻辑关闭后卸载，导航响应不等待发起者自身的在途操作退出，避免关闭与请求响应互相等待。
- 资源释放需要等待相关执行真正退出，再释放仍可能被访问的服务范围；提供可单独等待的释放完成信号。
- 不合作的外部任务可能使释放等待无时间上界；超时只能表示等待结束，不能据此强制释放其仍在使用的对象。
- 已从活动组合移除但尚未释放的实例，继续由资源管理跟踪到释放完成。

导航检验场景：Login 操作 await Navigate 请求，Shell 接纳导航并逻辑关闭 Login，然后返回响应；Login 操作结束后，其服务范围才释放。若 Shell 等待 Login 操作结束再返回导航响应，会形成等待环。

### Q23 验收与性能基线

- 对同一环境、相同状态表示与等价语义固定三组参照：最小手写实现、归档 v1、v2 实现；创建与 DI 另含标准 DI 对照。
- 四类性能分别测量，报告中位成本、尾延迟、分配与资源释放，不仅列平均派发时间。
- 高频纯转换路径以不增加每次消息包装、反射或 DI 查找为目标；业务状态对象本身的分配单独统计。具体优化以测量为依据，不先加入池化等复杂度。
- UI 用两平台的真实输入、慢 IO、更新风暴与多实例挂载/关闭验证，字段不相关的变化不应制造整页刷新。
- 稳定可复现的资源泄漏、错误写回、丢失业务事件与取消误判属于验收失败；计时回归在重复测量确认后判断。
- 编译测量冷构建、增量构建和单 Feature 修改；NuGet 验证独立项目仅引用一个平台包即可生成并运行。
- 不直接以历史文档中的纳秒数充当当前基线；绝对门槛须由目标环境和负载测量确定。

完整架构与 API 方案、验收场景及实施顺序整理在 [v2 架构设计](mvi-next/ARCHITECTURE.md)，作为整体确认入口。

## 性能验收维度

| 维度 | 待设计的验收负载 |
| --- | --- |
| 交互响应 | 慢 IO 期间输入与切换；两平台真实 UI 路径；更新风暴下的主线程工作量 |
| 实例与内存 | 多开与反复挂载/关闭；实例路由与本地绑定释放；大组合页局部更新；跨功能观察者订阅为零 |
| 派发成本 | 高频状态转换、真实并发、通信路由；吞吐、分配与尾延迟 |
| 编译与生成 | 冷构建、增量构建、修改单 Feature 后的生成范围与诊断成本 |

已有 benchmark 可作为可复用资产，尚需检查其负载是否覆盖新版本验收条件。具体指标、相对基线与允许预算后续确定。

## 决策树

第一轮已经回答：

1. Q1：复杂业务 UI 与游戏/实时交互 UI 都要。
2. Q2：四类使用负担全部解决。
3. Q3：保留组合与独立性，继续改善设计。
4. Q4：纯转换与异步业务方法都要。
5. Q5：四类性能都要；同时重新设计 NuGet 结构。

第二轮已经回答：

6. Q6：统一运行内核。
7. Q7：接受默认 State + Feature + View。
8. Q8：支持同类型多实例与运行时动态组合。
9. Q9：采用一个核心包加两个平台适配包。

第三轮已经回答：

10. Q10：坚持不可变状态快照。
11. Q11：接受按操作配置并发策略的推荐方案。
12. Q12：接受框架维护操作运行状态的推荐方案。
13. Q13：接受 View 与 Feature 实例分别管理生命周期的推荐方案。

第四轮已经回答：

14. Q14：采用一次输入声明与统一业务验证入口。
15. Q15：采用完整处理完成与执行/业务结果分别表达的推荐方案。
16. Q16：采用后台业务执行与按字段合并 UI 更新的推荐方案。
17. Q17：唯一目标自动绑定、歧义目标显式选择继续有效；原范围订阅改为 ADR 0025 的中介者定向消息。
18. Q18：采用先确认所有子实例、确认后关闭与拒绝后保持可用的推荐方案。

最后一轮已经回答：

19. Q19：采用标准 DI 与直接构造，生成器专注于 Feature 适配。
20. Q20：原通知发布模型被 ADR 0025 替代；定向 Post 报告目标接纳，SendAsync 等待处理完成。
21. Q21：分别定义取消等待与取消目标执行，接纳后默认由目标管理执行。
22. Q22：分别定义逻辑关闭与资源释放完成。
23. Q23：采用同环境、等价语义的固定基线与真实负载验收。

最新用户修订：不同 View 和子 View 经 Mediator 通信，不实现观察者订阅机制，避免组合规模扩大造成大量订阅。详见 [ADR 0025](adr/0025-vnext-mediator-without-observer-subscriptions.md)。

其余基础决策继续有效，完整架构与 API 方案已据此重写。

## 文档约定

- 推荐方案与已接受决策分开记录，未回答的问题保持待定。
- 术语达成共识时更新领域词汇表；涉及新旧版本语义差异时明确标识适用版本。
- 难以逆转且存在真实取舍的架构决策记录为 ADR。
- 本文是讨论记录，不替代现有版本的 CONTEXT.md 和已接受 ADR。
- 新版本已明确的领域术语保存在 [下一版本词汇表](mvi-next/CONTEXT.md)，当前版本根词汇表继续适用于现有代码。
