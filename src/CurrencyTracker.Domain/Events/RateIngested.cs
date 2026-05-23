using CurrencyTracker.Domain.Currencies;

namespace CurrencyTracker.Domain.Events;

/// <summary>
/// Domain event raised when a batch of exchange rates has been ingested for a
/// base currency and as-of date.
/// </summary>
/// <param name="Base">Base currency for the ingested rates.</param>
/// <param name="AsOf">Effective date for the ingested rates.</param>
/// <param name="RateCount">Number of rates included in the ingestion batch.</param>
public sealed record RateIngested(CurrencyCode Base, DateOnly AsOf, int RateCount);
