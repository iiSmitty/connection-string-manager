namespace ConnectionStringManager.Services;

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ConnectionStringManager.Models;
using Microsoft.Extensions.Logging;

public class JsonConfigurationFileHandler : IConfigurationFileHandler
{
    private readonly ILogger<JsonConfigurationFileHandler> _logger;

    public JsonConfigurationFileHandler(ILogger<JsonConfigurationFileHandler> logger)
    {
        _logger = logger;
    }

    public bool CanHandle(string filePath) =>
        Path.GetExtension(filePath).Equals(".json", StringComparison.OrdinalIgnoreCase);

    public async Task<IEnumerable<ConnectionStringInfo>> GetConnectionStringsAsync(string filePath)
    {
        var content = await File.ReadAllTextAsync(filePath);
        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var result = new List<ConnectionStringInfo>();

        string currentEnvironment = "Unknown";

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmedLine = line.Trim();

            // Check if this line contains an environment comment
            var envFromComment = ExtractEnvironmentFromComment(trimmedLine);
            if (!string.IsNullOrEmpty(envFromComment))
            {
                currentEnvironment = envFromComment;
                continue;
            }

            // Check if this is a connection string line
            if (IsConnectionStringLine(trimmedLine))
            {
                var isCommented = trimmedLine.StartsWith("//");
                var actualLine = isCommented ? trimmedLine.Substring(2).Trim() : trimmedLine;

                if (TryParseConnectionStringLine(actualLine, out var name, out var connectionString))
                {
                    var displayName = isCommented ? $"{name} (commented)" : name;
                    var environment = ParseEnvironmentType(currentEnvironment);

                    result.Add(new ConnectionStringInfo
                    {
                        Name = displayName,
                        ConnectionString = connectionString,
                        Environment = environment
                    });
                }
            }
        }

