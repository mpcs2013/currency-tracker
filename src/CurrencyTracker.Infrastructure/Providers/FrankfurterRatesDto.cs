using System.Text.Json.Serialization;

namespace CurrencyTracker.Infrastructure.Providers;

/// <summary>
/// Wire-format DTO mirroring the Frankfurter <c>/v1/{date}</c> response
/// exactly: a base currency, an observation date, and a map of quote
/// currency code to rate. Deliberately <c>internal</c> so the external
/// API's shape never crosses the anti-corruption boundary into
/// Application — the &lt;see cref="FrankfurterExchangeRateProvider"/&gt;
/// adapter (issue 9.4) is the only consumer, and it translates this
/// into the domain <c>RateSnapshot</c>.
/// </summary>
/// <param name="Base">Base currency code as returned by the provider.</param>
/// <param name="Date">Observation date the provider resolved the request to.</param>
/// <param name="Rates">Quote-code → rate map. The provider omits the base
/// currency from this map.</param>
internal sealed record FrankfurterRatesDto(
    [property: JsonPropertyName("base")] string Base,
    [property: JsonPropertyName("date")] DateOnly Date,
    [property: JsonPropertyName("rates")] IReadOnlyDictionary<string, decimal> Rates
);
