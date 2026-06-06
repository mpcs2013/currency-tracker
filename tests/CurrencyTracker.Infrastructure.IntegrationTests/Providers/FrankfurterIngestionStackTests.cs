using CurrencyTracker.Application.Abstractions.Persistence;
using CurrencyTracker.Application.Abstractions.Providers;
using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Infrastructure.IntegrationTests.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;
using Xunit;

namespace CurrencyTracker.Infrastructure.IntegrationTests.Providers;

/// <summary>
/// Integration tests for the Frankfurter ingestion stack: a real
/// <c>ApplicationDbContext</c> against a Testcontainers Postgres, the real
/// typed client + adapter + resilience pipeline, with the external HTTP
/// server faked by WireMock.Net. The live Frankfurter API is never hit.
/// </summary>
public sealed class FrankfurterIngestionStackTests : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private static readonly CurrencyCode Usd = CurrencyCode.Create("USD").Value;
    private static readonly DateOnly AsOf = new(2026, 5, 28);

    private readonly PostgresFixture _postgres;
    private WireMockServer _wireMock = null!;
    private ServiceProvider _services = null!;

    public FrankfurterIngestionStackTests(PostgresFixture postgres) => _postgres = postgres;

    public ValueTask InitializeAsync()
    {
        _wireMock = WireMockServer.Start();                       // plain HTTP — reliable
        _services = IngestionStackHarness.Build(_postgres.ConnectionString, _wireMock.Url!);
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _wireMock.Stop();
        await _services.DisposeAsync();
    }

    [Fact]
    public async Task Ingestion_persists_snapshot_when_provider_returns_rates()
    {
        _wireMock
            .Given(Request.Create().UsingGet().WithPath($"/v1/{AsOf:yyyy-MM-dd}"))
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(
                        """{"amount":1.0,"base":"USD","date":"2026-05-28","rates":{"EUR":0.92,"GBP":0.79}}"""
                    )
            );

        await using var scope = _services.CreateAsyncScope();
        var provider = scope.ServiceProvider.GetRequiredService<IExchangeRateProvider>();
        var repository = scope.ServiceProvider.GetRequiredService<IExchangeRateRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var ct = TestContext.Current.CancellationToken;

        var fetched = await provider.FetchAsync(Usd, AsOf, ct);
        fetched.IsSuccess.Should().BeTrue();
        await repository.SaveSnapshotAsync(fetched.Value, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var reloaded = await repository.GetSnapshotAsync(Usd, AsOf, ct);
        reloaded.Should().NotBeNull();
        reloaded!.Rates.Should().HaveCount(2);
    }

    [Fact]
    public async Task Provider_retries_then_succeeds_on_transient_5xx()
    {
        const string scenario = "transient-5xx";

        _wireMock
            .Given(Request.Create().UsingGet().WithPath($"/v1/{AsOf:yyyy-MM-dd}"))
            .InScenario(scenario)
            .WillSetStateTo("recovered")
            .RespondWith(Response.Create().WithStatusCode(500));

        _wireMock
            .Given(Request.Create().UsingGet().WithPath($"/v1/{AsOf:yyyy-MM-dd}"))
            .InScenario(scenario)
            .WhenStateIs("recovered")
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(
                        """{"amount":1.0,"base":"USD","date":"2026-05-28","rates":{"EUR":0.92}}"""
                    )
            );

        await using var scope = _services.CreateAsyncScope();
        var provider = scope.ServiceProvider.GetRequiredService<IExchangeRateProvider>();
        var ct = TestContext.Current.CancellationToken;

        var fetched = await provider.FetchAsync(Usd, AsOf, ct);

        fetched.IsSuccess.Should().BeTrue();
        fetched.Value.Rates.Should().ContainSingle();
    }

    [Fact]
    public async Task Provider_returns_unsupported_currency_on_404()
    {
        _wireMock
            .Given(Request.Create().UsingGet().WithPath($"/v1/{AsOf:yyyy-MM-dd}"))
            .RespondWith(Response.Create().WithStatusCode(404));

        await using var scope = _services.CreateAsyncScope();
        var provider = scope.ServiceProvider.GetRequiredService<IExchangeRateProvider>();
        var ct = TestContext.Current.CancellationToken;

        var fetched = await provider.FetchAsync(Usd, AsOf, ct);

        fetched.IsSuccess.Should().BeFalse();
        fetched.Error.Code.Should().Be("PROVIDER_UNSUPPORTED_CURRENCY");
    }
}
