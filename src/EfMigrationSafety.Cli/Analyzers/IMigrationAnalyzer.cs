using Microsoft.CodeAnalysis;
using EfMigrationSafety.Cli.Models;

namespace EfMigrationSafety.Cli.Analyzers;

public interface IMigrationAnalyzer
{
    string Name { get; }
    string Description { get; }
    IEnumerable<SafetyIssue> Analyze(SyntaxNode root, string filePath);
}
