using CurrencyTracker.Application.Messaging;

namespace CurrencyTracker.Application.UnitTests.Messaging;

/// <summary>
/// Tests for <see cref="PingHandler"/> — the static Wolverine handler
/// introduced in Phase 5. Called directly without a Wolverine host.
/// </summary>
public sealed class PingHandlerTests
{
    [Fact]
    public void Handle_returns_pong()
    {
        var query = new PingQuery();

        var result = PingHandler.Handle(query);

        result.Should().Be("pong");
    }
}
