using CurrencyTracker.Application.Abstractions.Providers;
using CurrencyTracker.Domain.Common;
using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Rates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CurrencyTracker.Api.IntegrationTests.Auth;

/// <summary>
/// Test-host plumbing that replaces the live Frankfurter
/// <see cref="IExchangeRateProvider"/> with a stub that always fails. This
/// keeps the Api integration suite hermetic — no outbound network I/O — and,
/// crucially, stops the admin-ingest gate tests from persisting a real,
/// newer snapshot into the shared Testcontainers database. Without it, an
/// ingest of USD (asOf in the past) writes a snapshot that outranks a
/// sibling test's seeded row in <c>GetLatestSnapshotAsync</c> (order by AsOf
/// descending), turning the latest-rates assertion order-dependent and flaky.
/// The admin-gate tests only assert the request clears authn/authz (not 401,
/// not 403); a provider failure yields a 5xx, which satisfies that contract.
/// </summary>
public static class StubExchangeRateProviderHost
{
    /// <summary>
    /// Swaps the registered <see cref="IExchangeRateProvider"/> for
    /// <see cref="AlwaysFailsExchangeRateProvider"/> in the test host, so
    /// <c>/admin/ingest</c> never reaches the network or writes to the DB.
    /// </summary>
    /// <param name="builder">The Alba/WebApplicationFactory web host builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IWebHostBuilder UseStubExchangeRateProvider(this IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IExchangeRateProvider>();
            services.AddScoped<IExchangeRateProvider, AlwaysFailsExchangeRateProvider>();
        });

    /// <summary>
    /// <see cref="IExchangeRateProvider"/> stub whose every fetch returns a
    /// <c>PROVIDER_UNAVAILABLE</c> failure, so no ingestion ever commits a
    /// snapshot in the test host.
    /// </summary>
    private sealed class AlwaysFailsExchangeRateProvider : IExchangeRateProvider
    {
        public Task<Result<RateSnapshot>> FetchAsync(
            CurrencyCode baseCurrency,
            DateOnly asOf,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(
                Result<RateSnapshot>.Failure(
                    new DomainError(
                        "PROVIDER_UNAVAILABLE",
                        "Stubbed provider: the Api integration host performs no live ingestion."
                    )
                )
            );
    }
}
