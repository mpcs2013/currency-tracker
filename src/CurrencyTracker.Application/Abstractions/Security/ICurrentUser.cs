namespace CurrencyTracker.Application.Abstractions.Security;

/// <summary>
/// Provides identity information for the caller of the current request.
/// Handlers use this port to answer "who is calling?" without taking a
/// dependency on <c>HttpContext</c>, <c>ClaimsPrincipal</c>, or any
/// ASP.NET runtime type. The Phase 11 adapter parses the JWT
/// <c>sub</c> claim and populates each member accordingly.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// Gets the unique identifier of the authenticated user, or
    /// <see langword="null"/> when the request is unauthenticated or the
    /// <c>sub</c> claim cannot be parsed as a <see cref="Guid"/>.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets a value indicating whether the current request is authenticated.
    /// A request is authenticated when a valid, unexpired token was presented;
    /// this is independent of whether the caller holds any particular role.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the set of role names granted to the current caller.
    /// Returns an empty collection for unauthenticated requests.
    /// The backing implementation is free to use any concrete collection
    /// type (e.g. <c>HashSet&lt;string&gt;</c>); callers must not cast.
    /// </summary>
    IReadOnlyCollection<string> Roles { get; }

    /// <summary>
    /// Gets the tenant identifier associated with the current request, or
    /// <see langword="null"/> when multi-tenancy is not yet in use or the
    /// claim is absent. Kept as a plain <see cref="string"/> because tenancy
    /// is not yet modelled as a domain value object.
    /// </summary>
    string? Tenant { get; }
}
