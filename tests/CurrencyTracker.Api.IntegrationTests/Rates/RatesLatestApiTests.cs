using Alba;
using CurrencyTracker.Api.IntegrationTests.Auth;
using CurrencyTracker.Application.Messaging;
using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Rates;

namespace CurrencyTracker.Api.IntegrationTests.Rates;

public sealed class RatesLatestApiTests : IClassFixture<RatesApiFixture>
{
    private static readonly CurrencyCode Usd = CurrencyCode.Create("USD").Value;
    private static readonly CurrencyCode Eur = CurrencyCode.Create("EUR").Value;
    private static readonly DateOnly AsOf = new(2026, 5, 28);

    private readonly RatesApiFixture _fixture;

    public RatesLatestApiTests(RatesApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Get_latest_returns_200_and_rates()
    {
        // Arrange
        await _fixture.SeedSnapshotAsync(
            RateSnapshot.Create(Usd, AsOf, [ExchangeRate.Create(Usd, Eur, 0.92m, AsOf).Value]).Value
        );
        var token = TestJwt.ForRoles("user");

        // Act + Assert
        var result = await _fixture.Host.Scenario(s =>
        {
            s.Get.Url("/api/v1/rates/latest?base=USD");
            s.WithBearerToken(token);
            s.StatusCodeShouldBeOk();
        });

        var dtos = result.ReadAsJson<IReadOnlyList<ExchangeRateDto>>();
        dtos.Should().ContainSingle(d => d.Quote == "EUR" && d.Rate == 0.92m);
    }

    [Fact]
    public async Task Get_latest_returns_404_when_no_snapshot()
    {
        var token = TestJwt.ForRoles("user");

        await _fixture.Host.Scenario(s =>
        {
            s.Get.Url("/api/v1/rates/latest?base=JPY"); // valid shape, no data
            s.WithBearerToken(token);
            s.StatusCodeShouldBe(404);
            s.ContentTypeShouldBe("application/problem+json");
        });
    }
}
