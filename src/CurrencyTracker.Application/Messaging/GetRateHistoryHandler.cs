using CurrencyTracker.Application.Abstractions.Persistence;
using CurrencyTracker.Domain.Currencies;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace CurrencyTracker.Application.Messaging;

/// <summary>
/// Reads a base/quote rate over a date range straight through the
/// repository — deliberately uncached (see docs/caching.md). An empty range
/// is a valid empty answer (200 []), not a 404.
/// </summary>
public sealed class GetRateHistoryHandler(IExchangeRateRepository repository)
{
    /// <summary>HTTP entry point for the rate-history query.</summary>
    /// <param name="query">The validated query (bound from the query string).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The rate for the requested quote on each date in range (ascending).</returns>
    [WolverineGet("/api/v1/rates/history")]
    public async Task<IReadOnlyList<RateHistoryPointDto>> Handle(
        [FromQuery] GetRateHistoryQuery query,
        CancellationToken cancellationToken
    )
    {
        var baseCode = CurrencyCode.Create(query.Base).Value;
        var quoteCode = CurrencyCode.Create(query.Quote).Value;

        var snapshots = await repository.GetSnapshotsInRangeAsync(
            baseCode,
            query.From,
            query.To,
            cancellationToken
        );

        return snapshots
            .Select(s =>
                s.TryGetRate(quoteCode, out var rate)
                    ? (s.AsOf, rate.Rate)
                    : ((DateOnly, decimal)?)null
            )
            .Where(p => p is not null)
            .Select(p => new RateHistoryPointDto(p!.Value.Item1, p.Value.Item2))
            .ToList();
    }
}
