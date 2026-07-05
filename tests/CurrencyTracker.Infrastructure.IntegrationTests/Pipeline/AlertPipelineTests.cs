using CurrencyTracker.Application.Messaging;
using CurrencyTracker.Domain.Alerts;
using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Rates;
using CurrencyTracker.Infrastructure.IntegrationTests.Persistence;
using CurrencyTracker.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Wolverine.Tracking;
using Xunit;

namespace CurrencyTracker.Infrastructure.IntegrationTests.Pipeline;

/// <summary>
/// End-to-end pipeline test over a started Worker-shaped Wolverine host:
/// ingest → evaluate → dispatch, with the Postgres outbox and the 12.9
/// idempotency key both live. Uses Wolverine's tracked-session API so the
/// asynchronous cascade is awaited deterministically — no sleeps.
/// </summary>
public sealed class AlertPipelineTests : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private static readonly CurrencyCode Usd = CurrencyCode.Create("USD").Value;
    private static readonly CurrencyCode Eur = CurrencyCode.Create("EUR").Value;

    private readonly PostgresFixture _postgres;
    private WireMockServer _frankfurter = null!;

    public AlertPipelineTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        _frankfurter = WireMockServer.Start();
        // Shared container across the class — start each test from a clean DB
        // so seeds and row-count assertions don't collide with a sibling test.
        await _postgres.ResetAsync();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _frankfurter.Stop();
        return ValueTask.CompletedTask;
    }

    private void StubFrankfurter(DateOnly asOf, decimal eurRate)
    {
        _frankfurter
            .Given(Request.Create().WithPath($"/v1/{asOf:yyyy-MM-dd}").UsingGet())
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(
                        $$$"""
                        {"amount":1.0,"base":"USD","date":"{{{asOf:yyyy-MM-dd}}}","rates":{"EUR":{{{eurRate}}}}}
                        """
                    )
            );
    }

    private static async Task SeedAsync(ApplicationDbContext ctx, DateOnly yesterday, Guid ownerId)
    {
        ctx.AlertRules.Add(AlertRule.Create(ownerId, Usd, Eur, 1.0m, AlertChannel.Email).Value);
        ctx.RateSnapshots.Add(
            RateSnapshot
                .Create(Usd, yesterday, [ExchangeRate.Create(Usd, Eur, 0.90m, yesterday).Value])
                .Value
        );
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Ingest_MovementAboveThreshold_PersistsOneAlertAndDispatchesOnce()
    {
        // Arrange — dates at runtime: the command validator rejects future AsOf.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);
        StubFrankfurter(today, 0.92m); // 0.90 -> 0.92 ≈ 2.2% > 1.0% threshold

        using var host = await AlertPipelineHarness.StartAsync(
            _postgres.ConnectionString,
            _frankfurter.Url!
        );
        using (var scope = host.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await SeedAsync(ctx, yesterday, ownerId: Guid.NewGuid());
        }

        var notifier = host.Services.GetRequiredService<RecordingAlertNotifier>();

        // Act — invoke and wait for the WHOLE tracked cascade.
        await host.TrackActivity()
            .Timeout(TimeSpan.FromSeconds(30))
            .InvokeMessageAndWaitAsync(new IngestDailyRatesCommand("USD", today));

        // Assert — a row in the database and exactly one dispatch.
        using var assertScope = host.Services.CreateScope();
        var assertCtx = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var alerts = await assertCtx
            .Alerts.AsNoTracking()
            .ToListAsync(TestContext.Current.CancellationToken);
        alerts.Should().ContainSingle();
        alerts[0].AsOfDate.Should().Be(today);
        notifier.SentAlerts.Should().ContainSingle().Which.Id.Should().Be(alerts[0].Id);

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Ingest_SameDayTwice_StillOneAlertAndOneDispatch()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);
        StubFrankfurter(today, 0.92m);

        using var host = await AlertPipelineHarness.StartAsync(
            _postgres.ConnectionString,
            _frankfurter.Url!
        );
        Guid ruleOwner = Guid.NewGuid();
        using (var scope = host.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await SeedAsync(ctx, yesterday, ruleOwner);
        }

        var notifier = host.Services.GetRequiredService<RecordingAlertNotifier>();
        var command = new IngestDailyRatesCommand("USD", today);

        // Act — the operator's double-POST / crash-replay shape.
        await host.TrackActivity()
            .Timeout(TimeSpan.FromSeconds(30))
            .InvokeMessageAndWaitAsync(command);
        await host.TrackActivity()
            .Timeout(TimeSpan.FromSeconds(30))
            .InvokeMessageAndWaitAsync(command);

        // Assert — 12.9 held: no duplicate row, no duplicate dispatch.
        using var assertScope = host.Services.CreateScope();
        var assertCtx = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        (await assertCtx.Alerts.CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
        notifier.SentAlerts.Should().ContainSingle();

        await host.StopAsync(TestContext.Current.CancellationToken);
    }
}
