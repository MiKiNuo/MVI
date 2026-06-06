### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
ARCH0007 | Architecture | Error | Presentation 抽象层禁止引用具体平台项目。
ARCH0008 | Architecture | Error | Infrastructure 源生成器与分析器禁止出现示例项目专属代码（命名空间、类型、字符串）。
ARCH0009 | Architecture | Error | Presentation 抽象层禁止直接引用 Avalonia / Godot 等具体平台 NuGet 包。
MVI0001 | MviBinding | Error | 命令 Intent 存在多个 payload 构造函数时要求显式指定 PayloadType。
MVI0002 | MviBinding | Error | 命令 Intent 缺少指定 PayloadType 对应的一参构造函数。
MVI0003 | MviBinding | Error | 命令 Intent 缺少公开无参构造函数或唯一的一参 payload 构造函数。
