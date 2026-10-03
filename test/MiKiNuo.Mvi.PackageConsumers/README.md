# 独立三包消费者验收

在 Windows 上从仓库运行：

```powershell
pwsh -NoProfile -File scripts/verify-package-consumers.ps1
```

前置环境为 Windows、PowerShell 7.2 或更新版本、git、兼容 net10.0 的 .NET SDK 与 .NET 10 运行时。可用 `-GodotPath <Godot 4.6.2 mono console executable>` 指定已有原生引擎；Godot.NET.Sdk 4.6.1 / GodotSharp 4.6.2 由 NuGet 还原。脚本直接调用 dotnet、git 和引擎，不依赖 RTK；GUI 超时会终止本次启动的整个进程树。

入口每次生成唯一候选版本，在仓库外新建临时目录和空 NuGet 缓存，先从当前源码 Rebuild，再只打包 `MiKiNuo.Mvi`、`MiKiNuo.Mvi.Avalonia`、`MiKiNuo.Mvi.Godot`。Core 唯一携带 `analyzers/dotnet/cs/MiKiNuo.Mvi.Generators.dll`；平台包通过 NuGet 转递该资产。包源映射使全部框架依赖只能来自本次本地 feed，不使用源码项目引用或仓库构建配置。

四种消费者分别只引用 Core、Avalonia、Godot、两个平台入口。普通英文无 XML 消费者实际执行生成输入、操作、标准 DI 工厂和同名请求处理器；双平台额外编译两个适配器并检查生成器、生成文件、程序集引用和运行依赖去重。平台宿主自行提供桌面包、FluentTheme 或 Godot SDK、场景和主题，运行既有真实认证表单与异步组合验收。

认证消费者复用样例源文件，仅在临时 `HttpAuthService.cs` 副本去除旧 DI using / 属性两行，仓库原文件保留到切片 20。诊断矩阵通过外部项目的真实 `dotnet build` / SARIF，验证 `MVI2001`–`MVI2016` 和非公开契约、ref-like 消息、工厂构造歧义的原声明行号。

控制台输出本次临时目录。该目录保留原始构建/运行日志、精确命令、当前源码候选 SHA-256、三个包及 SHA-256、完整包资产和 nuspec、每个消费者的 analyzer/reference/generated/deps、两个真实 UI 报告和截图，以及每个失败声明的源码、SARIF 与行号核对结果。`result.json` 仅在所有检查通过且源码哈希未改变时写为 PASS；后续集成必须重新运行该入口，不能复用旧目录作为新候选验收。
