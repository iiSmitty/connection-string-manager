namespace ConnectionStringManager.Services;

using ConnectionStringManager.Models;

public interface IConfigurationFileHandler
{
    bool CanHandle(string filePath);
    Task<IEnumerable<ConnectionStringInfo>> GetConnectionStringsAsync(string filePath);
    Task SwitchConnectionStringAsync(ConnectionStringConfig config);
}