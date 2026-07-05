using CurrencyTracker.Application.Exceptions;
using CurrencyTracker.Application.Messaging;
using CurrencyTracker.Application.UnitTests.Fakes;
using CurrencyTracker.Domain.Alerts;
using CurrencyTracker.Domain.Events;

namespace CurrencyTracker.Application.UnitTests.Messaging;

/// <summary>
/// Behavioural tests for <see cref="DispatchAlertHandler"/>: the persisted
/// alert is reloaded and handed to the notifier; a dangling AlertId is a
/// loud <see cref="NotFoundException"/>.
/// </summary>
public sealed class DispatchAlertHandlerTests
{
    private static Alert CreateAlert() =>
        Alert
            .Create(
                Guid.NewGuid(),
                asOfDate: new DateOnly(2026, 7, 4),
                previousRate: 0.90m,
                currentRate: 0.92m,
                firedAt: new DateTimeOffset(2026, 7, 4, 6, 0, 0, TimeSpan.Zero)
            )
            .Value;

    private static AlertTriggered EventFor(Alert alert) =>
        new(alert.Id, alert.RuleId, alert.ObservedChangePercent, alert.FiredAt);

    [Fact]
    public async Task Handle_AlertExists_SendsExactlyThatAlert()
    {
        // Arrange
        var alert = CreateAlert();
        var repository = new InMemoryAlertRepository();
        await repository.AddAsync(alert, TestContext.Current.CancellationToken);
        var notifier = new InMemoryAlertNotifier();

        // Act
        await DispatchAlertHandler.Handle(
            EventFor(alert),
            repository,
            notifier,
            TestContext.Current.CancellationToken
        );

        // Assert
        notifier.SentAlerts.Should().ContainSingle().Which.Should().Be(alert);
    }

    [Fact]
    public async Task Handle_AlertMissing_ThrowsNotFoundAndSendsNothing()
    {
        // Arrange
        var repository = new InMemoryAlertRepository();
        var notifier = new InMemoryAlertNotifier();
        var @event = EventFor(CreateAlert()); // never added to the repository

        // Act
        var act = () =>
            DispatchAlertHandler.Handle(
                @event,
                repository,
                notifier,
                TestContext.Current.CancellationToken
            );

        // Assert
        await act.Should()
            .ThrowAsync<NotFoundException>()
            .Where(e => e.Resource == "Alert" && e.Key == @event.AlertId.ToString());
        notifier.SentAlerts.Should().BeEmpty();
    }
}
