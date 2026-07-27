# UAT deploy runbook

How to put a build into UAT: what to run, where the pipeline stops and waits for
a human, and what to type when a step goes red.

[`pipelines.md`](pipelines.md) describes the *graph* — what blocks what, and why
the seams sit where they do. This file is the *procedure*. Where the two
disagree, the workflow files win and one of these documents is stale; run
`/drift`.

**UAT does not move when you merge.** It moves when someone dispatches
`deploy-uat` with the SHA of a build they have chosen to sign off. That is
recorded drift 14.36, not an oversight — see [§What this pipeline deliberately
does not do](#what-this-pipeline-deliberately-does-not-do).

---

## 0. Pre-flight

Four conditions decide whether a dispatch can succeed.

### Blocking: the `user` app role is not assigned to `gh-deploy-uat`

Smoke leg 3 requests a token for `SMOKE_TOKEN_RESOURCE` and calls
`/api/v1/rates/latest`. That variable **is** set on the `uat` environment, so the
leg runs. Without the app role the token carries no role, the Api refuses the
request, the payload assertion fails — and because `deploy-uat.yml` sets
`strict: true`, a failed smoke leg is a **failed deploy**, not a warning.

It fails at the *last* step, after Terraform has applied and both apps have been
updated. Resolve it before the first dispatch.

Listed as **pending** in [`pipelines.md`](pipelines.md) §Manual prerequisites.

```bash
# 1. The API app registration's appRole id for "user"
az ad app show --id e50b769e-1b9e-487d-baf5-7108f98935f2 \
  --query "appRoles[?value=='user'].{value:value,id:id}" -o table

# 2. Object ids: the API's service principal, and gh-deploy-uat's
az ad sp list --filter "appId eq 'e50b769e-1b9e-487d-baf5-7108f98935f2'" --query "[0].id" -o tsv
az ad sp list --display-name gh-deploy-uat --query "[0].id" -o tsv

# 3. What gh-deploy-uat already holds (empty output = the role is missing)
az rest --method GET \
  --url "https://graph.microsoft.com/v1.0/servicePrincipals/<gh-deploy-uat-sp-id>/appRoleAssignments"

# 4. Grant it. Needs an admin; this is the mutating step.
az rest --method POST \
  --url "https://graph.microsoft.com/v1.0/servicePrincipals/<gh-deploy-uat-sp-id>/appRoleAssignments" \
  --headers "Content-Type=application/json" \
  --body '{"principalId":"<gh-deploy-uat-sp-id>","resourceId":"<api-sp-object-id>","appRoleId":"<user-role-id>"}'
```

These are written from the identity values recorded in
[`../azure/bootstrap.md`](../azure/bootstrap.md), not from a successful run. An
app-role grant is a tenant change with an admin-consent step — read each command
before running it.

### The other three

| Condition | State | Note |
| --- | --- | --- |
| `uat` environment variables | ✅ present | `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `AZURE_RESOURCE_GROUP`, `SMOKE_TOKEN_RESOURCE` |
| Environment name resolution | ✅ verified | Workflows say `environment: uat`; the environment is stored as `UAT`. GitHub matches case-insensitively — the API returns `{"name":"UAT"}` with its rules intact, not a silently-created second environment. |
| Required reviewer | ⚠ expect a pause | `mpcs2013`. Every job declaring `environment: uat` waits. Unlike PR review, GitHub permits approving your own deployment. |
| Branch policy | ⚠ constraint | `main` only. Any other ref is refused before the first step runs. |

---

## 1. The first deploy ever (one-time)

Skip this section once it has succeeded.

No image pair has ever reached ACR. Every `main-ci` run failed at the Trivy gate:
`_reusable-trivy-scan.yml` points `trivyignores` at a `.trivyignore` that has
never existed, and the action treats a missing ignorefile as fatal. So `push-api`
and `push-worker` were always skipped, and `deploy-uat`'s gate has nothing to
find.

1. **Merge the unblocking PRs.** #398 adds the missing `.trivyignore` — empty of
   entries, which keeps the gate exactly as strict. #395 then #396 (stacked) turn
   `terraform-pr` green; not required for a deploy, but `deploy-uat` runs the same
   Terraform gate when `apply-terraform=true`.

2. **Let `main-ci` run on the merge commit.** Merging is itself a push to `main`.
   The trunk pipeline certifies the merge commit, builds both images, Trivy-gates
   them, and pushes them SHA-tagged to the UAT ACR. It does not deploy anything.

   ```powershell
   gh run watch $(gh run list --workflow=main-ci.yml --branch=main --limit 1 `
     --json databaseId -q '.[0].databaseId')
   ```

   > **Hold — first time this has ever happened.** `push-api` and `push-worker`
   > declare `environment: uat`, so `main-ci` stops and waits for approval before
   > pushing. These two jobs have never executed in the history of the repo, so
   > this pause has never been seen; it reads as a hang if you are not expecting
   > it. It is also the first time anything authenticates to ACR over OIDC — if
   > the federated credential subject or the `AcrPush` permission has a gap, it
   > surfaces here, not later in `deploy-uat`.

3. **Confirm the images landed.** Both repositories must show the same SHA; the
   gate checks for exactly that pair.

   ```powershell
   $rg  = "rg-currencytracker-uat"
   $acr = az acr list -g $rg --query "[0].name" -o tsv
   az acr repository show-tags -n $acr --repository api    --orderby time_desc --top 5 -o table
   az acr repository show-tags -n $acr --repository worker --orderby time_desc --top 5 -o table
   ```

---

## 2. Deploying a signed-off build

The repeatable loop. Roughly 8–15 minutes end to end, most of it Terraform and
revision polling.

### 2.1 Choose the build

A successful `main-ci` run is a release candidate: images in ACR tagged with the
merge commit. Pick the one you intend to sign off — not necessarily the newest.

```powershell
gh run list --workflow=main-ci.yml --branch=main --status=success --limit 10 `
  --json headSha,displayTitle,createdAt
```

An empty array means no trunk run has ever completed successfully, so there is
nothing deployable. Go back to §1.

### 2.2 Dispatch

```powershell
gh workflow run deploy-uat.yml `
  -f image-tag=<full-commit-sha> `
  -f apply-terraform=true
```

`image-tag` is the full SHA of a `main-ci` run that pushed images. Required —
there is no default and no "latest".

`apply-terraform` defaults to `true`. Use `false` when redeploying a different
image against infrastructure you have not changed; it skips the Terraform job and
shortens the run by several minutes.

> **Hold — the gate job waits.** `gate` declares `environment: uat`, so the run
> pauses immediately and does nothing until approved. Nothing in Azure has been
> touched at this point.

### 2.3 `gate` verifies the artifact

Once approved, `gate` logs into Azure over OIDC and checks that both
`api:<tag>` and `worker:<tag>` exist in the registry — *before* Terraform touches
anything.

That ordering is deliberate. Dispatch trusts whatever SHA you typed; without the
check, a typo would apply infrastructure and then die at image-pull time with
`MANIFEST_UNKNOWN`, a far more expensive way to learn you got it wrong.

### 2.4 `terraform` applies UAT

Skipped entirely when `apply-terraform=false`. It runs the full IaC gate — fmt,
init, validate, tflint, checkov — then applies. Applying *before* the deploy means
any pending registry or config change reaches Azure ahead of the image pull that
needs it. The `deploy` job is written as `always()` plus an explicit failure
check, so skipping this job does not skip the deploy.

### 2.5 `deploy` updates both apps

`az containerapp update --image` against the Api and the Worker, then polls each
new revision **up to 30 times at 10-second intervals** — a five-minute ceiling per
app. A revision counts as up only when all three agree:

- `provisioningState = Provisioned`
- `healthState` is `Healthy` **or** `None`
- `runningState` is `Running` **or** `RunningAtMaxScale`

`None` is accepted because the Worker has no ingress and therefore no probe
surface to report on.

### 2.6 Post-deploy smoke

Three legs against the Api's public FQDN:

| Leg | Auth | Asserts |
| --- | --- | --- |
| `/health/live` | none | 200 |
| `/health/ready` | none | 200 — checks Postgres *and* Redis, so this is the leg that catches a data-plane auth problem |
| `/api/v1/rates/latest?base=USD&target=EUR` | bearer token for `SMOKE_TOKEN_RESOURCE` | the **payload** names the requested pair |

Leg 3 checks the body rather than the status code because a 200 with an empty
body is a load balancer, not a service: behind a healthy gateway that path can
return an error page or a redirect-to-login rendered as 200 by a helpful proxy.
The assertion is deliberately shallow — smoke proves service, not correctness, and
the Alba suite owns correctness.

### 2.7 Collect the record

`record` runs unconditionally — it is most valuable precisely when something
upstream failed — and uploads a summary with the image tag, per-job results, actor
and ref. Retained 90 days.

```powershell
gh run download <run-id> -n deploy-summary-<run-id>
```

`notify` posts a Slack webhook only on genuine failure and only when the repo
variable `NOTIFICATIONS_ENABLED` is `'true'`. It is currently unset, so no
message will be sent.

---

## 3. Approving a deployment

Both holds clear the same way. The sole reviewer is `mpcs2013`.

**Browser:** Actions → the paused run → **Review deployments** → tick `uat` →
**Approve and deploy**.

**Terminal:**

```powershell
# What is waiting, and on which environment
gh api repos/mpcs2013/currency-tracker/actions/runs/<run-id>/pending_deployments `
  --jq '.[] | {env: .environment.name, id: .environment.id, can_approve: .current_user_can_approve}'

# Approve. 15500182117 is the UAT environment id.
gh api --method POST `
  repos/mpcs2013/currency-tracker/actions/runs/<run-id>/pending_deployments `
  -F "environment_ids[]=15500182117" `
  -f state=approved `
  -f comment="Signing off build <sha> for UAT"
```

Rejecting is the same call with `-f state=rejected`. A rejected deployment fails
the run cleanly and leaves the environment untouched.

---

## 4. Troubleshooting

| Symptom | Most likely cause | Command |
| --- | --- | --- |
| `No api:<tag> in <acr>` at `gate` | That SHA's `main-ci` run never reached the push jobs, or you used a short SHA | `az acr repository show-tags -n $acr --repository api --orderby time_desc --top 10 -o table` |
| Dispatch refused / no run appears | Dispatched from a branch other than `main` | `gh workflow run deploy-uat.yml --ref main -f image-tag=<sha>` |
| Run sits idle with no logs | Not a hang — waiting on the reviewer | `gh api repos/mpcs2013/currency-tracker/actions/runs/<id>/pending_deployments` |
| `azure/login` fails (AADSTS700213 etc.) | Federated credential subject mismatch | Compare against `repo:mpcs2013/currency-tracker:environment:uat` on `gh-deploy-uat` |
| Terraform apply fails on a lock | A previous run died mid-apply | Inspect before forcing — the lock lives in the `stcurrencytrackertfstate` backend |
| Revision never reaches a healthy state | The app is fail-fasting at boot on missing config | `az containerapp logs show -n ca-ct-uat-api -g rg-currencytracker-uat --type console --tail 200` |
| Provisioned but not running | Image pull, or the platform rejected the revision | `az containerapp logs show -n ca-ct-uat-api -g rg-currencytracker-uat --type system --tail 100` |
| `/health/live` 200 but `/health/ready` 503 | Postgres or Redis data-plane auth | `az containerapp revision list -n ca-ct-uat-api -g rg-currencytracker-uat -o table` |
| Smoke rates leg 401 / 403 / wrong payload | **The pending `user` app role** — see §0 | `az account get-access-token --resource $env:SMOKE_TOKEN_RESOURCE --query accessToken -o tsv` |
| Green deploy but the image did not change | Expected. Terraform `ignore_changes` the image; only the pipeline moves it | `az containerapp show -n ca-ct-uat-api -g rg-currencytracker-uat --query "properties.template.containers[0].image" -o tsv` |
| A second dispatch queues behind the first | Working as designed — `concurrency: deploy-uat`, `cancel-in-progress: false` | Wait. Deploys serialise so a half-applied environment is never abandoned |

Reading a failed run quickly:

```powershell
gh run view <run-id> --json jobs -q '.jobs[] | "\(.conclusion)\t\(.name)"'
gh run view <run-id> --log-failed | Select-Object -Last 60
```

---

## 5. Rolling back

There is no rollback command, and none is needed. **Roll forward:** re-dispatch
`deploy-uat` with the SHA of the last build you trusted, almost always with
`apply-terraform=false` since the infrastructure is not what broke.

UAT keeps serving throughout. Revision mode is `Single`, and a revision that never
reaches a healthy state never takes traffic — a failed deploy leaves the previous
revision handling requests. That is why [`pipelines.md`](pipelines.md) §Failure
playbook says a red `deploy-uat` blocks *convergence*, not the environment.

```powershell
az containerapp revision list -n ca-ct-uat-api -g rg-currencytracker-uat `
  --query "[].{rev:name,active:properties.active,traffic:properties.trafficWeight,created:properties.createdTime}" -o table
```

---

## What this pipeline deliberately does not do

- **No merge deploys to UAT.** The Phase 14.D plan specified a `workflow_run`
  trigger chaining `deploy-uat` off `main-ci`. Deliberately not implemented —
  recorded drift 14.36. Azure is touched when a human asks.
- **Merges still build.** Every merge produces a SHA-tagged release candidate in
  ACR. That is a registry artifact, not a deployment. It exists so PROD can later
  promote the identical bytes by digest instead of rebuilding (ADR 0015).
- **"Push to the UAT ACR" is not "deploy to UAT".** The registry happens to live
  in the UAT resource group; PROD imports from it by digest. One registry, two
  environments promoting from it.
- **Terraform never moves an image.** The `ignore_changes` lifecycle rule on the
  container image is the treaty: Terraform owns infrastructure shape, the pipeline
  owns the image.
- **Nothing prunes the registry.** Every merge adds an image pair and there is no
  retention policy — untagged-manifest cleanup needs the Premium ACR SKU, and UAT
  runs Standard.

---

## Related

- [`pipelines.md`](pipelines.md) — the graph, the gates, §Failure playbook, §Manual prerequisites.
- [`../azure/bootstrap.md`](../azure/bootstrap.md) — identity model, grants, environment variables.
- [ADR 0015](../decisions/0015-deploy-topology.md) — deploy topology and digest promotion.
