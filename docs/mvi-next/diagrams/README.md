# v2 设计图源与验证

这些图表达当前 v2 运行协议，入口为 [ARCHITECTURE.md](../ARCHITECTURE.md)。Mermaid 类型与组件名称对应当前 Feature、FeatureStore、Operation、RuntimeSnapshot、投影和定向端口；时序中的 Reduce/ExecuteAsync 是同一实例内的职责，不是额外独立运行对象。

七份 `.mmd` 保留可编辑图源；同名 `.svg` 可放大查看，`.png` 用于正文预览。`render.config.json` 统一配色、中文字体与排版。图包括 MVI 数据流、UML 类结构、组件边界、组合实例关系，以及登录、中介者和 Latest 三份时序。

Mermaid 图由 `@mermaid-js/mermaid-cli 12.0.0` 解析并渲染，七份均成功。已逐图查看 PNG，确认中文内容、关系和顺序可辨认。较长时序建议通过正文图索引打开 SVG 放大阅读。

2026-10-03 切片 20 已刷新七份当前图源及十四份 SVG/PNG，并逐图检查类型、关系、中文文本与时序可辨认。多参数泛型在 Mermaid 类图中以参数间下划线表达，准确 C# 签名以架构映射表和源码为准；组合图中的 EditorA/EditorB 是同类型独立实例示意。

渲染单张图的命令形状如下；浏览器路径按本机配置，`browser-config.json` 是本地 Puppeteer 配置，不提交到仓库。

```powershell
npx --yes --package @mermaid-js/mermaid-cli mmdc -p browser-config.json -c docs/mvi-next/diagrams/render.config.json -i docs/mvi-next/diagrams/uml-types.mmd -o docs/mvi-next/diagrams/uml-types.svg -b white --size 2000
```

交互图由 `mvi-closed-loop.dataflow.json` 生成 `mvi-closed-loop.html`，支持缩放、路径追踪、主题切换和导出。最终校验使用 Archify 3.0.1；`finalize` 的 validate、deliver、严格 provenance check 与 browser-check 四个门均通过，无诊断。可审阅精简回执 [mvi-closed-loop.finalize-summary.json](mvi-closed-loop.finalize-summary.json)；交付来源回执为 [mvi-closed-loop.delivery.json](mvi-closed-loop.delivery.json)。

另已执行绑定当前交付字节的 visual-check，containment、readability、viewerChrome、themeStates 与 captures 五项均通过。人工查看最终 1440×900 浅色与 2048×1320 深色截图，状态主路径、副作用反馈路径、图例和文字可辨认。视觉修正共两轮，最终人工视觉审阅通过。捕获与详细浏览器回执留在本地 `visual-review-final/`，不作为源码提交。

| 最终交付项 | 大小 | SHA-256 |
| --- | --- | --- |
| 图源 JSON | 3557 bytes | `8ceff5462cd069156033bd194597be1db1670515ca7281ee9737f7c3c20acada` |
| HTML | 758173 bytes | `2afa48b5267a6388da884aad4d01bbe25fd4c22f47632b46319c5263dc160241` |

交互 JSON 与 HTML 保持现有逻辑闭环表达，本次没有修改这两项交付字节；上述交互回执为历史图文证据。切片 20 刷新 Mermaid 与对应 SVG/PNG 的记录见 20 票据。这些校验证明图源和呈现可用，不代替运行行为或性能验收。修改交互图源后须重新生成并验证 HTML；修改 Mermaid 后须同步刷新对应 SVG/PNG。
