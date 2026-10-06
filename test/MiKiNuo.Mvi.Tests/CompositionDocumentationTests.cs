using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using MiKiNuo.Mvi.Generators.Analyzers;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>用真实记录声明和项目分析器验证位置属性的中文文档，不用字符串包含检查代替分析器。</summary>
public sealed class CompositionDocumentationTests
{
    /// <summary>移除参数说明后，原来的十五个位置属性都必须被分析器报告。</summary>
    [Test]
    public async Task UndocumentedPositionalPropertiesReportFifteenErrorsAsync()
    {
        CSharpCompilation compilation = CreateRecordsCompilation(includeParameterDocs: false);
        await VerifyCompilationAsync(compilation);
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(compilation);
        await Assert.That(diagnostics.Count(diagnostic => diagnostic.Id == "DOC0003")).IsEqualTo(15);
    }

    /// <summary>实际示例记录必须可编译，且所有位置属性均具有中文 XML 文档并通过分析器。</summary>
    [Test]
    public async Task SampleRecordsHaveChineseDocumentationAsync()
    {
        CSharpCompilation compilation = CreateRecordsCompilation(includeParameterDocs: true);
        await VerifyCompilationAsync(compilation);
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(compilation);
        await Assert.That(string.Join("\n", diagnostics)).IsEqualTo(string.Empty);

        int checkedProperties = 0;
        foreach (SyntaxTree tree in compilation.SyntaxTrees)
        {
            SemanticModel model = compilation.GetSemanticModel(tree);
            foreach (RecordDeclarationSyntax record in tree.GetRoot().DescendantNodes().OfType<RecordDeclarationSyntax>())
            {
                if (record.ParameterList is null) continue;
                INamedTypeSymbol symbol = (INamedTypeSymbol)model.GetDeclaredSymbol(record)!;
                foreach (ParameterSyntax parameter in record.ParameterList.Parameters)
                {
                    IPropertySymbol property = symbol.GetMembers(parameter.Identifier.ValueText).OfType<IPropertySymbol>().Single();
                    string documentation = property.GetDocumentationCommentXml() ?? string.Empty;
                    await Assert.That(Regex.IsMatch(documentation, "[\\u4e00-\\u9fff]")).IsTrue();
                    checkedProperties++;
                }
            }
        }
        await Assert.That(checkedProperties).IsEqualTo(15);
    }

    private static async Task VerifyCompilationAsync(CSharpCompilation compilation)
    {
        using MemoryStream assembly = new();
        Microsoft.CodeAnalysis.Emit.EmitResult result = compilation.Emit(assembly);
        await Assert.That(string.Join("\n", result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))).IsEqualTo(string.Empty);
        await Assert.That(result.Success).IsTrue();
    }

    private static Task<ImmutableArray<Diagnostic>> AnalyzeAsync(CSharpCompilation compilation)
        => compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new MiKiNuoChineseDocumentationAnalyzer()))
            .GetAnalyzerDiagnosticsAsync();

    private static CSharpCompilation CreateRecordsCompilation(bool includeParameterDocs)
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "MiKiNuo.Mvi.slnx"))) root = root.Parent;
        if (root is null) throw new InvalidOperationException("未找到解决方案根目录。");

        string directory = Path.Combine(root.FullName, "sample", "MiKiNuo.Mvi.Samples.Avalonia", "Features", "CompositionDemo");
        CSharpParseOptions options = new(LanguageVersion.Preview, DocumentationMode.Diagnose);
        List<SyntaxTree> trees = [];
        foreach (string name in new[] { "MedicationDetails.cs", "MedicineSearch.cs", "PrescriptionWorkspace.cs" })
        {
            string source = File.ReadAllText(Path.Combine(directory, name));
            if (!includeParameterDocs)
            {
                source = Regex.Replace(source, @"(?m)^[ \t]*///[ \t]*<param\b[^\r\n]*\r?\n", string.Empty);
            }
            CompilationUnitSyntax syntax = CSharpSyntaxTree.ParseText(source, options).GetCompilationUnitRoot();
            FileScopedNamespaceDeclarationSyntax namespaceDeclaration = syntax.Members.OfType<FileScopedNamespaceDeclarationSyntax>().Single();
            // 只隔离记录契约；保留真实命名空间、继承关系、属性、默认值与 XML 文档。
            // Handler 和 Reducer 由项目原有生成器测试覆盖，不为文档测试伪造它们。
            FileScopedNamespaceDeclarationSyntax records = namespaceDeclaration.WithMembers(
                SyntaxFactory.List<MemberDeclarationSyntax>(namespaceDeclaration.Members.OfType<RecordDeclarationSyntax>()));
            trees.Add(CSharpSyntaxTree.Create(syntax.ReplaceNode(namespaceDeclaration, records), options, name));
        }
        return GeneratorTestHost.CreateCompilation(string.Empty, GeneratorTestHost.FrameworkReferences)
            .RemoveAllSyntaxTrees().AddSyntaxTrees(trees);
    }
}
