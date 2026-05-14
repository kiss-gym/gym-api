using GymApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NUnit.Framework;

namespace GymApi.Tests.Infrastructure;

/// <summary>
/// Base class for repository integration tests.
/// Connects to the real Supabase database using the connection string
/// from appsettings.Development.json. Cleans up test data after each test.
/// </summary>
public abstract class RepositoryIntegrationTestBase
{
    protected GymApiDbContext DbContext { get; private set; } = null!;

    [SetUp]
    public void SetUpDatabase()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(FindSolutionRoot())
            .AddJsonFile("GymApi.Api/appsettings.json", optional: false)
            .AddJsonFile("GymApi.Api/appsettings.Development.json", optional: false)
            .Build();

        var connectionString = config["Supabase:ConnectionString"]
            ?? throw new InvalidOperationException(
                "Supabase:ConnectionString is missing from appsettings.Development.json");

        // EnableDynamicJson is required for Npgsql 8+ to serialize List<T> → jsonb
        var dataSource = new NpgsqlDataSourceBuilder(connectionString)
            .EnableDynamicJson()
            .Build();

        var options = new DbContextOptionsBuilder<GymApiDbContext>()
            .UseNpgsql(dataSource)
            .Options;

        DbContext = new GymApiDbContext(options);
    }

    [TearDown]
    public async Task TearDownDatabase()
    {
        // FK-safe order: exercise_entries first (child), then sessions, then users
        await DbContext.ExerciseEntries.ExecuteDeleteAsync();
        await DbContext.TrainingSessions.ExecuteDeleteAsync();
        await DbContext.Users.ExecuteDeleteAsync();
        await DbContext.DisposeAsync();
    }

    /// <summary>Walks up from the test binary output to find the solution root.</summary>
    private static string FindSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && dir.GetFiles("*.sln").Length == 0)
        {
            dir = dir.Parent;
        }
        return dir?.FullName
            ?? throw new InvalidOperationException("Could not locate solution root (.sln file).");
    }
}
