using CurrencyTracker.Api.IntegrationTests.Rates;
using CurrencyTracker.Application.Messaging;
//using CurrencyTracker.Application.Rates; // ExchangeRateDto (match the repo's DTO namespace)
using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Rates;

namespace CurrencyTracker.Api.IntegrationTests.Auth;

/// <summary>
/// Proves the Phase 11 authorization contract end-to-end through the real
/// pipeline: 401 without a token, 403 with the wrong role, 200 with the
/// right one, and that an admin token clears the admin-only gate. Tokens
/// are minted by <see cref="TestJwt"/> against the host's trusted test key
/// (no live Keycloak).
/// </summary>
public sealed class RatesAuthApiTests : IClassFixture<RatesApiFixture>
{
    private readonly RatesApiFixture _fixture;

    public RatesAuthApiTests(RatesApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Latest_without_token_returns_401_problem_details()
    {
        await _fixture.Host.Scenario(s =>
        {
            s.Get.Url("/api/v1/rates/latest?base=USD");

            s.StatusCodeShouldBe(401);
            s.Header("Content-Type").SingleValueShouldEqual("application/problem+json");
        });
    }

    [Fact]
    public async Task Admin_ingest_with_user_role_returns_403_problem_details()
    {
        var userToken = TestJwt.ForRoles("user");

        await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(new { baseCurrency = "USD", asOf = "2026-06-20" }).ToUrl("/admin/ingest");
            s.WithBearerToken(userToken);

            s.StatusCodeShouldBe(403);
            s.Header("Content-Type").SingleValueShouldEqual("application/problem+json");
        });
    }

    [Fact]
    public async Task Latest_with_user_token_returns_200_and_rates()
    {
        // Arrange
        var usd = CurrencyCode.Create("USD").Value;
        var eur = CurrencyCode.Create("EUR").Value;
        var asOf = new DateOnly(2026, 5, 28);
        await _fixture.SeedSnapshotAsync(
            RateSnapshot.Create(usd, asOf, [ExchangeRate.Create(usd, eur, 0.92m, asOf).Value]).Value
        );
        var userToken = TestJwt.ForRoles("user");

        // Act + Assert
        var result = await _fixture.Host.Scenario(s =>
        {
            s.Get.Url("/api/v1/rates/latest?base=USD");
            s.WithBearerToken(userToken);
            s.StatusCodeShouldBe(200);
        });

        var dtos = result.ReadAsJson<IReadOnlyList<ExchangeRateDto>>();
        dtos.Should().ContainSingle(d => d.Quote == "EUR" && d.Rate == 0.92m);
    }

    [Fact]
    public async Task Admin_ingest_with_admin_token_passes_the_admin_gate()
    {
        var adminToken = TestJwt.ForRoles("user", "admin");

        var result = await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(new { baseCurrency = "USD", asOf = "2026-06-20" }).ToUrl("/admin/ingest");
            s.WithBearerToken(adminToken);

            // The admin token clears authentication (not 401) and the admin
            // policy (not 403). The downstream outcome — 202 on a stubbed
            // Frankfurter, or a 5xx if the live provider is unreachable — is a
            // handler concern proved elsewhere (9.9); here we prove the gate.
            s.IgnoreStatusCode();
        });

        result.Context.Response.StatusCode.Should().NotBe(401);
        result.Context.Response.StatusCode.Should().NotBe(403);
    }
}
