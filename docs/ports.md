# Ports catalogue (Phase 4)

Every port in this catalogue is defined in the Application layer under
`src/CurrencyTracker.Application/Abstractions/`. Following the rule stated in
`AGENTS.md`'s "Ports and adapters" section — _interfaces live with the layer
that **needs** them, not with the layer that defines the model_ — Application
owns the socket shape; Infrastructure owns the plug. Adapters ship in later
phases; all eight ports were defined together in Phase 4 so handlers written
in Phase 5 onward could code against stable contracts from day one.

## `IDateTimeProvider`

**Purpose:** Replaces bare `DateTimeOffset.UtcNow` calls so tests can inject a
fixed clock without patching statics.

**Source:** `src/CurrencyTracker.Application/Abstractions/Time/IDateTimeProvider.cs`

```csharp
namespace CurrencyTracker.Application.Abstractions.Time;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
```

**Adapter:** `src/CurrencyTracker.Infrastructure/Time/SystemDateTimeProvider.cs` (Phase 4)  
**Fake:** `tests/CurrencyTracker.Application.UnitTests/Fakes/FixedDateTimeProvider.cs`

The single `UtcNow` property follows the "smallest possible surface" rule:
nothing about business logic requires wall-clock formatting, local-time zones,
or bare `DateTime`. The Phase 4 adapter wraps `DateTimeOffset.UtcNow`; the
fake stores a `DateTimeOffset` field seeded in the test constructor so every
handler invocation in a test sees a deterministic timestamp.

## `ICacheService`

**Purpose:** Provides get / set / remove and a cache-aside helper over a
key-value store so handlers can cache without taking a Redis dependency.

**Source:** `src/CurrencyTracker.Application/Abstractions/Caching/ICacheService.cs`

```csharp
namespace CurrencyTracker.Application.Abstractions.Caching;

public interface ICacheService
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken);

    Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken);

    Task RemoveAsync(string key, CancellationToken cancellationToken);

    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken
    );
}
```

**Adapter:** `src/CurrencyTracker.Infrastructure/Caching/RedisCacheService.cs` (Phase 10)  
**Fake:** `tests/CurrencyTracker.Application.UnitTests/Fakes/InMemoryCacheService.cs`

Cache values are `string`-shaped at the interface boundary so the contract
carries no JSON-library dependency; typed serialisation is the adapter's
concern. `GetOrSetAsync<T>` is included here because the cache-aside pattern
is ubiquitous enough that every adapter should implement it once rather than
each call-site reimplementing it. The TTL parameter is `TimeSpan` (relative)
rather than `DateTimeOffset` (absolute) because relative TTLs compose naturally
with jitter in the Redis adapter.

## `IExchangeRateProvider`

**Purpose:** Fetches an exchange-rate snapshot from an external data source
without exposing wire-level shapes to Application.

**Source:** `src/CurrencyTracker.Application/Abstractions/Providers/IExchangeRateProvider.cs`

```csharp
using CurrencyTracker.Domain.Common;
using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Rates;

namespace CurrencyTracker.Application.Abstractions.Providers;

public interface IExchangeRateProvider
{
    Task<Result<RateSnapshot>> FetchAsync(
        CurrencyCode baseCurrency,
        DateOnly asOf,
        CancellationToken cancellationToken
    );
}
```

**Adapter:** `src/CurrencyTracker.Infrastructure/Providers/FrankfurterRateProvider.cs` (Phase 9)  
**Fake:** `tests/CurrencyTracker.Application.UnitTests/Fakes/InMemoryExchangeRateProvider.cs`

The return type is `Result<RateSnapshot>` rather than a nullable or a thrown
exception because provider failures are expected during normal operation
(network unavailability, unsupported currency). Using the domain `Result` type
forces call-sites to handle both success and failure paths explicitly and avoids
the overhead of unwinding the stack for anticipated error cases.

## `IAlertNotifier`

**Purpose:** Dispatches a fired `Alert` to whatever delivery channel the
owning `AlertRule` specifies.

**Source:** `src/CurrencyTracker.Application/Abstractions/Notifications/IAlertNotifier.cs`

```csharp
using CurrencyTracker.Domain.Alerts;

namespace CurrencyTracker.Application.Abstractions.Notifications;

public interface IAlertNotifier
{
    Task SendAsync(Alert alert, CancellationToken cancellationToken);
}
```

**Adapter:** `src/CurrencyTracker.Infrastructure/Notifications/LogAlertNotifier.cs` (Phase 12)  
**Fake:** `tests/CurrencyTracker.Application.UnitTests/Fakes/InMemoryAlertNotifier.cs`

The interface has a single method that accepts the whole `Alert` aggregate so
the adapter can switch on `alert.Rule.Channel` internally; callers always use
one method regardless of how many channels are supported. The return type is
`Task` rather than `Task<bool>` because delivery confirmation is asynchronous
for most channels and a synchronous boolean would misrepresent semantics.
Bounce and delivery-receipt tracking is deferred to Phase 12's outbox.

