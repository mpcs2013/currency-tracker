namespace CurrencyTracker.Application.Ping;

/// <summary>
/// The Phase 4 hand-wired ping request payload. Replaced by a
/// Wolverine message in Phase 5.
/// </summary>
/// <param name="Message">Free-form text echoed back by the handler.</param>
public sealed record PingRequest(string Message);
