using GymApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NUnit.Framework;

namespace GymApi.Tests.Infrastructure;

/// <summary>
/// Base class for repository integration tests.
/// Shares a single NpgsqlDataSource across all tests to fit the connection pool limit
/// (15 connections in session mode, Supabase's free plan).
/// Cleans up test data after each test via ExecuteDelete.
/// </summary>
public abstract class RepositoryIntegrationTestBase
{
    // Shared across the entire test run — one pool, one data source
    private static readonly NpgsqlDataSource _sharedDataSource = BuildDataSource();
    
    protected GymApiDbContext DbContext { get; private set; } = null!;

    [SetUp]
    public void SetUpDatabase()
    {
        var options = new DbContextOptionsBuilder<GymApiDbContext>()
            .UseNpgsql(_sharedDataSource)
            .Options;

        DbContext = new GymApiDbContext(options);
    }

    [TearDown]
    public async Task TearDownDatabase()
    {
        // ExecuteDelete bypasses the change tracker — FK-safe order
        await DbContext.ExerciseEntries.ExecuteDeleteAsync();
        await DbContext.TrainingSessions.ExecuteDeleteAsync();
        await DbContext.Users.ExecuteDeleteAsync();
        await DbContext.DisposeAsync();
    }

    private static NpgsqlDataSource BuildDataSource()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(FindSolutionRoot())
            .AddJsonFile("GymApi.Api/appsettings.json", optional: false)
            .AddJsonFile("GymApi.Api/appsettings.Development.json", optional: false)
            .Build();

        var connectionString = config["Supabase:ConnectionString"]
            ?? throw new InvalidOperationException(
                "Supabase:ConnectionString is missing from appsettings.Development.json");

        return new NpgsqlDataSourceBuilder(connectionString)
            .EnableDynamicJson()
            .Build();
    }

    private static string FindSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !dir.GetFiles("*.sln").Any())
        {
            dir = dir.Parent;
        }

        return dir?.FullName
               ?? throw new InvalidOperationException("Could not locate solution root (.sln file).");
    }
}
