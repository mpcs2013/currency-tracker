using CurrencyTracker.Api.IntegrationTests.Errors;
using CurrencyTracker.Application.Messaging;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace CurrencyTracker.Api.IntegrationTests.Endpoints;

/// <summary>
/// Proves the FluentValidation middleware short-circuits an invalid
/// <see cref="IngestDailyRatesCommand"/> before the handler runs. Uses
/// <see cref="TestThrowsFactory"/> (the "Testing" environment, so the
/// dev-only MigrationRunner never registers and no Postgres is needed).
/// The validation-to-ProblemDetails HTTP mapping is covered generically
/// by ProblemDetailsContractTests; here we only assert the validator fires.
/// </summary>
public sealed class AdminIngestEndpointTests : IClassFixture<TestThrowsFactory>
{
    private readonly TestThrowsFactory _factory;

    public AdminIngestEndpointTests(TestThrowsFactory factory) => _factory = factory;

    [Fact]
    public async Task Invalid_command_short_circuits_via_validation_middleware()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var command = new IngestDailyRatesCommand(Base: "", AsOf: default);

        // Act
        Func<Task> act = async () => await bus.InvokeAsync(command);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }
}
