using GymApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Microsoft.EntityFrameworkCore.Storage;
using NUnit.Framework;

namespace GymApi.Tests.Infrastructure;

/// <summary>
/// Base class for repository integration tests against real Supabase databases.
/// Requires appsettings.Development.json with a valid Supabase:ConnectionString.
/// Shares a single _sharedDataSource (NpgsqlDataSource) across all tests to fit the connection pool limit
/// Cleans up test data via _transaction.RollbackAsync 
/// </summary>
public abstract class RepositoryIntegrationTestBase
{
    // Shared across the entire test run — one pool, one data source
    private static readonly NpgsqlDataSource _sharedDataSource = BuildDataSource();
    
    protected GymApiDbContext DbContext { get; private set; } = null!;
    private IDbContextTransaction? _transaction;

    [SetUp]
    public async Task SetUpDatabase()
    {
        var options = new DbContextOptionsBuilder<GymApiDbContext>()
            .UseNpgsql(_sharedDataSource)
            .Options;

        DbContext = new GymApiDbContext(options);
        _transaction = await DbContext.Database.BeginTransactionAsync();
    }

    [TearDown]
    public async Task TearDownDatabase()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
        }
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
