using Microsoft.CodeAnalysis;
using EfMigrationSafety.Cli.Models;

namespace EfMigrationSafety.Cli.Analyzers;

public class EmptyDownMethodAnalyzer : IMigrationAnalyzer
{
    public string Name => "EmptyDownMethod";
    public string Description => "Detects migrations whose Down() method is empty or throws NotImplementedException, making rollbacks impossible.";

    public IEnumerable<SafetyIssue> Analyze(SyntaxNode root, string filePath)
    {
        // TODO: implement
        return Enumerable.Empty<SafetyIssue>();
    }
}
