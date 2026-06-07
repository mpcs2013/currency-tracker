using Alba;
using Microsoft.AspNetCore.Hosting;

namespace CurrencyTracker.Api.IntegrationTests.Endpoints;

public sealed class RatesLatestEndpointSmokeTests
{
    static RatesLatestEndpointSmokeTests()
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__currencytracker",
            "Host=localhost;Database=latest-rates-tests;Username=noop;" + "******"
        );
        Environment.SetEnvironmentVariable("ConnectionStrings__cache", "localhost:6379");
    }

    [Theory]
    [InlineData("/api/v1/rates/latest?base=usd")]
    [InlineData("/api/v1/rates/latest")]
    public async Task Invalid_or_missing_base_returns_400_problem_details(string url)
    {
        await using var host = await AlbaHost.For<Program>(builder =>
            builder.UseEnvironment("Testing")
        );

        await host.Scenario(s =>
        {
            s.Get.Url(url);
            s.StatusCodeShouldBe(400);
            s.ContentTypeShouldBe("application/problem+json");
        });
    }
}
