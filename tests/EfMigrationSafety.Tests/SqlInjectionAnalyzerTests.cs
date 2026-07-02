using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using EfMigrationSafety.Cli.Analyzers;
using EfMigrationSafety.Cli.Models;

namespace EfMigrationSafety.Tests;

public class SqlInjectionAnalyzerTests
{
    private readonly SqlInjectionAnalyzer _analyzer = new();

    private static string LoadFixture(string filename)
    {
        var dir = Path.GetDirectoryName(typeof(SqlInjectionAnalyzerTests).Assembly.Location)!;
        return File.ReadAllText(Path.Combine(dir, "Fixtures", filename));
    }

    [Fact]
    public void SafeStaticSql_ReturnsNoIssues()
    {
        var source = LoadFixture("sql-injection-safe-static.cs.txt");
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var issues = _analyzer.Analyze(root, "safe-static.cs").ToList();

        Assert.Empty(issues);
    }

    [Fact]
    public void InterpolatedString_ReturnsOneError()
    {
        var source = LoadFixture("sql-injection-risky-interpolation.cs.txt");
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var issues = _analyzer.Analyze(root, "risky-interpolation.cs").ToList();

        Assert.Single(issues);
        Assert.Equal(Severity.Error, issues[0].Severity);
        Assert.Equal("SqlInjection", issues[0].AnalyzerName);
        Assert.Contains("interpolated string", issues[0].Message);
        Assert.Contains("name", issues[0].Message);
        Assert.NotNull(issues[0].Recommendation);
    }

    [Fact]
    public void StringConcatenation_ReturnsOneError()
    {
        var source = LoadFixture("sql-injection-risky-concatenation.cs.txt");
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var issues = _analyzer.Analyze(root, "risky-concatenation.cs").ToList();

        Assert.Single(issues);
        Assert.Equal(Severity.Error, issues[0].Severity);
        Assert.Equal("SqlInjection", issues[0].AnalyzerName);
        Assert.Contains("concatenation", issues[0].Message);
        Assert.NotNull(issues[0].Recommendation);
    }

    [Fact]
    public void StringFormat_ReturnsOneError()
    {
        var source = LoadFixture("sql-injection-risky-string-format.cs.txt");
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var issues = _analyzer.Analyze(root, "risky-string-format.cs").ToList();

        Assert.Single(issues);
        Assert.Equal(Severity.Error, issues[0].Severity);
        Assert.Equal("SqlInjection", issues[0].AnalyzerName);
        Assert.Contains("string.Format", issues[0].Message);
        Assert.NotNull(issues[0].Recommendation);
    }

    [Fact]
    public void SafeVerbatimString_ReturnsNoIssues()
    {
        var source = LoadFixture("sql-injection-safe-verbatim.cs.txt");
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var issues = _analyzer.Analyze(root, "safe-verbatim.cs").ToList();

        Assert.Empty(issues);
    }

    [Fact]
    public void MultipleSqlCallsMixed_ReturnsExactlyOneError()
    {
        var source = LoadFixture("sql-injection-multiple-sql-mixed.cs.txt");
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var issues = _analyzer.Analyze(root, "multiple-sql-mixed.cs").ToList();

        Assert.Single(issues);
        Assert.Equal(Severity.Error, issues[0].Severity);
        Assert.Equal("SqlInjection", issues[0].AnalyzerName);
        Assert.Contains("interpolated string", issues[0].Message);
    }
}
