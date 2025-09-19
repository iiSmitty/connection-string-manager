namespace ConnectionStringManager.Services;

using Microsoft.Extensions.Logging;

public class FileBackupService : IFileBackupService
{
    private readonly ILogger<FileBackupService> _logger;

    public FileBackupService(ILogger<FileBackupService> logger)
    {
        _logger = logger;
    }

    public async Task CreateBackupAsync(string filePath)
    {
        var backupPath = $"{filePath}.backup.{DateTime.Now:yyyyMMdd_HHmmss}";

        // Use async file operations for better performance
        var sourceBytes = await File.ReadAllBytesAsync(filePath);
        await File.WriteAllBytesAsync(backupPath, sourceBytes);

        Console.WriteLine($"Backup created: {backupPath}");
        _logger.LogDebug("Backup created at {BackupPath}", backupPath);
    }
}