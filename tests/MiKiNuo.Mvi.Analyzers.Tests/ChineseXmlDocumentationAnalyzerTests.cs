using MiKiNuo.Mvi.Analyzers;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Analyzers.Tests;

public sealed class ChineseXmlDocumentationAnalyzerTests
{
    [Test]
    public async Task Reports_MNK0006_for_public_class_missing_xml_documentation()
    {
        const string source = """
            namespace Fixture;

            public class MissingDocumentation
            {
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new ChineseXmlDocumentationAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.ChineseXmlDocumentation);
    }

    [Test]
    public async Task Reports_MNK0006_for_public_class_with_non_chinese_xml_documentation()
    {
        const string source = """
            namespace Fixture;

            /// <summary>
            /// Represents a public type documented only in English.
            /// </summary>
            public class EnglishOnlyDocumentation
            {
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new ChineseXmlDocumentationAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.ChineseXmlDocumentation);
    }

    [Test]
    public async Task Does_not_report_for_public_class_with_chinese_xml_documentation()
    {
        const string source = """
            namespace Fixture;

            /// <summary>
            /// 表示一个具有中文文档的公开类型。
            /// </summary>
            public class ChineseDocumentation
            {
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new ChineseXmlDocumentationAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task Reports_MNK0006_for_public_interface_missing_xml_documentation()
    {
        const string source = """
            namespace Fixture;

            public interface IMissingDocumentation
            {
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new ChineseXmlDocumentationAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.ChineseXmlDocumentation);
    }

