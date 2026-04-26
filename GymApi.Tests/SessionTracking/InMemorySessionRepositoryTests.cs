using GymApi.Domain.SessionTracking;
using GymApi.Infrastructure.SessionTracking;
using NUnit.Framework;

namespace GymApi.Tests.SessionTracking;

[TestFixture]
public sealed class InMemorySessionRepositoryTests
{
    private InMemorySessionRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new InMemorySessionRepository();
    }

    [Test]
    public async Task SaveAndFind_WorkCorrectly()
    {
        var session = TrainingSession.Create(Guid.NewGuid());
        
        await _repository.SaveAsync(session);
        var found = await _repository.FindAsync(session.Id);

        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Id, Is.EqualTo(session.Id));
    }

    [Test]
    public async Task FindLatestByUserAsync_ReturnsMostRecent()
    {
        var userId = Guid.NewGuid();
        var session1 = TrainingSession.Create(userId);
        var session2 = TrainingSession.Create(userId);

        await _repository.SaveAsync(session1);
        await _repository.SaveAsync(session2);

        var latest = await _repository.FindLatestByUserAsync(userId);

        Assert.That(latest, Is.Not.Null);
        Assert.That(latest!.Id, Is.EqualTo(session2.Id));
    }
}
