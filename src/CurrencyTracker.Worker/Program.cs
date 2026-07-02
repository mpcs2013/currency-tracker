using CurrencyTracker.Application;
using CurrencyTracker.Application.Abstractions.Persistence;
using CurrencyTracker.Application.Abstractions.Providers;
using CurrencyTracker.Infrastructure;
using CurrencyTracker.ServiceDefaults;
using JasperFx;
using Wolverine;
using Wolverine.EntityFrameworkCore;
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

    // Join Wolverine's messaging to the ApplicationDbContext transaction:
    // a handler's SaveChangesAsync and its outgoing/handled messages commit
    // together, or not at all.
    opts.UseEntityFrameworkCoreTransactions();

    // Apply that transactional wrapper to every handler automatically, so no
    // handler needs a [Transactional] attribute.
    opts.Policies.AutoApplyTransactions();

    // The ingestion handler depends on internal sealed adapters. Wolverine 6
    // cannot inline-construct internal types, and ServiceLocationPolicy.NotAllowed
    // (the default) forbids the fallback — opt these three ports into service
    // location, exactly as the Api does (ADR 0006).
    opts.CodeGeneration.AlwaysUseServiceLocationFor<IExchangeRateProvider>();
    opts.CodeGeneration.AlwaysUseServiceLocationFor<IExchangeRateRepository>();
    opts.CodeGeneration.AlwaysUseServiceLocationFor<IUnitOfWork>();
});

//var host = builder.Build();
//host.Run();

// Forward args to the JasperFx command line so the `describe` / `codegen`
// verbs work in this host, exactly as Phase 5.9 did for the Api. With no
// command argument this behaves identically to host.Run() — the AppHost
// launches the Worker with no args, so production startup is unchanged.
return await builder.Build().RunJasperFxCommands(args);
