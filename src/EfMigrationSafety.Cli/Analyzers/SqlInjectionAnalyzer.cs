using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using EfMigrationSafety.Cli.Models;

namespace EfMigrationSafety.Cli.Analyzers;

public class SqlInjectionAnalyzer : IMigrationAnalyzer
{
    public string Name => "SqlInjection";
    public string Description => "Detects potential SQL injection patterns in migrationBuilder.Sql() calls — interpolated strings, string concatenation with variables, and string.Format usage.";

    public IEnumerable<SafetyIssue> Analyze(SyntaxNode root, string filePath)
    {
        var issues = new List<SafetyIssue>();

        var migrationMethods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(m => m.Identifier.Text is "Up" or "Down");

        foreach (var method in migrationMethods)
        {
            var sqlCalls = method.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(IsMigrationBuilderSqlCall);

            foreach (var sqlCall in sqlCalls)
            {
                if (sqlCall.ArgumentList.Arguments.Count == 0)
                    continue;

                var firstArg = sqlCall.ArgumentList.Arguments[0].Expression;
                var issue = CheckForUnsafePattern(firstArg, sqlCall, filePath);
                if (issue is not null)
                    issues.Add(issue);
            }
        }

        return issues;
    }

    private static bool IsMigrationBuilderSqlCall(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax member)
            return false;

        if (member.Name.Identifier.Text != "Sql")
            return false;

        return member.Expression is IdentifierNameSyntax ident &&
               ident.Identifier.Text == "migrationBuilder";
    }

    private SafetyIssue? CheckForUnsafePattern(ExpressionSyntax arg, InvocationExpressionSyntax sqlCall, string filePath)
    {
        var line = sqlCall.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

        if (arg is InterpolatedStringExpressionSyntax interpolated)
        {
            var interpolations = interpolated.Contents.OfType<InterpolationSyntax>().ToList();
            if (!interpolations.Any())
                return null;

            var varNames = interpolations
                .Select(i => i.Expression)
                .OfType<IdentifierNameSyntax>()
                .Select(id => id.Identifier.Text)
                .ToList();

            var pattern = varNames.Any()
                ? $"interpolated string with variable(s): {string.Join(", ", varNames)}"
                : "interpolated string with expression(s)";

            return MakeIssue(pattern, line, filePath);
        }

        if (arg is BinaryExpressionSyntax binary && binary.OperatorToken.IsKind(SyntaxKind.PlusToken))
        {
            if (!HasNonLiteralOperand(binary))
                return null;

            return MakeIssue("String concatenation with non-literal in SQL", line, filePath);
        }

        if (arg is InvocationExpressionSyntax call &&
            call.Expression is MemberAccessExpressionSyntax methodAccess &&
            methodAccess.Name.Identifier.Text == "Format" &&
            IsStringType(methodAccess.Expression))
        {
            return MakeIssue("string.Format used to build SQL", line, filePath);
        }

        return null;
    }

    private SafetyIssue MakeIssue(string pattern, int line, string filePath) =>
        new(
            AnalyzerName: Name,
            Severity: Severity.Error,
            FilePath: filePath,
            LineNumber: line,
            Message: $"Potential SQL injection: {pattern} in migrationBuilder.Sql() call.",
            Recommendation: "Use parameterized SQL via ExecuteSqlRaw with parameters, or use static SQL literals only.");

    private static bool IsStringType(ExpressionSyntax expr) =>
        (expr is PredefinedTypeSyntax predefined && predefined.Keyword.IsKind(SyntaxKind.StringKeyword)) ||
        (expr is IdentifierNameSyntax ident && ident.Identifier.Text == "String");

    private static bool HasNonLiteralOperand(ExpressionSyntax expr)
    {
        if (expr is LiteralExpressionSyntax)
            return false;

        if (expr is BinaryExpressionSyntax binary && binary.OperatorToken.IsKind(SyntaxKind.PlusToken))
            return HasNonLiteralOperand(binary.Left) || HasNonLiteralOperand(binary.Right);

        return true;
    }
}
