using System.Text;
using System.Text.Json;
using CurrencyTracker.Application.Exceptions;
using CurrencyTracker.Application.Messaging;
using CurrencyTracker.Domain.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Wolverine;

namespace CurrencyTracker.Api.IntegrationTests.Errors;

public sealed class ProblemDetailsContractTests : IClassFixture<TestThrowsFactory>
{
    private readonly TestThrowsFactory _factory;

    public ProblemDetailsContractTests(TestThrowsFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Invalid_ping_returns_400_with_validation_problemdetails_shape()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        var payload = new StringContent(
            """{"message":"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"}""",
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await client.PostAsync("/ping-via-bus", payload, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;
        var errors = root.GetProperty("errors");
        var messageErrors = errors.GetProperty("Message").EnumerateArray().ToArray();

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        root.GetProperty("status").GetInt32().Should().Be(400);
        root.GetProperty("title").GetString().Should().Be("Validation failed");
        root.GetProperty("type")
            .GetString()
            .Should()
            .StartWith("https://tools.ietf.org/html/rfc9110");
        root.GetProperty("detail").GetString().Should().NotBeNullOrEmpty();
        root.GetProperty("instance").GetString().Should().StartWith("POST ");
        root.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
        messageErrors.Should().HaveCount(1);
        messageErrors[0].GetString().Should().Be("Message must be 100 characters or fewer.");
    }

    [Fact]
    public async Task NotFoundException_returns_404_with_resource_and_key_extensions()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/throws-notfound", cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        root.GetProperty("status").GetInt32().Should().Be(404);
        root.GetProperty("title").GetString().Should().Be("Resource not found");
        root.GetProperty("resource").GetString().Should().Be("ExchangeRate");
        root.GetProperty("key").GetString().Should().Be("USD/EUR/2026-05-21");
        root.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DomainException_returns_422_with_message_in_detail()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/throws-domain", cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.UnprocessableEntity);
        root.GetProperty("status").GetInt32().Should().Be(422);
        root.GetProperty("title").GetString().Should().Be("Domain rule violation");
        root.GetProperty("detail").GetString().Should().Be("test domain failure");
        root.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Generic_exception_returns_500_without_stack_trace_in_production()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var productionFactory = new TestThrowsFactory("Production");
        var client = productionFactory.CreateClient();

        // Act
        var response = await client.GetAsync("/throws-generic", cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.InternalServerError);
        root.GetProperty("status").GetInt32().Should().Be(500);
        root.GetProperty("detail").GetString().Should().Be("An unexpected error occurred.");
        root.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
        body.Should().NotContain("at CurrencyTracker.Api.IntegrationTests");
        body.Should().NotContain("secret leaked");
    }
}

public sealed class TestThrowsFactory : WebApplicationFactory<Program>
{
    private readonly string _environment;

    public TestThrowsFactory()
        : this("Testing") { }

    internal TestThrowsFactory(string environment)
    {
        _environment = environment;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);
        builder.Configure(app =>
        {
            app.UseRouting();
            app.UseExceptionHandler();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet(
                    "/throws-notfound",
                    (HttpContext _) =>
                        throw new NotFoundException("ExchangeRate", "USD/EUR/2026-05-21")
                );
                endpoints.MapGet(
                    "/throws-domain",
                    (HttpContext _) => throw new DomainException("test domain failure")
                );
                endpoints.MapGet(
                    "/throws-generic",
                    (HttpContext _) =>
                        throw new InvalidOperationException(
                            "secret leaked at CurrencyTracker.Api.IntegrationTests"
                        )
                );
                endpoints.MapPost(
                    "/ping-via-bus",
                    async (PingQuery ping, IMessageBus bus, CancellationToken cancellationToken) =>
                        await bus.InvokeAsync<string>(ping, cancellationToken)
                );
            });
        });
    }
}
