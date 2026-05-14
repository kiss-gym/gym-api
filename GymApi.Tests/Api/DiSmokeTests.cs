using GymApi.Application.SessionTracking;
using GymApi.Application.UserManagement;
using GymApi.Domain.SessionTracking;
using GymApi.Domain.UserManagement;
using GymApi.Infrastructure;
using GymApi.Infrastructure.SessionTracking;
using GymApi.Infrastructure.UserManagement;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace GymApi.Tests.Api;

/// <summary>
/// Smoke tests verify the full production wiring resolves correctly.
/// Uses WebApplicationFactory to boot the real Program.cs pipeline.
/// </summary>
[TestFixture]
[Category("Integration")]
public sealed class DiSmokeTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private IServiceScope _scope = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new WebApplicationFactory<Program>();
        _scope = _factory.Services.CreateScope();
    }

    [TearDown]
    public void TearDown()
    {
        _scope.Dispose();
        _factory.Dispose();
    }

    // ── Repositories ────────────────────────────────────────────────────────

    [Test]
    public void ITrainingSessionRepository_ResolvesAs_SupabaseImplementation()
    {
        var repo = _scope.ServiceProvider.GetRequiredService<ITrainingSessionRepository>();

        Assert.That(repo, Is.InstanceOf<SupabaseTrainingSessionRepository>());
    }

    [Test]
    public void IUserRepository_ResolvesAs_SupabaseImplementation()
    {
        var repo = _scope.ServiceProvider.GetRequiredService<IUserRepository>();

        Assert.That(repo, Is.InstanceOf<SupabaseUserRepository>());
    }

    // ── Auth ─────────────────────────────────────────────────────────────────

    [Test]
    public void IUserContext_ResolvesAs_JwtUserContext()
    {
        var context = _scope.ServiceProvider.GetRequiredService<IUserContext>();

        Assert.That(context, Is.InstanceOf<JwtUserContext>());
    }

    // ── Services ─────────────────────────────────────────────────────────────

    [Test]
    public void ITrainingSessionService_Resolves()
    {
        var service = _scope.ServiceProvider.GetRequiredService<ITrainingSessionService>();

        Assert.That(service, Is.Not.Null);
    }

    [Test]
    public void IUserService_Resolves()
    {
        var service = _scope.ServiceProvider.GetRequiredService<IUserService>();

        Assert.That(service, Is.Not.Null);
    }

    // ── DbContext ─────────────────────────────────────────────────────────────

    [Test]
    public void GymApiDbContext_Resolves()
    {
        var db = _scope.ServiceProvider.GetRequiredService<GymApiDbContext>();

        Assert.That(db, Is.Not.Null);
    }
}
