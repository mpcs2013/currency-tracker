# 0015 — Deploy topology: single-revision health-gated cutover, digest promotion

Date: 2026-07-26 · Status: accepted · Phase: 14.D (issue 14.37)

## Decision

Releases deploy as a new Container Apps revision in single-revision mode: the
platform provisions the new revision, evaluates it (probes per the 13.B contract
once 14.E arms them), and shifts traffic only when it stands up;
`_reusable-azure-deploy.yml` polls the revision to a pipeline verdict.

PROD receives the exact UAT artifact via `az acr import` by manifest **digest**
between the per-environment ACRs (ADR 0014 posture); **PROD never builds**. The
PROD deploy identity holds a single read-only AcrPull grant on the UAT ACR for
this path, declared in `modules/role-assignments` and valued only in the UAT
envelope.

## Rejected

- **Multiple-revision 0%→100% traffic split** (the build plan's 14.37 phrasing).
  Requires `revision_mode = "Multiple"` plus pipeline-driven
  `az containerapp ingress traffic` shifts that fight the Terraform-owned
  `traffic_weight { latest_revision = true }` block — which `ignore_changes`
  does not cover, so a pipeline shifting weights would be reverted by the next
  apply, or force the image treaty to grow a second exemption. It buys manual
  canarying nothing in this system's requirements asks for, while
  single-revision mode already guarantees the safety property that matters: a
  failed green never takes a request. Revisit if a genuine canary requirement
  appears.
- **Rebuild for PROD.** A rebuild — even of the same commit — is a different
  artifact: base-image drift between build times, timestamps, cache state.
  "Soaked in UAT" has to be a claim about the shipped bytes, not about a
  sibling built from the same source.
- **Tag-based promotion.** Tags are movable; digests are content. Addressing
  the source as `api@sha256:…` is what makes "the bytes UAT soaked are the bytes
  PROD runs" verifiable rather than asserted.

## Consequences

- Rollback is a redeploy of the previous SHA's digest; the UAT ACR retains it.
- The UAT→PROD AcrPull grant is the one deliberate cross-environment edge. It is
  pull-only, scoped to a single registry, and lives in the Terraform ledger
  rather than as a manual grant so it stays reviewable.
- What is genuinely lost is manual canarying. If that requirement appears, this
  ADR is the thing to supersede.

## Related

- ADR [0014](0014-OIDC-posture.md) — identity posture the promotion grant sits inside.
- [`docs/ci-cd/pipelines.md`](../ci-cd/pipelines.md) — the workflow graph and the strictness ladder.
