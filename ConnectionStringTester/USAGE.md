# Connection String Manager - Demo Usage Guide

This console application demonstrates the **Connection String Manager** tool in action with real database connections.

## Prerequisites

1. **Install the Connection String Manager globally:**
   ```bash
   dotnet tool install -g ConnectionStringManager --add-source /path/to/nupkg
   ```

2. **Build this demo app:**
   ```bash
   dotnet build
   ```

## Quick Start Demo

### Manual Testing (Step by Step)

```powershell
# 1. Start in Development (default)
dotnet run
# Expected: Shows "?? Development - Connected to: development.db"

# 2. Switch to QA environment
connstr -f appsettings.json -e QA
dotnet run
# Expected: Shows "?? QA/Testing - Connected to: qa-testing.db"

# 3. Switch to Production environment  
connstr -f appsettings.json -e Production
dotnet run
# Expected: Shows "?? Production - Connected to: production.db"

# 4. Back to Development
connstr -f appsettings.json -e Development
dotnet run
# Expected: Shows "?? Development - Connected to: development.db"
```

### PowerShell Batch Testing

For **PowerShell** users, create `demo-test.ps1`:

```powershell
Write-Host "=== Connection String Manager Demo ===" -ForegroundColor Green
Write-Host "Demonstrating automatic environment switching with real databases" -ForegroundColor Gray
Write-Host ""

Write-Host "?? Testing Development Environment..." -ForegroundColor Green
connstr -f appsettings.json -e Development
Write-Host "Running app in Development:" -ForegroundColor Cyan
dotnet run
Write-Host ("=" * 60) -ForegroundColor Gray
Write-Host ""

Write-Host "?? Testing QA Environment..." -ForegroundColor Yellow  
connstr -f appsettings.json -e QA
Write-Host "Running app in QA:" -ForegroundColor Cyan
dotnet run
Write-Host ("=" * 60) -ForegroundColor Gray
Write-Host ""

Write-Host "?? Testing Production Environment..." -ForegroundColor Red
connstr -f appsettings.json -e Production  
Write-Host "Running app in Production:" -ForegroundColor Cyan
dotnet run
Write-Host ("=" * 60) -ForegroundColor Gray
Write-Host ""

Write-Host "?? Demo Summary:" -ForegroundColor Magenta
Write-Host "Check your folder - you should see these database files:" -ForegroundColor White
Get-ChildItem "*.db" 2>$null | ForEach-Object { 
    Write-Host "  ? $($_.Name)" -ForegroundColor Green 
}

Write-Host ""
Write-Host "?? Backup files created:" -ForegroundColor Magenta  
Get-ChildItem "appsettings.json.backup.*" 2>$null | ForEach-Object { 
    Write-Host "  ?? $($_.Name)" -ForegroundColor Gray 
}

Write-Host ""
Write-Host "?? Demo Complete! Your Connection String Manager is working perfectly!" -ForegroundColor Green
```

Run with: `.\demo-test.ps1`

### Bash Testing (Linux/Mac/WSL)

For **Bash** users, create `demo-test.sh`:

```bash
#!/bin/bash
echo "=== Connection String Manager Demo ==="
echo "Demonstrating automatic environment switching with real databases"
echo ""

echo "?? Testing Development Environment..."
connstr -f appsettings.json -e Development && dotnet run
echo "============================================================"
echo ""

echo "?? Testing QA Environment..."  
connstr -f appsettings.json -e QA && dotnet run
echo "============================================================"
echo ""

echo "?? Testing Production Environment..."
connstr -f appsettings.json -e Production && dotnet run
echo "============================================================"
echo ""

echo "?? Demo Summary:"
echo "Database files created:"
ls -la *.db 2>/dev/null || echo "  (No .db files found)"

echo ""
echo "Backup files created:"
ls -la appsettings.json.backup.* 2>/dev/null || echo "  (No backup files found)"

echo ""
echo "?? Demo Complete! Your Connection String Manager is working perfectly!"
```

