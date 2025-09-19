namespace ConnectionStringManager.Services;

using ConnectionStringManager.Models;
using Microsoft.Extensions.Logging;

public class ConnectionStringService : IConnectionStringService
{
    private readonly IEnumerable<IConfigurationFileHandler> _fileHandlers;
    private readonly IFileBackupService _backupService;
    private readonly ILogger<ConnectionStringService> _logger;

    public ConnectionStringService(
        IEnumerable<IConfigurationFileHandler> fileHandlers,
        IFileBackupService backupService,
        ILogger<ConnectionStringService> logger)
    {
        _fileHandlers = fileHandlers;
        _backupService = backupService;
        _logger = logger;
    }

    public async Task SwitchConnectionStringAsync(ConnectionStringConfig config)
    {
        if (!File.Exists(config.FilePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {config.FilePath}");
        }

        var handler = GetFileHandler(config.FilePath);

        if (config.CreateBackup)
        {
            await _backupService.CreateBackupAsync(config.FilePath);
        }

        await handler.SwitchConnectionStringAsync(config);

        _logger.LogInformation("Connection string switched to {Environment}", config.Environment);
    }

    public async Task ListConnectionStringsAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {filePath}");
        }

        var handler = GetFileHandler(filePath);
        var connectionStrings = await handler.GetConnectionStringsAsync(filePath);

        Console.WriteLine("Available connection strings:");
        Console.WriteLine(new string('-', 50));

        foreach (var cs in connectionStrings)
        {
            var environmentDisplay = cs.Environment?.ToString() ?? "Unknown";
            var maskedConnectionString = MaskSensitiveData(cs.ConnectionString);
            Console.WriteLine($"{cs.Name,-25} [{environmentDisplay}]");
            Console.WriteLine($"  {maskedConnectionString}");
            Console.WriteLine();
        }
    }

    private IConfigurationFileHandler GetFileHandler(string filePath)
    {
        var handler = _fileHandlers.FirstOrDefault(h => h.CanHandle(filePath));
        return handler ?? throw new NotSupportedException($"Unsupported configuration file type: {Path.GetExtension(filePath)}");
    }

    private static string MaskSensitiveData(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return string.Empty;

        var sensitiveKeys = new[] { "password", "pwd", "user id", "uid" };
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);

        var maskedParts = parts.Select(part =>
        {
            var keyValue = part.Split('=', 2);
            if (keyValue.Length == 2 &&
                sensitiveKeys.Any(key => keyValue[0].Trim().ToLowerInvariant().Contains(key)))
            {
                return $"{keyValue[0]}=****";
            }
            return part;
        });

        return string.Join(";", maskedParts);
    }
}