namespace CurrencyTracker.Application.Exceptions;

/// <summary>
/// Thrown by Application-layer handlers when a queried resource is not
/// found. Mapped to HTTP 404 by
/// <c>CurrencyTracker.Api.ErrorHandling.NotFoundExceptionHandler</c>.
/// </summary>
/// <remarks>
/// Distinct from <c>CurrencyTracker.Domain.Exceptions.RateNotFoundException</c>,
/// which is a domain invariant failure (mapped to 422). Use this type
/// when a handler's lookup returns no result and the lookup failure is
/// a normal outcome rather than an invariant violation.
/// </remarks>
/// <param name="resource">
/// The conceptual resource type, e.g. <c>"User"</c>, <c>"ExchangeRate"</c>,
/// <c>"Alert"</c>. Used in the message and exposed in the response.
/// </param>
/// <param name="key">
/// The lookup key that did not match, e.g. <c>"USD/EUR/2026-05-21"</c>.
/// Must not contain sensitive data — it appears in the response body.
/// </param>
public sealed class NotFoundException(string resource, string key)
    : Exception($"{resource} '{key}' was not found.")
{
    /// <summary>Resource type name (e.g. <c>"ExchangeRate"</c>).</summary>
    public string Resource { get; } = resource;

    /// <summary>Lookup key that did not match (e.g. <c>"USD/EUR/2026-05-21"</c>).</summary>
    public string Key { get; } = key;
}
