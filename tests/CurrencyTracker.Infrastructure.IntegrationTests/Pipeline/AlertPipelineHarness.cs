using CurrencyTracker.Application;
using CurrencyTracker.Application.Abstractions.Alerts;
using CurrencyTracker.Application.Abstractions.Caching;
using CurrencyTracker.Application.Abstractions.Notifications;
using CurrencyTracker.Application.Abstractions.Persistence;
using CurrencyTracker.Application.Abstractions.Providers;
using CurrencyTracker.Domain.Alerts;
using CurrencyTracker.Infrastructure.Providers;
using JasperFx.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.FluentValidation;
using Wolverine.Postgresql;

namespace CurrencyTracker.Infrastructure.IntegrationTests.Pipeline;

/// <summary>
/// Builds and starts a Worker-shaped Wolverine host for the Phase 12.10
/// pipeline test: full <c>AddInfrastructure</c>, the Worker's exact
/// <c>UseWolverine</c> options (Postgres outbox in the "wolverine"
/// schema, EF transactions, durable local queues, all six
/// service-location opt-ins), pointed at a Testcontainers Postgres and a
/// plain-HTTP WireMock Frankfurter.
/// </summary>
/// <remarks>
/// TEST-ONLY swaps, mirroring the 9.9 harness's discipline:
/// <list type="bullet">
/// <item>The <c>FrankfurterOptions</c> validators are removed so the
/// client accepts WireMock's plain-HTTP URL (the https-only rule is a
/// production boot control, not this test's subject).</item>
/// <item><see cref="ICacheService"/> is replaced by a no-op — the
/// ingestion handler's post-commit eviction is not under test and a
/// Redis container would be dead weight.</item>
/// <item><see cref="IAlertNotifier"/> is replaced by
/// <see cref="RecordingAlertNotifier"/> so dispatch is asserted on the
/// port's calls rather than scraped from log output.</item>
/// </list>
/// Never make these swaps in a real host.
/// </remarks>
internal static class AlertPipelineHarness
{
    /// <summary>
    /// Builds and starts the pipeline host.
    /// </summary>
    /// <param name="connectionString">Testcontainers Postgres connection
    /// string (from <c>PostgresFixture</c>, already migrated).</param>
    /// <param name="frankfurterBaseUrl">The WireMock server's HTTP URL.</param>
    /// <returns>A started <see cref="IHost"/>; stop and dispose it when done.</returns>
    public static async Task<IHost> StartAsync(string connectionString, string frankfurterBaseUrl)
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:currencytracker"] = connectionString,
                ["ConnectionStrings:cache"] = "localhost:6379", // inert — ICacheService is swapped below
                ["Frankfurter:BaseUrl"] = frankfurterBaseUrl,
                ["Frankfurter:Timeout"] = "00:00:10",
                ["Frankfurter:UserAgent"] = "CurrencyTracker-IntegrationTests/1.0",
            }
        );

        builder.AddInfrastructure();

        // TEST-ONLY: accept WireMock's plain-HTTP URL (see remarks).
        builder.Services.RemoveAll<IValidateOptions<FrankfurterOptions>>();

        // TEST-ONLY: swap the cache and the notifier (see remarks).
        builder.Services.RemoveAll<ICacheService>();
        builder.Services.AddSingleton<ICacheService, NoOpCacheService>();
        builder.Services.RemoveAll<IAlertNotifier>();
        builder.Services.AddSingleton<RecordingAlertNotifier>();
        builder.Services.AddSingleton<IAlertNotifier>(sp =>
            sp.GetRequiredService<RecordingAlertNotifier>()
        );

        // The Worker's UseWolverine block, mirrored line for line — this
        // harness IS the Worker for the pipeline's purposes.
        builder.UseWolverine(opts =>
        {
            opts.Durability.Mode = DurabilityMode.Solo;
            opts.ApplicationAssembly = typeof(ApplicationAssemblyAnchor).Assembly;
            opts.UseFluentValidation();
            opts.PersistMessagesWithPostgresql(connectionString, "wolverine");
            opts.UseEntityFrameworkCoreTransactions();
            opts.Policies.AutoApplyTransactions();
            opts.Policies.UseDurableLocalQueues();
            opts.CodeGeneration.AlwaysUseServiceLocationFor<IExchangeRateProvider>();
            opts.CodeGeneration.AlwaysUseServiceLocationFor<IExchangeRateRepository>();
            opts.CodeGeneration.AlwaysUseServiceLocationFor<IUnitOfWork>();
            opts.CodeGeneration.AlwaysUseServiceLocationFor<IAlertRuleEvaluator>();
            opts.CodeGeneration.AlwaysUseServiceLocationFor<IAlertRepository>();
            opts.CodeGeneration.AlwaysUseServiceLocationFor<IAlertNotifier>();
        });

        // Provision the wolverine_* envelope tables in the test container,
        // exactly as the dev host does.
        builder.Services.AddResourceSetupOnStartup();

        var host = builder.Build();
        await host.StartAsync();
        return host;
    }

    /// <summary>
    /// No-op <see cref="ICacheService"/> for the pipeline test: misses on
    /// every read, swallows every write, and runs the
    /// <c>GetOrSetAsync</c> factory directly.
    /// </summary>
    private sealed class NoOpCacheService : ICacheService
    {
        /// <inheritdoc />
        public Task<string?> GetAsync(string key, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(null);

        /// <inheritdoc />
        public Task SetAsync(
            string key,
            string value,
            TimeSpan ttl,
            CancellationToken cancellationToken
        ) => Task.CompletedTask;

        /// <inheritdoc />
        public Task RemoveAsync(string key, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        /// <inheritdoc />
        public Task<T> GetOrSetAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            TimeSpan ttl,
            CancellationToken cancellationToken
        ) => factory(cancellationToken);
    }
}

/// <summary>
/// Thread-safe recording <see cref="IAlertNotifier"/> for the pipeline
/// test — the started host may dispatch on Wolverine worker threads.
/// </summary>
internal sealed class RecordingAlertNotifier : IAlertNotifier
{
    private readonly List<Alert> _sentAlerts = [];
    private readonly Lock _gate = new();

    /// <summary>Gets a snapshot of the alerts dispatched so far.</summary>
    public IReadOnlyList<Alert> SentAlerts
    {
        get
        {
            lock (_gate)
            {
                return [.. _sentAlerts];
            }
        }
    }

    /// <inheritdoc />
    public Task SendAsync(Alert alert, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _sentAlerts.Add(alert);
        }

        return Task.CompletedTask;
    }
}
