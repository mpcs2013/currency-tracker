using CurrencyTracker.Application;
using CurrencyTracker.Application.Abstractions.Persistence;
using CurrencyTracker.Application.Abstractions.Providers;
using CurrencyTracker.Infrastructure;
using CurrencyTracker.ServiceDefaults;
using Wolverine;
using Wolverine.FluentValidation;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Registers ApplicationDbContext + the EF repositories, Frankfurter provider,
// cache, clock — the same seam the Api uses (Phase 8). The handlers the Worker
// dispatches resolve their ports through this.
builder.AddInfrastructure();

builder.UseWolverine(opts =>
{
    // Same convention discovery as the Api: scan the Application assembly for
    // *Handler types. The Worker is an IHost, not a WebApplication — there is
    // no MapWolverineEndpoints and no WolverineFx.Http here.
    opts.ApplicationAssembly = typeof(ApplicationAssemblyAnchor).Assembly;

    // The ingestion command carries a FluentValidation validator (Phase 9.5);
    // run it here too so a malformed command is rejected before the handler,
    // exactly as on the HTTP side.
    opts.UseFluentValidation();

    // The ingestion handler depends on internal sealed adapters. Wolverine 6
    // cannot inline-construct internal types, and ServiceLocationPolicy.NotAllowed
    // (the default) forbids the fallback — opt these three ports into service
    // location, exactly as the Api does (ADR 0006).
    opts.CodeGeneration.AlwaysUseServiceLocationFor<IExchangeRateProvider>();
    opts.CodeGeneration.AlwaysUseServiceLocationFor<IExchangeRateRepository>();
    opts.CodeGeneration.AlwaysUseServiceLocationFor<IUnitOfWork>();
});

var host = builder.Build();
host.Run();
