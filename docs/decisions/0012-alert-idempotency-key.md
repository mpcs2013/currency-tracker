# 0012 — Alert idempotency: (RuleId, AsOfDate) business key

## Status

Accepted — Phase 12 (issue 12.9).

## Context

The Worker's alert pipeline must satisfy the Phase 12 DoD: killing the
Worker mid-pipeline and restarting must not produce duplicate alert
dispatches. Wolverine's durable inbox (ADR 0011) dedupes by ENVELOPE id
— the same physical message is never handled twice — but a replayed
ingestion (crash-recovery re-run, an operator re-POSTing /admin/ingest
for the same day) produces NEW envelopes with the same business meaning.
The inbox cannot know that two different envelopes describe the same
fact; only the domain can.

## Decision

- An alert's business identity is (RuleId, AsOfDate): a rule fires at
  most once per observation date. `Alert` carries `AsOfDate` as a
  Domain property with a non-default invariant; `Alert.Create` takes it.
- A UNIQUE index on alerts(rule_id, as_of_date) enforces the identity
  in Postgres (migration AddAlertIdempotencyKey). The old single
  rule_id index is dropped — the composite's leftmost prefix covers it.
- The EfAlertRuleEvaluator pre-filters rules that already alerted for
  the date (the polite layer); the unique index catches concurrent
  races the query cannot see (the guarantee layer).

## Considered and rejected

- **Inbox-only ("configure Wolverine harder").** The inbox layer is
  correct and kept, but it dedupes envelopes, not facts; no inbox
  configuration can equate two distinct envelopes. Retention of handled
  envelope ids is also time-bounded.
- **A message-id convention (deterministic envelope ids from the
  business key).** Couples domain identity to transport plumbing and
  still dies at the inbox retention window.
- **A separate dedupe table keyed by (rule, date).** The date is part
  of what the alert MEANS; hiding it in a side table leaves the entity
  lying about its own identity.
- **Handler-level duplicate check only (no index).** Two concurrent
  evaluations both pass the check and both insert. Checks advise;
  constraints enforce.

## Consequences

- Alert.Create's signature widened; all construction sites updated in
  the same PR (test helpers funnel through one CreateAlert()).
- Re-ingesting a day is now safe end-to-end: the evaluator skips,
  and anything that slips past fails on the index instead of
  double-dispatching.
- The alerts table is empty pre-Phase 14, so the non-nullable column
  needs no backfill.

## Notes

The DoD's replay proof needs BOTH ADR 0011's durability (don't lose
work) AND this key (don't repeat work). Neither alone is sufficient.