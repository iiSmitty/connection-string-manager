# Connection String Manager

A .NET Global Tool for easily managing connection strings across different environments in C# applications.

## Features

- ?? **Easy Environment Switching** - Switch between Development, QA, and Production with one command
- ??? **Automatic Backups** - Creates timestamped backups before making changes
- ?? **Multiple File Formats** - Supports both `appsettings.json` and `web.config`/`app.config`
- ?? **Security Conscious** - Masks sensitive information when displaying connection strings
- ? **Global Tool** - Install once, use anywhere
- ?? **Simple Commands** - Intuitive command-line interface

## Quick Start Demo

Want to see it in action with real databases? Create our **ConnectionStringTester demo app** - a complete example showing the tool working with real SQLite databases. The demo visually shows your connection strings switching between Development, QA, and Production environments. See the demo documentation for setup instructions.

## Installation

Install as a .NET Global Tool:

```bash
dotnet tool install -g ConnectionStringManager
```

Or install from local source:

```bash
dotnet tool install -g ConnectionStringManager --add-source ./path/to/nupkg
```

## Usage

### Basic Commands

```bash
# Switch to different environments
connstr -f appsettings.json -e Development
connstr -f appsettings.json -e QA
connstr -f appsettings.json -e Production

# List all connection strings
connstr -f appsettings.json --list

# Switch without creating backup
connstr -f appsettings.json -e Production --no-backup
```

### Command Options

- `-f, --file <path>` - Path to configuration file (required)
- `-e, --environment <env>` - Target environment (Development, QA, Production)
- `-l, --list` - List all available connection strings
- `-n, --name <pattern>` - Custom naming pattern (default: `{Environment}Connection`)
- `--no-backup` - Skip creating backup file
- `-h, --help` - Show help information

## Configuration File Setup

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyApp;Trusted_Connection=true;",
    "DevelopmentConnection": "Server=dev-server;Database=MyApp_Dev;Trusted_Connection=true;",
    "QAConnection": "Server=qa-server;Database=MyApp_QA;User Id=qa_user;Password=qa_pass;",
    "ProductionConnection": "Server=prod-server;Database=MyApp_Prod;User Id=prod_user;Password=secure_pass;"
  }
}
```

### web.config / app.config

```xml
<configuration>
  <connectionStrings>
    <add name="DefaultConnection" 
         connectionString="Server=localhost;Database=MyApp;Trusted_Connection=true;" 
         providerName="System.Data.SqlClient" />
    <add name="DevelopmentConnection" 
         connectionString="Server=dev-server;Database=MyApp_Dev;Trusted_Connection=true;" 
         providerName="System.Data.SqlClient" />
    <add name="QAConnection" 
         connectionString="Server=qa-server;Database=MyApp_QA;User Id=qa_user;Password=qa_pass;" 
         providerName="System.Data.SqlClient" />
    <add name="ProductionConnection" 
         connectionString="Server=prod-server;Database=MyApp_Prod;User Id=prod_user;Password=secure_pass;" 
         providerName="System.Data.SqlClient" />
  </connectionStrings>
</configuration>
```

## How It Works

The tool updates your `DefaultConnection` to match the selected environment's connection string:

1. **Before**: `DefaultConnection` points to development database
2. **Run**: `connstr -f appsettings.json -e Production`  
3. **After**: `DefaultConnection` now points to production database
4. **Backup**: Original file saved as `appsettings.json.backup.20241218_143022`

Your application code remains unchanged - it continues to use `DefaultConnection`.

## Integration Examples

### Build Scripts

**PowerShell**:
```powershell
param([string]$Environment = "Development")
connstr -f "src/MyApp/appsettings.json" -e $Environment
dotnet build
dotnet run
```

**Batch File**:
```batch
@echo off
set ENV=%1
if "%ENV%"=="" set ENV=Development
connstr -f appsettings.json -e %ENV%
dotnet run
```

### CI/CD Pipeline

```yaml
# Azure DevOps / GitHub Actions example
- name: Switch to Production Environment
  run: connstr -f src/MyApp/appsettings.json -e Production

- name: Build Application  
  run: dotnet build -c Release
```

## Troubleshooting

### Tool Not Found
Ensure .NET tools are in your PATH:
```bash
# Windows
set PATH=%PATH%;%USERPROFILE%\.dotnet\tools

# Linux/Mac  
export PATH="$PATH:$HOME/.dotnet/tools"
```

### Connection String Not Found
Verify your connection string naming:
- Default pattern: `{Environment}Connection` 
- Examples: `DevelopmentConnection`, `QAConnection`, `ProductionConnection`
- Use custom pattern: `connstr -f config.json -e Development -n "{Environment}DB"`

### File Access Errors
- Close the config file in your IDE
- Check file permissions
- Verify file path is correct

## Requirements

- .NET 8.0 or later
- Windows, macOS, or Linux

## Updating

```bash
# Update to latest version
dotnet tool update -g ConnectionStringManager

# Uninstall if needed
dotnet tool uninstall -g ConnectionStringManager
```

## License

MIT License - feel free to use in your projects.

## Contributing

Found a bug or want to add a feature? Issues and pull requests welcome!

---

**Made with ?? for C# developers who are tired of manually switching connection strings.**