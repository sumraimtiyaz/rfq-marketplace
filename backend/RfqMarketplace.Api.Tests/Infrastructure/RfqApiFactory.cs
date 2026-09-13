using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using RfqMarketplace.Api.Data;

namespace RfqMarketplace.Api.Tests.Infrastructure;

/// <summary>
/// Boots the real application pipeline - real routing, real authentication, real authorization,
/// real validators - against a throwaway database.
///
/// By default that database is SQLite in memory, so the suite runs anywhere with no Docker.
/// Set TEST_POSTGRES_CONNECTION (CI does) to run the same tests against real PostgreSQL, which
/// is what catches provider-specific drift.
/// </summary>
public class RfqApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>
    /// Shared-cache in-memory database, addressed by name. Every DbContext opens its own
    /// connection to it, which is what lets the concurrency tests issue genuinely parallel
    /// writes - a single shared SqliteConnection is not thread-safe.
    /// </summary>
    private readonly string _sqliteConnectionString =
        $"DataSource=file:rfq-tests-{Guid.NewGuid():N}?mode=memory&cache=shared";

    /// <summary>The in-memory database exists only while at least one connection is open to it.</summary>
    private SqliteConnection? _keepAlive;

    private static string? ConfiguredPostgresConnection =>
        Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION");

    public static bool UsesPostgres => !string.IsNullOrWhiteSpace(ConfiguredPostgresConnection);

    /// <summary>
    /// xUnit runs test classes in parallel and each one owns a fixture, so every fixture needs its
    /// own database - otherwise they drop and recreate the schema underneath each other.
    /// </summary>
    private readonly string _postgresConnectionString = UsesPostgres
        ? new NpgsqlConnectionStringBuilder(ConfiguredPostgresConnection)
        {
            Database = $"rfq_tests_{Guid.NewGuid():N}"
        }.ConnectionString
        : string.Empty;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "integration-test-signing-key-must-be-at-least-32-chars",
            ["Jwt:Issuer"] = "RfqMarketplace",
            ["Jwt:Audience"] = "RfqMarketplaceClient",
            ["Jwt:CookieSecure"] = "false",
            // The fixture owns schema creation, so the host must not migrate or seed demo data.
            ["Database:MigrateOnStartup"] = "false",
            ["Database:SeedDemoData"] = "false",
            ["Swagger:Enabled"] = "false",
            // Request logs would otherwise bury the assertion failures in the test output.
            ["Serilog:MinimumLevel:Default"] = "Warning",
            ["Serilog:MinimumLevel:Override:Microsoft.AspNetCore"] = "Warning"
        }));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            if (UsesPostgres)
            {
                services.AddDbContext<AppDbContext>(options => options.UseNpgsql(_postgresConnectionString));
                return;
            }

            _keepAlive = new SqliteConnection(_sqliteConnectionString);
            _keepAlive.Open();

            services.AddDbContext<AppDbContext>(options => options
                .UseSqlite(_sqliteConnectionString, sqlite => sqlite.CommandTimeout(30))
                .ReplaceService<IModelCustomizer, SqliteTypeModelCustomizer>());
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (UsesPostgres)
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
        }

        // Registration assigns an Identity role, so both roles have to exist first.
        await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync();
    }

    public new async Task DisposeAsync()
    {
        if (UsesPostgres)
        {
            using var scope = Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureDeletedAsync();
        }

        if (_keepAlive is not null)
            await _keepAlive.DisposeAsync();

        await base.DisposeAsync();
    }

    /// <summary>Reaches the database directly, to arrange states the API deliberately forbids.</summary>
    public async Task WithDbAsync(Func<AppDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }
}
