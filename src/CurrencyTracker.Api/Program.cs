using System.Diagnostics;
using CurrencyTracker.Api.ErrorHandling;
using CurrencyTracker.Application;
using CurrencyTracker.Application.Abstractions.Persistence;
using CurrencyTracker.Application.Abstractions.Providers;
using CurrencyTracker.Application.Messaging;
using CurrencyTracker.Infrastructure;
using CurrencyTracker.ServiceDefaults;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Wolverine;
using Wolverine.FluentValidation;
using Wolverine.Http;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// ServiceDefaults wires the generic OTel instrumentation but can't see the
// app's telemetry scope. Register the ingestion meter + source here so the
// rates.ingested counter and the ingest.daily_rates span are exported.
builder
    .Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics.AddMeter(IngestionTelemetry.SourceName))
    .WithTracing(tracing => tracing.AddSource(IngestionTelemetry.SourceName));

builder.AddInfrastructure();

if (builder.Environment.IsDevelopment())
{
    builder.AddInfrastructureDevelopment();
}

builder.UseWolverine(opts =>
{
    opts.ApplicationAssembly = typeof(ApplicationAssemblyAnchor).Assembly;
    opts.Discovery.IncludeAssembly(typeof(Program).Assembly); // scan the Api for [Wolverine*] endpoints
    opts.UseFluentValidation();

    // The ingestion handler depends on internal adapters (the Frankfurter
    // provider and the EF repositories / unit-of-work). Wolverine 6 can't
    // inline-construct internal types, and ServiceLocationPolicy.NotAllowed
    // (the 6.0 default) forbids the service-locator fallback. Opt these
    // specific ports into service location so the adapters stay internal
    // sealed, per the cross-layer guardrails.
    opts.CodeGeneration.AlwaysUseServiceLocationFor<IExchangeRateProvider>();
    opts.CodeGeneration.AlwaysUseServiceLocationFor<IExchangeRateRepository>();
    opts.CodeGeneration.AlwaysUseServiceLocationFor<IUnitOfWork>();
});

builder.Services.AddOpenApi();
builder.Services.AddWolverineHttp();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        var http = ctx.HttpContext;
        var traceId = Activity.Current?.Id ?? http.TraceIdentifier;
        ctx.ProblemDetails.Instance ??= $"{http.Request.Method} {http.Request.Path}";
        ctx.ProblemDetails.Extensions["traceId"] = traceId;
    };
});

// Read the issuer authority the AppHost injects (11.3) and fail fast at BOOT
// if it's missing. This is a top-level statement, so a misconfigured host
// dies at startup — not lazily on the first authenticated request, which is
// what a `?? throw` inside the AddJwtBearer callback would do. Mirrors the
// connection-string fail-fast in AddInfrastructure. (Audience is read in 11.5,
// where it's first consumed, so its own fail-fast lands there.)
var authAuthority =
    builder.Configuration["Authentication:Authority"]
    ?? throw new InvalidOperationException(
        "Authentication:Authority is not configured. The AppHost injects it as "
            + "Authentication__Authority (Phase 11.3); non-Aspire hosts (including "
            + "integration tests) must set it explicitly."
    );
var authAudience =
    builder.Configuration["Authentication:Audience"]
    ?? throw new InvalidOperationException(
        "Authentication:Audience is not configured. The AppHost injects it as "
            + "Authentication__Audience (Phase 11.3); non-Aspire hosts (including "
            + "integration tests) must set it explicitly."
    );
builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var auth = builder.Configuration.GetSection("Authentication");

        options.RequireHttpsMetadata = true; // it's real https now
        options.BackchannelHttpHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        };

        // Don't rewrite sub/roles to legacy WS-* schema URIs; the adapter (11.6)
        // and Part 2's RBAC read the claim names the realm mappers emit.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authAuthority,

            ValidateAudience = true,
            ValidAudience = authAudience,

            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            // Tight, NTP-grade tolerance — not the 5-minute default that keeps an
            // expired token live for minutes past exp.
            ClockSkew = TimeSpan.FromSeconds(30),

            // With MapInboundClaims=false, name these explicitly so User.Identity.Name
            // and [Authorize(Roles=...)] (Part 2) read the right claims.
            NameClaimType = "preferred_username",
            RoleClaimType = "roles",
        };
    });

builder.Services.AddAuthorization(options =>
    options.AddPolicy("admin", policy => policy.RequireRole("admin"))
);

builder.Services.AddExceptionHandler<ValidationExceptionHandler>(); // ← added in 6.4
builder.Services.AddExceptionHandler<NotFoundExceptionHandler>(); // ← added in 6.6
builder.Services.AddExceptionHandler<DomainExceptionHandler>(); // ← added in 6.6
builder.Services.AddExceptionHandler<GlobalExceptionHandler>(); // ← added in 6.4

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler(); // ← added in 6.4
app.UseStatusCodePages(); // ← added in 11.7 (empty-body 401/403 -> problem+json via IProblemDetailsService)
app.UseAuthentication(); // ← added in 11.4 (validates a presented token; rejects nothing yet)
app.UseAuthorization(); // ← added in 11.4 (no RequireAuthorization until 11.7)
app.MapDefaultEndpoints();
app.MapWolverineEndpoints(opts => opts.RequireAuthorizeOnAll()); // ← 11.7: secure-by-default

app.UseHttpsRedirection();

app.Run();

/// <summary>
/// Marker type for Alba's <c>WebApplicationFactory&lt;Program&gt;</c>.
/// </summary>
public partial class Program;
