# Wolverine in CurrencyTracker

This doc covers the Wolverine setup at the end of Phase 5: one
in-process message, one handler, one HTTP route. Phase 6 will add
FluentValidation middleware; Phase 12 will add the Postgres-backed
outbox, scheduled messages, and the Worker. Until then, Wolverine is
the project's mediator: a message goes in, a handler runs, a result
comes back. No queues, no persistence, no transports.

## What Wolverine is in this project

[WolverineFx](https://wolverinefx.net/) is the message bus this
project uses to dispatch CQRS messages. The choice is recorded in
`docs/decisions/0001-stack-choices.md` (the rejection of MediatR /
MassTransit). The convention-based-discovery rule that comes with
WolverineFx — *no `IRequest<T>` markers on messages* — is recorded in
`docs/decisions/0003-wolverine-no-marker-interfaces.md`.

At the end of Phase 5, Wolverine runs in-process only. There are no
external transports configured (no RabbitMQ, no Azure Service Bus, no
Postgres outbox). Every call to `IMessageBus.InvokeAsync<T>(...)`
dispatches synchronously inside the awaited call.

## Where messages and handlers live

Messages and their handlers live in
`src/CurrencyTracker.Application/Messaging/`. The Application layer
owns the message contract and the handler; Api and Worker are
composition roots that *invoke* handlers but don't define them. This
is the same Clean Architecture pattern named in the
[Application row of the mindset taxonomy](../AGENTS.md#the-eleven-mindset-taxonomy):
*Owns: Ports, CQRS messages, Wolverine handlers, validators.*

When a handler is small (six lines, no dependencies — like
`PingHandler`), the message record and the handler live in the same
file. When a handler grows past ~80 lines or gains constructor
dependencies, split into `<Name>Query.cs` / `<Name>Handler.cs` in the
same folder.

## Dispatching a message: `IMessageBus.InvokeAsync<T>`

The dispatch shape from `Api/Program.cs` (or any other call site that
holds a registered `IMessageBus`):

```csharp
app.MapGet("/something", (IMessageBus bus, CancellationToken ct) =>
    bus.InvokeAsync<string>(new PingQuery(), ct));
```

## Diagnostics

JasperFx's command-line integration (wired via `RunJasperFxCommands` in
`Program.cs`) exposes a `describe` command that prints every discovered
handler, every routed message, every HTTP endpoint, and every configured
Wolverine option. To capture it locally:

1. Publish a local debug build:
       dotnet publish src/CurrencyTracker.Api -c Debug -o ./publish-debug
2. Invoke the published exe directly (Windows cmd.exe shown; equivalent
   path on macOS/Linux):
       publish-debug\CurrencyTracker.Api.exe describe --file docs\wolverine-describe.txt

The current capture lives at [`docs/wolverine-describe-phase-5.txt`](./wolverine-describe-phase-5.txt).

Refresh the capture each time the Wolverine surface area materially
changes — Phases 6, 9, and 12 are good moments. Do *not* use
`dotnet run -- describe`; on Windows PowerShell the `--` separator is
swallowed and the host starts normally instead of running the command.
