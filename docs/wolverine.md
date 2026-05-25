# Wolverine basics (Phase 5)

## 1) What Wolverine is in this project

Wolverine is this project's in-process messaging runtime for command/query
handler dispatch and HTTP endpoint integration. At this stage it is used to keep
message handling convention-first and explicit, without adding MediatR-style
marker interfaces.

For the team rule on why marker interfaces are intentionally excluded, see
[ADR 0003](./decisions/0003-wolverine-no-marker-interfaces.md). For framework
reference docs, see <https://wolverinefx.net/>.

## 2) Where messages and handlers live

Messages and their small handlers live in the Application layer under
`src/CurrencyTracker.Application/Messaging/`. For Phase 5, `PingQuery` and
`PingHandler` are co-located in the same file so the request shape and handler
logic are visible together.

This follows the
[Application mindset in AGENTS.md](../AGENTS.md#the-eleven-mindset-taxonomy):
Application owns CQRS messages and Wolverine handlers; API and Worker compose
and host them.

## 3) `IMessageBus.InvokeAsync<T>` shape

Phase 5.4 introduced the manual API endpoint shape that dispatches a message via
`IMessageBus` and awaits a typed response:

```csharp
app.MapGet(
    "/ping",
    (IMessageBus bus, CancellationToken ct) => bus.InvokeAsync<string>(new PingQuery(), ct)
);
```

Key points in that shape:

- `IMessageBus` is injected into the endpoint delegate.
- `InvokeAsync<T>` expresses the expected response type (`string` here).
- The endpoint forwards the request cancellation token to dispatch.

## 4) `[WolverineGet]` and the attribute family

Phase 5.7 moved the ping route declaration to the handler method itself using
Wolverine HTTP attributes:

```csharp
[WolverineGet("/ping")]
public static string Handle(PingQuery query) => "pong";
```

`[WolverineGet]` is one member of Wolverine's HTTP attribute family (`Get`,
`Post`, `Put`, `Patch`, `Delete`) used to map message handlers directly to HTTP
routes when that style is preferred.

## 5) Diagnostics (`dotnet run -- describe`)

Wolverine can print discovered handlers, routes, and runtime wiring using the
host diagnostic command:

- `dotnet run -- describe`

The project-specific walkthrough for reading this output lands in Phase 5.9 and
extends this section.

## 6) What lands later

This Phase 5 document stays intentionally minimal. Later phases build on it:

- **Phase 6:** FluentValidation middleware.
- **Phase 12:** Postgres outbox, scheduled messages, Worker.

Those capabilities are intentionally name-dropped here only; detailed guidance
belongs to the phases where they ship.
