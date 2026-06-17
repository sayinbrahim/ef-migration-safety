namespace EfMigrationSafety.Cli.Models;

public record SafetyIssue(
    string AnalyzerName,
    Severity Severity,
    string FilePath,
    int LineNumber,
    string Message,
    string? Recommendation = null);
