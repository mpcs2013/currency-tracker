using System.Net;
using CurrencyTracker.Infrastructure.Caching;
using FluentAssertions;
using StackExchange.Redis;
using Xunit;

namespace CurrencyTracker.Infrastructure.UnitTests.Caching;

/// <summary>
/// Behavioural tests for <see cref="CacheConnection"/>'s option shaping. No
/// network is touched: the value under test is the
/// <see cref="ConfigurationOptions"/> the connection would be made with, which
/// is where every Azure Managed Redis mistake actually lives — wrong port, TLS
/// off, RESP2, or a password smuggled in.
/// </summary>
public sealed class CacheConnectionTests
{
    private const string LocalEndpoint = "localhost:6379";

    private const string AzureEndpoint = "redis-ct-uat-9f3a.switzerlandnorth.redis.azure.net:10000";

    [Fact]
    public void BuildOptions_ForTheLocalCache_LeavesTlsOff()
    {
        // Arrange & Act
        var options = CacheConnection.BuildOptions(LocalEndpoint, useManagedIdentity: false);

        // Assert
        options.Ssl.Should().BeFalse();
    }

    [Fact]
    public void BuildOptions_ForManagedRedis_RequiresTls()
    {
        // Arrange & Act
        var options = CacheConnection.BuildOptions(AzureEndpoint, useManagedIdentity: true);

        // Assert
        options.Ssl.Should().BeTrue();
    }

    [Fact]
    public void BuildOptions_ForManagedRedis_SelectsResp3SoTheTokenRefreshCoversEveryConnection()
    {
        // Arrange & Act
        var options = CacheConnection.BuildOptions(AzureEndpoint, useManagedIdentity: true);

        // Assert
        options.Protocol.Should().Be(RedisProtocol.Resp3);
    }

    [Fact]
    public void BuildOptions_ForManagedRedis_KeepsThePortTheResourceReported()
    {
        // Arrange & Act
        var options = CacheConnection.BuildOptions(AzureEndpoint, useManagedIdentity: true);

        // Assert
        options
            .EndPoints.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<DnsEndPoint>()
            .Which.Port.Should()
            .Be(10000);
    }

    [Fact]
    public void BuildOptions_ForManagedRedis_CarriesNoPassword()
    {
        // Arrange & Act
        var options = CacheConnection.BuildOptions(AzureEndpoint, useManagedIdentity: true);

        // Assert
        options.Password.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildOptions_WithABlankEndpoint_FailsFast(string endpoint)
    {
        // Arrange & Act
        var act = () => CacheConnection.BuildOptions(endpoint, useManagedIdentity: true);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
