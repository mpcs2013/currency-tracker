using CurrencyTracker.Application.Abstractions.Caching;
using CurrencyTracker.Application.Abstractions.Persistence;
using CurrencyTracker.Application.Caching;
using CurrencyTracker.Application.Exceptions;
using CurrencyTracker.Domain.Currencies;

namespace CurrencyTracker.Application.Messaging;

/// <summary>
/// Cache-aside handler for <see cref="GetLatestRatesQuery"/>. Returns the
/// latest persisted rates for the requested base currency, served from the
/// cache on a hit and read through <see cref="IExchangeRateRepository"/> on
/// a miss (then cached with a jittered TTL). The query's validity is
/// guaranteed by the <c>UseFluentValidation()</c> middleware (Phase 6), so
/// the handler does no input checking; a missing snapshot throws
/// <see cref="NotFoundException"/> (translated to 404 by the pipeline), so
/// the handler contains no <c>try</c>/<c>catch</c>.
/// </summary>
public sealed class GetLatestRatesHandler(IExchangeRateRepository repository, ICacheService cache)
{
    /// <summary>Relative TTL for a cached latest-rates entry. Jitter is the adapter's concern.</summary>
    private static readonly TimeSpan LatestRatesTtl = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Handles the latest-rates query with cache-aside semantics.
    /// </summary>
    /// <param name="query">The validated query.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The latest rates for the base currency.</returns>
    /// <exception cref="NotFoundException">No snapshot exists for the base
    /// currency; mapped to 404 by the <c>IExceptionHandler</c> pipeline.</exception>
    public Task<IReadOnlyList<ExchangeRateDto>> Handle(
        GetLatestRatesQuery query,
        CancellationToken cancellationToken
    ) =>
        cache.GetOrSetAsync(
            CacheKeys.LatestRates(query.Base),
            async token =>
            {
                // Validated by the middleware — parse is safe.
                var baseCode = CurrencyCode.Create(query.Base).Value;

                var snapshot = await repository.GetLatestSnapshotAsync(baseCode, token);

                return snapshot is null
                    ? throw new NotFoundException("ExchangeRate", query.Base)
                    : ExchangeRateDto.FromSnapshot(snapshot);
            },
            LatestRatesTtl,
            cancellationToken
        );
}
