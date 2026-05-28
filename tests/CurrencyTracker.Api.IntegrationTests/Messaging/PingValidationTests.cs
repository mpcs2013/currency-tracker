using CurrencyTracker.Api.IntegrationTests.Errors;
using CurrencyTracker.Application.Messaging;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace CurrencyTracker.Api.IntegrationTests.Messaging;

/// <summary>
/// Integration tests proving the Wolverine FluentValidation middleware
/// fires before the handler. Uses <see cref="TestThrowsFactory"/> — the
/// shared <c>WebApplicationFactory&lt;Program&gt;</c> subclass that
/// supplies a placeholder <c>currencytracker</c> connection string so
/// the Phase 8.4 <c>AddInfrastructure</c> call can build without a real
/// Postgres. The bus, middleware, and validator are wired exactly as
/// production runs them.
/// </summary>
public sealed class PingValidationTests : IClassFixture<TestThrowsFactory>
{
    private readonly TestThrowsFactory _factory;

    /// <summary>Class-fixture-injected web application factory.</summary>
    public PingValidationTests(TestThrowsFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Invalid_ping_message_throws_ValidationException_through_the_bus()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var query = new PingQuery(Message: new string('x', 101));

        // Act
        Func<Task> act = async () => await bus.InvokeAsync<string>(query);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }
}
