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

// Postgres server with a named data volume so local data persists across
// AppHost restarts. The named volume "currencytracker-pgdata" scopes the
// volume to this project; remove it manually with
// `docker volume rm currencytracker-pgdata` to wipe local state.
var postgres = builder.AddPostgres("postgres").WithDataVolume("currencytracker-pgdata");

// Database "currencytracker" is what Phase 8's ApplicationDbContext binds
// to via IConfiguration.GetConnectionString("currencytracker"). The
// connection string is injected by Aspire as the environment variable
// ConnectionStrings__currencytracker when Api or Worker calls
// WithReference(currencytrackerDb) (lands in 7.5).
var currencytrackerDb = postgres.AddDatabase("currencytracker");

// Redis cache with a named data volume. Phase 10's RedisCacheService
// will resolve ConnectionStrings__cache from IConfiguration when Api
// calls WithReference(cache) in 7.5.
var cache = builder.AddRedis("cache").WithDataVolume("currencytracker-redisdata");

// Keycloak OIDC provider. Stable host port 8080 keeps the issuer URL
// (http://localhost:8080/realms/currency-tracker) constant across AppHost
// restarts, so tokens minted on one run still validate on the next. The
// image tag is pinned (ADR 0009 discipline) so a transitive Aspire bump
// can't silently change the engine and break realm import. The named data
// volume persists realm/user state; wipe it with
// `docker volume rm currencytracker-keycloakdata`. Realm import and the
// api env wiring land in 11.2/11.3.
var keycloak = builder
    .AddKeycloak("keycloak", 8080)
    .WithImageTag("26.6.0")
    .WithDataVolume("currencytracker-keycloakdata");

builder
    .AddProject<Projects.CurrencyTracker_Api>("api")
    .WithReference(currencytrackerDb)
    .WaitFor(currencytrackerDb)
    .WithReference(cache)
    .WaitFor(cache);

builder
    .AddProject<Projects.CurrencyTracker_Worker>("worker")
    .WithReference(currencytrackerDb)
    .WaitFor(currencytrackerDb)
    .WithReference(cache)
    .WaitFor(cache);

builder.Build().Run();
