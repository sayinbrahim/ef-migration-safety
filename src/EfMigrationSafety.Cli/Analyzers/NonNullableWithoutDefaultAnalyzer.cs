using Microsoft.CodeAnalysis;
using EfMigrationSafety.Cli.Models;

namespace EfMigrationSafety.Cli.Analyzers;

public class NonNullableWithoutDefaultAnalyzer : IMigrationAnalyzer
{
    public string Name => "NonNullableWithoutDefault";
    public string Description => "Detects AddColumn calls that add a non-nullable column to an existing table without a default value, which will fail on non-empty tables.";

    public IEnumerable<SafetyIssue> Analyze(SyntaxNode root, string filePath)
    {
        // TODO: implement
        return Enumerable.Empty<SafetyIssue>();
    }
}
