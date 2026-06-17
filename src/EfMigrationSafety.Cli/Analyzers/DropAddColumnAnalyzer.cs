using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using EfMigrationSafety.Cli.Models;

namespace EfMigrationSafety.Cli.Analyzers;

public class DropAddColumnAnalyzer : IMigrationAnalyzer
{
    public string Name => "DropAddColumn";
    public string Description => "Detects DropColumn + AddColumn on the same column name inside Up(), which destroys data. Use RenameColumn or AlterColumn instead.";

    public IEnumerable<SafetyIssue> Analyze(SyntaxNode root, string filePath)
    {
        var issues = new List<SafetyIssue>();

        var upMethods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(m => m.Identifier.Text == "Up");

        foreach (var upMethod in upMethods)
        {
            var invocations = upMethod.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .ToList();

            var dropColumns = new List<(string ColumnName, int Line)>();
            var addColumnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var invocation in invocations)
            {
                var methodName = GetMethodName(invocation);

                if (methodName == "DropColumn")
                {
                    var columnName = ExtractNameArgument(invocation);
                    if (columnName is not null)
                    {
                        var line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        dropColumns.Add((columnName, line));
                    }
                }
                else if (methodName == "AddColumn")
                {
                    var columnName = ExtractNameArgument(invocation);
                    if (columnName is not null)
                        addColumnNames.Add(columnName);
                }
            }

            foreach (var (columnName, line) in dropColumns)
            {
                if (!addColumnNames.Contains(columnName))
                    continue;

                issues.Add(new SafetyIssue(
                    AnalyzerName: Name,
                    Severity: Severity.Warning,
                    FilePath: filePath,
                    LineNumber: line,
                    Message: $"DropColumn + AddColumn pattern detected for column '{columnName}' (potential data loss). Use RenameColumn instead to preserve data.",
                    Recommendation: "If this is a rename, replace with migrationBuilder.RenameColumn(). If this is intentional column replacement, document the data backfill strategy."));
            }
        }

        return issues;
    }

    private static string? GetMethodName(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch
        {
            MemberAccessExpressionSyntax member => member.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };

    private static string? ExtractNameArgument(InvocationExpressionSyntax invocation)
    {
        var args = invocation.ArgumentList.Arguments;

        var namedArg = args.FirstOrDefault(a =>
            a.NameColon?.Name.Identifier.Text == "name");

        if (namedArg?.Expression is LiteralExpressionSyntax namedLiteral)
            return namedLiteral.Token.ValueText;

        if (args.Count > 0 && args[0].Expression is LiteralExpressionSyntax positionalLiteral)
            return positionalLiteral.Token.ValueText;

        return null;
    }
}
