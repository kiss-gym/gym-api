using GymApi.Domain.UserManagement;
using GymApi.Infrastructure.UserManagement;
using GymApi.Tests.Infrastructure;
using NUnit.Framework;

namespace GymApi.Tests.UserManagement;

/// <summary>
/// Integration tests for SupabaseUserRepository against a real Supabase database.
/// </summary>
[TestFixture]
[Category("Integration")]
public sealed class SupabaseUserRepositoryTests : RepositoryIntegrationTestBase
{
    private SupabaseUserRepository _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new SupabaseUserRepository(DbContext);

    // ── SaveAsync + GetByIdAsync ────────────────────────────────────────────

    [Test]
    public async Task SaveAsync_NewUser_CanBeRetrievedById()
    {
        var user = User.Create(Guid.NewGuid(), "alice@gym.com", "Alice");

        await _sut.SaveAsync(user);
        var retrieved = await _sut.GetByIdAsync(user.Id);

        Assert.That(retrieved, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(retrieved!.Id, Is.EqualTo(user.Id));
            Assert.That(retrieved.Email, Is.EqualTo("alice@gym.com"));
            Assert.That(retrieved.Name, Is.EqualTo("Alice"));
        });
    }

    [Test]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.That(result, Is.Null);
    }

    // ── GetByEmailAsync ─────────────────────────────────────────────────────

    [Test]
    public async Task GetByEmailAsync_ExistingEmail_ReturnsUser()
    {
        var user = User.Create(Guid.NewGuid(), "bob@gym.com", "Bob");
        await _sut.SaveAsync(user);

        var retrieved = await _sut.GetByEmailAsync("bob@gym.com");

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Id, Is.EqualTo(user.Id));
    }

    [Test]
    public async Task GetByEmailAsync_IsCaseInsensitive()
    {
        var user = User.Create(Guid.NewGuid(), "carol@gym.com", "Carol");
        await _sut.SaveAsync(user);

        var retrieved = await _sut.GetByEmailAsync("CAROL@GYM.COM");

        Assert.That(retrieved, Is.Not.Null);
    }

    [Test]
    public async Task GetByEmailAsync_UnknownEmail_ReturnsNull()
    {
        var result = await _sut.GetByEmailAsync("nobody@gym.com");

        Assert.That(result, Is.Null);
    }

    // ── SaveAsync (update) ──────────────────────────────────────────────────

    [Test]
    public async Task SaveAsync_ExistingUser_UpdatesPersisted()
    {
        var id = Guid.NewGuid();
        var user = User.Create(id, "dave@gym.com", "Dave");
        await _sut.SaveAsync(user);

        // Simulate profile update — requires Update method or re-create for now
        var updated = User.Create(id, "dave@gym.com", "David");
        await _sut.SaveAsync(updated);

        var retrieved = await _sut.GetByIdAsync(id);
        Assert.That(retrieved!.Name, Is.EqualTo("David"));
    }
}
