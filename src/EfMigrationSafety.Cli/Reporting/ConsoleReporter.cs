using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;
using EfMigrationSafety.Cli.Models;

namespace EfMigrationSafety.Cli.Reporting;

public class ConsoleReporter
{
    public void Report(IReadOnlyList<SafetyIssue> issues, int totalFilesScanned, string outputFormat)
    {
        if (outputFormat.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            ReportJson(issues, totalFilesScanned);
            return;
        }

        ReportText(issues, totalFilesScanned);
    }

    private static void ReportText(IReadOnlyList<SafetyIssue> issues, int totalFilesScanned)
    {
        if (issues.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]✓[/] No safety issues found across [bold]{0}[/] migration file(s).", totalFilesScanned);
            return;
        }

        var grouped = issues.GroupBy(i => i.FilePath);

        foreach (var group in grouped)
        {
            AnsiConsole.MarkupLine("[bold]{0}[/]", Markup.Escape(group.Key));

            foreach (var issue in group)
            {
                var (icon, color) = issue.Severity switch
                {
                    Severity.Error   => ("✗", "red"),
                    Severity.Warning => ("⚠", "yellow"),
                    _                => ("i", "blue"),
                };

                AnsiConsole.MarkupLine(
                    "  [{0}]{1}[/] Line {2}: {3}",
                    color, icon, issue.LineNumber, Markup.Escape(issue.Message));

                if (issue.Recommendation is not null)
                {
                    AnsiConsole.MarkupLine(
                        "    [dim]Recommendation: {0}[/]",
                        Markup.Escape(issue.Recommendation));
                }
            }

            AnsiConsole.WriteLine();
        }

        var filesWithIssues = issues.Select(i => i.FilePath).Distinct().Count();
        AnsiConsole.MarkupLine(
            "[yellow]Summary:[/] {0} of {1} migration(s) have safety warnings.",
            filesWithIssues, totalFilesScanned);
    }

    private static void ReportJson(IReadOnlyList<SafetyIssue> issues, int totalFilesScanned)
    {
        var filesWithIssues = issues.Select(i => i.FilePath).Distinct().Count();

        var payload = new
        {
            files = issues
                .GroupBy(i => i.FilePath)
                .Select(g => new
                {
                    path = g.Key,
                    issues = g.Select(i => new
                    {
                        analyzerName = i.AnalyzerName,
                        severity = i.Severity.ToString(),
                        lineNumber = i.LineNumber,
                        message = i.Message,
                        recommendation = i.Recommendation,
                    })
                }),
            summary = new
            {
                totalFiles = totalFilesScanned,
                filesWithIssues,
                totalIssues = issues.Count,
            }
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        Console.WriteLine(JsonSerializer.Serialize(payload, options));
    }
}
