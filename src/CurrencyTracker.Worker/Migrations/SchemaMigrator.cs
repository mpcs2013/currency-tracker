using CurrencyTracker.Infrastructure.Persistence;
using JasperFx.Resources;
using Microsoft.EntityFrameworkCore;

namespace CurrencyTracker.Worker.Migrations;

/// <summary>
/// Migrate-and-exit mode for the Worker (ADR 0018). Applies pending EF Core
/// migrations and provisions Wolverine's durability tables, then returns an
/// exit code instead of starting a host.
/// </summary>
/// <remarks>
/// <para>
/// This exists because neither Azure environment can be migrated from a CI
/// runner: PROD Postgres has no public endpoint, and the deploy identities are
/// not registered database administrators. A Container Apps job runs this
/// image, in the environment, under an identity that already is one.
/// </para>
/// <para>
/// Both halves of the schema are applied here on purpose. EF Core owns the
/// application tables; Wolverine owns the outbox/inbox tables in the
/// <c>wolverine</c> schema, created locally by
/// <c>AddResourceSetupOnStartup</c> — which is Development-only. Applying just
/// the EF half would leave the Worker unable to start in Azure.
/// </para>
/// </remarks>
internal static partial class SchemaMigrator
{
    /// <summary>The one argument that selects this mode.</summary>
    private const string MigrateArgument = "--migrate";

    /// <summary>Exit code for a database whose schema predates migration tracking.</summary>
    private const int InconsistentHistoryExitCode = 2;

    /// <summary>
    /// Counts base tables in the <c>public</c> schema. The <c>Value</c> alias is
    /// what <c>SqlQueryRaw&lt;T&gt;</c> binds a scalar projection to. The
    /// <c>wolverine</c> schema is deliberately excluded: its tables say nothing
    /// about whether EF's migration history is trustworthy.
    /// </summary>
    private const string CountPublicTablesSql = """
        SELECT COUNT(*)::int AS "Value"
        FROM information_schema.tables
        WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
        """;

    /// <summary>
    /// Determines whether the process was asked to migrate rather than run.
    /// </summary>
    /// <param name="args">The process arguments.</param>
    /// <returns><see langword="true"/> only for an exact <c>--migrate</c> argument.</returns>
    /// <remarks>
    /// The match is ordinal and exact. A near-miss must fall through to the
    /// JasperFx command line, which rejects it — the failure mode this prevents
    /// is a mistyped flag starting a normal Worker inside a job that then runs
    /// until its timeout and reports something misleading.
    /// </remarks>
    internal static bool IsRequested(string[] args) =>
        Array.Exists(
            args,
            argument => string.Equals(argument, MigrateArgument, StringComparison.Ordinal)
        );

    /// <summary>
    /// Applies the schema and returns a process exit code.
    /// </summary>
    /// <param name="host">The built host, not started.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>0 on success, 2 on an untracked pre-existing schema, 1 otherwise.</returns>
    internal static async Task<int> RunAsync(IHost host, CancellationToken cancellationToken)
    {
        var logger = host
            .Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(SchemaMigrator).FullName!);

        try
        {
            await using var scope = host.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var applied = (
                await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken)
            ).ToList();

            // A schema that exists without a migration history is the one state
            // MigrateAsync cannot resolve: it would replay InitialCreate and
            // fail on CREATE TABLE. Refuse it with a distinct exit code rather
            // than emitting a confusing 42P07 from inside EF.
            if (applied.Count == 0)
            {
                var publicTables = await dbContext
                    .Database.SqlQueryRaw<int>(CountPublicTablesSql)
                    .SingleAsync(cancellationToken);

                if (publicTables > 0)
                {
                    LogInconsistentHistory(logger, publicTables);
                    return InconsistentHistoryExitCode;
                }
            }

            var pending = (
                await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)
            ).ToList();

            if (pending.Count == 0)
            {
                LogNoPendingMigrations(logger, applied.Count);
            }
            else
            {
                LogApplyingMigrations(logger, pending.Count, pending);
                await dbContext.Database.MigrateAsync(cancellationToken);
                LogMigrationsApplied(logger, pending.Count);
            }

            // Wolverine's outbox/inbox tables. Idempotent — it creates what is
            // missing and leaves what exists.
            LogProvisioningResources(logger);
            await host.SetupResources();
            LogResourcesProvisioned(logger);

            return 0;
        }
        catch (Exception exception)
        {
            LogMigrationFailed(logger, exception);
            return 1;
        }
    }

    [LoggerMessage(
        EventId = 1100,
        Level = LogLevel.Information,
        Message = "Applying {PendingCount} pending migration(s): {Migrations}."
    )]
    private static partial void LogApplyingMigrations(
        ILogger logger,
        int pendingCount,
        IReadOnlyList<string> migrations
    );

    [LoggerMessage(
        EventId = 1101,
        Level = LogLevel.Information,
        Message = "Applied {PendingCount} migration(s)."
    )]
    private static partial void LogMigrationsApplied(ILogger logger, int pendingCount);

    [LoggerMessage(
        EventId = 1102,
        Level = LogLevel.Information,
        Message = "No pending migrations; {AppliedCount} already applied."
    )]
    private static partial void LogNoPendingMigrations(ILogger logger, int appliedCount);

    [LoggerMessage(
        EventId = 1103,
        Level = LogLevel.Information,
        Message = "Provisioning Wolverine durability resources."
    )]
    private static partial void LogProvisioningResources(ILogger logger);

    [LoggerMessage(
        EventId = 1104,
        Level = LogLevel.Information,
        Message = "Wolverine durability resources provisioned."
    )]
    private static partial void LogResourcesProvisioned(ILogger logger);

    [LoggerMessage(
        EventId = 1105,
        Level = LogLevel.Error,
        Message = "Refusing to migrate: the database has {TableCount} table(s) in the public schema but no EF migration history. Restore from backup or baseline __EFMigrationsHistory by hand."
    )]
    private static partial void LogInconsistentHistory(ILogger logger, int tableCount);

    [LoggerMessage(EventId = 1106, Level = LogLevel.Error, Message = "Schema migration failed.")]
    private static partial void LogMigrationFailed(ILogger logger, Exception exception);
}
