# 0019 — Environment hibernation: park UAT by scaling to zero and deleting the cache, not by destroying it

- **Status:** Accepted
- **Date:** 21.09.2026
- **Authors:** Marco Silva
- **Supersedes:** —
- **Related:** 0007-redis-distributed-cache.md (the cache this deletes and
  recreates), 0014-OIDC-posture.md (14.58's teardown, the heavier option this
  rejects for now), 0015-deploy-topology.md (the image treaty that makes the
  ACR worth keeping), 0018 (the migration job, a third consumer of the cache
  secret), Phase 14.60

## Context

Phase 14 closed with 338 issues and none open. Nothing has been deployed to UAT
since, and nothing is scheduled to be. The environment kept billing anyway.

Cost Management for August 2026, by meter, in CHF:

| Meter | Cost |
| --- | --- |
| Container Apps — idle memory + idle vCPU | 17.74 |
| `B0 Cache Instance` (744 h) | 13.96 |
| `Standard IPv4 Static Public IP` | 3.05 |
| Container Apps — active memory + active vCPU | 2.34 |
| Key Vault operations | 0.02 |
| **Total** | **37.12** |

September holds at CHF 1.165/day. Two facts shaped the decision:

**Essentially the whole bill is idle.** `min_replicas = 1` on the Api and the
Worker keeps two containers warm around the clock with no traffic to serve, and
Azure Managed Redis bills a flat hourly rate with no scale-to-zero at any tier —
`Balanced_B0` is already the floor.

**Everything that would be expensive to rebuild is already free.** The
subscription is inside its 12-month free-services window, and the meters name it
outright: `B1MS Compute - Free`, `Storage Data Stored - Free` (32 GB),
`Standard Registry Unit - Free`, `Standard Included LB Rules - Free`. Postgres
with the Phase 8 schema, the ACR with every image `main-ci` has pushed, Key
Vault, the VNet and Log Analytics cost CHF 0.00 between them.

That inverts the obvious move. "Delete it all and redeploy for a deep test"
removes the free things whose rebuild is expensive, to save nothing. And the
rebuild genuinely is expensive: `main.tf` hardcodes `use_acr_registry = true`,
populated `key_vault_secrets` and `health_probes_enabled = true`, each documented
in place as requiring a fresh-environment two-pass, because a system-assigned
identity cannot be granted `AcrPull` before the resource that mints it exists. A
destroyed ACR also reds every `main-ci` run at `push-api`/`push-worker`, and a
destroyed Postgres means redeploying onto an empty database — the failure mode
already recorded against 14.48.

## Decision

- **One root variable, `hibernated`, with no default, set explicitly in both
  environment envelopes** — `true` in UAT, `false` in PROD. It follows
  `postgres_zone_redundant` and `enable_public_network_access`: an
  environment-shape flag is stated by each envelope, never defaulted.
- **It does exactly two things, and they are coupled deliberately:** both
  container apps drop to `min_replicas = 0`, and `module.redis` leaves the
  configuration via `count`. Half the money lives in each. Parking the apps
  while a cache bills a full month for no clients saves half the money for all
  of the inconvenience, so the flag does not offer that state.
- **Everything downstream of the cache is conditional on the same flag:** the
  `connectionstrings-cache` Key Vault secret, the container-app secret and its
  `ConnectionStrings__cache` env var on all three consumers (Api, Worker,
  migrate job), and the access-policy assignment in `modules/role-assignments`
  — the last via `count` on a nullable `managed_redis_id`, the same shape
  `promotion_acr_pull` already uses.
- **A hibernated environment is deliberately unable to serve traffic or run a
  migration.** `AddInfrastructure()` fail-fasts on a missing
  `ConnectionStrings__cache` in every host, so waking a container without
  applying first dies at boot with a message naming the cause. That is the
  intended behaviour, not a gap to paper over.
- **Nothing else is touched.** Postgres, the ACR, the Key Vault resource, the
  VNet, Log Analytics, App Insights, the storage account, the migrate identity
  and every grant survive. The plan is 3 updates and 4 deletes.

Measured effect: CHF ~35/month to CHF ~3/month. The residual is the Container
Apps environment's public IP, which belongs to the environment resource and can
only be removed by deleting it — that is 14.58's teardown, not this.

## Consequences

- **Waking UAT is one flag and one apply**, then the migration job, then a
  `deploy-uat` dispatch against a SHA `main-ci` has already pushed. The images
  are still in the ACR and the schema is still in Postgres, so neither step
  rebuilds anything.
- **The cache returns at a new hostname.** `modules/redis` suffixes the name
  with a `random_string` that is destroyed with the module, so a woken cache is
  never at its old address. The `connectionstrings-cache` vault secret is
  recreated from the new module output in the same apply — but the vault has
  `purge_protection_enabled = true` and a 90-day soft delete, so that create
  goes through the provider's recover-then-set path. **Verify the secret's value
  matches the live cache hostname before smoking a woken environment.** The
  failure mode if it does not is loud (readiness cannot resolve a dead host),
  and the fix is a second apply.
- **`/health/ready` cannot pass while hibernated.** It checks Postgres *and*
  Redis. Any monitoring that treats UAT readiness as a signal will be red for
  as long as the environment is parked; that is accurate, not noise.
- **The Worker does not wake on its own.** It has no ingress and therefore no
  HTTP scale rule, so `min_replicas = 0` is terminal until an apply raises it.
  The Api does wake on its own — the first request through ingress activates a
  replica — which keeps UAT addressable at the cost of one cold start.
- **This is not a substitute for 14.58.** When the free-services window closes,
  Postgres and the ACR start billing at roughly CHF 30/month combined and the
  arithmetic here inverts. At that point the real teardown, and the single-pass
  cold apply that 14.59 needs, become the correct answer. Revisit this ADR on
  that date rather than on a schedule.

## Alternatives considered

- **`terraform destroy` the UAT environment (CHF ~0/month).** Saves CHF 3 more
  than this does, and costs the ACR images, the database schema, a red
  `main-ci` on every merge, and the three-flag bootstrap dance above. Rejected
  on the arithmetic: the marginal saving is CHF 3/month while the free-services
  window holds. Recorded as the right answer once it does not.
- **Scale to zero and keep the cache (CHF ~17/month).** Keeps `/health/ready`
  honest and the wake-up trivial, but leaves the second-largest line item
  untouched for a convenience nobody needs while no issue is open.
- **A smaller cache tier.** There is none. `Balanced_B0` is Azure Managed
  Redis's entry SKU, and the Basic/C0 that would have been cheaper died with
  Azure Cache for Redis (see `modules/redis`).
- **Stopping the Postgres server.** Azure Flexible Server auto-restarts after
  seven days, and the compute is free anyway. No saving, plus a recurring chore.
- **`az containerapp update --min-replicas 0` by hand.** Stops the meter the
  same afternoon, but `min_replicas` is not in the image treaty's
  `ignore_changes`, so the next apply silently reverts it. State drift as a
  cost-control mechanism is how the bill comes back without anyone noticing.
