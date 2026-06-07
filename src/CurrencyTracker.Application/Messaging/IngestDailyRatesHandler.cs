using CurrencyTracker.Application.Abstractions.Caching;
using CurrencyTracker.Application.Abstractions.Persistence;
using CurrencyTracker.Application.Abstractions.Providers;
using CurrencyTracker.Application.Caching;
using CurrencyTracker.Domain.Common;
using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Exceptions;

namespace CurrencyTracker.Application.Messaging;

/// <summary>
/// Cascading event returned by <see cref="IngestDailyRatesHandler"/> and
/// dispatched by Wolverine after a successful ingestion. Application-owned
/// (distinct from the Domain <c>RateIngested</c> event): this is the
/// Wolverine message the handler returns, carrying the persisted
/// snapshot's identity and rate count. Phase 10's cache invalidator and
/// Phase 12's alert evaluator are its future consumers.
/// </summary>
/// <param name="Base">Base currency of the ingested snapshot.</param>
/// <param name="AsOf">Observation date of the ingested snapshot.</param>
/// <param name="RateCount">Number of rates persisted in the snapshot.</param>
public sealed record DailyRatesIngested(CurrencyCode Base, DateOnly AsOf, int RateCount);

/// <summary>
/// Wolverine handler for <see cref="IngestDailyRatesCommand"/>. Fetches
/// the snapshot via <see cref="IExchangeRateProvider"/>, persists it
/// through the Phase 8 repository + unit-of-work, and returns a cascading
/// <see cref="DailyRatesIngested"/> event. The command's validity is
/// guaranteed by the <c>UseFluentValidation()</c> middleware (Phase 6),
/// so the handler does no input checking; provider failures are thrown
/// and translated by the <c>IExceptionHandler</c> pipeline, so the
/// handler contains no <c>try</c>/<c>catch</c>.
/// </summary>
public static class IngestDailyRatesHandler
{
    /// <summary>
    /// Handles the ingestion command.
    /// </summary>
    /// <param name="command">The validated ingestion command.</param>
    /// <param name="provider">External rate provider port.</param>
    /// <param name="repository">Exchange-rate repository port.</param>
    /// <param name="unitOfWork">Unit-of-work port.</param>
    /// <param name="cache">Cache service port, for post-commit eviction of the latest-rates key.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The cascading <see cref="DailyRatesIngested"/> event.</returns>
    /// <exception cref="DomainException">Thrown when the provider returns
    /// a failure; translated to a ProblemDetails by the
    /// <c>IExceptionHandler</c> pipeline.</exception>
    public static async Task<DailyRatesIngested> Handle(
        IngestDailyRatesCommand command,
        IExchangeRateProvider provider,
        IExchangeRateRepository repository,
        IUnitOfWork unitOfWork,
        ICacheService cache, // ← added in 10.6
        CancellationToken cancellationToken
    )
    {
        using var activity = IngestionTelemetry.ActivitySource.StartActivity("ingest.daily_rates");
        activity?.SetTag("ingest.base", command.BaseCurrency);
        activity?.SetTag("ingest.as_of", command.AsOf.ToString("yyyy-MM-dd"));

        // Validated by the FluentValidation middleware — parse is safe.
        var baseCurrency = CurrencyCode.Create(command.BaseCurrency).Value;

        var snapshot = await provider.FetchAsync(baseCurrency, command.AsOf, cancellationToken);

        if (!snapshot.IsSuccess)
        {
            // No try/catch building an HTTP response — throw and let the
            // IExceptionHandler pipeline translate it to ProblemDetails.
            throw new DomainException(
                $"Ingestion failed for {command.BaseCurrency} on {command.AsOf:yyyy-MM-dd}: "
                    + $"{snapshot.Error.Code}."
            );
        }

        await repository.SaveSnapshotAsync(snapshot.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Read/write coherence: evict the exact key the latest-rates query
        // writes, AFTER the commit, so the next read repopulates with the
        // freshly-ingested data. Shared CacheKeys helper — never a KEYS/SCAN
        // sweep (Phase 10.11).
        await cache.RemoveAsync(CacheKeys.LatestRates(command.BaseCurrency), cancellationToken);

        IngestionTelemetry.RatesIngested.Add(snapshot.Value.Rates.Count);

        return new DailyRatesIngested(baseCurrency, command.AsOf, snapshot.Value.Rates.Count);
    }
}
