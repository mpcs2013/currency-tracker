using Azure.Core;
using CurrencyTracker.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CurrencyTracker.Infrastructure.UnitTests.Persistence;

/// <summary>
/// Behavioural tests for <see cref="ApplicationDataSource"/>: the fail-fast on
/// missing configuration, the local (password) path, and the Entra path — in
/// particular that the token requested is scoped to the Azure Database for
/// PostgreSQL resource, which is the single most typo-prone value in Phase 14.E
/// and the one whose mistake surfaces only as a login failure in Azure.
/// </summary>
public sealed class ApplicationDataSourceTests
{
    private const string LocalConnectionString =
        "Host=localhost;Port=5432;Database=currencytracker;Username=postgres;Password=local-dev";

    private const string AzureConnectionString =
        "Host=psql-ct-uat.postgres.database.azure.com;Port=5432;"
        + "Database=currencytracker;Username=ca-api-identity;SSL Mode=Require";

    private const string PostgresScope = "https://ossrdbms-aad.database.windows.net/.default";

    /// <summary>
    /// Builds host configuration with the switch set and, optionally, the
    /// connection string present.
    /// </summary>
    /// <param name="connectionString">
    /// Connection string to publish, or null to model an absent one.
    /// </param>
    /// <param name="useManagedIdentity">Value of the Entra switch.</param>
    /// <returns>Configuration to hand to the factory.</returns>
    private static IConfiguration Configuration(string? connectionString, bool useManagedIdentity)
    {
        var values = new Dictionary<string, string?>
        {
            [ApplicationDataSource.ManagedIdentityKey] = useManagedIdentity ? "true" : "false",
        };

        if (connectionString is not null)
        {
            values[$"ConnectionStrings:{ApplicationDataSource.ConnectionStringName}"] =
                connectionString;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public void Create_WithoutAConnectionString_FailsFastWithAnActionableMessage()
    {
        // Arrange
        var configuration = Configuration(connectionString: null, useManagedIdentity: false);

        // Act
        var act = () => ApplicationDataSource.Create(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*currencytracker*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithABlankConnectionString_FailsFastRatherThanBuildingANonsenseDataSource(
        string connectionString
    )
    {
        // Arrange
        var configuration = Configuration(connectionString, useManagedIdentity: false);

        // Act
        var act = () => ApplicationDataSource.Create(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*currencytracker*");
    }

    [Fact]
    public void Create_WithManagedIdentityOff_UsesTheSuppliedConnectionStringAsIs()
    {
        // Arrange
        var configuration = Configuration(LocalConnectionString, useManagedIdentity: false);

        // Act
        using var dataSource = ApplicationDataSource.Create(configuration);

        // Assert
        dataSource.ConnectionString.Should().Contain("Host=localhost");
    }

    [Fact]
    public void Create_WithManagedIdentityOn_RequestsNoTokenUntilAConnectionIsOpened()
    {
        // Arrange
        var configuration = Configuration(AzureConnectionString, useManagedIdentity: true);
        var credential = new RecordingTokenCredential();

        // Act
        using var dataSource = ApplicationDataSource.Create(configuration, credential);

        // Assert
        credential.RequestedScopes.Should().BeEmpty();
    }

    [Fact]
    public void AcquireToken_RequestsATokenScopedToAzureDatabaseForPostgreSql()
    {
        // Arrange
        var credential = new RecordingTokenCredential();

        // Act
        var token = ApplicationDataSource.AcquireToken(credential);

        // Assert
        token.Should().Be(RecordingTokenCredential.StubToken);
        credential.RequestedScopes.Should().ContainSingle().Which.Should().Equal(PostgresScope);
    }

    [Fact]
    public async Task AcquireTokenAsync_RequestsATokenScopedToAzureDatabaseForPostgreSql()
    {
        // Arrange
        var credential = new RecordingTokenCredential();

        // Act
        var token = await ApplicationDataSource.AcquireTokenAsync(
            credential,
            TestContext.Current.CancellationToken
        );

        // Assert
        token.Should().Be(RecordingTokenCredential.StubToken);
        credential.RequestedScopes.Should().ContainSingle().Which.Should().Equal(PostgresScope);
    }

    /// <summary>
    /// In-memory <see cref="TokenCredential"/> that records the scopes it was
    /// asked for. A fake rather than a substitute: the recorded state is what
    /// the assertions read (AGENTS.md §Fakes live with tests).
    /// </summary>
    private sealed class RecordingTokenCredential : TokenCredential
    {
        /// <summary>Token value every request returns.</summary>
        public const string StubToken = "stub-token";

        /// <summary>Scopes captured from each token request, in order.</summary>
        public List<string[]> RequestedScopes { get; } = [];

        /// <inheritdoc />
        public override AccessToken GetToken(
            TokenRequestContext requestContext,
            CancellationToken cancellationToken
        )
        {
            RequestedScopes.Add(requestContext.Scopes);
            return new AccessToken(StubToken, DateTimeOffset.UtcNow.AddHours(1));
        }

        /// <inheritdoc />
        public override ValueTask<AccessToken> GetTokenAsync(
            TokenRequestContext requestContext,
            CancellationToken cancellationToken
        ) => new(GetToken(requestContext, cancellationToken));
    }
}
