namespace ConnectionStringManager.Services;

using System.Xml.Linq;
using ConnectionStringManager.Models;
using Microsoft.Extensions.Logging;

public class XmlConfigurationFileHandler : IConfigurationFileHandler
{
    private readonly ILogger<XmlConfigurationFileHandler> _logger;

    public XmlConfigurationFileHandler(ILogger<XmlConfigurationFileHandler> logger)
    {
        _logger = logger;
    }

    public bool CanHandle(string filePath) =>
        Path.GetExtension(filePath).Equals(".config", StringComparison.OrdinalIgnoreCase);

    public async Task<IEnumerable<ConnectionStringInfo>> GetConnectionStringsAsync(string filePath)
    {
        var doc = await Task.Run(() => XDocument.Load(filePath));
        var connectionStrings = doc.Descendants("connectionStrings").Elements("add");

        return connectionStrings.Select(element => new ConnectionStringInfo
        {
            Name = element.Attribute("name")?.Value ?? "",
            ConnectionString = element.Attribute("connectionString")?.Value ?? "",
            ProviderName = element.Attribute("providerName")?.Value,
            Environment = ParseEnvironmentFromName(element.Attribute("name")?.Value ?? "")
        }).ToList();
    }

    public async Task SwitchConnectionStringAsync(ConnectionStringConfig config)
    {
        var doc = await Task.Run(() => XDocument.Load(config.FilePath));
        var connectionStringsElement = doc.Descendants("connectionStrings").FirstOrDefault();

        if (connectionStringsElement == null)
        {
            throw new InvalidOperationException("connectionStrings section not found in XML file");
        }

        var targetKey = GetConnectionStringKey(config.Environment, config.NamePattern);
        const string defaultKey = "DefaultConnection";

        var targetConnection = connectionStringsElement.Elements("add")
            .FirstOrDefault(x => x.Attribute("name")?.Value == targetKey);

        if (targetConnection == null)
        {
            throw new InvalidOperationException($"Connection string '{targetKey}' not found");
        }

        var defaultConnection = connectionStringsElement.Elements("add")
            .FirstOrDefault(x => x.Attribute("name")?.Value == defaultKey);

        var targetConnectionString = targetConnection.Attribute("connectionString")?.Value;
        var targetProviderName = targetConnection.Attribute("providerName")?.Value ?? "System.Data.SqlClient";

        if (string.IsNullOrEmpty(targetConnectionString))
        {
            throw new InvalidOperationException($"Connection string value is empty for '{targetKey}'");
        }

        if (defaultConnection != null)
        {
            defaultConnection.SetAttributeValue("connectionString", targetConnectionString);
            defaultConnection.SetAttributeValue("providerName", targetProviderName);
        }
        else
        {
            var newConnection = new XElement("add",
                new XAttribute("name", defaultKey),
                new XAttribute("connectionString", targetConnectionString),
                new XAttribute("providerName", targetProviderName));
            connectionStringsElement.Add(newConnection);
        }

        await Task.Run(() => doc.Save(config.FilePath));
        _logger.LogDebug("XML configuration updated successfully");
    }

    private static string GetConnectionStringKey(EnvironmentType environment, string namePattern) =>
        namePattern.Replace("{Environment}", environment.ToString(), StringComparison.OrdinalIgnoreCase);

    private static EnvironmentType? ParseEnvironmentFromName(string name)
    {
        var lowerName = name?.ToLowerInvariant() ?? "";
        if (lowerName.Contains("dev")) return EnvironmentType.Development;
        if (lowerName.Contains("qa") || lowerName.Contains("test")) return EnvironmentType.QA;
        if (lowerName.Contains("prod")) return EnvironmentType.Production;
        return null;
    }
}