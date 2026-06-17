using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using EfMigrationSafety.Cli.Analyzers;
using EfMigrationSafety.Cli.Models;

namespace EfMigrationSafety.Tests;

public class DropAddColumnAnalyzerTests
{
    private readonly DropAddColumnAnalyzer _analyzer = new();

    private static string LoadFixture(string filename)
    {
        var dir = Path.GetDirectoryName(typeof(DropAddColumnAnalyzerTests).Assembly.Location)!;
        return File.ReadAllText(Path.Combine(dir, "Fixtures", filename));
    }

    [Fact]
    public void SafeMigration_NoDropAddPair_ReturnsNoIssues()
    {
        var source = LoadFixture("safe-migration.cs.txt");
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var issues = _analyzer.Analyze(root, "safe-migration.cs").ToList();

        Assert.Empty(issues);
    }

    [Fact]
    public void DropAndAddSameColumnName_InUpMethod_ReturnsOneWarning()
    {
        var source = LoadFixture("drop-add-column-migration.cs.txt");
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var issues = _analyzer.Analyze(root, "drop-add-column.cs").ToList();

        Assert.Single(issues);
        Assert.Equal(Severity.Warning, issues[0].Severity);
        Assert.Contains("Email", issues[0].Message);
        Assert.Equal("DropAddColumn", issues[0].AnalyzerName);
        Assert.NotNull(issues[0].Recommendation);
    }

    [Fact]
    public void DropOneColumn_AddDifferentColumn_ReturnsNoIssues()
    {
        const string source = """
            using Microsoft.EntityFrameworkCore.Migrations;
            namespace MyApp.Migrations
            {
                public partial class UnrelatedDropAdd : Migration
                {
                    protected override void Up(MigrationBuilder migrationBuilder)
                    {
                        migrationBuilder.DropColumn(
                            name: "ObsoleteCode",
                            table: "Products");

                        migrationBuilder.AddColumn<string>(
                            name: "Sku",
                            table: "Products",
                            type: "nvarchar(50)",
                            nullable: true);
                    }

                    protected override void Down(MigrationBuilder migrationBuilder) { }
                }
            }
            """;

        var root = CSharpSyntaxTree.ParseText(source).GetRoot();
        var issues = _analyzer.Analyze(root, "unrelated.cs").ToList();

        Assert.Empty(issues);
    }
}
