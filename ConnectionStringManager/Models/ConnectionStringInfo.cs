namespace ConnectionStringManager.Models;

public class ConnectionStringInfo
{
    public required string Name { get; init; }
    public required string ConnectionString { get; init; }
    public string? ProviderName { get; init; }
    public EnvironmentType? Environment { get; init; }
}