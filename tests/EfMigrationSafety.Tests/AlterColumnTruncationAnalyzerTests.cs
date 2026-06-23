using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using EfMigrationSafety.Cli.Analyzers;
using EfMigrationSafety.Cli.Models;

namespace EfMigrationSafety.Tests;

public class AlterColumnTruncationAnalyzerTests
{
    private readonly AlterColumnTruncationAnalyzer _analyzer = new();

    private static string LoadFixture(string filename)
    {
        var dir = Path.GetDirectoryName(typeof(AlterColumnTruncationAnalyzerTests).Assembly.Location)!;
        return File.ReadAllText(Path.Combine(dir, "Fixtures", filename));
    }

    [Fact]
    public void NoAlterColumn_ReturnsNoIssues()
    {
        var source = LoadFixture("no-alter-column-migration.cs.txt");
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var issues = _analyzer.Analyze(root, "no-alter-column.cs").ToList();

        Assert.Empty(issues);
    }

    [Fact]
    public void AlterColumnWidening_NvarcharFiftyToTwoFiftySix_ReturnsNoIssues()
    {
        var source = LoadFixture("alter-column-widening-migration.cs.txt");
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var issues = _analyzer.Analyze(root, "alter-column-widening.cs").ToList();

        Assert.Empty(issues);
    }

    [Fact]
    public void AlterColumnShrinking_NvarcharTwoFiftySixToFifty_ReturnsOneWarning()
    {
        var source = LoadFixture("alter-column-truncation-migration.cs.txt");
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var issues = _analyzer.Analyze(root, "alter-column-truncation.cs").ToList();

        Assert.Single(issues);
        Assert.Equal(Severity.Warning, issues[0].Severity);
        Assert.Equal("AlterColumnTruncation", issues[0].AnalyzerName);
        Assert.Contains("Description", issues[0].Message);
        Assert.Contains("256", issues[0].Message);
        Assert.Contains("50", issues[0].Message);
        Assert.NotNull(issues[0].Recommendation);
    }

    [Fact]
    public void AlterColumnMaxToSized_ReturnsOneWarningWithUnlimitedMessage()
    {
        var source = LoadFixture("alter-column-max-to-sized-migration.cs.txt");
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var issues = _analyzer.Analyze(root, "alter-column-max-to-sized.cs").ToList();

        Assert.Single(issues);
        Assert.Equal(Severity.Warning, issues[0].Severity);
        Assert.Equal("AlterColumnTruncation", issues[0].AnalyzerName);
        Assert.Contains("Biography", issues[0].Message);
        Assert.Contains("unlimited", issues[0].Message);
        Assert.Contains("100", issues[0].Message);
        Assert.NotNull(issues[0].Recommendation);
    }

    [Fact]
    public void AlterColumnNonString_IntToSmallint_ReturnsNoIssues()
    {
        var source = LoadFixture("alter-column-non-string-migration.cs.txt");
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var issues = _analyzer.Analyze(root, "alter-column-non-string.cs").ToList();

        Assert.Empty(issues);
    }

    [Fact]
    public void AlterColumnVarcharShrinking_ReturnsOneWarning()
    {
        var source = LoadFixture("alter-column-varchar-truncation-migration.cs.txt");
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var issues = _analyzer.Analyze(root, "alter-column-varchar-truncation.cs").ToList();

        Assert.Single(issues);
        Assert.Equal(Severity.Warning, issues[0].Severity);
        Assert.Equal("AlterColumnTruncation", issues[0].AnalyzerName);
        Assert.Contains("PostalCode", issues[0].Message);
        Assert.Contains("100", issues[0].Message);
        Assert.Contains("50", issues[0].Message);
        Assert.NotNull(issues[0].Recommendation);
    }

    [Fact]
    public void AlterColumnCharShrinking_ReturnsOneWarning()
    {
        var source = LoadFixture("alter-column-char-truncation-migration.cs.txt");
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var issues = _analyzer.Analyze(root, "alter-column-char-truncation.cs").ToList();

        Assert.Single(issues);
        Assert.Equal(Severity.Warning, issues[0].Severity);
        Assert.Equal("AlterColumnTruncation", issues[0].AnalyzerName);
        Assert.Contains("CountryCode", issues[0].Message);
        Assert.Contains("10", issues[0].Message);
        Assert.Contains("5", issues[0].Message);
        Assert.NotNull(issues[0].Recommendation);
    }

    [Fact]
    public void AlterColumnEncodingChange_NvarcharToVarchar_ReturnsNoIssues()
    {
        var source = LoadFixture("alter-column-encoding-change-migration.cs.txt");
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var issues = _analyzer.Analyze(root, "alter-column-encoding-change.cs").ToList();

        Assert.Empty(issues);
    }

    [Fact]
    public void AlterColumnInsideCreateTable_ReturnsNoIssues()
    {
        var source = LoadFixture("alter-column-inside-createtable-migration.cs.txt");
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var issues = _analyzer.Analyze(root, "alter-column-inside-createtable.cs").ToList();

        Assert.Empty(issues);
    }
}
