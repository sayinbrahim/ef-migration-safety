using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using EfMigrationSafety.Cli.Models;

namespace EfMigrationSafety.Cli.Analyzers;

public class RenameOperationAnalyzer : IMigrationAnalyzer
{
    public string Name => "RenameOperation";
    public string Description => "Detects rename operations (RenameColumn, RenameTable, RenameIndex, RenameSequence) that require coordinated application-code updates.";

    private static readonly HashSet<string> RenameOperations =
        ["RenameColumn", "RenameTable", "RenameIndex", "RenameSequence"];

    public IEnumerable<SafetyIssue> Analyze(SyntaxNode root, string filePath)
    {
        var issues = new List<SafetyIssue>();

        var upMethods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(m => m.Identifier.Text == "Up");

        foreach (var upMethod in upMethods)
        {
            var renameInvocations = upMethod.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(inv => RenameOperations.Contains(AnalyzerHelpers.GetMethodName(inv) ?? ""))
                .Where(inv => !AnalyzerHelpers.IsInsideCreateTable(inv));

            foreach (var invocation in renameInvocations)
            {
                var methodName = AnalyzerHelpers.GetMethodName(invocation)!;
                var line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

                var oldName = AnalyzerHelpers.ExtractNameArgument(invocation);
                var newName = AnalyzerHelpers.ExtractArgument("newName", invocation);
                var table   = AnalyzerHelpers.ExtractArgument("table", invocation);

                var message = BuildMessage(methodName, oldName, newName, table);

                issues.Add(new SafetyIssue(
                    AnalyzerName: Name,
                    Severity: Severity.Info,
                    FilePath: filePath,
                    LineNumber: line,
                    Message: message,
                    Recommendation: "Search the codebase for the old name before deploying. Common locations: entity classes, LINQ queries, raw SQL, stored procedures, reports, integration mappings."));
            }
        }

        return issues;
    }

    private static string BuildMessage(string methodName, string? oldName, string? newName, string? table)
    {
        if (oldName is null || newName is null)
            return $"Rename operation detected ({methodName}). Ensure all application code and downstream references are updated coordinately.";

        return methodName switch
        {
            "RenameColumn" =>
                $"Column rename detected: '{oldName}' → '{newName}'" +
                (table is not null ? $" on table '{table}'" : "") +
                $". Ensure all application code, queries, and ORM mappings referencing '{oldName}' are updated in the same deployment.",

            "RenameTable" =>
                $"Table rename detected: '{oldName}' → '{newName}'. Ensure all application code, queries, foreign key references, and ORM mappings are updated coordinately.",

            "RenameIndex" =>
                $"Index rename detected: '{oldName}' → '{newName}'. Verify no application code or maintenance scripts reference the old index name.",

            "RenameSequence" =>
                $"Sequence rename detected: '{oldName}' → '{newName}'. Verify no application code or stored procedures reference the old sequence name.",

            _ =>
                $"Rename operation detected ({methodName}). Ensure all application code and downstream references are updated coordinately."
        };
    }
}
