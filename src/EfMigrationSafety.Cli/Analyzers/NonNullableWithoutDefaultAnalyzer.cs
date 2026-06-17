using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using EfMigrationSafety.Cli.Models;

namespace EfMigrationSafety.Cli.Analyzers;

public class NonNullableWithoutDefaultAnalyzer : IMigrationAnalyzer
{
    public string Name => "NonNullableWithoutDefault";
    public string Description => "Detects AddColumn calls that add a non-nullable column to an existing table without a default value, which will fail on non-empty tables.";

    public IEnumerable<SafetyIssue> Analyze(SyntaxNode root, string filePath)
    {
        var issues = new List<SafetyIssue>();

        var upMethods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(m => m.Identifier.Text == "Up");

        foreach (var upMethod in upMethods)
        {
            var addColumnCalls = upMethod.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(inv => AnalyzerHelpers.GetMethodName(inv) == "AddColumn")
                .Where(inv => !AnalyzerHelpers.IsInsideCreateTable(inv));

            foreach (var invocation in addColumnCalls)
            {
                var args = invocation.ArgumentList.Arguments;

                // TODO v2: handle purely positional arguments (no NameColon on any arg)
                if (args.All(a => a.NameColon is null))
                    continue;

                var nullableArg = args.FirstOrDefault(a =>
                    a.NameColon?.Name.Identifier.Text == "nullable");

                var hasNullableFalse =
                    nullableArg?.Expression is LiteralExpressionSyntax lit &&
                    lit.Token.IsKind(SyntaxKind.FalseKeyword);

                if (!hasNullableFalse)
                    continue;

                var hasDefault = args.Any(a =>
                    a.NameColon?.Name.Identifier.Text is "defaultValue" or "defaultValueSql");

                if (hasDefault)
                    continue;

                var columnName = AnalyzerHelpers.ExtractNameArgument(invocation) ?? "unknown";
                var line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

                issues.Add(new SafetyIssue(
                    AnalyzerName: Name,
                    Severity: Severity.Warning,
                    FilePath: filePath,
                    LineNumber: line,
                    Message: $"Non-nullable column '{columnName}' added without defaultValue. Will fail on first deploy if the table contains rows.",
                    Recommendation: "Either: (1) make the column nullable initially, backfill in a separate migration, then alter to non-nullable; or (2) provide defaultValue / defaultValueSql in this AddColumn call."));
            }
        }

        return issues;
    }
}
