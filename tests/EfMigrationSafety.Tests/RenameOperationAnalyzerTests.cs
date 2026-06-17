using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using EfMigrationSafety.Cli.Analyzers;
using EfMigrationSafety.Cli.Models;

namespace EfMigrationSafety.Tests;

public class RenameOperationAnalyzerTests
{
    private readonly RenameOperationAnalyzer _analyzer = new();

    private static string LoadFixture(string filename)
    {
        var dir = Path.GetDirectoryName(typeof(RenameOperationAnalyzerTests).Assembly.Location)!;
        return File.ReadAllText(Path.Combine(dir, "Fixtures", filename));
    }

    [Fact]
    public void NoRenameOperations_ReturnsNoIssues()
    {
        var source = LoadFixture("no-rename-migration.cs.txt");
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var issues = _analyzer.Analyze(root, "no-rename.cs").ToList();

        Assert.Empty(issues);
    }

    [Fact]
    public void RenameColumn_ReturnsOneInfoIssueWithOldAndNewNames()
    {
        var source = LoadFixture("rename-column-migration.cs.txt");
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var issues = _analyzer.Analyze(root, "rename-column.cs").ToList();

        Assert.Single(issues);
        Assert.Equal(Severity.Info, issues[0].Severity);
        Assert.Equal("RenameOperation", issues[0].AnalyzerName);
        Assert.Contains("Email", issues[0].Message);
        Assert.Contains("EmailAddress", issues[0].Message);
        Assert.Contains("Users", issues[0].Message);
        Assert.NotNull(issues[0].Recommendation);
    }

    [Fact]
    public void RenameTable_ReturnsOneInfoIssueWithOldAndNewNames()
    {
        var source = LoadFixture("rename-table-migration.cs.txt");
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var issues = _analyzer.Analyze(root, "rename-table.cs").ToList();

        Assert.Single(issues);
        Assert.Equal(Severity.Info, issues[0].Severity);
        Assert.Equal("RenameOperation", issues[0].AnalyzerName);
        Assert.Contains("UserProfiles", issues[0].Message);
        Assert.Contains("Profiles", issues[0].Message);
        Assert.NotNull(issues[0].Recommendation);
    }

    [Fact]
    public void RenameColumnAndRenameIndex_ReturnsTwoInfoIssues()
    {
        var source = LoadFixture("multiple-rename-migration.cs.txt");
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var issues = _analyzer.Analyze(root, "multiple-rename.cs").ToList();

        Assert.Equal(2, issues.Count);
        Assert.All(issues, i => Assert.Equal(Severity.Info, i.Severity));
        Assert.All(issues, i => Assert.Equal("RenameOperation", i.AnalyzerName));

        var columnIssue = issues.Single(i => i.Message.Contains("Column rename"));
        Assert.Contains("CustomerId", columnIssue.Message);
        Assert.Contains("ClientId", columnIssue.Message);

        var indexIssue = issues.Single(i => i.Message.Contains("Index rename"));
        Assert.Contains("IX_Orders_CustomerId", indexIssue.Message);
        Assert.Contains("IX_Orders_ClientId", indexIssue.Message);
    }
}
