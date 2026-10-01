# MVI 下一版本设计讨论

状态：讨论中。更新日期：2026-10-01。

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

纯状态转换方法根据输入计算新 State；异步业务方法表达 IO 流程，并通过受控状态提交入口更新 State。两种表达共用实例身份、状态提交、观察订阅、错误报告和生命周期规则。IO 等待期间不持有状态提交门。

统一内核指统一实现和执行协议。每个 Feature 实例仍拥有自己的状态、在途操作和生命周期，不将所有 Feature 合并为一个全局 Store 或全局操作队列。参见 [ADR 0010](adr/0010-vnext-unified-runtime-and-feature-authoring.md)。

已接受的默认用户代码是 State + Feature + View。Feature 可包含纯转换方法与异步操作方法；普通业务不要求另写 Intent、Effect、Dispatcher、ViewModel 和生成容器声明。高级场景是否提供显式消息类型扩展点，后续再决定。

以下只展示业务表达语法；Operation、特性及方法名均为候选 API。框架维护操作状态、按操作选择并发策略的方向已接受，完成结果和取消等具体契约继续讨论；此片段不作为完整登录实现。

```csharp
public sealed record LoginState(
    string UserName = "",
    string Password = "",
    string? DisplayName = null,
    string? ErrorMessage = null);

public sealed partial class LoginFeature(IAuthService authService)
    : Feature<LoginState>
{
    [Input(nameof(LoginState.UserName))]
    private static LoginState ChangeUserName(LoginState state, string value)
        => state with { UserName = value, ErrorMessage = null };

    [Operation]
    private async ValueTask SubmitAsync(Operation<LoginState> operation)
    {
        LoginState input = operation.Snapshot;
        AuthResult result = await authService.LoginAsync(
            input.UserName,
            input.Password,
            operation.CancellationToken);

        await operation.UpdateAsync(ApplyLoginResult, result);
    }

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

Q10～Q12 已接受，见 [ADR 0013](adr/0013-vnext-state-and-operation-semantics.md)。Q13 仍待确认：

| 问题 | 状态 | 规则 | 要检验的场景 |
| --- | --- | --- | --- |
| Q10 状态模型 | 已接受 | 每个 Feature 独立持有不可变业务状态快照；纯转换基于当前快照计算新状态 | 高频小状态更新、大列表与多个独立编辑器；不复制整个组合树 |
| Q11 操作并发 | 已接受 | 按实例、按操作设策略；默认拒绝同操作重复启动，搜索支持 Latest，排队与并行按需声明 | 重复登录点击；旧搜索晚于新搜索完成；不同操作并行期间仍可提交输入 |
| Q12 操作运行状态 | 已接受 | 框架维护每个操作的运行、取消与异常状态，View 选择加载和错误表现；业务结果仍由业务转换表达 | 少写 IsBusy/try-finally；失败或取消后结束忙碌；后台刷新与提交显示不同加载状态 |
| Q13 视图与实例生命周期 | 待确认 | 推荐 View 卸载释放视图订阅；业务实例由逻辑所有者管理，显式关闭时停止接纳、请求取消并拒绝迟到写回 | Tab 切换后恢复同实例；关闭子编辑器后从组合移除；父组合关闭所有拥有的子实例 |

不可变快照是状态内容的约束，使用 record 声明本身不能保证其嵌套集合或对象不可变；状态表示与诊断规则需落实这个边界。

业务方法返回成功调用结果，不等于业务已经成功。例如接口返回 AuthResult.IsSuccess=false，属于业务结果；业务转换决定如何展示，不能仅因方法正常返回而把业务标记成功。

Latest 策略需要在提交状态时核对操作有效性。请求取消并不能保证外部任务立即结束，旧请求即使返回结果也不得覆盖新请求；参数与业务对象变化造成的其他结果冲突，需要明确业务条件。取消、错误、操作完成和结果是否已提交是不同的可观察事实。

上述选择明确后，继续确定输入声明与验证、操作调用者看到的完成结果、状态观察与 UI 更新调度、通信寻址与通知完成含义、关闭确认与在途资源释放、生成器与依赖注入，以及性能预算。

## 性能验收维度

| 维度 | 待设计的验收负载 |
| --- | --- |
| 交互响应 | 慢 IO 期间输入与切换；两平台真实 UI 路径；更新风暴下的主线程工作量 |
| 实例与内存 | 多开与反复挂载/关闭；订阅释放；大组合页局部更新 |
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

当前未回答：Q13 视图与实例生命周期。

上述选择明确之后，再讨论：

- 业务作者面对的最小概念与代码形态。
- 输入绑定、异步操作、取消、重复提交及迟到结果的语义。
- 状态粒度、观察订阅和 UI 更新方式。
- Feature 创建、组合、通信与生命周期。
- 源生成器、依赖注入和平台适配的职责。
- 可执行示例、性能基准及新版本验收条件。

## 文档约定

- 推荐方案与已接受决策分开记录，未回答的问题保持待定。
- 术语达成共识时更新领域词汇表；涉及新旧版本语义差异时明确标识适用版本。
- 难以逆转且存在真实取舍的架构决策记录为 ADR。
- 本文是讨论记录，不替代现有版本的 CONTEXT.md 和已接受 ADR。
- 新版本已明确的领域术语保存在 [下一版本词汇表](mvi-next/CONTEXT.md)，当前版本根词汇表继续适用于现有代码。
