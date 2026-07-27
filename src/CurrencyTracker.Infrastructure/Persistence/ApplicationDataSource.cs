using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CurrencyTracker.Infrastructure.Persistence;

/// <summary>
/// Factory for the application's <see cref="NpgsqlDataSource"/>. Two callers:
/// the EF Core registration in <see cref="DependencyInjection"/>, and the
/// Worker's Wolverine outbox. Both need the same authentication behaviour, so
/// the token logic is written once, here.
/// </summary>
/// <remarks>
/// <para>
/// When <c>Azure:UseManagedIdentity</c> is <c>false</c> (local, Aspire, tests)
/// the connection string is used exactly as supplied. When it is <c>true</c>, a
/// password provider attaches: Azure Database for PostgreSQL accepts a Microsoft
/// Entra access token where the PostgreSQL wire protocol expects a password.
/// Nothing about the protocol changes — only where the bytes come from.
/// </para>
/// <para>
/// Both the synchronous and asynchronous providers are implemented on purpose.
/// Npgsql's documentation suggests throwing from the synchronous one, which is
/// sound advice for connections you open yourself; Wolverine's durability
/// subsystem owns connection lifecycles this project does not control, and a
/// synchronous open inside it must not throw.
/// </para>
/// </remarks>
public static class ApplicationDataSource
{
    /// <summary>
    /// Configuration key that switches the connection onto Entra tokens.
    /// Supplied in Azure as the <c>Azure__UseManagedIdentity</c> environment
    /// variable (root <c>main.tf</c>, 14.43); defaults to <c>false</c> in
    /// <c>appsettings.json</c> (14.46).
    /// </summary>
    public const string ManagedIdentityKey = "Azure:UseManagedIdentity";

    /// <summary>
    /// Name of the connection string this application reads. Aspire injects it
    /// locally as <c>ConnectionStrings__currencytracker</c> (Phase 7.5);
    /// Container Apps resolves it from Key Vault under the same name (14.43).
    /// </summary>
    public const string ConnectionStringName = "currencytracker";

    /// <summary>
    /// Token scope for the Azure Database for PostgreSQL resource. This exact
    /// string is what the server validates against: a token for any other
    /// resource is acquired successfully and then rejected at login, which is
    /// why the scope lives in one named place rather than inline.
    /// </summary>
    private static readonly string[] Scopes =
    [
        "https://ossrdbms-aad.database.windows.net/.default",
    ];

    /// <summary>
    /// Builds the data source for the current configuration.
    /// </summary>
    /// <param name="configuration">Host configuration.</param>
    /// <param name="credential">
    /// Credential used to acquire Entra tokens. Defaults to
    /// <see cref="DefaultAzureCredential"/>, which resolves the container's
    /// managed identity in Azure and a signed-in developer locally. Injected
    /// only by tests.
    /// </param>
    /// <returns>A data source the caller owns and must dispose.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the <c>currencytracker</c> connection string is absent,
    /// empty, or whitespace.
    /// </exception>
    public static NpgsqlDataSource Create(
        IConfiguration configuration,
        TokenCredential? credential = null
    )
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // Whitespace, not just null. An empty string is a configuration VALUE
        // in .NET, so an appsettings "placeholder" would satisfy a null check
        // and silently disarm this guard — see docs/configuration.md (14.46).
        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"The '{ConnectionStringName}' connection string is not configured. "
                    + "Locally it is injected by the Aspire AppHost; in Azure it is a "
                    + "Key Vault reference on the Container App (Phase 14.43)."
            );
        }

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);

        if (!configuration.GetValue<bool>(ManagedIdentityKey))
        {
            return dataSourceBuilder.Build();
        }

        var tokenCredential = credential ?? new DefaultAzureCredential();

        // Invoked on every PHYSICAL connection open, not on every command.
        // Azure.Identity caches the token internally and hands back the cached
        // value until it nears expiry, so this stays cheap without a second
        // cache here — which is exactly what UsePeriodicPasswordProvider would
        // add, with a hardcoded interval free to drift from the real lifetime.
        dataSourceBuilder.UsePasswordProvider(
            _ => AcquireToken(tokenCredential),
            (_, cancellationToken) => AcquireTokenAsync(tokenCredential, cancellationToken)
        );

        return dataSourceBuilder.Build();
    }

    /// <summary>
    /// Acquires an Entra access token for Azure Database for PostgreSQL.
    /// </summary>
    /// <param name="credential">Credential to acquire the token with.</param>
    /// <returns>The raw JWT, used as the connection password.</returns>
    internal static string AcquireToken(TokenCredential credential) =>
        credential.GetToken(new TokenRequestContext(Scopes), CancellationToken.None).Token;

    /// <summary>
    /// Acquires an Entra access token for Azure Database for PostgreSQL.
    /// </summary>
    /// <param name="credential">Credential to acquire the token with.</param>
    /// <param name="cancellationToken">Cancels the token request.</param>
    /// <returns>The raw JWT, used as the connection password.</returns>
    internal static async ValueTask<string> AcquireTokenAsync(
        TokenCredential credential,
        CancellationToken cancellationToken
    )
    {
        var token = await credential.GetTokenAsync(
            new TokenRequestContext(Scopes),
            cancellationToken
        );

        return token.Token;
    }
}
