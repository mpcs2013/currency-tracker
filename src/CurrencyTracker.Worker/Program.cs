using CurrencyTracker.Infrastructure;
using CurrencyTracker.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddInfrastructure();

var host = builder.Build();
host.Run();
