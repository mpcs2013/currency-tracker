using System.Security.Claims;
using CurrencyTracker.Application.Abstractions.Security;
using CurrencyTracker.Infrastructure.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace CurrencyTracker.Infrastructure.UnitTests.Security;

/// <summary>
/// Tests the <see cref="HttpContextCurrentUser"/> projection from a
/// <see cref="ClaimsPrincipal"/> to the Phase 4 <see cref="ICurrentUser"/>
/// shape: anonymous, authenticated-with-roles, an unparseable <c>sub</c>
/// (null id, no throw), and a tenant round-trip.
/// </summary>
public sealed class HttpContextCurrentUserTests
{
    /// <summary>
    /// Builds an adapter over an <see cref="IHttpContextAccessor"/> whose
    /// <see cref="HttpContext.User"/> carries the supplied claims. A null
    /// <paramref name="claims"/> models an anonymous (no-identity) request.
    /// </summary>
    /// <param name="claims">The claims to place on an authenticated identity,
    /// or null for an anonymous request.</param>
    /// <returns>The adapter under test, typed as the port.</returns>
    private static HttpContextCurrentUser ForClaims(IEnumerable<Claim>? claims)
    {
        var principal = claims is null
            ? new ClaimsPrincipal(new ClaimsIdentity()) // not authenticated
            : new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));

        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext { User = principal });

        return new HttpContextCurrentUser(accessor);
    }

    [Fact]
    public void Anonymous_request_is_unauthenticated_with_no_id_roles_or_tenant()
    {
        HttpContextCurrentUser sut = ForClaims(null);

        sut.IsAuthenticated.Should().BeFalse();
        sut.UserId.Should().BeNull();
        sut.Roles.Should().BeEmpty();
        sut.Tenant.Should().BeNull();
    }

    [Fact]
    public void Authenticated_request_projects_sub_roles_and_tenant()
    {
        var userId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim("sub", userId.ToString()),
            new Claim("roles", "user"),
            new Claim("roles", "admin"),
            new Claim("tenant", "acme-corp"),
        };

        HttpContextCurrentUser sut = ForClaims(claims);

        sut.IsAuthenticated.Should().BeTrue();
        sut.UserId.Should().Be(userId);
        sut.Roles.Should().BeEquivalentTo(["user", "admin"]);
        sut.Tenant.Should().Be("acme-corp");
    }

    [Fact]
    public void Unparseable_sub_yields_null_user_id_without_throwing()
    {
        var claims = new[] { new Claim("sub", "not-a-guid"), new Claim("roles", "user") };

        HttpContextCurrentUser sut = ForClaims(claims);

        sut.IsAuthenticated.Should().BeTrue();
        sut.UserId.Should().BeNull();
        sut.Roles.Should().BeEquivalentTo(["user"]);
    }

    [Fact]
    public void Missing_tenant_claim_is_null()
    {
        var claims = new[] { new Claim("sub", Guid.NewGuid().ToString()) };

        HttpContextCurrentUser sut = ForClaims(claims);

        sut.Tenant.Should().BeNull();
    }
}
