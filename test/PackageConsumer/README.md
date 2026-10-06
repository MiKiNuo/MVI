# 已打包 NuGet 消费测试

这些 `.template` 文件不是新的框架或第二个业务示例，也不直接参加源码解决方案编译。
`tools/release.py` 会把它们复制到系统临时目录，创建独立的消费项目。

消费项目不继承仓库 Directory.Build.props/targets、Directory.Packages.props 或 .editorconfig，
没有源码 ProjectReference，也不手工添加生成器 DLL。自家包仅允许从本次本地 feed 还原。

覆盖 Runtime、Binding、Avalonia、Godot、双平台+显式生成器以及通过中间 NuGet 包传递六种情况。
Runtime/Binding/Avalonia/传递案例执行纯托管 Handler→Mutation→Reducer 和命令快照；
Godot/双平台只编译，不宣称已验证原生引擎启动。Avalonia 还编译真实 XAML 和 MviView 生成成员。

每个消费项目在 CoreCompile 前断言生成器只加载一次，构建后检查输出目录没有生成器和 Roslyn DLL。
额外负例验证错误绑定仍产生 MVI0200；普通外部 public 类型没有中文注释仍能编译。

运行入口：仓库根目录 `python tools/release.py pack --version 2.0.0-preview.4`。
所有条件以真实执行结果为准；模板存在不表示测试已通过。
