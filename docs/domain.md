# Domain model (Phase 3)

`RateSnapshot` is the sole aggregate root in the Phase 3 domain because it enforces cross-rate invariants for one base currency and one as-of date (shared base/date and unique quote rates). `ExchangeRate` records are part of that aggregate boundary and are composed through the snapshot.

```mermaid
classDiagram
    class Currency {
        +CurrencyCode Code
        +string Name
        +int NumericCode
    }
    <<entity>> Currency

    class ExchangeRate {
        +CurrencyCode Base
        +CurrencyCode Quote
        +decimal Rate
        +DateOnly AsOf
    }
    <<entity>> ExchangeRate

    class RateSnapshot {
        +CurrencyCode Base
        +DateOnly AsOf
        +IReadOnlyList~ExchangeRate~ Rates
    }
    <<entity>> RateSnapshot
    <<aggregate root>> RateSnapshot

    class AlertRule {
        +Guid Id
        +Guid OwnerId
        +CurrencyCode Base
        +CurrencyCode Quote
        +decimal ThresholdPercent
        +AlertChannel Channel
        +bool Enabled
    }
    <<entity>> AlertRule

    class Alert {
        +Guid Id
        +Guid RuleId
        +decimal PreviousRate
        +decimal CurrentRate
        +decimal ObservedChangePercent
        +DateTimeOffset FiredAt
    }
    <<entity>> Alert

    class CurrencyCode {
        +string Value
    }
    <<value object>> CurrencyCode

    class Money {
        +decimal Amount
        +CurrencyCode Currency
    }
    <<value object>> Money

    class RateIngested {
        +CurrencyCode Base
        +DateOnly AsOf
        +int RateCount
    }
    <<event>> RateIngested

    class AlertTriggered {
        +Guid AlertId
        +Guid RuleId
        +decimal ObservedChangePercent
        +DateTimeOffset FiredAt
    }
    <<event>> AlertTriggered

    RateSnapshot "1" *-- "1..*" ExchangeRate : rates
    Currency "1" --> "1" CurrencyCode : code
    ExchangeRate "1" --> "1" CurrencyCode : base
    ExchangeRate "1" --> "1" CurrencyCode : quote
    AlertRule "1" --> "1" CurrencyCode : base
    AlertRule "1" --> "1" CurrencyCode : quote
    Money "1" --> "1" CurrencyCode : currency
    Alert "*" --> "1" AlertRule : triggered by
    RateIngested ..> RateSnapshot : emitted after ingest
    AlertTriggered ..> Alert : emitted when fired
```
