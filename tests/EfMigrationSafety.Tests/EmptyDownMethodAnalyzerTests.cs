using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using EfMigrationSafety.Cli.Analyzers;
using EfMigrationSafety.Cli.Models;

namespace EfMigrationSafety.Tests;

public class EmptyDownMethodAnalyzerTests
{
    private readonly EmptyDownMethodAnalyzer _analyzer = new();

    private static string LoadFixture(string filename)
    {
        var dir = Path.GetDirectoryName(typeof(EmptyDownMethodAnalyzerTests).Assembly.Location)!;
        return File.ReadAllText(Path.Combine(dir, "Fixtures", filename));
    }

    [Fact]
    public void ImplementedDownMethod_ReturnsNoIssues()
    {
        var source = LoadFixture("implemented-down-migration.cs.txt");
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var issues = _analyzer.Analyze(root, "implemented-down.cs").ToList();

        Assert.Empty(issues);
    }

    [Fact]
    public void EmptyDownBody_ReturnsOneIssueWithEmptyMessage()
    {
        var source = LoadFixture("empty-down-migration.cs.txt");
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var issues = _analyzer.Analyze(root, "empty-down.cs").ToList();

        Assert.Single(issues);
        Assert.Equal(Severity.Warning, issues[0].Severity);
        Assert.Equal("EmptyDownMethod", issues[0].AnalyzerName);
        Assert.Contains("empty", issues[0].Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(issues[0].Recommendation);
    }

    [Fact]
    public void ThrowsNotImplementedBlockBody_ReturnsOneIssueWithThrowMessage()
    {
        var source = LoadFixture("throws-down-migration.cs.txt");
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var issues = _analyzer.Analyze(root, "throws-down.cs").ToList();

        Assert.Single(issues);
        Assert.Equal(Severity.Warning, issues[0].Severity);
        Assert.Equal("EmptyDownMethod", issues[0].AnalyzerName);
        Assert.Contains("NotImplementedException", issues[0].Message);
        Assert.NotNull(issues[0].Recommendation);
    }

    [Fact]
    public void ThrowsNotImplementedExpressionBody_ReturnsOneIssueWithThrowMessage()
    {
        var source = LoadFixture("expression-bodied-throws-down-migration.cs.txt");
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var issues = _analyzer.Analyze(root, "expression-bodied-throws-down.cs").ToList();

        Assert.Single(issues);
        Assert.Equal(Severity.Warning, issues[0].Severity);
        Assert.Equal("EmptyDownMethod", issues[0].AnalyzerName);
        Assert.Contains("NotImplementedException", issues[0].Message);
        Assert.NotNull(issues[0].Recommendation);
    }
}
