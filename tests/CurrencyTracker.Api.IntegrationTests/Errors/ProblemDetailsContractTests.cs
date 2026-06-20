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

        // Re-host the existing fixture with the Production environment.
        // WithWebHostBuilder inherits the parent's ConfigureWebHost (so the
        // test endpoints stay wired); the env-var injected by
        // TestThrowsFactory's static constructor remains visible to
        // Program.cs because the process env-var is set once for the whole
        // test run.
        using var productionFactory = _factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Production")
        );
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
    // CRITICAL: this static constructor runs once, the first time any
    // member of TestThrowsFactory is referenced — which is BEFORE
    // WebApplicationFactory boots Program.cs. The env var lands in the
    // process environment table, IConfiguration's default sources read
    // it under ConnectionStrings:currencytracker (the "__" → ":" mapping
    // is built into the .NET configuration provider), and Program.cs's
    // AddInfrastructure() finds the value at its own line 14 — before
    // ConfigureWebHost / ConfigureAppConfiguration would get a chance.
    //
    // This is the documented workaround for the minimal-API top-level-
    // Program.cs fail-fast pattern: WebApplicationFactory can layer
    // config ON TOP of Program.cs's, but cannot intercept what Program.cs
    // reads from IConfiguration during its own top-level statements.
    // Process env vars are visible to those statements.
    //
    // The value is junk — these tests never open a Postgres connection.
    // The fail-fast in production code is unchanged and still protects
    // real hosts started without a configured database.
    static TestThrowsFactory()
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__currencytracker",
            "Host=localhost;Database=ping-tests;Username=noop;Password=noop"
        );

        // 10.1 — the cache fail-fast in AddInfrastructure also runs during
        // Program.cs's top-level statements, before WebApplicationFactory can
        // layer config. Set a junk ConnectionStrings__cache so AddInfrastructure
        // builds. These tests never resolve ICacheService and
        // AddStackExchangeRedisCache connects lazily, so no Redis connection is
        // ever opened.
        Environment.SetEnvironmentVariable("ConnectionStrings__cache", "localhost:6379");

        Environment.SetEnvironmentVariable(
            "Authentication__Authority",
            "https://test.local/realms/currency-tracker"
        );
        Environment.SetEnvironmentVariable("Authentication__Audience", "currency-tracker-api");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

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
