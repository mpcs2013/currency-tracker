using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.StackExchangeRedis;
using StackExchange.Redis;

namespace CurrencyTracker.Infrastructure.Caching;

/// <summary>
/// Builds the cache connection for the current environment. Locally the
/// endpoint is a plaintext Aspire container; in Azure it is Azure Managed
/// Redis, which has <b>no access-key path at all</b> (14.C disabled key
/// authentication at creation) and listens on port 10000 rather than the 6380
/// the retired Azure Cache for Redis used.
/// </summary>
/// <remarks>
/// Nothing here reads configuration. The Entra switch is declared once, on
/// <see cref="Persistence.ApplicationDataSource.ManagedIdentityKey"/>, and read
/// once in <see cref="DependencyInjection"/>. One key, one declaration, two
/// consumers.
/// </remarks>
public static class CacheConnection
{
    /// <summary>
    /// Name of the cache connection string. Aspire injects it locally as
    /// <c>ConnectionStrings__cache</c> (Phase 7.5); Container Apps resolves it
    /// from Key Vault under the same name (14.43). In Azure the value is a bare
    /// <c>host:port</c> — there is no credential to carry.
    /// </summary>
    public const string ConnectionStringName = "cache";

    /// <summary>
    /// Shapes the connection options for an endpoint.
    /// </summary>
    /// <param name="endpoint">Endpoint in <c>host:port</c> form.</param>
    /// <param name="useManagedIdentity">
    /// When <c>true</c>, apply the Azure Managed Redis posture: TLS required and
    /// RESP3 selected.
    /// </param>
    /// <returns>Options ready for authentication and connection.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="endpoint"/> is null, empty or whitespace.
    /// </exception>
    public static ConfigurationOptions BuildOptions(string endpoint, bool useManagedIdentity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        var options = ConfigurationOptions.Parse(endpoint);

        if (!useManagedIdentity)
        {
            return options;
        }

        // Explicit rather than inferred from the hostname: modules/redis pins
        // client_protocol = "Encrypted", so a plaintext attempt is refused, and
        // being explicit turns that into a readable TLS error rather than a
        // connection reset.
        options.Ssl = true;

        // RESP3 puts interactive and pub/sub traffic on ONE connection, which
        // the token extension can proactively re-authenticate. Under RESP2 the
        // separate subscription connection cannot be re-authenticated: the
        // server drops it at every token expiry and the client restores it —
        // harmless, and it emits MicrosoftEntraTokenExpired into the cache's
        // error metrics forever, which is the metric 14.52 will alert on.
        options.Protocol = RedisProtocol.Resp3;

        // The multiplexer is created lazily, on first cache use. Aborting on a
        // transient first-connect failure would leave a permanently dead
        // multiplexer; false lets it reconnect in the background. This does not
        // hide a broken cache from readiness — an operation against an
        // unavailable multiplexer still throws, and the Phase 13.B check has no
        // catch, so it still reports Unhealthy.
        options.AbortOnConnectFail = false;

        return options;
    }

    /// <summary>
    /// Connects to the cache, authenticating with Microsoft Entra when
    /// <paramref name="useManagedIdentity"/> is set.
    /// </summary>
    /// <param name="endpoint">Endpoint in <c>host:port</c> form.</param>
    /// <param name="useManagedIdentity">Whether to authenticate with Entra.</param>
    /// <param name="credential">
    /// Credential used to acquire Entra tokens. Defaults to
    /// <see cref="DefaultAzureCredential"/>. Injected only by tests.
    /// </param>
    /// <returns>A connected multiplexer.</returns>
    public static async Task<IConnectionMultiplexer> ConnectAsync(
        string endpoint,
        bool useManagedIdentity,
        TokenCredential? credential = null
    )
    {
        var options = BuildOptions(endpoint, useManagedIdentity);

        if (useManagedIdentity)
        {
            // Sets the connection user to the identity's object id and installs
            // the re-authentication timer. Azure Managed Redis rejects a raw
            // token placed in Password — this extension is the supported path.
            await options.ConfigureForAzureWithTokenCredentialAsync(
                credential ?? new DefaultAzureCredential()
            );
        }

        return await ConnectionMultiplexer.ConnectAsync(options);
    }
}
