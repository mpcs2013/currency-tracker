# 0013 — Serilog for structured logging (Console JSON + Seq + OTLP sinks)

## Status

Accepted (Phase 13.1).

## Context

Through Phase 12 the hosts used the default Microsoft.Extensions.Logging
provider with a single MEL→OTel logging registration in ServiceDefaults.
That path has no enrichment, no redaction seam (the Phase 11.12 forward
rule requires one), no structured console output, and no dev-searchable
log store. All production log sites are [LoggerMessage] source-generated
partials (CA1848 as error) — any logging change must preserve them.

## Decision

Register Serilog as the logging provider behind ILogger<T> in
ServiceDefaults (one generic ConfigureSerilog method, both hosts), with
three sinks: Console via CompactJsonFormatter (always), Seq via
Serilog.Sinks.Seq (when ConnectionStrings:seq is present — injected by
the Aspire Seq resource, 13.7), and OTLP via Serilog.Sinks.OpenTelemetry
(when OTEL_EXPORTER_OTLP_ENDPOINT is set — Aspire in dev, the Azure
collector in Phase 14). The MEL→OTel logging registration is removed;
metrics and tracing keep the Phase 7 OTel SDK path. Call sites are
untouched. Levels come from each host's "Serilog" configuration section.
This ADR covers the Serilog dependency family for the phase:
Serilog.AspNetCore, Serilog.Sinks.Seq, Serilog.Sinks.OpenTelemetry, and
the 13.2 enricher package Serilog.Enrichers.Environment. Versions are
pinned in Directory.Packages.props.

## Alternatives considered

- **Stay on the MEL OTel provider alone.** No enricher pipeline, so the
  13.3 redaction rule has no seam; no Seq sink; console output stays
  unstructured text. Rejected.
- **Aspire.Seq client integration (AddSeqEndpoint).** Ships logs+traces
  to Seq over OTLP without Serilog. Rejected: it solves only the Seq
  destination — no enrichment/redaction pipeline, no structured console
  — and running it alongside Serilog would double-ship every event.
- **Rewrite call sites to Serilog's static Log.* API.** Breaks the
  CA1848 [LoggerMessage] convention, loses compile-time template checks
  and ILogger<T> categories. Rejected on standing convention.

## Consequences

Every log event flows through one pipeline with enrichment (13.2) and
redaction (13.3) applied uniformly across hosts. The Aspire dashboard's
structured-log view is fed by the OTLP sink rather than the MEL
provider. Serilog family upgrades are a props-file + ADR concern.

Seq runs as a dev-only Aspire resource (13.7), excluded from the
deployment manifest; production log search is the OTLP path's concern.