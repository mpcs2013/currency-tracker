using CurrencyTracker.Application.Abstractions.Security;

namespace CurrencyTracker.Application.UnitTests.Fakes;

/// <summary>
/// In-memory <see cref="ICurrentUser"/> fake for unit tests. Construct with
/// explicit values, or use the convenience factories <see cref="Anonymous"/>
/// and <see cref="WithRoles"/> for the most common scenarios.
/// </summary>
public sealed class FakeCurrentUser : ICurrentUser
{
    /// <summary>
    /// Initializes a new instance of <see cref="FakeCurrentUser"/> with
    /// explicit values for all four members.
    /// </summary>
    /// <param name="userId">The user's unique identifier, or <see langword="null"/> for unauthenticated.</param>
    /// <param name="isAuthenticated">Whether the request is authenticated.</param>
    /// <param name="roles">The role names granted to the caller.</param>
    /// <param name="tenant">The tenant identifier, or <see langword="null"/>.</param>
    public FakeCurrentUser(
        Guid? userId,
        bool isAuthenticated,
        IReadOnlyCollection<string> roles,
        string? tenant
    )
    {
        UserId = userId;
        IsAuthenticated = isAuthenticated;
        Roles = roles;
        Tenant = tenant;
    }

    /// <inheritdoc />
    public Guid? UserId { get; }

    /// <inheritdoc />
    public bool IsAuthenticated { get; }

    /// <inheritdoc />
    public IReadOnlyCollection<string> Roles { get; }

    /// <inheritdoc />
    public string? Tenant { get; }

    /// <summary>
    /// Gets a <see cref="FakeCurrentUser"/> representing an unauthenticated
    /// caller: <see cref="UserId"/> is <see langword="null"/>,
    /// <see cref="IsAuthenticated"/> is <see langword="false"/>,
    /// <see cref="Roles"/> is empty, and <see cref="Tenant"/> is
    /// <see langword="null"/>.
    /// </summary>
    public static FakeCurrentUser Anonymous { get; } =
        new(userId: null, isAuthenticated: false, roles: [], tenant: null);

    /// <summary>
    /// Creates an authenticated <see cref="FakeCurrentUser"/> with the given
    /// <paramref name="userId"/> and <paramref name="roles"/>. <see cref="Tenant"/>
    /// is <see langword="null"/>.
    /// </summary>
    /// <param name="userId">The authenticated user's unique identifier.</param>
    /// <param name="roles">The role names to assign.</param>
    public static FakeCurrentUser WithRoles(Guid userId, params string[] roles) =>
        new(userId: userId, isAuthenticated: true, roles: roles, tenant: null);
}