        return result;
    }

    public async Task SwitchConnectionStringAsync(ConnectionStringConfig config)
    {
        var content = await File.ReadAllTextAsync(config.FilePath);

        if (config.Mode.Equals("comment", StringComparison.OrdinalIgnoreCase))
        {
            await SwitchUsingCommentModeAsync(config, content);
        }
        else
        {
            await SwitchUsingDefaultModeAsync(config, content);
        }

        _logger.LogDebug("JSON configuration updated successfully");
    }

    private async Task SwitchUsingDefaultModeAsync(ConnectionStringConfig config, string content)
    {
        var jsonOptions = new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        };

        var jsonNode = JsonNode.Parse(content, new JsonNodeOptions(), jsonOptions)
            ?? throw new InvalidOperationException("Invalid JSON file");

        if (jsonNode["ConnectionStrings"] is not JsonObject connectionStrings)
        {
            throw new InvalidOperationException("ConnectionStrings section not found in JSON file");
        }

        var targetKey = GetConnectionStringKey(config.Environment, config.NamePattern);
        var defaultKey = config.TargetConnectionName ?? "DefaultConnection";

        if (!connectionStrings.ContainsKey(targetKey))
        {
            throw new InvalidOperationException($"Connection string '{targetKey}' not found");
        }

        // Update the target connection (DefaultConnection or specified target)
        connectionStrings[defaultKey] = connectionStrings[targetKey]?.DeepClone();

        var options = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllTextAsync(config.FilePath, jsonNode.ToJsonString(options));
    }

    private async Task SwitchUsingCommentModeAsync(ConnectionStringConfig config, string originalContent)
    {
        var targetEnvironment = config.Environment.ToString();
        var lines = originalContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var result = new List<string>();

        string currentEnvironment = "";
        bool inTargetEnvironmentBlock = false;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmedLine = line.Trim();

            // Check for environment comments
            var envFromComment = ExtractEnvironmentFromComment(trimmedLine);
            if (!string.IsNullOrEmpty(envFromComment))
            {
                currentEnvironment = envFromComment;
                inTargetEnvironmentBlock = EnvironmentMatches(currentEnvironment, targetEnvironment);
                result.Add(line);
                _logger.LogDebug("Found environment block: {Environment}, Target: {IsTarget}", currentEnvironment, inTargetEnvironmentBlock);
                continue;
            }

            // Handle connection string lines
            if (IsConnectionStringLine(trimmedLine))
            {
                if (inTargetEnvironmentBlock)
                {
                    // Uncomment target environment lines
                    if (trimmedLine.StartsWith("//"))
                    {
                        var cleanedLine = line;
                        if (cleanedLine.Contains("//"))
                        {
                            var index = cleanedLine.IndexOf("//");
                            var beforeComment = cleanedLine.Substring(0, index);
                            var afterComment = cleanedLine.Substring(index + 2);
                            cleanedLine = beforeComment + afterComment;
                        }

                        result.Add(cleanedLine);
                        _logger.LogDebug("Uncommented line for {Environment}: {Line}", targetEnvironment, trimmedLine);
                    }
                    else
                    {
                        result.Add(line); // Already uncommented
                    }
                }
                else if (!string.IsNullOrEmpty(currentEnvironment))
                {
                    // Comment out other environment lines
                    if (!trimmedLine.StartsWith("//"))
                    {
                        var indent = GetIndentation(line);
                        var commented = indent + "//" + line.TrimStart();
                        result.Add(commented);
                        _logger.LogDebug("Commented line for {Environment}: {Line}", currentEnvironment, trimmedLine);
                    }
                    else
                    {
                        result.Add(line); // Already commented
                    }
                }
                else
                {
                    result.Add(line); // No environment context
                }
            }
            else
            {
                result.Add(line); // Non-connection string line
            }
        }

        await File.WriteAllTextAsync(config.FilePath, string.Join(Environment.NewLine, result));
    }

    private static string GetIndentation(string line)
    {
        var indent = "";
        foreach (char c in line)
        {
            if (c == ' ' || c == '\t')
                indent += c;
            else
                break;
        }
        return indent;
    }

    private async Task AddCommentedConnectionStrings(string content, List<ConnectionStringInfo> result)
    {
        // This method is no longer needed since we handle everything in GetConnectionStringsAsync
        await Task.CompletedTask;
    }

    private static string ExtractEnvironmentFromComment(string line)
    {
        if (!line.StartsWith("//")) return "";

        var comment = line.Substring(2).Trim().ToLowerInvariant();

        // Only match environment comments that don't contain connection string patterns
        // Skip lines that look like connection strings (contain quotes and colons)
        if (comment.Contains("\"") && comment.Contains(":"))
        {
            return "";
        }

        // Match patterns like "// Azure Dev", "//Azure QA SA", "// Azure Prod SA"
        if (comment.Contains("azure") && comment.Contains("dev")) return "Azure Dev";
        if (comment.Contains("azure") && comment.Contains("qa")) return "Azure QA";
        if (comment.Contains("azure") && comment.Contains("prod")) return "Azure Prod";

        // Be more specific with fallback patterns - require "environment" context
        if (comment.Contains("dev") && !comment.Contains("connection")) return "Development";
        if ((comment.Contains("qa") || comment.Contains("test")) && !comment.Contains("connection")) return "QA";
        if (comment.Contains("prod") && !comment.Contains("connection")) return "Production";

        return "";
    }

    private static bool EnvironmentMatches(string commentEnvironment, string targetEnvironment)
    {
        var comment = commentEnvironment.ToLowerInvariant();
        var target = targetEnvironment.ToLowerInvariant();

        // Direct matches
        if (comment.Contains(target)) return true;

        // Handle specific mappings
        return target switch
        {
            "development" => comment.Contains("dev"),
            "qa" => comment.Contains("qa") || comment.Contains("test"),
            "production" => comment.Contains("prod"),
            _ => false
        };
    }

    private static EnvironmentType? ParseEnvironmentType(string environment)
    {
        var env = environment.ToLowerInvariant();
        if (env.Contains("dev")) return EnvironmentType.Development;
        if (env.Contains("qa") || env.Contains("test")) return EnvironmentType.QA;
        if (env.Contains("prod")) return EnvironmentType.Production;
        return null;
    }

    private static bool IsConnectionStringLine(string line)
    {
        // Remove any comment prefix and whitespace
        var actualLine = line.Trim();
        if (actualLine.StartsWith("//"))
        {
            actualLine = actualLine.Substring(2).Trim();
        }

        // Check for connection string patterns - be more flexible
        return actualLine.Contains("SysSetupConnection") ||
               actualLine.Contains("AvbobPoetryConnection") ||
               (actualLine.Contains("Connection") && actualLine.Contains(":"));
    }

    private static bool TryParseConnectionStringLine(string line, out string name, out string connectionString)
    {
        name = "";
        connectionString = "";

        // Clean the line first - remove comments and extra whitespace
        var cleanLine = line.Trim();
        if (cleanLine.StartsWith("//"))
        {
            cleanLine = cleanLine.Substring(2).Trim();
        }

        // Try specific connection names first (most reliable)
        if (cleanLine.Contains("SysSetupConnection"))
        {
            name = "SysSetupConnection";
            // Try to extract the actual connection string value
            var match = Regex.Match(cleanLine, @"""SysSetupConnection""\s*:\s*""([^""]+)""");
            connectionString = match.Success ? match.Groups[1].Value : "***";
            return true;
        }

        if (cleanLine.Contains("AvbobPoetryConnection"))
        {
            name = "AvbobPoetryConnection";
            // Try to extract the actual connection string value  
            var match = Regex.Match(cleanLine, @"""AvbobPoetryConnection""\s*:\s*""([^""]+)""");
            connectionString = match.Success ? match.Groups[1].Value : "***";
            return true;
        }

        // Fallback: try the general pattern
        var fullMatch = Regex.Match(cleanLine, @"""(\w*Connection)""\s*:\s*""([^""]+)""");
        if (fullMatch.Success)
        {
            name = fullMatch.Groups[1].Value;
            connectionString = fullMatch.Groups[2].Value;
            return true;
        }

        // Final fallback: just connection name
        var nameMatch = Regex.Match(cleanLine, @"""(\w*Connection)""\s*:");
        if (nameMatch.Success)
        {
            name = nameMatch.Groups[1].Value;
            connectionString = "***";
            return true;
        }

        return false;
    }

    private static string GetConnectionStringKey(EnvironmentType environment, string namePattern) =>
        namePattern.Replace("{Environment}", environment.ToString(), StringComparison.OrdinalIgnoreCase);

    private static EnvironmentType? ParseEnvironmentFromName(string name)
    {
        // Keep this for backward compatibility with traditional naming
        var lowerName = name.ToLowerInvariant();
        if (lowerName.Contains("dev")) return EnvironmentType.Development;
        if (lowerName.Contains("qa") || lowerName.Contains("test")) return EnvironmentType.QA;
        if (lowerName.Contains("prod")) return EnvironmentType.Production;
        return null;
    }
}