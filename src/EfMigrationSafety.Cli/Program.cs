using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using EfMigrationSafety.Cli.Analyzers;
using EfMigrationSafety.Cli.Models;
using EfMigrationSafety.Cli.Reporting;

var directoryArgument = new Argument<DirectoryInfo>(
    name: "directory",
    description: "Directory to scan for EF Core migration files");

var strictOption = new Option<bool>(
    name: "--strict",
    description: "Exit with code 1 if any issues are found");

var outputOption = new Option<string>(
    name: "--output",
    getDefaultValue: () => "text",
    description: "Output format: text or json");

var checkCommand = new Command("check", "Analyze migration files for safety issues")
{
    directoryArgument,
    strictOption,
    outputOption,
};

checkCommand.SetHandler(async (InvocationContext context) =>
{
    var directory = context.ParseResult.GetValueForArgument(directoryArgument);
    var strict    = context.ParseResult.GetValueForOption(strictOption);
    var output    = context.ParseResult.GetValueForOption(outputOption) ?? "text";

    if (!directory.Exists)
    {
        Console.Error.WriteLine($"Directory not found: {directory.FullName}");
        context.ExitCode = 1;
        return;
    }

    var migrationPattern = new Regex(@"^\d{14}_.*\.cs$", RegexOptions.Compiled);

    var migrationFiles = directory
        .GetFiles("*.cs", SearchOption.AllDirectories)
        .Where(f => migrationPattern.IsMatch(f.Name) && !f.Name.EndsWith(".Designer.cs"))
        .ToList();

    IMigrationAnalyzer[] analyzers =
    [
        new DropAddColumnAnalyzer(),
        new NonNullableWithoutDefaultAnalyzer(),
        new EmptyDownMethodAnalyzer(),
        new RenameOperationAnalyzer(),
    ];

    var allIssues = new List<SafetyIssue>();

    foreach (var file in migrationFiles)
    {
        var source = await File.ReadAllTextAsync(file.FullName);
        var root   = CSharpSyntaxTree.ParseText(source).GetRoot();

        foreach (var analyzer in analyzers)
            allIssues.AddRange(analyzer.Analyze(root, file.FullName));
    }

    new ConsoleReporter().Report(allIssues, migrationFiles.Count, output);

    if (strict && allIssues.Count > 0)
        context.ExitCode = 1;
});

var rootCommand = new RootCommand("Static safety analyzer for EF Core migrations")
{
    checkCommand,
};

return await rootCommand.InvokeAsync(args);
