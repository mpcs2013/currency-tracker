// Aspire DistributedApplication bootstrap for CurrencyTracker.
//
// The project is the orchestrator for the local-dev topology:
// Api + Worker + Postgres + Redis behind the Aspire dashboard. It is
// not deployed to Azure — Phase 14 deploys Api and Worker directly to
// Azure Container Apps with connection strings sourced from Key Vault
// instead of from this AppHost.
//
// Resources are added in:
//   - 7.3: Postgres (with named data volume + database "currencytracker")
//   - 7.4: Redis (with named data volume)
//   - 7.5: Api + Worker project references with WithReference/WaitFor

var builder = DistributedApplication.CreateBuilder(args);

// Resources land in 7.3 and 7.4.
// Project references land in 7.5.

builder.Build().Run();
