using CurrencyTracker.ServiceDefaults.Logging;
using FluentAssertions;
using Serilog;
using Serilog.Events;
using Xunit;

namespace CurrencyTracker.ServiceDefaults.UnitTests.Logging;

/// <summary>
/// Drives a real Serilog pipeline (enricher under test + collecting
/// sink) and asserts on the post-enrichment event, i.e. exactly what
/// every configured sink would receive.
/// </summary>
public sealed class RedactingEnricherTests
{
    private readonly CollectingSink _sink = new();
    private readonly ILogger _logger;

    public RedactingEnricherTests()
    {
        _logger = new LoggerConfiguration()
            .Enrich.With(new RedactingEnricher())
            .WriteTo.Sink(_sink)
            .CreateLogger();
    }

    private string PropertyValue(string name)
    {
        var scalar = (ScalarValue)_sink.Events.Single().Properties[name];
        return (string)scalar.Value!;
    }

    [Fact]
    public async Task Property_named_like_a_secret_is_fully_redacted()
    {
        // Arrange
        const string secret = "hunter2-super-secret";

        // Act
        _logger.Information("Configured client with {ApiKey}", secret);

        // Assert
        PropertyValue("ApiKey").Should().Be("[REDACTED]");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Email_inside_a_value_is_redacted_in_place()
    {
        // Arrange
        const string detail = "user alice@example.com asked for EUR";

        // Act
        _logger.Information("Request detail {Detail}", detail);

        // Assert
        PropertyValue("Detail").Should().Be("user [REDACTED] asked for EUR");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Jwt_inside_a_value_is_redacted()
    {
        // Arrange
        const string detail =
            "presented eyJhbGciOiJSUzI1NiJ9.eyJzdWIiOiJhbGljZSJ9.sig-part_0k and left";

        // Act
        _logger.Information("Auth detail {Detail}", detail);

        // Assert
        PropertyValue("Detail").Should().Be("presented [REDACTED] and left");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Bearer_header_value_is_redacted()
    {
        // Arrange
        const string detail = "header was Bearer abc.def-ghi_jkl";

        // Act
        _logger.Information("Header detail {Detail}", detail);

        // Assert
        PropertyValue("Detail").Should().Be("header was [REDACTED]");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Domain_shaped_values_survive_untouched()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        _logger.Information(
            "Ingested {Base} on {AsOf} for rule {RuleId}",
            "USD",
            new DateOnly(2026, 7, 9),
            id
        );

        // Assert
        var properties = _sink.Events.Single().Properties;
        ((ScalarValue)properties["Base"]).Value.Should().Be("USD");
        ((ScalarValue)properties["AsOf"]).Value.Should().Be(new DateOnly(2026, 7, 9));
        ((ScalarValue)properties["RuleId"]).Value.Should().Be(id);
    }
}
