using System.Net.Http.Json;
using CurrencyTracker.Domain.Currencies;

namespace CurrencyTracker.Infrastructure.Providers;

/// <summary>
/// Typed <see cref="HttpClient"/> wrapper over the Frankfurter API.
/// Registered via <c>AddHttpClient&lt;FrankfurterClient&gt;</c> in
/// <c>AddInfrastructure</c>, so the injected <see cref="HttpClient"/>
/// arrives pre-configured with the base address, timeout, and
/// <c>User-Agent</c> from <see cref="FrankfurterOptions"/>. This class
/// only performs the HTTP call and deserialisation; it does not map to
/// domain types (that is the &lt;see cref="FrankfurterExchangeRateProvider"/&gt;
/// adapter's job) and it does not translate exceptions (the adapter does
/// that too).
/// </summary>
internal sealed class FrankfurterClient
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initialises a new instance of <see cref="FrankfurterClient"/>.
    /// </summary>
    /// <param name="httpClient">The factory-configured HTTP client.</param>
    public FrankfurterClient(HttpClient httpClient) => _httpClient = httpClient;

    /// <summary>
    /// Fetches the rate snapshot for <paramref name="baseCurrency"/> on
    /// <paramref name="asOf"/> from the Frankfurter dated endpoint.
    /// </summary>
    /// <param name="baseCurrency">Base currency of the requested snapshot.</param>
    /// <param name="asOf">Calendar date of the requested snapshot.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    /// <returns>The deserialised wire DTO.</returns>
    /// <exception cref="HttpRequestException">Thrown on a non-success
    /// status code or a transport failure. The adapter translates these
    /// into <c>Result.Failure</c> at the anti-corruption boundary.</exception>
    public async Task<FrankfurterRatesDto?> GetRatesAsync(
        CurrencyCode baseCurrency,
        DateOnly asOf,
        CancellationToken cancellationToken
    )
    {
        var path = $"/v1/{asOf:yyyy-MM-dd}?base={baseCurrency.Value}";

        using var response = await _httpClient.GetAsync(path, cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<FrankfurterRatesDto>(cancellationToken);
    }
}