## `ICurrentUser`

**Purpose:** Provides identity information for the caller of the current
request so handlers can answer "who is calling?" without taking a dependency
on `HttpContext` or `ClaimsPrincipal`.

**Source:** `src/CurrencyTracker.Application/Abstractions/Security/ICurrentUser.cs`

```csharp
namespace CurrencyTracker.Application.Abstractions.Security;

public interface ICurrentUser
{
    Guid? UserId { get; }

    bool IsAuthenticated { get; }

    IReadOnlyCollection<string> Roles { get; }

    string? Tenant { get; }
}
```

**Adapter:** `src/CurrencyTracker.Infrastructure/Security/HttpContextCurrentUser.cs` (Phase 11)  
**Fake:** `tests/CurrencyTracker.Application.UnitTests/Fakes/FakeCurrentUser.cs`

`UserId` is `Guid?` rather than a domain value object because user identity is
not yet modelled in the domain — adopting `Guid` avoids a premature domain
concept while keeping the type richer than `string`. `Roles` is
`IReadOnlyCollection<string>` rather than an enum so roles can be extended at
the adapter layer without touching Application. `Tenant` is `string?` for the
same reason: multi-tenancy is not yet modelled as a domain concept.

## `IUnitOfWork`

**Purpose:** Atomically persists outstanding changes tracked by the active
repository instances.

**Source:** `src/CurrencyTracker.Application/Abstractions/Persistence/IUnitOfWork.cs`

```csharp
namespace CurrencyTracker.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
```

**Adapter:** `src/CurrencyTracker.Infrastructure/Persistence/EfUnitOfWork.cs` (Phase 8)  
**Fake:** `tests/CurrencyTracker.Application.UnitTests/Fakes/RecordingUnitOfWork.cs`

Explicit transaction-control methods (`BeginTransactionAsync`, `CommitAsync`,
`RollbackAsync`) are deliberately omitted: the EF Core adapter relies on the
implicit transaction opened by `SaveChangesAsync`, and Application code that
needs distributed transactional behaviour will use Phase 12's Wolverine outbox
rather than reaching through this interface. The `int` return value mirrors EF
Core's convention of returning the number of state entries written.

## `ICurrencyRepository`

**Purpose:** Loads and saves `Currency` entities so handlers can query the
supported-currency catalogue without depending on EF Core.

**Source:** `src/CurrencyTracker.Application/Abstractions/Persistence/ICurrencyRepository.cs`

```csharp
using CurrencyTracker.Domain.Currencies;

namespace CurrencyTracker.Application.Abstractions.Persistence;

public interface ICurrencyRepository
{
    Task<IReadOnlyList<Currency>> GetAllAsync(CancellationToken cancellationToken);

    Task<Currency?> GetByCodeAsync(CurrencyCode code, CancellationToken cancellationToken);

    Task AddAsync(Currency currency, CancellationToken cancellationToken);
}
```

**Adapter:** `src/CurrencyTracker.Infrastructure/Persistence/EfCurrencyRepository.cs` (Phase 8)  
**Fake:** `tests/CurrencyTracker.Application.UnitTests/Fakes/InMemoryCurrencyRepository.cs`

This interface was originally defined in Domain during Phase 3 and moved here
in Phase 4, embodying the rule that interfaces live with the layer that needs
them. `GetAllAsync` returns `IReadOnlyList<Currency>` — not `IEnumerable` —
because callers typically materialise the full catalogue synchronously after
awaiting. `AddAsync` tracks, rather than immediately writes, so the caller
controls the flush boundary via `IUnitOfWork.SaveChangesAsync`.

## `IExchangeRateRepository`

**Purpose:** Loads and saves `RateSnapshot` aggregates so ingestion and query
handlers operate on domain objects rather than EF Core entities.

**Source:** `src/CurrencyTracker.Application/Abstractions/Persistence/IExchangeRateRepository.cs`

```csharp
using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Rates;

namespace CurrencyTracker.Application.Abstractions.Persistence;

public interface IExchangeRateRepository
{
    Task<RateSnapshot?> GetSnapshotAsync(
        CurrencyCode baseCurrency,
        DateOnly asOf,
        CancellationToken cancellationToken
    );

    Task SaveSnapshotAsync(RateSnapshot snapshot, CancellationToken cancellationToken);
}
```

**Adapter:** `src/CurrencyTracker.Infrastructure/Persistence/EfExchangeRateRepository.cs` (Phase 8)  
**Fake:** `tests/CurrencyTracker.Application.UnitTests/Fakes/InMemoryExchangeRateRepository.cs`

Like `ICurrencyRepository`, this interface migrated from Domain to Application
in Phase 4. The composite key `(CurrencyCode, DateOnly)` matches the
`RateSnapshot` aggregate identity so no separate ID type is needed.
`SaveSnapshotAsync` is named "save" rather than "add" to signal upsert
semantics: the Phase 8 adapter replaces an existing snapshot for the same
`(Base, AsOf)` pair rather than throwing on a duplicate.
