using System.Net;

namespace CurrencyTracker.Infrastructure.UnitTests.Providers;

/// <summary>
/// Minimal <see cref="HttpMessageHandler"/> stub that returns a
/// pre-canned <see cref="HttpResponseMessage"/> (or throws a
/// pre-canned exception) for every request. Lets a
/// <see cref="System.Net.Http.HttpClient"/> be constructed for unit
/// tests of <c>FrankfurterExchangeRateProvider</c> without a network.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _body;

    /// <summary>
    /// Initialises the stub to return <paramref name="statusCode"/> with
    /// <paramref name="body"/> as the JSON content for every request.
    /// </summary>
    /// <param name="statusCode">Status code to return.</param>
    /// <param name="body">Response body (JSON).</param>
    public StubHttpMessageHandler(HttpStatusCode statusCode, string body)
    {
        _statusCode = statusCode;
        _body = body;
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(
            new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_body, System.Text.Encoding.UTF8, "application/json"),
            }
        );
}
