using OpenGameVerse.Console.Commands;
using OpenGameVerse.Console.DependencyInjection;

// Manual dependency injection for AOT compatibility
var services = new ServiceContainer();
services.RegisterServices();

// Parse command line arguments
var command = args.Length > 0 ? args[0].ToLowerInvariant() : "help";

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    return command switch
    {
        "scan" => await new ScanCommand(services).ExecuteAsync(cts.Token),
        "list" => await new ListCommand(services).ExecuteAsync(cts.Token),
        "help" or "--help" or "-h" => ShowHelp(),
        "version" or "--version" or "-v" => ShowVersion(),
        _ => ShowHelp(),
    };
}
catch (OperationCanceledException)
{
    Console.WriteLine("\nOperation cancelled by user.");
    return 1;
}
catch (Exception ex)
{
    Console.WriteLine($"\nError: {ex.Message}");
    if (args.Contains("--verbose") || args.Contains("-V"))
    {
        Console.WriteLine(ex.StackTrace);
    }
    return 1;
}

static int ShowHelp()
{
    Console.WriteLine("OpenGameVerse - Cross-Platform Game Library Manager");
    Console.WriteLine("Version 0.1.0 (Phase 1 - Core Kernel)");
    Console.WriteLine();
    Console.WriteLine("Usage: OpenGameVerse.Console [command]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  scan      Scan for installed games");
    Console.WriteLine("  list      List all games in library");
    Console.WriteLine("  help      Show this help message");
    Console.WriteLine("  version   Show version information");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  OpenGameVerse.Console scan");
    Console.WriteLine("  OpenGameVerse.Console list");
    Console.WriteLine();
    return 0;
}

static int ShowVersion()
{
    Console.WriteLine("OpenGameVerse v0.1.0-alpha");
    Console.WriteLine("Phase 1: Core Kernel");
    Console.WriteLine($"Platform: {Environment.OSVersion}");
    Console.WriteLine($"Runtime: .NET {Environment.Version}");
    Console.WriteLine("Build: Native AOT");
    return 0;
}
