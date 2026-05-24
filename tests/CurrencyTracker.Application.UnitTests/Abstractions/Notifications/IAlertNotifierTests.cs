using CurrencyTracker.Application.Abstractions.Notifications;
using CurrencyTracker.Application.UnitTests.Fakes;
using CurrencyTracker.Domain.Alerts;

namespace CurrencyTracker.Application.UnitTests.Abstractions.Notifications;

/// <summary>
/// Tests exercising the <see cref="IAlertNotifier"/> contract via the
/// <see cref="InMemoryAlertNotifier"/> fake: single-alert recording,
/// ordering of multiple alerts, and cancellation propagation.
/// </summary>
public sealed class IAlertNotifierTests
{
    private static readonly DateTimeOffset FiredAt = new(2026, 5, 24, 0, 0, 0, TimeSpan.Zero);

    private static Alert MakeAlert() => Alert.Create(Guid.NewGuid(), 1.00m, 1.05m, FiredAt).Value;

    [Fact]
    public async Task SendAsync_records_single_alert()
    {
        var alert = MakeAlert();
        var sut = new InMemoryAlertNotifier();

        await sut.SendAsync(alert, CancellationToken.None);

        sut.SentAlerts.Should().ContainSingle().Which.Should().BeSameAs(alert);
    }

    [Fact]
    public async Task SendAsync_records_multiple_alerts_in_order()
    {
        var first = MakeAlert();
        var second = MakeAlert();
        var sut = new InMemoryAlertNotifier();

        await sut.SendAsync(first, CancellationToken.None);
        await sut.SendAsync(second, CancellationToken.None);

        sut.SentAlerts.Should().HaveCount(2);
        sut.SentAlerts[0].Should().BeSameAs(first);
        sut.SentAlerts[1].Should().BeSameAs(second);
    }

    [Fact]
    public async Task SendAsync_honours_cancellation_before_recording()
    {
        var alert = MakeAlert();
        var sut = new InMemoryAlertNotifier();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await sut.SendAsync(alert, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        sut.SentAlerts.Should().BeEmpty();
    }
}