Run with: `chmod +x demo-test.sh && ./demo-test.sh`

## Expected Output

Each environment should produce output like this:

### Development Environment
```
=== CONNECTION STRING TESTER ===
Connected to: development.db
Full connection string: Data Source=development.db;Cache=Shared

?? Detected Environment: ?? Development

? Created initial data for ?? Development environment
?? Environment Data:
------------------------------------------------------------
Name: ?? Development
Description: This is the ?? Development environment database
Database: development.db
Created: 2024-12-18 14:30:22

Total records in this database: 1
```

### QA Environment
```
=== CONNECTION STRING TESTER ===
Connected to: qa-testing.db
Full connection string: Data Source=qa-testing.db;Cache=Shared

?? Detected Environment: ?? QA/Testing

? Created initial data for ?? QA/Testing environment
?? Environment Data:
------------------------------------------------------------
Name: ?? QA/Testing
Description: This is the ?? QA/Testing environment database
Database: qa-testing.db
Created: 2024-12-18 14:31:45

Total records in this database: 1
```

## What the Demo Proves

? **Real Database Connections**: Uses actual SQLite databases, not mock data  
? **Environment Separation**: Each environment has its own database and data  
? **Connection String Switching**: Tool successfully updates DefaultConnection  
? **Data Persistence**: Each environment maintains its own persistent data  
? **Automatic Backups**: Creates backup files before each change  
? **Error Handling**: Gracefully handles missing files or invalid environments  

## File Structure After Demo

After running the demo, your folder will contain:

```
ConnectionStringTester/
??? Program.cs
??? ConnectionStringTester.csproj
??? appsettings.json
??? development.db              ? Created automatically
??? qa-testing.db              ? Created automatically  
??? production.db              ? Created automatically
??? appsettings.json.backup.20241218_143022  ? Backup files
??? appsettings.json.backup.20241218_143045
??? appsettings.json.backup.20241218_143108
??? demo-test.ps1              ? Your demo script
```

## Advanced Testing

### Verify Database Contents

Install SQLite command line tool and inspect databases:

```bash
# Windows: Install via chocolatey
choco install sqlite

# View development data
sqlite3 development.db "SELECT * FROM Environments;"

# View QA data  
sqlite3 qa-testing.db "SELECT * FROM Environments;"

# View production data
sqlite3 production.db "SELECT * FROM Environments;"
```

### Connection String Validation

```powershell
# List all connection strings
connstr -f appsettings.json --list

# Test invalid environment (should fail gracefully)
connstr -f appsettings.json -e InvalidEnvironment

# Test missing file (should fail gracefully)  
connstr -f nonexistent.json -e Development
```

### Integration with Your Real Projects

This same pattern works with any .NET application:

1. Copy your real `appsettings.json` to a test folder
2. Run your Connection String Manager on it
3. Run your real application
4. Verify it connects to the correct environment

## Troubleshooting

### Common Issues

**Tool not found:**
```bash
# Add .NET tools to PATH
export PATH="$PATH:$HOME/.dotnet/tools"  # Linux/Mac
# Windows: Add %USERPROFILE%\.dotnet\tools to PATH
```

**Database access errors:**
- Close any database browsers/tools
- Check file permissions
- Ensure SQLite package is restored

**Connection string not found:**
- Verify naming: `DevelopmentConnection`, `QAConnection`, `ProductionConnection`
- Check JSON syntax in appsettings.json
- Use `--list` to see available connection strings

## Extending the Demo

Want to add more environments? Just update `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=development.db;Cache=Shared",
    "DevelopmentConnection": "Data Source=development.db;Cache=Shared",
    "QAConnection": "Data Source=qa-testing.db;Cache=Shared", 
    "StagingConnection": "Data Source=staging.db;Cache=Shared",
    "ProductionConnection": "Data Source=production.db;Cache=Shared"
  }
}
```

Then use: `connstr -f appsettings.json -e Staging`

---

**This demo proves your Connection String Manager works perfectly with real applications and databases! ??**