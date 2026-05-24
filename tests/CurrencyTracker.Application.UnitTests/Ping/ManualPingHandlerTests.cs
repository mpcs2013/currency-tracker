using CurrencyTracker.Application.Abstractions.Security;
using CurrencyTracker.Application.Ping;
using CurrencyTracker.Application.UnitTests.Fakes;
using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Rates;

namespace CurrencyTracker.Application.UnitTests.Ping;

/// <summary>
/// Tests for <see cref="ManualPingHandler"/> — the deliberately
/// hand-wired four-port exercise that closes Phase 4 before Phase 5
/// introduces Wolverine.
/// </summary>
public sealed class ManualPingHandlerTests
{
    private static readonly CancellationToken NoCt = CancellationToken.None;
    private static readonly DateTimeOffset NowUtc = new(2026, 5, 21, 9, 0, 0, TimeSpan.Zero);
    private static readonly CurrencyCode Usd = CurrencyCode.Create("USD").Value;
    private static readonly CurrencyCode Eur = CurrencyCode.Create("EUR").Value;

    private static (
        FixedDateTimeProvider Clock,
        InMemoryExchangeRateProvider Provider,
        RecordingUnitOfWork UnitOfWork,
        ICurrentUser User,
        ManualPingHandler Handler
    ) Build(ICurrentUser? user = null)
    {
        var clock = new FixedDateTimeProvider(NowUtc);
        var provider = new InMemoryExchangeRateProvider();
        var uow = new RecordingUnitOfWork();
        var resolvedUser = user ?? FakeCurrentUser.WithRoles(Guid.NewGuid(), "user");
        var handler = new ManualPingHandler(clock, provider, uow, resolvedUser);
        return (clock, provider, uow, resolvedUser, handler);
    }

    [Fact]
    public async Task HandleAsync_returns_success_with_user_id_and_clock_value_when_provider_succeeds()
    {
        var (_, provider, _, user, handler) = Build();
        var asOf = DateOnly.FromDateTime(NowUtc.UtcDateTime);
        var snapshot = RateSnapshot
            .Create(Usd, asOf, [ExchangeRate.Create(Usd, Eur, 0.92m, asOf).Value])
            .Value;
        provider.Seed(Usd, asOf, snapshot);

        var actual = await handler.HandleAsync(new PingRequest("ping"), NoCt);

        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().Contain(user.UserId!.Value.ToString());
        actual.Value.Should().Contain("2026-05-21");
    }

    [Fact]
    public async Task HandleAsync_returns_failure_when_user_is_anonymous()
    {
        var (_, provider, _, _, handler) = Build(user: FakeCurrentUser.Anonymous);
        var asOf = DateOnly.FromDateTime(NowUtc.UtcDateTime);
        provider.Seed(
            Usd,
            asOf,
            RateSnapshot.Create(Usd, asOf, [ExchangeRate.Create(Usd, Eur, 0.92m, asOf).Value]).Value
        );

        var actual = await handler.HandleAsync(new PingRequest("ping"), NoCt);

        actual.IsFailure.Should().BeTrue();
        actual.Error.Code.Should().Be("PING_UNAUTHENTICATED");
    }

    [Fact]
    public async Task HandleAsync_returns_failure_when_provider_fails()
    {
        var (_, _, _, _, handler) = Build();
        // Provider has nothing seeded → returns PROVIDER_UNAVAILABLE.

        var actual = await handler.HandleAsync(new PingRequest("ping"), NoCt);

        actual.IsFailure.Should().BeTrue();
        actual.Error.Code.Should().Be("PROVIDER_UNAVAILABLE");
    }

    [Fact]
    public async Task HandleAsync_calls_SaveChangesAsync_exactly_once_on_success()
    {
        var (_, provider, uow, _, handler) = Build();
        var asOf = DateOnly.FromDateTime(NowUtc.UtcDateTime);
        provider.Seed(
            Usd,
            asOf,
            RateSnapshot.Create(Usd, asOf, [ExchangeRate.Create(Usd, Eur, 0.92m, asOf).Value]).Value
        );

        await handler.HandleAsync(new PingRequest("ping"), NoCt);

        uow.SaveCount.Should().Be(1);
    }
}
