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
    .WithDataVolume("currencytracker-keycloakdata")
    .WithRealmImport("./realms") // ← 11.3: mounts realms/*.json into the import dir
//.WithEnvironment("KC_HOSTNAME", "localhost")
//.WithEnvironment("KC_HTTP_ENABLED", "true")
//.WithEnvironment("KC_HOSTNAME_STRICT", "false")
//.WithEnvironment("KC_HOSTNAME_STRICT_HTTPS", "false")
;

// Seq log server (13.7, dev-only — ExcludeFromManifest keeps it out of
// any deployment; Azure logs travel the OTLP sink instead, ADR 0013).
// The resource name "seq" is a contract: WithReference injects
// ConnectionStrings__seq, the key ServiceDefaults' conditional Serilog
// sink reads. ACCEPT_EULA is Seq's container license gate; the image
// tag is pinned per ADR 0009; the named volume + persistent lifetime
// keep log history (and Seq's slow cold-start) across AppHost restarts.
var seq = builder
    .AddSeq("seq")
    .WithImageTag("2025.2") // ← confirm current stable on hub.docker.com/r/datalust/seq
    .WithDataVolume("currencytracker-seqdata")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEnvironment("ACCEPT_EULA", "Y")
    .ExcludeFromManifest();

builder
    .AddProject<Projects.CurrencyTracker_Api>("api")
    .WithReference(currencytrackerDb)
    .WaitFor(currencytrackerDb)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(keycloak) // ← 11.3
    .WaitFor(keycloak) // ← 11.3
    .WithReference(seq) // ← 13.7: injects ConnectionStrings__seq; no WaitFor — logging never gates startup
    .WithEnvironment(
        "Authentication__Authority",
        ReferenceExpression.Create(
            $"https://{keycloak.GetEndpoint("http").Property(EndpointProperty.Host)}:{keycloak.GetEndpoint("http").Property(EndpointProperty.Port)}/realms/currency-tracker"
        )
    ) // ← 11.3: composed issuer, never hardcoded
    .WithEnvironment("Authentication__Audience", "currency-tracker-api"); // ← 11.3

builder
    .AddProject<Projects.CurrencyTracker_Worker>("worker")
    .WithReference(currencytrackerDb)
    .WaitFor(currencytrackerDb)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(seq); // ← 13.7

builder.Build().Run();
