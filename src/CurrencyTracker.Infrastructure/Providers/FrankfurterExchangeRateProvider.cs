using System.Net;
using CurrencyTracker.Application.Abstractions.Providers;
using CurrencyTracker.Domain.Common;
using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Rates;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace CurrencyTracker.Infrastructure.Providers;

/// <summary>
/// Anti-corruption adapter implementing <see cref="IExchangeRateProvider"/>
/// over the Frankfurter API. Translates the wire DTO
/// (<see cref="FrankfurterRatesDto"/>) into a domain
/// <see cref="RateSnapshot"/> and translates HTTP / resilience failures
/// into <see cref="Result{T}"/> failures carrying the Phase 4 failure
/// codes. No HTTP type and no Frankfurter DTO crosses back to the caller.
/// </summary>
internal sealed partial class FrankfurterExchangeRateProvider : IExchangeRateProvider
{
    private readonly FrankfurterClient _client;
    private readonly ILogger<FrankfurterExchangeRateProvider> _logger;

    /// <summary>
    /// Initialises a new instance of
    /// <see cref="FrankfurterExchangeRateProvider"/>.
    /// </summary>
    /// <param name="client">The typed Frankfurter HTTP client.</param>
    /// <param name="logger">Structured logger.</param>
    public FrankfurterExchangeRateProvider(
        FrankfurterClient client,
        ILogger<FrankfurterExchangeRateProvider> logger
    )
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<RateSnapshot>> FetchAsync(
        CurrencyCode baseCurrency,
        DateOnly asOf,
        CancellationToken cancellationToken
    )
    {
        FrankfurterRatesDto? dto;

        try
        {
            dto = await _client.GetRatesAsync(baseCurrency, asOf, cancellationToken);
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode
                    is >= HttpStatusCode.BadRequest
                        and < HttpStatusCode.InternalServerError
            )
        {
            LogUnsupportedCurrency(baseCurrency.Value, asOf, ex);
            return Result<RateSnapshot>.Failure(
                new DomainError(
                    "PROVIDER_UNSUPPORTED_CURRENCY",
                    $"Frankfurter has no rates for {baseCurrency.Value} on {asOf:yyyy-MM-dd}."
                )
            );
        }
        catch (Exception ex)
            when (ex is HttpRequestException or TimeoutRejectedException or BrokenCircuitException)
        {
            LogProviderUnavailable(baseCurrency.Value, asOf, ex);
            return Result<RateSnapshot>.Failure(
                new DomainError(
                    "PROVIDER_UNAVAILABLE",
                    $"Frankfurter is unavailable for {baseCurrency.Value} on {asOf:yyyy-MM-dd}."
                )
            );
        }

        if (dto is null)
        {
            LogProviderUnavailable(baseCurrency.Value, asOf, exception: null);
            return Result<RateSnapshot>.Failure(
                new DomainError(
                    "PROVIDER_UNAVAILABLE",
                    $"Frankfurter returned an empty body for {baseCurrency.Value} on {asOf:yyyy-MM-dd}."
                )
            );
        }

        var rates = new List<ExchangeRate>(dto.Rates.Count);

        foreach (var (quoteCode, rateValue) in dto.Rates)
        {
            var quote = CurrencyCode.Create(quoteCode);
            if (!quote.IsSuccess)
            {
                // A currency Frankfurter knows but our domain doesn't model.
                // Skip it rather than failing the whole snapshot.
                continue;
            }

            var rate = ExchangeRate.Create(baseCurrency, quote.Value, rateValue, asOf);
            if (rate.IsSuccess)
            {
                rates.Add(rate.Value);
            }
        }

        return RateSnapshot.Create(baseCurrency, asOf, rates);
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Frankfurter returned an unsupported-currency response for {BaseCurrency} on {AsOf}."
    )]
    private partial void LogUnsupportedCurrency(
        string baseCurrency,
        DateOnly asOf,
        Exception exception
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Frankfurter was unavailable for {BaseCurrency} on {AsOf}."
    )]
    private partial void LogProviderUnavailable(
        string baseCurrency,
        DateOnly asOf,
        Exception? exception
    );
}
