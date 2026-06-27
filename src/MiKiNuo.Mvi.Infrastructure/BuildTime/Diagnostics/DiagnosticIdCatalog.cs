namespace MiKiNuo.Mvi.Infrastructure.BuildTime.Diagnostics;

/// <summary>
/// 表示 MVI 框架全部诊断 ID 的集中目录。
/// <para>
/// 所有分析器和源生成器必须从此处读取 ID，禁止在 <see cref="Microsoft.CodeAnalysis.DiagnosticDescriptor"/> 构造里直接写 ID 字符串。
/// 该目录同步驱动 <c>AnalyzerReleases.Shipped.md</c> 和 <c>AnalyzerReleases.Unshipped.md</c>，
/// 仓库内的发布追踪回归测试（位于 test 项目中）会校验 ID 是否被两边的发布文件完整覆盖。
/// </para>
/// </summary>
public static class DiagnosticIdCatalog
{
    /// <summary>整洁架构分层引用检查 ID。</summary>
    public const string ArchDomainReference = "ARCH0001";

    /// <summary>Application 层禁止引用外层。</summary>
    public const string ArchApplicationReference = "ARCH0002";

    /// <summary>Infrastructure 层禁止引用 Presentation。</summary>
    public const string ArchInfrastructureReference = "ARCH0003";

    /// <summary>src 禁止引用 sample。</summary>
    public const string ArchSourceReferenceSample = "ARCH0004";

    /// <summary>sample 禁止被 src 反向引用。</summary>
    public const string ArchSampleReference = "ARCH0005";

    /// <summary>test 禁止被 src/sample 反向引用。</summary>
    public const string ArchTestReference = "ARCH0006";

    /// <summary>Presentation 禁止引用具体平台项目。</summary>
    public const string ArchPresentationReferencePlatform = "ARCH0007";

    /// <summary>Infrastructure 禁止出现示例项目专属代码（命名空间、类型、字符串）。</summary>
    public const string ArchInfrastructureSampleIsolation = "ARCH0008";

    /// <summary>
    /// Presentation 抽象层禁止直接引用 Avalonia / Godot 等具体平台 NuGet 包。
    /// <para>
    /// 与 <see cref="ArchPresentationReferencePlatform"/> 互为补集：
    /// 后者拦截 MiKiNuo.Mvi.Platforms.* 的项目引用，
    /// 本规则拦截通过 <c>&lt;PackageReference&gt;</c> 直接引入 Avalonia / Godot 的情况，
    /// 避免 Presentation 在编译期就握住具体平台类型。
    /// </para>
    /// </summary>
    public const string ArchPresentationPackageIsolation = "ARCH0009";

    /// <summary>公共类型必须提供中文 XML 注释。</summary>
    public const string DocTypeMissing = "DOC0001";

    /// <summary>公共方法必须提供中文 XML 注释。</summary>
    public const string DocMethodMissing = "DOC0002";

    /// <summary>公共属性必须提供中文 XML 注释。</summary>
    public const string DocPropertyMissing = "DOC0003";

    /// <summary>公共字段和常量必须提供中文 XML 注释。</summary>
    public const string DocFieldMissing = "DOC0004";

    /// <summary>接口成员必须提供中文 XML 注释。</summary>
    public const string DocInterfaceMemberMissing = "DOC0005";

    /// <summary>公共方法参数必须提供中文说明。</summary>
    public const string DocParameterMissing = "DOC0006";

    /// <summary>公共方法返回值必须提供中文说明。</summary>
    public const string DocReturnMissing = "DOC0007";

    /// <summary>XML 注释不能为空或占位内容。</summary>
    public const string DocInvalidPlaceholder = "DOC0008";

    /// <summary>类型命名必须符合微软 C# 编码规范。</summary>
    public const string CodeTypeNaming = "CODE0001";

    /// <summary>成员命名必须符合微软 C# 编码规范。</summary>
    public const string CodeMemberNaming = "CODE0002";

    /// <summary>命令 Intent 存在多个 payload 构造函数。</summary>
    public const string MviAmbiguousPayloadConstructor = "MVI0001";

    /// <summary>命令 Intent 缺少指定 payload 构造函数。</summary>
    public const string MviMissingPayloadConstructor = "MVI0002";

    /// <summary>命令 Intent 缺少可用构造函数。</summary>
    public const string MviMissingIntentConstructor = "MVI0003";

    /// <summary>规约器类未标记 partial 修饰符。</summary>
    public const string MviReducerNotPartial = "MVI0004";

    /// <summary>意图子类型缺少对应的规约方法。</summary>
    public const string MviReduceHandlerMissing = "MVI0005";

    /// <summary>多个规约方法标记同一意图子类型。</summary>
    public const string MviReduceHandlerDuplicate = "MVI0006";

    /// <summary>规约方法签名不符合约定。</summary>
    public const string MviReduceHandlerSignatureInvalid = "MVI0007";

    /// <summary>守卫谓词方法不存在或签名不匹配。</summary>
    public const string MviReduceGuardInvalid = "MVI0008";

    /// <summary>声明 [MviBind] 时禁止手写 ApplyStateCore 重写。</summary>
    public const string MviApplyStateCoreConflict = "MVI0009";

    /// <summary>
    /// 获取全部已发布的诊断 ID 列表。
    /// 列表顺序与诊断类别（ARCH → DOC → CODE → MVI）保持一致，便于人工审查。
    /// </summary>
    public static IReadOnlyList<string> AllIds { get; } =
    [
        ArchDomainReference,
        ArchApplicationReference,
        ArchInfrastructureReference,
        ArchSourceReferenceSample,
        ArchSampleReference,
        ArchTestReference,
        ArchPresentationReferencePlatform,
        ArchInfrastructureSampleIsolation,
        ArchPresentationPackageIsolation,
        DocTypeMissing,
        DocMethodMissing,
        DocPropertyMissing,
        DocFieldMissing,
        DocInterfaceMemberMissing,
        DocParameterMissing,
        DocReturnMissing,
        DocInvalidPlaceholder,
        CodeTypeNaming,
        CodeMemberNaming,
        MviAmbiguousPayloadConstructor,
        MviMissingPayloadConstructor,
        MviMissingIntentConstructor,
        MviReducerNotPartial,
        MviReduceHandlerMissing,
        MviReduceHandlerDuplicate,
        MviReduceHandlerSignatureInvalid,
        MviReduceGuardInvalid,
        MviApplyStateCoreConflict,
    ];
}
