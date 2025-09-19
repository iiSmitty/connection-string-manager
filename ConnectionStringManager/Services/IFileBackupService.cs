namespace ConnectionStringManager.Services;

public interface IFileBackupService
{
    Task CreateBackupAsync(string filePath);
}