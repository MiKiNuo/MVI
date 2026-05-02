using Microsoft.CodeAnalysis;

namespace MiKiNuo.Mvi.Analyzers;

public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor ChineseXmlDocumentation = new(
        DiagnosticIds.ChineseXmlDocumentation,
        "缺少或不符合要求的中文 XML 注释",
        "公开符号“{0}”缺少或不符合要求的中文 XML 注释",
        "Documentation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "公开 API 必须提供清晰的中文 XML 注释.");

    public static readonly DiagnosticDescriptor MicrosoftNaming = new(
        DiagnosticIds.MicrosoftNaming,
        "不符合微软命名规范",
        "符号“{0}”不符合微软命名规范：{1}",
        "Naming",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "类、接口、属性、字段、方法和常量必须符合本项目采用的微软命名规范.");

    public static readonly DiagnosticDescriptor CleanArchitecture = new(
        DiagnosticIds.CleanArchitecture,
        "项目引用违反 Clean Architecture",
        "{0} 层不允许引用 {1} 层类型“{2}”",
        "Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "项目引用必须遵守 v1.1 Clean Architecture 分层规则.");

    public static readonly DiagnosticDescriptor PlatformUiReference = new(
        DiagnosticIds.PlatformUiReference,
        "Domain/Application 引用了平台 UI",
        "{0} 层不允许引用平台 UI 类型“{1}”",
        "Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Domain 和 Application 不允许引用 Avalonia、WinForms、Godot 或 Unity 等平台 UI 类型.");
}
