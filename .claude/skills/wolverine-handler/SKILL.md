---
name: wolverine-handler
description: Wolverine conventions and footguns for this codebase — convention-based handler discovery with no marker interfaces, the AlwaysUseServiceLocationFor codegen opt-in that must match across both hosts, the Postgres outbox/inbox, and business-key idempotency. Use whenever adding or changing a message, a Wolverine handler, the outbox, scheduling, or either host's UseWolverine block.
---

# Wolverine in this repo

Wolverine 6 is the in-process bus. **Not MediatR** (ADR 0001), and the MediatR
shape dominates training data — so the instinct to reach for `IRequest<T>` will
show up. Reject it every time.

## No marker interfaces (ADR 0003)

Handlers are discovered by **scanning for public types named `*Handler`** and
binding on the `Handle`/`Consume` method's **parameter type**. The parameter
type *is* the contract.

- No `IRequest<T>`, `IRequestHandler<T>`, `ICommand`, `IMessage`, `IDomainEvent`.
  Markers fight the framework.
- Messages live in `src/CurrencyTracker.Application/Messaging/`. One file per
  message; one folder per slice once slices grow.
- Handler goes in the **same file as the message** while it is small; split past
  ~80 lines.
- Handler shape is `public static class` with a `public static` Handle method —
  instance only when it needs scoped DI on the instance itself.
- Cascading messages: return the next message from the handler rather than
  injecting a bus and publishing inline.

## The `AlwaysUseServiceLocationFor<T>` footgun

The most expensive recurring failure here. The mechanism (ADR 0006):

Infrastructure adapters are `internal sealed` by the cross-layer guardrails.
Wolverine 6 generates dispatch code into a separate assembly and prefers to
**inline-construct** dependencies, which needs a *public* concrete type. For an
internal concrete it would emit a service-locator call — but Wolverine 6's
default `ServiceLocationPolicy.NotAllowed` forbids that and throws
`InvalidServiceLocationException` **at host startup**.

The opt-in resolves the collision, keeping adapters internal:

```csharp
opts.CodeGeneration.AlwaysUseServiceLocationFor<IExchangeRateProvider>();
```

**There are two hosts and both set `ApplicationAssembly` to the same Application
assembly**, so both discover the same handlers:

| | `src/CurrencyTracker.Api/Program.cs` | `src/CurrencyTracker.Worker/Program.cs` |
| --- | --- | --- |
| `IExchangeRateProvider` | ✅ | ✅ |
| `IExchangeRateRepository` | ✅ | ✅ |
| `IUnitOfWork` | ✅ | ✅ |
| `IAlertNotifier` | ✅ | ✅ |
| `IAlertRuleEvaluator` | — | ✅ |
| `IAlertRepository` | — | ✅ |

**When you add a port that a handler depends on, add the opt-in to both hosts in
the same PR** unless you have positively established the Api never codegens a
handler that needs it. The lists are asymmetric today, so don't infer the rule
from the current state — the two alert ports are the exception, not the pattern,
and an opt-in added to one host alone has broken the other before.

The failure is at **startup**, not compile time. `AppHost.SmokeTests` is what
catches it, so run the full suite, not just unit tests.

## Outbox, inbox and transactions (ADR 0011)

Worker wiring, in `UseWolverine`:

```csharp
opts.PersistMessagesWithPostgresql(currencyTrackerConnectionString, "wolverine");
opts.Policies.AutoApplyTransactions();
```

- The durable inbox/outbox rides the **existing** `currencytracker` Postgres in a
  dedicated `wolverine` schema. One database, one transaction spanning app state
  and the outgoing message.
- `AutoApplyTransactions()` means **no handler needs a `[Transactional]`
  attribute**. Don't add them.
- Any handler that changes state *and* cascades a message depends on this. Don't
  publish through an injected bus to "keep it simple" — that reintroduces the
  divergence the outbox exists to prevent.

## Idempotency is a business key, not an envelope id (ADR 0012)

The inbox dedupes by **envelope id** — the same physical message is never
handled twice. It cannot know that two *different* envelopes describe the same
fact (a crash-recovery re-run, an operator re-POSTing `/admin/ingest` for the
same day). Only the domain can.

The pattern, both layers required:

1. **The polite layer** — a query that pre-filters work already done
   (`EfAlertRuleEvaluator` skips rules that already alerted for the date).
2. **The guarantee layer** — a UNIQUE index enforcing the business identity in
   Postgres (`alerts(rule_id, as_of_date)`), catching concurrent races the query
   cannot see.

When you add a new cascade, ask what its business key is. "The inbox handles it"
is the wrong answer.

## Checklist for a new message + handler

- [ ] Message is a `sealed record` in `Application/Messaging/`, no marker interface.
- [ ] Handler is `public static class *Handler` with a static `Handle`.
- [ ] Any new port it depends on has an `AlwaysUseServiceLocationFor` opt-in in
      **both** hosts (see above).
- [ ] Async methods take `CancellationToken` with **no default value**.
- [ ] Logging uses `[LoggerMessage]` source-generated methods on a `partial`
      class — CA1848 is a build error. Don't pass
      `exception.GetType().FullName` as a parameter; pass the `Exception`
      (CA1873).
- [ ] If it cascades and changes state, the business key and its unique index exist.
- [ ] Failing tests first. Ports get in-memory **fakes** under
      `tests/CurrencyTracker.Application.UnitTests/Fakes/`, not NSubstitute
      mocks — mocks only when the call shape is the contract.
- [ ] `dotnet test -c Release` including `AppHost.SmokeTests`.

## Debugging

`docs/wolverine-describe-phase-5.txt` and `docs/worker-wolverine-describe.txt`
are captured `WolverineDescribe` dumps — compare against a fresh dump to see
what discovery and codegen actually resolved. `docs/wolverine.md` is the
narrative companion.

## Related

- ADR [0003](../../../docs/decisions/0003-wolverine-no-marker-interfaces.md) — discovery, no markers.
- ADR [0006](../../../docs/decisions/0006-wolverine-service-location-for-internal-adapters.md) — the codegen opt-in.
- ADR [0011](../../../docs/decisions/0011-worker-outbox-and-scheduling.md) — outbox/inbox and scheduling.
- ADR [0012](../../../docs/decisions/0012-alert-idempotency-key.md) — business-key idempotency.
