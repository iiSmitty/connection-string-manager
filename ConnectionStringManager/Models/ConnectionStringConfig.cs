namespace ConnectionStringManager.Models;

public class ConnectionStringConfig
{
    public required string FilePath { get; init; }
    public EnvironmentType Environment { get; init; }
    public string NamePattern { get; init; } = "{Environment}Connection";
    public string? TargetConnectionName { get; init; }
    public string Mode { get; init; } = "default";
    public bool CreateBackup { get; init; } = true;
}