    [Test]
    public async Task Reports_MNK0006_for_public_interface_with_non_chinese_xml_documentation()
    {
        const string source = """
            namespace Fixture;

            /// <summary>
            /// Represents a public interface documented only in English.
            /// </summary>
            public interface IEnglishOnlyDocumentation
            {
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new ChineseXmlDocumentationAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.ChineseXmlDocumentation);
    }

    [Test]
    public async Task Does_not_report_for_public_interface_with_chinese_xml_documentation()
    {
        const string source = """
            namespace Fixture;

            /// <summary>
            /// 表示一个具有中文文档的公开接口。
            /// </summary>
            public interface IChineseDocumentation
            {
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new ChineseXmlDocumentationAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task Reports_MNK0006_for_public_property_missing_xml_documentation()
    {
        const string source = """
            namespace Fixture;

            /// <summary>
            /// 表示一个具有中文文档的公开类型。
            /// </summary>
            public class PropertyFixture
            {
                public int Count { get; set; }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new ChineseXmlDocumentationAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.ChineseXmlDocumentation);
    }

    [Test]
    public async Task Reports_MNK0006_for_public_property_with_non_chinese_xml_documentation()
    {
        const string source = """
            namespace Fixture;

            /// <summary>
            /// 表示一个具有中文文档的公开类型。
            /// </summary>
            public class PropertyFixture
            {
                /// <summary>
                /// Gets the count.
                /// </summary>
                public int Count { get; set; }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new ChineseXmlDocumentationAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.ChineseXmlDocumentation);
    }

    [Test]
    public async Task Does_not_report_for_public_property_with_chinese_xml_documentation()
    {
        const string source = """
            namespace Fixture;

            /// <summary>
            /// 表示一个具有中文文档的公开类型。
            /// </summary>
            public class PropertyFixture
            {
                /// <summary>
                /// 获取当前数量。
                /// </summary>
                public int Count { get; set; }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new ChineseXmlDocumentationAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task Reports_MNK0006_for_public_method_missing_xml_documentation()
    {
        const string source = """
            namespace Fixture;

            /// <summary>
            /// 表示一个具有中文文档的公开类型。
            /// </summary>
            public class MethodFixture
            {
                public void Execute()
                {
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new ChineseXmlDocumentationAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.ChineseXmlDocumentation);
    }

    [Test]
    public async Task Reports_MNK0006_for_public_method_with_non_chinese_xml_documentation()
    {
        const string source = """
            namespace Fixture;

            /// <summary>
            /// 表示一个具有中文文档的公开类型。
            /// </summary>
            public class MethodFixture
            {
                /// <summary>
                /// Executes the action.
                /// </summary>
                public void Execute()
                {
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new ChineseXmlDocumentationAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.ChineseXmlDocumentation);
    }

    [Test]
    public async Task Does_not_report_for_public_method_with_chinese_xml_documentation()
    {
        const string source = """
            namespace Fixture;

            /// <summary>
            /// 表示一个具有中文文档的公开类型。
            /// </summary>
            public class MethodFixture
            {
                /// <summary>
                /// 执行当前动作。
                /// </summary>
                public void Execute()
                {
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new ChineseXmlDocumentationAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task Reports_MNK0006_for_public_field_missing_xml_documentation()
    {
        const string source = """
            namespace Fixture;

            /// <summary>
            /// 表示一个具有中文文档的公开类型。
            /// </summary>
            public class FieldFixture
            {
                public int currentCount;
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new ChineseXmlDocumentationAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.ChineseXmlDocumentation);
    }

    [Test]
    public async Task Reports_MNK0006_for_public_field_with_non_chinese_xml_documentation()
    {
        const string source = """
            namespace Fixture;

            /// <summary>
            /// 表示一个具有中文文档的公开类型。
            /// </summary>
            public class FieldFixture
            {
                /// <summary>
                /// Stores the current count.
                /// </summary>
                public int currentCount;
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new ChineseXmlDocumentationAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.ChineseXmlDocumentation);
    }

    [Test]
    public async Task Does_not_report_for_public_field_with_chinese_xml_documentation()
    {
        const string source = """
            namespace Fixture;

            /// <summary>
            /// 表示一个具有中文文档的公开类型。
            /// </summary>
            public class FieldFixture
            {
                /// <summary>
                /// 存储当前数量。
                /// </summary>
                public int currentCount;
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new ChineseXmlDocumentationAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task Reports_MNK0006_for_public_constant_missing_xml_documentation()
    {
        const string source = """
            namespace Fixture;

            /// <summary>
            /// 表示一个具有中文文档的公开类型。
            /// </summary>
            public class ConstantFixture
            {
                public const int MaxCount = 10;
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new ChineseXmlDocumentationAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.ChineseXmlDocumentation);
    }

    [Test]
    public async Task Reports_MNK0006_for_public_constant_with_non_chinese_xml_documentation()
    {
        const string source = """
            namespace Fixture;

            /// <summary>
            /// 表示一个具有中文文档的公开类型。
            /// </summary>
            public class ConstantFixture
            {
                /// <summary>
                /// Stores the maximum count.
                /// </summary>
                public const int MaxCount = 10;
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new ChineseXmlDocumentationAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.ChineseXmlDocumentation);
    }

    [Test]
    public async Task Does_not_report_for_public_constant_with_chinese_xml_documentation()
    {
        const string source = """
            namespace Fixture;

            /// <summary>
            /// 表示一个具有中文文档的公开类型。
            /// </summary>
            public class ConstantFixture
            {
                /// <summary>
                /// 表示最大数量。
                /// </summary>
                public const int MaxCount = 10;
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new ChineseXmlDocumentationAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task Does_not_report_for_full_valid_public_api_with_chinese_xml_documentation()
    {
        const string source = """
            namespace Fixture;

            /// <summary>
            /// 表示完整的公开接口。
            /// </summary>
            public interface ICompleteApi
            {
                /// <summary>
                /// 执行公开操作。
                /// </summary>
                void Execute();
            }

            /// <summary>
            /// 表示完整的公开类型。
            /// </summary>
            public class CompleteApi
            {
                /// <summary>
                /// 存储当前数量。
                /// </summary>
                public int currentCount;

                /// <summary>
                /// 表示最大数量。
                /// </summary>
                public const int MaxCount = 10;

                /// <summary>
                /// 获取当前数量。
                /// </summary>
                public int Count { get; set; }

                /// <summary>
                /// 执行公开操作。
                /// </summary>
                public void Execute()
                {
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new ChineseXmlDocumentationAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task Does_not_report_for_non_public_api_surface()
    {
        const string source = """
            namespace Fixture;

            internal interface IInternalApi
            {
                void Execute();
            }

            internal class InternalContainer
            {
                public int Count { get; set; }

                public int currentCount;

                public const int MaxCount = 10;

                public void Execute()
                {
                }
            }

            /// <summary>
            /// 表示一个具有中文文档的公开类型。
            /// </summary>
            public class PublicContainer
            {
                private int privateCount;

                internal int internalCount;

                private int PrivateProperty { get; set; }

                internal int InternalProperty { get; set; }

                private void PrivateMethod()
                {
                }

                internal void InternalMethod()
                {
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetAnalyzerDiagnosticsAsync(
            new ChineseXmlDocumentationAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }
}
