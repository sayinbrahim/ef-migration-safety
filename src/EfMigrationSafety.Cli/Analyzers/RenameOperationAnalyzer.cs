using Microsoft.CodeAnalysis;
using EfMigrationSafety.Cli.Models;

namespace EfMigrationSafety.Cli.Analyzers;

public class RenameOperationAnalyzer : IMigrationAnalyzer
{
    public string Name => "RenameOperation";
    public string Description => "Detects RenameColumn and RenameTable operations that may break application code still referencing the old name.";

    public IEnumerable<SafetyIssue> Analyze(SyntaxNode root, string filePath)
    {
        // TODO: implement
        return Enumerable.Empty<SafetyIssue>();
    }
}
