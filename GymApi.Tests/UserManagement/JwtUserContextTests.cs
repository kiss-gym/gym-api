using System.Security.Claims;
using GymApi.Domain.UserManagement;
using GymApi.Infrastructure.UserManagement;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using NUnit.Framework;

namespace GymApi.Tests.UserManagement;

/// <summary>
/// Unit tests for JwtUserContext.
/// RED: these fail until JwtUserContext is implemented.
/// </summary>
[TestFixture]
public sealed class JwtUserContextTests
{
    private IHttpContextAccessor _httpContextAccessor = null!;
    private IUserContext _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _sut = new JwtUserContext(_httpContextAccessor);
    }

    [Test]
    public void UserId_WhenHttpContextIsNull_ReturnsNull()
    {
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        Assert.That(_sut.UserId, Is.Null);
    }

    [Test]
    public void UserId_WhenUserIsNotAuthenticated_ReturnsNull()
    {
        var context = new DefaultHttpContext();
        // No claims principal set — anonymous request
        _httpContextAccessor.HttpContext.Returns(context);

        Assert.That(_sut.UserId, Is.Null);
    }

    [Test]
    public void UserId_WhenValidSubClaimPresent_ReturnsCorrectGuid()
    {
        var userId = Guid.NewGuid();
        var context = BuildContextWithSubClaim(userId.ToString());
        _httpContextAccessor.HttpContext.Returns(context);

        Assert.That(_sut.UserId, Is.EqualTo(userId));
    }

    [Test]
    public void UserId_WhenSubClaimIsMalformedGuid_ReturnsNull()
    {
        var context = BuildContextWithSubClaim("not-a-valid-guid");
        _httpContextAccessor.HttpContext.Returns(context);

        Assert.That(_sut.UserId, Is.Null);
    }

    [Test]
    public void UserId_WhenSubClaimIsMissing_ReturnsNull()
    {
        var context = new DefaultHttpContext();
        // Authenticated user but no sub claim (edge case)
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Email, "user@gym.com")],
            authenticationType: "Bearer");
        context.User = new ClaimsPrincipal(identity);
        _httpContextAccessor.HttpContext.Returns(context);

        Assert.That(_sut.UserId, Is.Null);
    }

    [Test]
    public void IsAuthenticated_WhenValidSubClaimPresent_ReturnsTrue()
    {
        var context = BuildContextWithSubClaim(Guid.NewGuid().ToString());
        _httpContextAccessor.HttpContext.Returns(context);

        Assert.That(_sut.IsAuthenticated, Is.True);
    }

    [Test]
    public void IsAuthenticated_WhenNoSubClaim_ReturnsFalse()
    {
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        Assert.That(_sut.IsAuthenticated, Is.False);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static DefaultHttpContext BuildContextWithSubClaim(string subValue)
    {
        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity(
            // Supabase JWT uses "sub" claim for the user's UUID
            [new Claim(ClaimTypes.NameIdentifier, subValue)],
            authenticationType: "Bearer");
        context.User = new ClaimsPrincipal(identity);
        return context;
    }
}
