using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using EfMigrationSafety.Cli.Analyzers;
using EfMigrationSafety.Cli.Models;

namespace EfMigrationSafety.Tests;

public class NonNullableWithoutDefaultAnalyzerTests
{
    private readonly NonNullableWithoutDefaultAnalyzer _analyzer = new();

    private static string LoadFixture(string filename)
    {
        var dir = Path.GetDirectoryName(typeof(NonNullableWithoutDefaultAnalyzerTests).Assembly.Location)!;
        return File.ReadAllText(Path.Combine(dir, "Fixtures", filename));
    }

    [Fact]
    public void NullableColumn_ReturnsNoIssues()
    {
        var source = LoadFixture("nullable-column-migration.cs.txt");
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var issues = _analyzer.Analyze(root, "nullable-column.cs").ToList();

        Assert.Empty(issues);
    }

    [Fact]
    public void NonNullableColumnWithDefault_ReturnsNoIssues()
    {
        var source = LoadFixture("non-nullable-with-default-migration.cs.txt");
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var issues = _analyzer.Analyze(root, "non-nullable-with-default.cs").ToList();

        Assert.Empty(issues);
    }

    [Fact]
    public void NonNullableColumnWithoutDefault_ReturnsOneWarning()
    {
        var source = LoadFixture("non-nullable-without-default-migration.cs.txt");
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var issues = _analyzer.Analyze(root, "non-nullable-without-default.cs").ToList();

        Assert.Single(issues);
        Assert.Equal(Severity.Warning, issues[0].Severity);
        Assert.Equal("NonNullableWithoutDefault", issues[0].AnalyzerName);
        Assert.Contains("StatusCode", issues[0].Message);
        Assert.NotNull(issues[0].Recommendation);
    }
}
