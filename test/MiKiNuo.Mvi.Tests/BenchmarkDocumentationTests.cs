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

/// <summary>用真实基准记录和中文文档分析器验证五个位置属性的注释。</summary>
public sealed class BenchmarkDocumentationTests
{
    /// <summary>移除参数注释后，应能重现五个位置属性的文档诊断。</summary>
    [Test]
    public async Task MissingParameterDocumentationReportsFiveErrorsAsync()
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeRecordsAsync(includeParameterDocs: false);
        await Assert.That(diagnostics.Count(diagnostic => diagnostic.Id == "DOC0003")).IsEqualTo(5);
    }

    /// <summary>实际基准记录应能编译，且不再产生中文文档诊断。</summary>
    [Test]
    public async Task BenchmarkRecordsHaveChineseDocumentationAsync()
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeRecordsAsync(includeParameterDocs: true);
        await Assert.That(string.Join("\n", diagnostics)).IsEqualTo(string.Empty);
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeRecordsAsync(bool includeParameterDocs)
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "MiKiNuo.Mvi.slnx"))) root = root.Parent;
        if (root is null) throw new InvalidOperationException("未找到解决方案根目录。");

        string path = Path.Combine(root.FullName, "test", "MiKiNuo.Mvi.Benchmarks", "Scenarios", "Mvi", "LoginReplica", "BenchLoginScenario.cs");
        string source = File.ReadAllText(path);
        if (!includeParameterDocs)
            source = Regex.Replace(source, @"(?m)^[ \t]*///[ \t]*<param\b[^\r\n]*\r?\n", string.Empty);

        CSharpParseOptions options = new(LanguageVersion.Preview, DocumentationMode.Diagnose);
        CompilationUnitSyntax syntax = CSharpSyntaxTree.ParseText(source, options).GetCompilationUnitRoot();
        FileScopedNamespaceDeclarationSyntax declaration = syntax.Members.OfType<FileScopedNamespaceDeclarationSyntax>().Single();
        // 保留源码中的真实记录及文档，仅隔离 Handler、Reducer 和生成式 ViewModel。
        FileScopedNamespaceDeclarationSyntax records = declaration.WithMembers(
            SyntaxFactory.List<MemberDeclarationSyntax>(declaration.Members.OfType<RecordDeclarationSyntax>()));
        SyntaxTree tree = CSharpSyntaxTree.Create(syntax.ReplaceNode(declaration, records), options, path);
        CSharpCompilation compilation = GeneratorTestHost.CreateCompilation(string.Empty, GeneratorTestHost.FrameworkReferences)
            .RemoveAllSyntaxTrees().AddSyntaxTrees(tree);
        using MemoryStream assembly = new();
        Microsoft.CodeAnalysis.Emit.EmitResult result = compilation.Emit(assembly);
        await Assert.That(string.Join("\n", result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))).IsEqualTo(string.Empty);
        await Assert.That(result.Success).IsTrue();
        return await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new MiKiNuoChineseDocumentationAnalyzer()))
            .GetAnalyzerDiagnosticsAsync();
    }
}
