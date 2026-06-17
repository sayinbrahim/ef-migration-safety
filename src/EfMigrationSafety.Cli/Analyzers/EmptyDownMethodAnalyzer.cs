using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using EfMigrationSafety.Cli.Models;

namespace EfMigrationSafety.Cli.Analyzers;

public class EmptyDownMethodAnalyzer : IMigrationAnalyzer
{
    public string Name => "EmptyDownMethod";
    public string Description => "Detects migrations whose Down() method is empty or throws NotImplementedException, making rollbacks impossible.";

    private enum DownStatus { Empty, ThrowsNotImplemented, Implemented }

    public IEnumerable<SafetyIssue> Analyze(SyntaxNode root, string filePath)
    {
        var issues = new List<SafetyIssue>();

        var downMethods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(m => m.Identifier.Text == "Down");

        foreach (var method in downMethods)
        {
            var status = Classify(method);
            if (status == DownStatus.Implemented)
                continue;

            var line = method.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

            var message = status == DownStatus.Empty
                ? "Down() method is empty. This migration cannot be rolled back. If rollback is intentionally unsupported, throw an explicit exception documenting why."
                : "Down() method throws NotImplementedException. This blocks rollback. Either implement the inverse migration, or document the reason for the unsupported rollback in the exception message.";

            issues.Add(new SafetyIssue(
                AnalyzerName: Name,
                Severity: Severity.Warning,
                FilePath: filePath,
                LineNumber: line,
                Message: message,
                Recommendation: "Implement the inverse of the Up() operations. For example, if Up() calls AddColumn, Down() should call DropColumn for the same column. This is essential for production incident response."));
        }

        return issues;
    }

    private static DownStatus Classify(MethodDeclarationSyntax method)
    {
        // Expression-bodied: => throw new NotImplementedException();
        if (method.ExpressionBody is { } exprBody)
        {
            return exprBody.Expression is ThrowExpressionSyntax throwExpr &&
                   IsNotImplementedException(throwExpr.Expression)
                ? DownStatus.ThrowsNotImplemented
                : DownStatus.Implemented;
        }

        // No body at all (abstract or interface — unusual here, treat as empty)
        if (method.Body is null)
            return DownStatus.Empty;

        var statements = method.Body.Statements;

        if (statements.Count == 0)
            return DownStatus.Empty;

        if (statements.Count == 1 &&
            statements[0] is ThrowStatementSyntax throwStmt &&
            IsNotImplementedException(throwStmt.Expression))
            return DownStatus.ThrowsNotImplemented;

        return DownStatus.Implemented;
    }

    private static bool IsNotImplementedException(ExpressionSyntax? expr)
    {
        if (expr is not ObjectCreationExpressionSyntax objCreation)
            return false;

        var typeName = objCreation.Type.ToString();
        return typeName is "NotImplementedException" or "System.NotImplementedException";
    }
}
