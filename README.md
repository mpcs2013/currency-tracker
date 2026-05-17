# CurrencyTracker

A Clean Architecture currency tracker built on
.NET 10, Wolverine, Aspire, EF Core + Postgres, Redis, JWT auth,
OpenTelemetry, and Azure — demonstrating production-grade patterns and practices
across every phase of development.

<!-- Workflow status badges (resolve once Phase 0.D ships the workflows). -->

[![PR validation](https://github.com/mpcs2013/currency-tracker/actions/workflows/pr-validation.yml/badge.svg)](https://github.com/mpcs2013/currency-tracker/actions/workflows/pr-validation.yml)
[![main CI](https://github.com/mpcs2013/currency-tracker/actions/workflows/main-ci.yml/badge.svg?branch=main)](https://github.com/mpcs2013/currency-tracker/actions/workflows/main-ci.yml)
[![UAT deploy](https://github.com/mpcs2013/currency-tracker/actions/workflows/deploy-uat.yml/badge.svg)](https://github.com/mpcs2013/currency-tracker/actions/workflows/deploy-uat.yml)
[![PROD deploy](https://github.com/mpcs2013/currency-tracker/actions/workflows/deploy-prod.yml/badge.svg)](https://github.com/mpcs2013/currency-tracker/actions/workflows/deploy-prod.yml)
[![License: Apache 2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)

---

## What this is

CurrencyTracker is a small SaaS-style service:

- **Ingests** daily FX rates from a public provider (Frankfurter) on a schedule.
- **Persists** rates to Postgres via EF Core.
- **Exposes** a cached JSON API (`/api/v1/rates/latest`, `/api/v1/rates/history`) behind JWT auth.
- **Alerts** on user-defined rules ("tell me if EUR/USD moves more than 1.5% in a day").
- **Emits** OpenTelemetry traces, metrics, and structured logs to the Aspire dashboard locally and Azure Monitor / Application Insights in UAT and PROD.
- **Deploys** to Azure Container Apps via Terraform and a four-workflow GitHub Actions pipeline using OIDC federation (no long-lived secrets).

A reference implementation of production microservice patterns: comprehensive enough to exercise every cross-cutting concern, yet focused enough to remain approachable. The full multi-phase build plan is in [`docs/CurrencyTracker-BuildPlan-v3.md`](docs/CurrencyTracker-BuildPlan-v3.md).

## Prerequisites

| Tool             | Minimum version | Install                                              |
| ---------------- | --------------- | ---------------------------------------------------- |
| .NET SDK         | 10.0.300 (LTS)  | <https://dotnet.microsoft.com/download/dotnet/10.0> |
| Git              | 2.40+           | <https://git-scm.com/downloads>                      |
| Docker Desktop\* | 4.30+           | <https://www.docker.com/products/docker-desktop>     |
| Node.js\*\*      | 22.x LTS        | <https://nodejs.org/en/download>                     |
| Azure CLI\*\*\*  | 2.60+           | <https://learn.microsoft.com/cli/azure/install-azure-cli> |
| Terraform\*\*\*  | 1.9+            | <https://developer.hashicorp.com/terraform/install>  |

\* Required from Phase 7 (Aspire AppHost spins up Postgres + Redis containers).<br>
\*\* Required from Phase 16 (React + TypeScript frontend; optional).<br>
\*\*\* Required from Phase 14 (Azure deploy).

## Quickstart

From a fresh clone, today:

```bash
git clone https://github.com/mpcs2013/currency-tracker.git
cd currency-tracker

# Verify the SDK is pinned correctly (will fail loudly if you're on the wrong major):
dotnet --version
# Expect: 10.0.x (matching global.json's pin)

# Restore the local toolchain (CSharpier, etc):
dotnet tool restore
# Expect: "Tool 'csharpier' was restored."

# Sanity-check the formatter:
dotnet csharpier --version
# Expect: a 1.x version (matching .config/dotnet-tools.json)
```