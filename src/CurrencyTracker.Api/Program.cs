using CurrencyTracker.Application;
using CurrencyTracker.Application.Messaging;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.UseWolverine(opts =>
{
    opts.ApplicationAssembly = typeof(ApplicationAssemblyAnchor).Assembly;
});

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet(
    "/ping",
    (IMessageBus bus, CancellationToken ct) => bus.InvokeAsync<string>(new PingQuery(), ct)
);

app.UseHttpsRedirection();

app.Run();
