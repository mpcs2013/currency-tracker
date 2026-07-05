using CurrencyTracker.Domain.Alerts;
using CurrencyTracker.Infrastructure.Notifications;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CurrencyTracker.Infrastructure.UnitTests.Notifications;

/// <summary>
/// Behavioural tests for <see cref="LogAlertNotifier"/>: one structured
/// Information entry per dispatched alert, carrying the alert identity.
/// </summary>
public sealed class LogAlertNotifierTests
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

    [Fact]
    public async Task SendAsync_LogsExactlyOneInformationEntryCarryingTheAlertId()
    {
        // Arrange
        var logger = new CapturingLogger<LogAlertNotifier>();
        var notifier = new LogAlertNotifier(logger);
        var alert = CreateAlert();

        // Act
        await notifier.SendAsync(alert, TestContext.Current.CancellationToken);

        // Assert
        logger.Entries.Should().ContainSingle();
        logger.Entries[0].Level.Should().Be(LogLevel.Information);
        logger.Entries[0].Message.Should().Contain(alert.Id.ToString());
    }

    [Fact]
    public async Task SendAsync_CancelledToken_ThrowsAndLogsNothing()
    {
        // Arrange
        var logger = new CapturingLogger<LogAlertNotifier>();
        var notifier = new LogAlertNotifier(logger);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = () => notifier.SendAsync(CreateAlert(), cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        logger.Entries.Should().BeEmpty();
    }

    /// <summary>
    /// Minimal capturing <see cref="ILogger{TCategoryName}"/> for asserting
    /// on rendered log entries. Test-local by design — promote it to a
    /// shared helper only when a second test file needs it.
    /// </summary>
    private sealed class CapturingLogger<T> : ILogger<T>
    {
        private readonly List<(LogLevel Level, string Message)> _entries = [];

        public IReadOnlyList<(LogLevel Level, string Message)> Entries => _entries;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        ) => _entries.Add((logLevel, formatter(state, exception)));
    }
}
