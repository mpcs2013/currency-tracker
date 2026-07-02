using CurrencyTracker.Application.Messaging;
using CurrencyTracker.Worker.Configuration;
using CurrencyTracker.Worker.Scheduling;
using Microsoft.Extensions.Options;
using NSubstitute;
using Wolverine;

namespace CurrencyTracker.Worker.UnitTests.Scheduling;

/// <summary>
/// Tests for <see cref="DailyIngestionScheduleJob"/>: it publishes one
/// <see cref="IngestDailyRatesCommand"/> per configured base for the clock's
/// current UTC date, and honours cancellation.
/// </summary>
public sealed class DailyIngestionScheduleJobTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 21, 6, 0, 0, TimeSpan.Zero);

    private static DailyIngestionScheduleJob CreateSut(IMessageBus bus, params string[] bases)
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var options = Options.Create(new WorkerOptions { IngestBases = bases });

        return new DailyIngestionScheduleJob(clock, options);
    }

    [Fact]
    public async Task ExecuteAsync_publishes_one_command_per_base_for_today()
    {
        var bus = Substitute.For<IMessageBus>();
        var sut = CreateSut(bus, "USD", "EUR");

        await sut.ExecuteAsync(bus, CancellationToken.None);

        await bus.Received(1)
            .PublishAsync(
                Arg.Is<IngestDailyRatesCommand>(c =>
                    c.BaseCurrency == "USD" && c.AsOf == new DateOnly(2026, 6, 21)
                )
            );
        await bus.Received(1)
            .PublishAsync(
                Arg.Is<IngestDailyRatesCommand>(c =>
                    c.BaseCurrency == "EUR" && c.AsOf == new DateOnly(2026, 6, 21)
                )
            );
    }

    [Fact]
    public async Task ExecuteAsync_honours_cancellation_before_publishing()
    {
        var bus = Substitute.For<IMessageBus>();
        var sut = CreateSut(bus, "USD");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await sut.ExecuteAsync(bus, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        await bus.DidNotReceiveWithAnyArgs().PublishAsync(default(IngestDailyRatesCommand)!);
    }
}
