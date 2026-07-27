---
name: ci-workflow-authoring
description: House style for authoring or changing GitHub Actions workflows in .github/workflows/ — the _reusable- library convention, rationale header comments, permissions model, pinned action versions, and the rule that pipelines.md is updated in the same PR. Use whenever creating, editing, reviewing or debugging any file under .github/workflows/.
---

# Authoring workflows in this repo

17 workflows: 10 `_reusable-*.yml` (libraries) and 7 leaves. The style is
regular enough that a new file should be indistinguishable from the existing
ones. Read the two closest neighbours before writing.

## The library rule

`_reusable-` prefix means **`workflow_call` only** — no `push`, no
`pull_request`, no `schedule`. A leaf is anything with a real trigger.

**A reusable with zero callers gets deleted, not kept.** `_reusable-build.yml`
was removed for exactly this (recorded drift 14.28/14.35): its steps duplicated
`_reusable-test.yml`'s first three, and an uncalled library is the anti-pattern
the prefix exists to prevent.

## Every file opens with a rationale header

Not a description — the *reasoning*, and for reusables the caller list. This is
the single most distinctive thing about the repo's workflows.

```yaml
# _reusable-terraform.yml — the enforced IaC gate (fmt/init/validate/
# tflint/checkov/plan[/apply]) over ARM_* OIDC. The `environment` input
# selects backend config, tfvars, OIDC trust, and AZURE_* vars TOGETHER
# (the 14.B switch-together rule, encoded). Callers: terraform-pr.yml,
# deploy-uat.yml, deploy-prod.yml.
name: _reusable-terraform
```

The headers carry archaeology, and it is load-bearing. `_reusable-format.yml`
records why restore must precede `dotnet format`. `_reusable-coverage-gate.yml`
records why the gate cannot live inside a single test job. When you change a
file, **update its header** — a stale rationale is worse than none.

If you add a caller to a reusable, add it to that reusable's `Callers:` line in
the same edit.

## Skeleton

```yaml
# <filename> — <what and why>. Callers: <list>.   (reusables only)
name: <matches the filename, minus .yml>

on:
  workflow_call:
    inputs:
      thing:
        description: Sentence with a capital and a full stop.
        type: string
        required: true
    outputs:
      result:
        description: What it is.
        value: ${{ jobs.job.outputs.result }}

permissions:
  contents: read          # workflow-level floor, always least-privilege

jobs:
  job:
    name: job
    runs-on: ubuntu-latest
    timeout-minutes: 10   # every job has one
    permissions:          # escalate per-job, never at workflow level
      contents: read
      id-token: write
    steps:
      - name: Checkout
        uses: actions/checkout@v7
```

Rules encoded above:

- **Every `workflow_call` input has a `description`.** No exceptions.
- **`permissions: contents: read` at workflow level**; widen inside the one job
  that needs it (`id-token: write` for OIDC, `security-events: write` for
  CodeQL, `pull-requests: write` for sticky comments).
- **Every job has `timeout-minutes`.**
- `name:` matches the filename.
- Leaves that can overlap declare `concurrency` with
  `cancel-in-progress: ${{ github.event_name == 'pull_request' }}` — cancel
  redundant PR runs, never cancel a trunk or deploy run.

## Pinned versions — check, don't remember

Grep for what the other 16 files use and match. Currently:

| Action | Version |
| --- | --- |
| `actions/checkout` | `v7` |
| `actions/setup-dotnet` | `v6` |
| `actions/cache` | `v6` |
| `actions/upload-artifact` / `download-artifact` | `v7` |

Majors move independently and `AGENTS.md` §Gotchas was wrong about
`setup-dotnet` for thirteen phases. Verify before you write.

`setup-dotnet` takes `global-json-file: global.json` — never a literal
`dotnet-version`. `global.json` is the single source.

## .NET step ordering

```yaml
- run: dotnet restore --locked-mode
- run: dotnet format --verify-no-changes --no-restore
```

`dotnet format` needs a restored project graph. csharpier is a **local** tool
(`.config/dotnet-tools.json`), so `dotnet csharpier check .` — a `check`
subcommand, not a `--check` flag.

## Deploy and IaC workflows

- **Switch together.** One `environment` input selects backend config, tfvars,
  OIDC trust and `AZURE_*` vars as a unit. Never let a caller mix a UAT backend
  with PROD credentials.
- **OIDC only.** `ARM_USE_OIDC: "true"` with `ARM_CLIENT_ID` / `ARM_TENANT_ID` /
  `ARM_SUBSCRIPTION_ID` from `vars`, plus `id-token: write` on that job. Never a
  long-lived secret (ADR 0014).
- **The image treaty.** Terraform `ignore_changes` the container image; the
  pipeline is the only thing that moves tags. Don't add a workflow step that
  makes Terraform own an image, or a Terraform change that makes the pipeline
  own infrastructure shape.
- **Single responsibility per stage.** `_reusable-docker-build.yml` exports a
  tarball and deliberately cannot push — a `push` input would let a caller skip
  the Trivy gate (recorded drift 14.29). Preserve that seam.
- **PROD never builds.** It imports the UAT artifact by manifest digest
  (ADR 0015). Never add a build step to a prod path.

## Coverage producers

Any new test job uploads its raw Cobertura as `coverage-raw-<something>`.
`_reusable-coverage-gate.yml` pattern-matches `coverage-raw-*`, so following the
naming convention is the entire integration. The floor is `58`, ratchet **up**
only.

## Before you finish: update `docs/ci-cd/pipelines.md`

That doc is explicitly derived from these files and rots the moment a workflow
changes without it. **In the same PR:**

1. Update the graph / gate description for what you changed.
2. If the change departs from the phase plan, add a row to §Recorded drifts:
   `| <phase> | Plan said | Implemented | Why |`.

That ledger is a ratified record, not a bug list — it is why the
`docs-drift-auditor` agent treats listed drifts as settled.

## Don't

- Don't add a `_reusable-` file without a caller in the same PR.
- Don't grant a permission at workflow level to satisfy one job.
- Don't skip the header comment because the file "is obvious".
- Don't fix a CI failure by loosening a gate — `.trivyignore` is a reviewed
  allowlist, the coverage floor only ratchets up, and `TreatWarningsAsErrors`
  stays on.
- Don't run workflows from a PR head against real Azure. `.github/CODEOWNERS`
  guards `/.github/workflows/` and `/infra/` for this reason.
