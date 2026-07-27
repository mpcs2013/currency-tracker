using Alba;
using CurrencyTracker.Api.IntegrationTests.Auth;
using Microsoft.AspNetCore.Hosting;

namespace CurrencyTracker.Api.IntegrationTests.Endpoints;

public sealed class RatesLatestEndpointSmokeTests
{
    static RatesLatestEndpointSmokeTests()
    {
        // The string must PARSE, even though nothing ever connects: 14.44 moved
        // the Npgsql data source behind ApplicationDataSource.Create, which
        // builds it during AddInfrastructure() instead of lazily on first use.
        // This line previously ended in a literal "******" — a redaction that
        // produced an unparseable connection string and went unnoticed for as
        // long as UseNpgsql(string) deferred the parse.
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__currencytracker",
            "Host=localhost;Database=latest-rates-tests;Username=noop;Password=noop"
        );
        Environment.SetEnvironmentVariable("ConnectionStrings__cache", "localhost:6379");
    }

    [Theory]
    [InlineData("/api/v1/rates/latest?base=usd")]
    [InlineData("/api/v1/rates/latest")]
    public async Task Invalid_or_missing_base_returns_400_problem_details(string url)
    {
        await using var host = await AlbaHost.For<Program>(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting(
                "Authentication:Authority",
                "https://test.local/realms/currency-tracker"
            );
            builder.UseSetting("Authentication:Audience", "currency-tracker-api");
            builder.UseTestJwtBearer();
        });

        await host.Scenario(s =>
        {
            s.Get.Url(url);
            s.WithBearerToken(TestJwt.ForRoles("user"));
            s.StatusCodeShouldBe(400);
            s.ContentTypeShouldBe("application/problem+json");
        });
    }
}
