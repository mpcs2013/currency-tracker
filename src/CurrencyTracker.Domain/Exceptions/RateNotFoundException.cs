namespace CurrencyTracker.Domain.Exceptions;

/// <summary>
/// Thrown when a rate lookup is expected to succeed but the rate is
/// missing — e.g. the alert evaluator queries for yesterday's
/// USD/EUR rate and the ingestion didn't run. The happy path for
/// "rate may or may not be present" is to return
/// <c>Result&lt;ExchangeRate&gt;</c>; this exception is for paths
/// where the caller has already asserted the rate must exist.
/// </summary>
public sealed class RateNotFoundException : DomainException
{
    /// <summary>
    /// Creates a new <see cref="RateNotFoundException"/>.
    /// </summary>
    /// <param name="baseCode">Base currency code (e.g. <c>USD</c>).</param>
    /// <param name="quoteCode">Quote currency code (e.g. <c>EUR</c>).</param>
    /// <param name="asOf">Date the missing rate was sought for.</param>
    public RateNotFoundException(string baseCode, string quoteCode, DateOnly asOf)
        : base($"No exchange rate found for {baseCode}/{quoteCode} on {asOf:yyyy-MM-dd}.") { }
}
