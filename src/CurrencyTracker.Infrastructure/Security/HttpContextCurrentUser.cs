using System.Security.Claims;
using CurrencyTracker.Application.Abstractions.Security;
using Microsoft.AspNetCore.Http;

namespace CurrencyTracker.Infrastructure.Security;

/// <summary>
/// The only adapter in the system that touches <see cref="HttpContext"/> /
/// <see cref="ClaimsPrincipal"/>. It projects the validated request
/// principal onto the Phase 4 <see cref="ICurrentUser"/> shape so
/// Application code reasons about identity without any ASP.NET dependency.
/// Registered scoped in <c>AddInfrastructure</c>; resolves to an anonymous
/// view when there is no active <see cref="HttpContext"/> (e.g. in the
/// Worker host).
/// </summary>
internal sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    /// <summary>Initialises the adapter over the ambient HTTP context.</summary>
    /// <param name="accessor">Accessor for the current request's
    /// <see cref="HttpContext"/>; its <c>HttpContext</c> is null outside a
    /// request, which the adapter treats as anonymous.</param>
    public HttpContextCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    /// <inheritdoc />
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    /// <inheritdoc />
    public Guid? UserId =>
        IsAuthenticated && Guid.TryParse(Principal!.FindFirstValue("sub"), out var id) ? id : null;

    /// <inheritdoc />
    public IReadOnlyCollection<string> Roles =>
        IsAuthenticated
            ? Principal!
                .FindAll("roles")
                .Select(static c => c.Value)
                .ToHashSet(StringComparer.Ordinal)
            : [];

    /// <inheritdoc />
    public string? Tenant => IsAuthenticated ? Principal!.FindFirstValue("tenant") : null;
}
