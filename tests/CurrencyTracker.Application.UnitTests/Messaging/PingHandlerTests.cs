using CurrencyTracker.Application.Messaging;

namespace CurrencyTracker.Application.UnitTests.Messaging;

/// <summary>
/// Unit tests for <see cref="PingHandler"/>. The handler is a static
/// method that returns <c>"pong"</c> for every <see cref="PingQuery"/>;
/// the tests call it directly without a Wolverine host, demonstrating
/// the property the static-method form was chosen for.
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
