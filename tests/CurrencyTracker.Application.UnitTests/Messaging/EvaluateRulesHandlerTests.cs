using CurrencyTracker.Application.Messaging;
using CurrencyTracker.Application.UnitTests.Fakes;
using CurrencyTracker.Domain.Alerts;
using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Events;

namespace CurrencyTracker.Application.UnitTests.Messaging;

/// <summary>
/// Behavioural tests for <see cref="EvaluateRulesHandler"/>: fired alerts
/// are persisted and cascaded as <see cref="AlertTriggered"/> messages;
/// an empty evaluation cascades nothing and commits nothing.
/// </summary>
public sealed class EvaluateRulesHandlerTests
{
    private static readonly CurrencyCode Usd = CurrencyCode.Create("USD").Value;
    private static readonly DateOnly Today = new(2026, 7, 4);
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 6, 0, 0, TimeSpan.Zero);

    private static readonly DailyRatesIngested Event = new(Usd, Today, RateCount: 2);

    // Single construction seam — 12.9 changes Alert.Create's signature and
    // this is the only line that will need to follow it.
    private static Alert CreateAlert() =>
        Alert
            .Create(
                Guid.NewGuid(),
                asOfDate: Today,
                previousRate: 0.90m,
                currentRate: 0.92m,
                firedAt: Now
            )
            .Value;

    [Fact]
    public async Task Handle_TwoAlertsFired_CascadesOneAlertTriggeredPerAlertAndPersistsBoth()
    {
        // Arrange
        var first = CreateAlert();
        var second = CreateAlert();
        var evaluator = new InMemoryAlertRuleEvaluator { AlertsToReturn = [first, second] };
        var repository = new InMemoryAlertRepository();
        var unitOfWork = new InMemoryUnitOfWork();

        // Act
        var messages = await EvaluateRulesHandler.Handle(
            Event,
            evaluator,
            repository,
            unitOfWork,
            TestContext.Current.CancellationToken
        );

        // Assert
        repository.Alerts.Should().BeEquivalentTo([first, second]);
        unitOfWork.SaveCount.Should().Be(1);
        var triggered = messages.OfType<AlertTriggered>().ToList();
        triggered.Should().HaveCount(2);
        triggered
            .Should()
            .ContainSingle(m =>
                m.AlertId == first.Id
                && m.RuleId == first.RuleId
                && m.ObservedChangePercent == first.ObservedChangePercent
                && m.FiredAt == first.FiredAt
            );
        triggered.Should().ContainSingle(m => m.AlertId == second.Id);
    }

    [Fact]
    public async Task Handle_NoAlertsFired_CascadesNothingAndDoesNotCommit()
    {
        // Arrange
        var evaluator = new InMemoryAlertRuleEvaluator();
        var repository = new InMemoryAlertRepository();
        var unitOfWork = new InMemoryUnitOfWork();

        // Act
        var messages = await EvaluateRulesHandler.Handle(
            Event,
            evaluator,
            repository,
            unitOfWork,
            TestContext.Current.CancellationToken
        );

        // Assert
        messages.Should().BeEmpty();
        repository.Alerts.Should().BeEmpty();
        unitOfWork.SaveCount.Should().Be(0);
    }
}
