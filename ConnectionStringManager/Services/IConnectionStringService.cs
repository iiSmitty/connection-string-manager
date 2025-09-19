namespace ConnectionStringManager.Services;

using ConnectionStringManager.Models;

public interface IConnectionStringService
{
    Task SwitchConnectionStringAsync(ConnectionStringConfig config);
    Task ListConnectionStringsAsync(string filePath);
}