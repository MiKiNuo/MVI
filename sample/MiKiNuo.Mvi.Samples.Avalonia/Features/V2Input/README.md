# v2 本地输入投影演示

从仓库根目录运行：

```powershell
rtk proxy dotnet run --project sample/MiKiNuo.Mvi.Samples.Avalonia/MiKiNuo.Mvi.Samples.Avalonia.csproj -- --v2-input
```

作者代码只有 `InputFormState`、`InputFormFeature`、`InputFormWindow`。State 上的 `[Input]` 同时生成强类型 `Set<Property>` 和投影的可编辑属性；`[OnInput]` 附加纯转换复用同一入口。View 使用 `AvaloniaProjection.Create(feature.CreateProjection)` 建立连接，将生成投影作为原生绑定的 DataContext。普通状态属性和派生属性只有 getter。

输入使用 `AvaloniaProjection.BindInput(projection, nameInput, TextBox.TextProperty, static view => view.Name)` 接线，返回的连接在 View 关闭时先于投影释放。该接缝以本地原生属性连接实现双向行为：控件变化调用生成 setter，有关投影属性通知使用原生 `SetCurrentValue` 展示已提交值。输出抑制只包住本连接实际写控件属性的调用，随后立即恢复；通知回调中的其他字段、同字段不同新值、A/B/A 输入都逐次提交。Avalonia 12 内置 TwoWay 的缓存不能保证同值归一化纠正，也没有提供精确的输出写入范围，因此输入字段采用这一原生属性连接。输入属性只声明一次，不需要手写 setter 委托或 ViewModel；只读展示继续使用普通原生 OneWay 绑定。

投影显示已提交快照，输入提交不会等待 UI 绘制。默认 `ProjectionMode.Coalesce` 合并待展示快照，每次 UI 回调最多展示一个版本，然后归还消息泵；`ProjectionMode.EveryCommit` 按提交顺序展示全部中间快照。绑定属性 getter 与变化通知在 UI 线程使用。一个 Feature 同时只允许一个活动的本地 View 投影，重复创建明确失败；View 关闭时释放投影，随后可重新创建。这是本地展示连接，不是 Feature 间的通信接口。

自动验收启动实际 Windows Avalonia Window，操作原生 TextBox/TextBlock 绑定，然后关闭宿主并返回退出码；它不模拟物理键盘。构建 Release 后运行：

```powershell
rtk proxy dotnet run --project sample/MiKiNuo.Mvi.Samples.Avalonia/MiKiNuo.Mvi.Samples.Avalonia.csproj -c Release --no-build -- --verify-v2-input
```

结果默认写入可执行目录的 `v2-input-verification.txt`；可通过 `--v2-input-result=<绝对路径>` 指定已有目录中的结果文件。验收包括普通回写、名称纯转换及同值归一化、展示等待期间的 A/B/A 输入、通知回调中的跨字段及同字段新输入、同字段通知内 A/B/A 的全部提交、只读输入拒绝、连接释放、金额中间输入、只读派生展示、字段过滤、UI 线程、500 次后台提交合并一次展示以及逐次 A/B/C 展示。
