using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConnectionStringTester;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            // Ensure database exists and is up to date
            await context.Database.EnsureCreatedAsync();

            // Show current environment info
            ShowEnvironmentInfo(context, logger);

            // Show sample data
            await ShowSampleData(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while running the application");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static void ShowEnvironmentInfo(AppDbContext context, ILogger<Program> logger)
    {
        // Get the database file path from connection string
        var connectionString = context.Database.GetConnectionString();
        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("Error: No connection string found!");
            return;
        }

        var dbPath = ExtractDatabasePath(connectionString);

        Console.WriteLine("=== CONNECTION STRING TESTER ===");
        Console.WriteLine($"Connected to: {dbPath}");
        Console.WriteLine($"Full connection string: {connectionString}");
        Console.WriteLine();

        // Try to determine environment from database path
        var environment = DetermineEnvironment(dbPath);
        Console.WriteLine($"🌍 Detected Environment: {environment}");
        Console.WriteLine();

        logger.LogInformation("Connected to database: {DatabasePath}", dbPath);
    }

    private static async Task ShowSampleData(AppDbContext context, ILogger<Program> logger)
    {
        // Seed some environment-specific data if not exists
        if (!await context.Environments.AnyAsync())
        {
            var connectionString = context.Database.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString))
            {
                logger.LogError("No connection string found for seeding data");
                return;
            }

            var dbPath = ExtractDatabasePath(connectionString);
            var envName = DetermineEnvironment(dbPath);

            var envRecord = new EnvironmentInfo
            {
                Name = envName,
                DatabasePath = dbPath,
                CreatedAt = DateTime.Now,
                Description = $"This is the {envName} environment database"
            };

            context.Environments.Add(envRecord);
            await context.SaveChangesAsync();

            Console.WriteLine($"✅ Created initial data for {envName} environment");
        }

        // Show current environment data
        var environments = await context.Environments.OrderBy(e => e.CreatedAt).ToListAsync();

        Console.WriteLine("📊 Environment Data:");
        Console.WriteLine(new string('-', 60));

        foreach (var env in environments)
        {
            Console.WriteLine($"Name: {env.Name}");
            Console.WriteLine($"Description: {env.Description}");
            Console.WriteLine($"Database: {env.DatabasePath}");
            Console.WriteLine($"Created: {env.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();
        }

        // Show record count
        var count = await context.Environments.CountAsync();
        Console.WriteLine($"Total records in this database: {count}");
    }

    private static string ExtractDatabasePath(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return "Unknown";

        // Extract Data Source from SQLite connection string
        var parts = connectionString.Split(';');
        var dataSource = parts.FirstOrDefault(p => p.Trim().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase));
        return dataSource?.Split('=', 2)[1].Trim() ?? "Unknown";
    }

    private static string DetermineEnvironment(string dbPath)
    {
        if (string.IsNullOrEmpty(dbPath) || dbPath == "Unknown")
            return "🔵 Unknown";

        var fileName = Path.GetFileNameWithoutExtension(dbPath).ToLowerInvariant();

        return fileName switch
        {
            var name when name.Contains("dev") => "🟢 Development",
            var name when name.Contains("qa") || name.Contains("test") => "🟡 QA/Testing",
            var name when name.Contains("prod") => "🔴 Production",
            _ => "🔵 Unknown"
        };
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                var configuration = hostContext.Configuration;

                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

                services.AddLogging(builder =>
                    builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            });
}

// Models/EnvironmentInfo.cs
public class EnvironmentInfo
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string DatabasePath { get; set; }
    public required string Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Data/AppDbContext.cs
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<EnvironmentInfo> Environments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EnvironmentInfo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.Property(e => e.DatabasePath).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
        });
    }
}