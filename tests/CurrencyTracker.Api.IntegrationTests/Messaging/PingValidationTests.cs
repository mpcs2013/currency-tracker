using CurrencyTracker.Application.Messaging;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace CurrencyTracker.Api.IntegrationTests.Messaging;

/// <summary>
/// Integration tests proving the Wolverine FluentValidation middleware
/// fires before the handler. Uses <see cref="WebApplicationFactory{TEntryPoint}"/>
/// to bootstrap the full Api host so the bus, the middleware, and the
/// validator are all wired exactly as production runs them.
/// </summary>
public sealed class PingValidationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Class-fixture-injected web application factory.</summary>
    public PingValidationTests(WebApplicationFactory<Program> factory)
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
