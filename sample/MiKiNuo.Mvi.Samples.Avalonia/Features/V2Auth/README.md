# v2 认证表单

任务 04 将已有的登录、注册、重置密码业务接入 v2。每个表单由自己的 State、Feature、View 构成；三个实例分别拥有状态和操作身份。窗口使用本地标签页展示三个表单，成功结果在原表单展示。当前无参数启动默认进入这三个 v2 表单；跨 Feature 导航与关闭已在 V2Workspace 演示中实现。

从仓库根目录启动真实 Avalonia 窗口：

```powershell
dotnet run --project sample/MiKiNuo.Mvi.Samples.Avalonia/MiKiNuo.Mvi.Samples.Avalonia.csproj
```

正常运行复用已有的 `IAuthService`、`HttpAuthService` 与 `AuthResult`。登录、注册和重置密码继续采用现有业务校验；共享纯规则由 AuthValidation 提供。旧中间件与 DI 属性已退役。网络服务保留原有示例行为，包括重置密码通过 DummyJSON 的用户更新接口演示网络往返。

## 作者声明

| 表单 | 业务类型 | 可编辑字段声明 | 操作声明 | 启动验证方法 |
| --- | --- | ---: | ---: | ---: |
| 登录 | LoginState / LoginFeature / LoginForm | 2 | 1 | 1 |
| 注册 | RegisterState / RegisterFeature / RegisterForm | 4 | 1 | 1 |
| 重置密码 | ResetPasswordState / ResetPasswordFeature / ResetPasswordForm | 3 | 1 | 1 |
| 合计 | 3 State、3 Feature、3 View | 9 | 3 | 3 |

当前实写的三个 State 与三个 Feature 共 144 个物理行，三个 View 共 168 行；共享本地反馈 Panel 和窗口宿主各 49 行，合计 410 行。复用的纯业务校验另有 25 行。统计包含中文注释与空行；生成代码、既有网络服务、测试与自动验收分别计算。表单的业务声明之外，原生控件布局和本地 View 接线仍由作者编写。

每个 `[Input]` 生成 `Set<Property>` 与投影的可编辑属性。每个 `[Operation]` 生成可等待的 `SubmitAsync(CancellationToken)`，以及投影的 `SubmitAsyncCommand`。作者无需为这些路径手写 Intent、Effect、Dispatcher、ViewModel、命令类或运行中的布尔字段。

View 使用 `AvaloniaProjection.Create(feature.CreateProjection)` 建立本地投影；输入使用 `AvaloniaProjection.BindInput`，只读展示使用 Avalonia 原生绑定。命令的 `CanExecute` 依据已展示快照提供按钮反馈，实际启动仍执行 Feature 的验证。直接调用 `SubmitAsync` 或 `SubmitAsyncCommand.Execute` 都不能绕过启动验证。

## 提交与展示

启动验证、重复提交准入与输入采样共用短原子区间。无效输入不调用认证服务；需要展示的拒绝反馈来自已提交的操作状态和业务状态。操作开始后使用 `Operation.Snapshot` 作为服务输入，完成反馈以当前 State 做纯转换，因此慢服务期间继续输入的内容能够保留。

加载表现依据该快照中的 `SubmitAsync` 操作是否仍在运行。重复提交会得到 `Rejected`，同时保留已有执行的身份和加载表现。输入控件继续可用。

业务结果和执行结果分别表达：

- 成功与可预期业务失败都返回 `Completed`，由强类型 `AuthResult` 区分。
- 协作取消返回 `Canceled`，不回滚已经提交的编辑。
- 非预期异常返回 `Faulted`，界面显示通用故障反馈，避免泄露异常中的业务载荷。
- 正常等待完成时，有关状态已经提交；原生控件展示可以稍后由 UI 调度器完成。

View 的输入连接与投影随 View 释放；投影释放后，保留的旧命令不能继续提交业务。后台操作调用网络服务，原生控件更新和命令可执行反馈通过 Avalonia 平台调度发生在 UI 线程。

## 自动验收

Release 构建后运行：

```powershell
dotnet run --project sample/MiKiNuo.Mvi.Samples.Avalonia/MiKiNuo.Mvi.Samples.Avalonia.csproj -c Release --no-build -- --verify-v2-auth
```

验收使用可控认证服务和实际 Windows Avalonia 窗口，检查平台窗口句柄、原生输入与命令、提交验证、慢 IO 中的输入与加载、重复拒绝、结果分类、等待与绘制的边界及 UI 线程。它操作原生控件属性，不模拟物理键盘；可控服务使结果可复现，不依赖线上认证接口。

结果默认写入样例可执行目录的 `v2-auth-verification.txt`。通过 `--v2-auth-result=<绝对路径>` 可以指定已有目录中的结果文件。宿主在验收后退出，以退出码表示通过或失败。

## 历史切片 04 验证（2026-10-02）

本次实现位于 `codex/v2-auth-04`，基线为 `0d70b53507a543b9a063c71c1b204f7943de5f8c`。原共享工作区出现其他任务的并发实现后，任务 04 转入独立工作树进行构建、验收和提交。

| 检查 | 结果 |
| --- | --- |
| 整套解决方案 Release 构建 | 0 错误；5 条既有测试夹具的 MVI0013 警告，相关源码与基线一致 |
| 整套解决方案测试 | 346/346，通过；既有与样例测试 239，v2 测试 107 |
| 针对性回归 | 认证流程 22/22、旧校验中间件 6/6、命令 2/2、操作声明 20/20 |
| 实际 Windows Avalonia 窗口 | PASS；平台句柄、三个表单的原生输入与按钮、加载、结果、线程和释放行为已检查 |
| 文件格式与补丁检查 | 24 个 C# 文件的 UTF-8 BOM/CRLF 检查通过，`git diff --check` 通过 |

真实窗口验收同时验证：服务使用通过验证的启动输入，慢 IO 期间的新编辑保留，重复尝试不清空运行状态，取消被旧服务吞成业务失败时仍正确归类，非预期错误不显示敏感异常文本，以及阻塞 UI 展示期间操作仍能结束并提交。验收报告同目录的 `v2-auth-verification.txt.png` 保存了实际窗口内容截图。

### Standards

发现 1 项兼容性回归，已修复，未解决项为 0。生成器删除了误拒绝合法 `CreateOperationCommand` 输入的新增保留名限制，补充两个真实消费编译回归。Fowler smell 为 0。新 View 使用 Avalonia 12 的 `PlaceholderText`，没有新增弃用 API 警告。

### Spec

实质发现为 0。三个表单的输入、验证、服务调用、加载、四类结果与真实 UI 验收符合任务 04；作者声明量及后续任务的边界已记录。

当前集成验收见切片 20；上述历史测试数量与分支说明保留为原始切片证据，不作为当前集成结果。
