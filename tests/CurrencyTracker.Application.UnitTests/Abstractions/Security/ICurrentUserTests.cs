using CurrencyTracker.Application.Abstractions.Security;
using CurrencyTracker.Application.UnitTests.Fakes;

namespace CurrencyTracker.Application.UnitTests.Abstractions.Security;

/// <summary>
/// Tests exercising the <see cref="ICurrentUser"/> contract via the
/// <see cref="FakeCurrentUser"/> fake: anonymous case, authenticated with
/// roles, role membership lookup, and tenant round-trip.
/// </summary>
public sealed class ICurrentUserTests
{
    [Fact]
    public void Anonymous_returns_unauthenticated_user_with_no_roles_or_tenant()
    {
        FakeCurrentUser sut = FakeCurrentUser.Anonymous;

        sut.UserId.Should().BeNull();
        sut.IsAuthenticated.Should().BeFalse();
        sut.Roles.Should().BeEmpty();
        sut.Tenant.Should().BeNull();
    }

    [Fact]
    public void WithRoles_returns_authenticated_user_with_supplied_roles()
    {
        var userId = Guid.NewGuid();

        FakeCurrentUser sut = FakeCurrentUser.WithRoles(userId, "admin", "editor");

        sut.UserId.Should().Be(userId);
        sut.IsAuthenticated.Should().BeTrue();
        sut.Roles.Should().BeEquivalentTo(["admin", "editor"]);
        sut.Tenant.Should().BeNull();
    }

    [Fact]
    public void Roles_Contains_returns_true_for_assigned_role_and_false_for_absent_role()
    {
        FakeCurrentUser sut = FakeCurrentUser.WithRoles(Guid.NewGuid(), "reader");

        sut.Roles.Contains("reader").Should().BeTrue();
        sut.Roles.Contains("admin").Should().BeFalse();
    }

    [Fact]
    public void Constructor_preserves_tenant_round_trip()
    {
        var userId = Guid.NewGuid();
        var tenant = "acme-corp";

        FakeCurrentUser sut = new FakeCurrentUser(
            userId: userId,
            isAuthenticated: true,
            roles: ["viewer"],
            tenant: tenant
        );

        sut.Tenant.Should().Be(tenant);
        sut.UserId.Should().Be(userId);
    }
}
