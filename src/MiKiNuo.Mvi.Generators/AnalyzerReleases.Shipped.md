; Shipped analyzer releases
; https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.5.2

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
ARCH0001 | Architecture | Error | Domain 层禁止引用外层项目。
ARCH0002 | Architecture | Error | Application 层禁止引用 Infrastructure 或 Presentation。
ARCH0003 | Architecture | Error | Infrastructure 层禁止引用 Presentation。
ARCH0004 | Architecture | Error | src 项目禁止引用 sample 项目。
ARCH0005 | Architecture | Error | sample 项目禁止被 src 反向引用。
ARCH0006 | Architecture | Error | test 项目禁止被业务或框架项目引用。
CODE0001 | CodingStyle | Error | 类型命名必须符合微软 C# 编码规范。
CODE0002 | CodingStyle | Error | 成员命名必须符合微软 C# 编码规范。
DOC0001 | Documentation | Error | 公共类型必须提供中文 XML 注释。
DOC0002 | Documentation | Error | 公共方法必须提供中文 XML 注释。
DOC0003 | Documentation | Error | 公共属性必须提供中文 XML 注释。
DOC0004 | Documentation | Error | 公共字段和常量必须提供中文 XML 注释。
DOC0005 | Documentation | Error | 接口成员必须提供中文 XML 注释。
DOC0006 | Documentation | Error | 公共方法参数必须提供中文说明。
DOC0007 | Documentation | Error | 公共方法返回值必须提供中文说明。
DOC0008 | Documentation | Error | XML 注释不能为空或占位内容。
