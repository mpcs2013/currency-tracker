using CurrencyTracker.Domain.Alerts;
using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Rates;
using CurrencyTracker.Infrastructure.Alerts;
using CurrencyTracker.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;
using IDateTimeProvider = CurrencyTracker.Application.Abstractions.Time.IDateTimeProvider;

namespace CurrencyTracker.Infrastructure.UnitTests.Alerts;

/// <summary>
/// Behavioural tests for <see cref="EfAlertRuleEvaluator"/> over the EF
/// InMemory provider: the adapter fetches snapshots and rules, and the
/// domain (ShouldTrigger / Alert.Create) decides. LINQ-only queries, so
/// InMemory is representative; relational behaviour (the 12.9 unique
/// index) is covered by the integration tier.
/// </summary>
public sealed class EfAlertRuleEvaluatorTests
{
    private static readonly CurrencyCode Usd = CurrencyCode.Create("USD").Value;
    private static readonly CurrencyCode Eur = CurrencyCode.Create("EUR").Value;
    private static readonly CurrencyCode Gbp = CurrencyCode.Create("GBP").Value;
    private static readonly DateOnly Today = new(2026, 7, 4);
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 6, 0, 0, TimeSpan.Zero);

    private static ApplicationDbContext NewContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options
        );

    private static IDateTimeProvider FixedClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(Now);
        return clock;
    }

    private static async Task SeedSnapshotAsync(
        ApplicationDbContext ctx,
        DateOnly asOf,
        decimal eurRate
    )
    {
        var snapshot = RateSnapshot
            .Create(Usd, asOf, [ExchangeRate.Create(Usd, Eur, eurRate, asOf).Value])
            .Value;
        ctx.RateSnapshots.Add(snapshot);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<AlertRule> SeedRuleAsync(
        ApplicationDbContext ctx,
        decimal thresholdPercent,
        bool enabled = true
    )
    {
        var rule = AlertRule
            .Create(Guid.NewGuid(), Usd, Eur, thresholdPercent, AlertChannel.Email)
            .Value;
        if (!enabled)
        {
            rule.Disable();
        }

        ctx.AlertRules.Add(rule);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        return rule;
    }

    [Fact]
    public async Task EvaluateAsync_ChangeAboveThreshold_ReturnsOneAlertForTheRule()
    {
        // Arrange — 0.90 -> 0.92 is ~2.22%; threshold 1.0% fires.
        await using var ctx = NewContext();
        await SeedSnapshotAsync(ctx, Today.AddDays(-1), 0.90m);
        await SeedSnapshotAsync(ctx, Today, 0.92m);
        var rule = await SeedRuleAsync(ctx, thresholdPercent: 1.0m);
        var evaluator = new EfAlertRuleEvaluator(ctx, FixedClock());

        // Act
        var fired = await evaluator.EvaluateAsync(
            Usd,
            Today,
            TestContext.Current.CancellationToken
        );

        // Assert
        fired.Should().ContainSingle();
        fired[0].RuleId.Should().Be(rule.Id);
        fired[0].PreviousRate.Should().Be(0.90m);
        fired[0].CurrentRate.Should().Be(0.92m);
        fired[0].FiredAt.Should().Be(Now);
    }

    [Fact]
    public async Task EvaluateAsync_ChangeBelowThreshold_ReturnsEmpty()
    {
        // Arrange — ~2.22% change; threshold 5% does not fire.
        await using var ctx = NewContext();
        await SeedSnapshotAsync(ctx, Today.AddDays(-1), 0.90m);
        await SeedSnapshotAsync(ctx, Today, 0.92m);
        await SeedRuleAsync(ctx, thresholdPercent: 5.0m);
        var evaluator = new EfAlertRuleEvaluator(ctx, FixedClock());

        // Act
        var fired = await evaluator.EvaluateAsync(
            Usd,
            Today,
            TestContext.Current.CancellationToken
        );

        // Assert
        fired.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_DisabledRule_IsSkipped()
    {
        // Arrange — same movement as the firing case, but the rule is off.
        await using var ctx = NewContext();
        await SeedSnapshotAsync(ctx, Today.AddDays(-1), 0.90m);
        await SeedSnapshotAsync(ctx, Today, 0.92m);
        await SeedRuleAsync(ctx, thresholdPercent: 1.0m, enabled: false);
        var evaluator = new EfAlertRuleEvaluator(ctx, FixedClock());

        // Act
        var fired = await evaluator.EvaluateAsync(
            Usd,
            Today,
            TestContext.Current.CancellationToken
        );

        // Assert
        fired.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_MissingYesterdaySnapshot_ReturnsEmpty()
    {
        // Arrange — first-ever ingestion day: nothing to compare against.
        await using var ctx = NewContext();
        await SeedSnapshotAsync(ctx, Today, 0.92m);
        await SeedRuleAsync(ctx, thresholdPercent: 1.0m);
        var evaluator = new EfAlertRuleEvaluator(ctx, FixedClock());

        // Act
        var fired = await evaluator.EvaluateAsync(
            Usd,
            Today,
            TestContext.Current.CancellationToken
        );

        // Assert
        fired.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_RuleForQuoteMissingFromSnapshot_IsSkippedOthersStillFire()
    {
        // Arrange — snapshots carry EUR only; the GBP rule can't be
        // evaluated and must not sink the EUR rule.
        await using var ctx = NewContext();
        await SeedSnapshotAsync(ctx, Today.AddDays(-1), 0.90m);
        await SeedSnapshotAsync(ctx, Today, 0.92m);
        var eurRule = await SeedRuleAsync(ctx, thresholdPercent: 1.0m);
        ctx.AlertRules.Add(
            AlertRule.Create(Guid.NewGuid(), Usd, Gbp, 1.0m, AlertChannel.Email).Value
        );
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        var evaluator = new EfAlertRuleEvaluator(ctx, FixedClock());

        // Act
        var fired = await evaluator.EvaluateAsync(
            Usd,
            Today,
            TestContext.Current.CancellationToken
        );

        // Assert
        fired.Should().ContainSingle(a => a.RuleId == eurRule.Id);
    }
}
