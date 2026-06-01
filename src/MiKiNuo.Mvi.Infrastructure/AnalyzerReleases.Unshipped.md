### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
ARCH0007 | Architecture | Error | Presentation 抽象层禁止引用具体平台项目。
MVI0001 | MviBinding | Error | 命令 Intent 存在多个 payload 构造函数时要求显式指定 PayloadType。
MVI0002 | MviBinding | Error | 命令 Intent 缺少指定 PayloadType 对应的一参构造函数。
MVI0003 | MviBinding | Error | 命令 Intent 缺少公开无参构造函数或唯一的一参 payload 构造函数。
