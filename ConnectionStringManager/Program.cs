using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ConnectionStringManager.Services;
using ConnectionStringManager.Models;

namespace ConnectionStringManager;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var host = CreateHostBuilder().Build();

        var rootCommand = new RootCommand("A tool for managing connection strings across different environments");

        var fileOption = new Option<string>(
            aliases: new[] { "--file", "-f" },
            description: "Path to the configuration file")
        {
            IsRequired = true
        };

        var environmentOption = new Option<EnvironmentType?>(
            aliases: new[] { "--environment", "-e" },
            description: "Target environment (Development, QA, Production)")
        {
            IsRequired = false
        };

        var listOption = new Option<bool>(
            aliases: new[] { "--list", "-l" },
            description: "List all available connection strings");

        var nameOption = new Option<string>(
            aliases: new[] { "--name", "-n" },
            description: "Custom connection string name pattern (default: {Environment}Connection)",
            getDefaultValue: () => "{Environment}Connection");

        var targetOption = new Option<string>(
            aliases: new[] { "--target", "-t" },
            description: "Specific connection string to update (when no DefaultConnection exists)");

        var modeOption = new Option<string>(
            aliases: new[] { "--mode", "-m" },
            description: "Update mode: 'default' (update DefaultConnection) or 'comment' (comment/uncomment pattern)",
            getDefaultValue: () => "default");

        var backupOption = new Option<bool>(
            aliases: new[] { "--no-backup" },
            description: "Skip creating backup file");

        rootCommand.AddOption(fileOption);
        rootCommand.AddOption(environmentOption);
        rootCommand.AddOption(listOption);
        rootCommand.AddOption(nameOption);
        rootCommand.AddOption(targetOption);
        rootCommand.AddOption(modeOption);
        rootCommand.AddOption(backupOption);

        rootCommand.SetHandler(async (context) =>
        {
            var filePath = context.ParseResult.GetValueForOption(fileOption);
            var environment = context.ParseResult.GetValueForOption(environmentOption);
            var listOnly = context.ParseResult.GetValueForOption(listOption);
            var namePattern = context.ParseResult.GetValueForOption(nameOption);
            var targetConnection = context.ParseResult.GetValueForOption(targetOption);
            var mode = context.ParseResult.GetValueForOption(modeOption);
            var noBackup = context.ParseResult.GetValueForOption(backupOption);

            var connectionStringService = host.Services.GetRequiredService<IConnectionStringService>();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();

            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    Console.WriteLine("Error: File path is required. Use -f or --file option.");
                    context.ExitCode = 1;
                    return;
                }

                if (listOnly)
                {
                    await connectionStringService.ListConnectionStringsAsync(filePath);
                    return;
                }

                if (environment == null)
                {
                    Console.WriteLine("Error: Environment is required when not listing. Use -e or --environment option.");
                    Console.WriteLine("Valid environments: Development, QA, Production");
                    context.ExitCode = 1;
                    return;
                }

                var config = new ConnectionStringConfig
                {
                    FilePath = filePath,
                    Environment = environment.Value,
                    NamePattern = namePattern ?? "{Environment}Connection",
                    TargetConnectionName = targetConnection,
                    Mode = mode ?? "default",
                    CreateBackup = !noBackup
                };

                await connectionStringService.SwitchConnectionStringAsync(config);
                Console.WriteLine($"Connection string successfully switched to {environment}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while processing the connection string");
                Console.WriteLine($"Error: {ex.Message}");
                context.ExitCode = 1;
            }
        });

        return await rootCommand.InvokeAsync(args);
    }

    private static IHostBuilder CreateHostBuilder() =>
        Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
                services.AddTransient<IConnectionStringService, ConnectionStringService>();
                services.AddTransient<IConfigurationFileHandler, JsonConfigurationFileHandler>();
                services.AddTransient<IConfigurationFileHandler, XmlConfigurationFileHandler>();
                services.AddTransient<IFileBackupService, FileBackupService>();
            });
}