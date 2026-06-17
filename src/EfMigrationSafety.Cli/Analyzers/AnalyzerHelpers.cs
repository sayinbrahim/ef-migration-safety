using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EfMigrationSafety.Cli.Analyzers;

internal static class AnalyzerHelpers
{
    internal static string? GetMethodName(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch
        {
            MemberAccessExpressionSyntax member => member.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };

    internal static string? ExtractNameArgument(InvocationExpressionSyntax invocation)
    {
        var args = invocation.ArgumentList.Arguments;

        var namedArg = args.FirstOrDefault(a =>
            a.NameColon?.Name.Identifier.Text == "name");

        if (namedArg?.Expression is LiteralExpressionSyntax namedLiteral)
            return namedLiteral.Token.ValueText;

        // Positional: first argument
        if (args.Count > 0 && args[0].Expression is LiteralExpressionSyntax positionalLiteral)
            return positionalLiteral.Token.ValueText;

        return null;
    }

    internal static string? ExtractArgument(string parameterName, InvocationExpressionSyntax invocation)
    {
        var arg = invocation.ArgumentList.Arguments.FirstOrDefault(a =>
            a.NameColon?.Name.Identifier.Text == parameterName);

        if (arg?.Expression is LiteralExpressionSyntax lit)
            return lit.Token.ValueText;

        return null;
    }

    internal static bool IsInsideCreateTable(SyntaxNode node)
    {
        var current = node.Parent;
        while (current is not null)
        {
            if (current is InvocationExpressionSyntax invocation &&
                GetMethodName(invocation) == "CreateTable")
                return true;
            current = current.Parent;
        }
        return false;
    }
}
