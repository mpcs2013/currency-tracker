using CurrencyTracker.Infrastructure;
using CurrencyTracker.Infrastructure.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CurrencyTracker.Infrastructure.IntegrationTests.Providers;

/// <summary>
/// Builds a real Infrastructure service provider for the Phase 9.9
/// ingestion-stack integration test: the full <c>AddInfrastructure</c>
/// wiring (typed <c>FrankfurterClient</c> + resilience pipeline +
/// <c>FrankfurterExchangeRateProvider</c> + EF Core repositories) pointed
/// at a Testcontainers Postgres connection string and a (plain-HTTP)
/// WireMock server.
/// </summary>
/// <remarks>
/// Two test-only adjustments are made here:
/// <list type="bullet">
/// <item><c>AddInfrastructure</c> is an extension on
/// <see cref="IHostApplicationBuilder"/>, so a <see cref="HostApplicationBuilder"/>
/// is created to host the registrations; the provider is built from its
/// <c>Services</c> without ever starting a host.</item>
/// <item>The production <c>FrankfurterOptions</c> validation requires an
/// <c>https</c> base URL. WireMock serves plain HTTP (its HTTPS support is
/// unreliable in tests), so the options validators are removed in this
/// harness — the https-only rule is a production control verified by 9.1's
/// own boot check, not 9.9's concern. TEST-ONLY: never do this in a host.</item>
/// </list>
/// </remarks>
internal static class IngestionStackHarness
{
    /// <summary>
    /// Builds the service provider for the ingestion stack.
    /// </summary>
    /// <param name="connectionString">Testcontainers Postgres connection
    /// string (from <c>PostgresFixture</c>, already migrated).</param>
    /// <param name="frankfurterBaseUrl">The WireMock server's HTTP URL.</param>
    /// <returns>A built <see cref="ServiceProvider"/>; dispose it when done.</returns>
    public static ServiceProvider Build(string connectionString, string frankfurterBaseUrl)
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:currencytracker"] = connectionString,
                ["Frankfurter:BaseUrl"] = frankfurterBaseUrl,
                ["Frankfurter:Timeout"] = "00:00:10",
                ["Frankfurter:UserAgent"] = "CurrencyTracker-IntegrationTests/1.0",
            }
        );

        builder.AddInfrastructure();

        // TEST-ONLY: drop the FrankfurterOptions validators so the harness can
        // point the client at a plain-HTTP WireMock URL. The https-only rule
        // is a production control exercised by 9.1's boot check; 9.9 tests the
        // stack behaviour, not the config validation.
        builder.Services.RemoveAll<IValidateOptions<FrankfurterOptions>>();

        return builder.Services.BuildServiceProvider();
    }
}
