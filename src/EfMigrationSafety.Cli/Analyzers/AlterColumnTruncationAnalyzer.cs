using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using EfMigrationSafety.Cli.Models;

namespace EfMigrationSafety.Cli.Analyzers;

public class AlterColumnTruncationAnalyzer : IMigrationAnalyzer
{
    public string Name => "AlterColumnTruncation";
    public string Description => "Detects AlterColumn calls that reduce the size of nvarchar, varchar, char, or nchar columns — causes silent data truncation in production.";

    private record SizedTypeInfo(string TypeName, int? Size, bool IsMax);

    public IEnumerable<SafetyIssue> Analyze(SyntaxNode root, string filePath)
    {
        var issues = new List<SafetyIssue>();

        var upMethods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(m => m.Identifier.Text == "Up");

        foreach (var upMethod in upMethods)
        {
            var alterInvocations = upMethod.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(inv => AnalyzerHelpers.GetMethodName(inv) == "AlterColumn")
                .Where(inv => !AnalyzerHelpers.IsInsideCreateTable(inv));

            foreach (var invocation in alterInvocations)
            {
                var newTypeStr = AnalyzerHelpers.ExtractArgument("type", invocation);
                var oldTypeStr = AnalyzerHelpers.ExtractArgument("oldType", invocation);

                if (newTypeStr is null || oldTypeStr is null)
                    continue;

                var newType = ParseSizedType(newTypeStr);
                var oldType = ParseSizedType(oldTypeStr);

                if (newType is null || oldType is null)
                    continue;

                // Skip encoding changes (nvarchar→varchar etc.) — separate concern, v0.2
                if (newType.TypeName != oldType.TypeName)
                    continue;

                bool isTruncation =
                    (oldType.IsMax && !newType.IsMax && newType.Size.HasValue) ||
                    (!oldType.IsMax && !newType.IsMax && oldType.Size.HasValue && newType.Size.HasValue && newType.Size < oldType.Size);

                if (!isTruncation)
                    continue;

                var columnName = AnalyzerHelpers.ExtractNameArgument(invocation);
                var tableName  = AnalyzerHelpers.ExtractArgument("table", invocation);
                var line       = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

                var oldSizeStr = oldType.IsMax ? "unlimited (max)" : oldType.Size!.Value.ToString();
                var newSizeStr = newType.Size!.Value.ToString();

                var colPart = columnName is not null ? $"Column '{columnName}'" : "Column";
                var message = $"{colPart} size reduced from {oldSizeStr} to {newSizeStr}. Data longer than {newSizeStr} characters will be silently truncated.";

                var recommendation = columnName is not null && tableName is not null
                    ? $"Before reducing column size, verify no existing rows exceed the new length. Run: SELECT MAX(LEN({columnName})) FROM {tableName}. If existing data exceeds the new size, either widen the new size or implement a data-cleanup migration first."
                    : "Before reducing column size, verify no existing rows exceed the new length. Run: SELECT MAX(LEN([column])) FROM [table]. If existing data exceeds the new size, either widen the new size or implement a data-cleanup migration first.";

                issues.Add(new SafetyIssue(
                    AnalyzerName: Name,
                    Severity: Severity.Warning,
                    FilePath: filePath,
                    LineNumber: line,
                    Message: message,
                    Recommendation: recommendation));
            }
        }

        return issues;
    }

    private static SizedTypeInfo? ParseSizedType(string typeString)
    {
        var match = Regex.Match(typeString.Trim(),
            @"^(nvarchar|varchar|nchar|char)\((\d+|max)\)$",
            RegexOptions.IgnoreCase);

        if (!match.Success)
            return null;

        var typeName = match.Groups[1].Value.ToLowerInvariant();
        var sizeStr  = match.Groups[2].Value;

        if (sizeStr.Equals("max", StringComparison.OrdinalIgnoreCase))
            return new SizedTypeInfo(typeName, null, true);

        if (int.TryParse(sizeStr, out var size))
            return new SizedTypeInfo(typeName, size, false);

        return null;
    }
}
