using CurrencyTracker.Application.Abstractions.Caching;
using CurrencyTracker.Application.Abstractions.Persistence;
using CurrencyTracker.Application.Caching;
using CurrencyTracker.Application.Exceptions;
using CurrencyTracker.Domain.Currencies;
using FluentValidation;
using Microsoft.AspNetCore.Mvc; // [FromQuery]
using Wolverine.Http; // [WolverineGet]

namespace CurrencyTracker.Application.Messaging;

/// <summary>
/// Query-bound request for the latest-rates endpoint. Wolverine binds a
/// [FromQuery] complex type by matching each public settable property to a
/// query-string key by NAME (case-insensitive) and ignores
/// [FromQuery(Name = …)] (Wolverine #641). Hence a settable, nullable
/// property named Base (binds ?base=) with a no-arg ctor — the shape
/// Wolverine recommends for a bag of optional query values.
/// </summary>
public sealed class LatestRatesRequest
{
    public string? Base { get; set; }
}

public sealed class GetLatestRatesHandler(
    IExchangeRateRepository repository,
    ICacheService cache,
    IValidator<GetLatestRatesQuery> validator
)
{
    private static readonly TimeSpan LatestRatesTtl = TimeSpan.FromMinutes(5);

    [WolverineGet("/api/v1/rates/latest")]
    public async Task<IReadOnlyList<ExchangeRateDto>> Handle(
        [FromQuery] LatestRatesRequest request,
        CancellationToken cancellationToken
    )
    {
        var query = new GetLatestRatesQuery(request.Base ?? string.Empty);
        await validator.ValidateAndThrowAsync(query, cancellationToken);
        return await Handle(query, cancellationToken);
    }

    public Task<IReadOnlyList<ExchangeRateDto>> Handle(
        GetLatestRatesQuery query,
        CancellationToken cancellationToken
    ) =>
        cache.GetOrSetAsync(
            CacheKeys.LatestRates(query.Base),
            async token =>
            {
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
