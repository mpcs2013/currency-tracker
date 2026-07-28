# Azure + GitHub bootstrap (one-time, manual)

This is the **manual** foundation for Phase 14. Everything else in Phase 14
is Terraform (`infra/terraform/`, from 14.B). These steps exist because
identity and Terraform state must exist *before* Terraform can run. Re-run
them by hand to reproduce the stack in a clean tenant (14.59).

> Contains identifiers and scopes only. **No secret** (client secret,
> webhook, password) belongs in this file or the repo. Authentication is
> OIDC federation — there is no stored credential to record.

## Tenant / subscription
| Field           | Value                					|
| --------------- | ----------------------------------------|
| Tenant ID       | `04b94fa0-2449-42e3-b19d-3275d586556a`  |
| Subscription ID | `ef598e8f-9e6d-46f3-9756-1f94cf829263`	|
| Region          | `switzerlandnorth`            			|

## Registered resource providers (14.3 pre-flight)
Registered once on the subscription so resource creation doesn't fail with a
misleading `SubscriptionNotFound`: `Microsoft.Storage`, `Microsoft.KeyVault`,
`Microsoft.DBforPostgreSQL`, `Microsoft.Cache`, `Microsoft.App`,
`Microsoft.ContainerRegistry`, `Microsoft.OperationalInsights`,
`Microsoft.Insights`, `Microsoft.Network`. (Re-register in a clean tenant — 14.59.)

## App Registrations (14.1) — deploy identities, no client secret
| App             | App (client) ID 						| Object ID        							| Service principal |
| --------------- | --------------------------------------- | ----------------------------------------- | ----------------- |
| gh-deploy-uat   | `c21f81b0-c5fe-4854-bc53-e88e913f59d3`  | `b011b5ac-1da4-4986-920e-6c5f29271e33`	| created           |
| gh-deploy-prod  | `4fbd780a-9e9f-41c9-bed2-98d3509d0aa1`  | `b99eedea-11b9-4817-8141-1e1f7625e055`	| created           |

### MSApi — the API app registration the Api validates tokens against

Not a deploy identity. This is the resource `Authentication__Audience` names and
the audience `SMOKE_TOKEN_RESOURCE` requests a token for.

| Field | Value |
| ----- | ----- |
| App (client) ID | `e50b769e-1b9e-487d-baf5-7108f98935f2` |
| **Application** object ID | `f5914113-53a9-4d31-96bc-2d96c6751525` |
| **Service principal** object ID | `6424a67b-d03b-4174-a7a7-f86e5b754bd3` |
| `appRoleAssignmentRequired` | `false` |

> **Both rows said `6424a67b-…` until 2026-07-28.** They are never the same
> value: `az ad app show --id <client-id> --query id` returns the application
> object, `az ad sp show` returns the service principal. Graph
> `PATCH /applications/{id}` takes the **application** object id, so every call
> aimed at the SP's id 404s. Resolve both live rather than trusting this table.

**Configured 2026-07-28 — this section previously said it was blocking.** It
shipped bare, discovered 2026-07-27 while trying to call `/api/v1/rates/latest`
by hand: no `identifierUris`, no `oauth2PermissionScopes`, no
`preAuthorizedApplications`, no `appRoles`, and `requestedAccessTokenVersion`
unset. The four settings below are now applied and verified end to end — an
unauthenticated call returns 401, an authenticated one reaches the handler.

| Setting | Required value | Why |
| ------- | -------------- | --- |
| `identifierUris` | `["api://e50b769e-1b9e-487d-baf5-7108f98935f2"]` | Something for a client to request a token *for*. |
| `api.requestedAccessTokenVersion` | `2` | **The one that is not optional.** Unset means v1, whose issuer is `https://sts.windows.net/<tid>/`. `Program.cs` sets `ValidIssuer = authAuthority`, an exact match against `https://login.microsoftonline.com/<tid>/v2.0`, so a v1 token 401s on issuer before audience is even examined. |
| `api.oauth2PermissionScopes` | one scope, `access_as_user` | Without a scope there is nothing to consent to, and every token request fails `AADSTS65001`. |
| `api.preAuthorizedApplications` | Azure CLI `04b07795-8ddb-461a-bbee-02f9e1bf7b46` | Lets `az account get-access-token` mint a token non-interactively, for smoke and for humans. |

A `PATCH` to `api` replaces the whole object, so set the scope and the
pre-authorized app in that order and reuse the same scope `id` in both calls —
the second call otherwise silently erases the first.

**Two calls are mandatory; merging them does not work.** The obvious
simplification — one `PATCH` carrying the scope *and* its pre-authorization —
is rejected outright:

```
InvalidValue: Property api.preAuthorizedApplications.delegatedPermissionIds
has a Permission Id that cannot be found in the AppPermissions sets.
```

Graph validates `delegatedPermissionIds` against **persisted** scopes, not
against the ones in the same request. So: call 1 sets
`requestedAccessTokenVersion` + `oauth2PermissionScopes`; call 2 re-sends both
*and* adds `preAuthorizedApplications`. The re-send is what survives the
whole-object replace.

The scope id in use is `9faf0955-99b2-4f50-b516-b022ce99a54d`. PowerShell mangles
inline JSON — `\"` is not an escape there, and `az` sees the body split into
separate arguments — so write the body to a file and pass `--body "@<path>"`.

Fetching a token afterwards:

```powershell
az account get-access-token `
  --scope "api://e50b769e-1b9e-487d-baf5-7108f98935f2/access_as_user" `
  --query accessToken -o tsv
```

`--scope` is the v2 form. `--resource` requests a v1 token and will fail issuer
validation no matter what else is correct.

**No app roles are defined, and the read paths do not need any.**
`Program.cs`'s `MapWolverineEndpoints(opts => opts.RequireAuthorizeOnAll())`
applies the default policy — authenticated user, no role. Only
`AdminIngestEndpoint` carries `[Authorize(Policy = "admin")]`, so the `user` /
`admin` roles that [`docs/auth.md`](../auth.md) recommends are needed for the
admin endpoint and nothing else. They are **not** a prerequisite for smoke.

## Federated credentials (14.2) — issuer `https://token.actions.githubusercontent.com`, audience `api://AzureADTokenExchange`
| App             | Credential name  | Subject                                                     |
| --------------- | ---------------- | ----------------------------------------------------------- |
| gh-deploy-uat   | `github-env-uat` | `repo:mpcs2013/currency-tracker:environment:uat`            |
| gh-deploy-uat   | `gh-uat-plan`    | `repo:mpcs2013/currency-tracker:environment:uat-plan`       |
| gh-deploy-prod  | `github-env-prod`| `repo:mpcs2013/currency-tracker:environment:prod`           |

`gh-uat-plan` (added 2026-07-27) is what makes `terraform-pr.yml` able to run at
all. `uat`'s deployment-branch policy allows `main` only, so a plan job on a PR
branch was refused before its first step — a 2-second failure with no logs.
Widening that policy would have let PR branches *deploy*; a second subject that
only the plan-only path can reach lets them plan and nothing else. Same app
registration, so the token's Azure rights are unchanged — the trust boundary
moved, not the authorisation.

> **What this accepts.** Any branch in this repo that can open a PR can now
> obtain a token for `gh-deploy-uat`, which holds Contributor on
> `rg-currencytracker-uat`. That is inherent to running `terraform plan` on a PR
> (plan reads every resource and writes a state lock), and it is why the plan
> path is UAT-only and PROD is planned at release time instead. A fork PR cannot
> reach it: fork workflows get no environment vars and no OIDC token.

## Resource groups (14.3, 14.4)
| RG                            | Purpose                          | Managed by Terraform? |
| ----------------------------- | -------------------------------- | --------------------- |
| rg-currencytracker-uat        | UAT application resources        | yes (from 14.B)       |
| rg-currencytracker-prod       | PROD application resources       | yes (from 14.B)       |
| rg-currencytracker-tfstate    | Terraform remote state backend   | **no — never**        |

## Terraform state backend (14.4)
- Storage account: `stcurrencytrackertfstate` (StorageV2, TLS 1.2 min, no public blob access)
- Container: `tfstate`  •  versioning + blob & container soft-delete (30 days)
- State keys (written by Terraform in 14.B): `uat.terraform.tfstate`, `prod.terraform.tfstate`

## Role assignments (14.5) — least privilege
| Identity        | Role        | Scope                          |
| --------------- | ----------- | ------------------------------ |
| gh-deploy-uat   | Contributor | `rg-currencytracker-uat` only  |
| gh-deploy-prod  | Contributor | `rg-currencytracker-prod` only |

No subscription-scope or Owner assignment. Data-plane roles for the app's
managed identity (AcrPull, KV Secrets User, etc.) are separate — 14.24. 
The deploy identities' data-plane access is separate too — to the state account, 
and (from 14.42) to each environment's Key Vault — and is recorded in the two sections below.

Granted 2026-07-26 (14.32). Recorded assignment IDs (the `name` GUID is the
assignment itself; `principalId` is the **service principal's** object ID —
distinct from the App Registration object IDs in the 14.1 table):

| Identity       | SP object ID (`principalId`)           | Role-assignment ID                     |
| -------------- | -------------------------------------- | -------------------------------------- |
| gh-deploy-uat  | `176ad327-50b8-4e32-a83b-9fc5f0d8315f` | `0be26f57-2fd4-446f-90d2-9fa69134db73` |
| gh-deploy-prod | `e3f1ba33-b6bb-4ad3-8339-dd2a4122e70a` | `3902e547-fbbe-4115-a44f-9d39daff0394` |

Both scoped to
`.../resourceGroups/rg-currencytracker-tfstate/providers/Microsoft.Storage/storageAccounts/stcurrencytrackertfstate`
(role definition `ba92f5b4-2d11-453d-a403-e96b0029c9fe`, Storage Blob Data
Contributor). In a clean tenant (14.59) these GUIDs will differ — re-run the
grant above and re-record.

## State-backend data-plane access (RBAC) — required for `use_azuread_auth`
The `tfstate` container was created with `--auth-mode login`, and `backend.tf`
(14.10) sets `use_azuread_auth = true`. Terraform therefore reaches the state
blob with an **Entra token, not a storage account key** — which needs a
*data-plane* role. The control-plane `Contributor` in the table above does
**not** grant blob access, so any identity that runs `terraform init` against
this backend fails with `403 AuthorizationPermissionMismatch` until it holds
`Storage Blob Data Contributor` on the state account.

| Identity                          | Role                          | Scope                            |
| --------------------------------- | ----------------------------- | -------------------------------- |
| gh-deploy-uat (SP)                | Storage Blob Data Contributor | `stcurrencytrackertfstate` only  |
| gh-deploy-prod (SP)               | Storage Blob Data Contributor | `stcurrencytrackertfstate` only  |
| operators running Terraform local | Storage Blob Data Contributor | `stcurrencytrackertfstate` only  |

This is the **one deliberate cross-RG grant**: each deploy identity is
`Contributor` on *its* environment RG (above) and additionally holds blob-data
access on the *state* account in `rg-currencytracker-tfstate` — because that is
where its Terraform state lives. The role is scoped to the single storage
account, not the RG, so nothing else in the state RG is reachable. The UAT SP
still cannot touch PROD state, and vice versa is enforced by the state *keys*
(`uat.terraform.tfstate` / `prod.terraform.tfstate`), not by RBAC.

Grant (PowerShell), per identity — object IDs are in the App Registrations
table above; use `--assignee-principal-type ServicePrincipal` for the deploy
apps, `User` for a human operator:

```powershell
$saId = az storage account show -n stcurrencytrackertfstate `
  -g rg-currencytracker-tfstate --query id -o tsv

az role assignment create `
  --assignee-object-id <objectId> `
  --assignee-principal-type ServicePrincipal `
  --role "Storage Blob Data Contributor" `
  --scope $saId
```

Data-plane RBAC can take a few minutes to propagate; a `terraform init` retried
immediately after the grant may still 403. Verify with
`az storage blob list --account-name stcurrencytrackertfstate --container-name
tfstate --auth-mode login` — it stops 403ing (returns an empty list) once the
role lands. Re-register these grants in a clean tenant (14.59).

> **Why this wasn't in the 14.5 table originally.** 14.5 scoped the deploy
> identities to their environment RGs and deliberately kept them out of the
> state RG. That's correct for *resource* provisioning, but Terraform must also
> read/write *state*, which lives in the state account under AAD auth — so a
> narrowly-scoped blob-data role on that one account is required in addition.
> The gap surfaced on the first `terraform init` of 14.B (`403
> AuthorizationPermissionMismatch`); this section is the fix, recorded for
> reproducibility.

## Key Vault data-plane access (RBAC) — required for `azurerm_key_vault_secret` (14.42)

`modules/keyvault` (14.19) creates the vault in **RBAC authorization mode**
(`rbac_authorization_enabled = true`), which splits management plane from data
plane. The control-plane `Contributor` in the 14.5 table lets the deploy
identity *create and configure* a vault; it does **not** let it write a secret.
From 14.42 the root module writes three connection strings as
`azurerm_key_vault_secret` resources, which are data-plane calls — so any
identity that runs `terraform apply` fails with
`403 … does not have secrets set permission on key vault` until it holds
`Key Vault Secrets Officer` on that environment's vault.

| Identity            | Role                      | Scope                          | Status                        |
| ------------------- | ------------------------- | ------------------------------ | ----------------------------- |
| gh-deploy-uat (SP)  | Key Vault Secrets Officer | `kv-ct-uat-0pjb` only          | granted 2026-07-27            |
| gh-deploy-prod (SP) | Key Vault Secrets Officer | PROD vault only                | **pending** — vault not created yet |

Granted 2026-07-27 (14.42). Recorded assignment ID (`principalId` is the
**service principal's** object ID, as in the 14.5 table):

| Identity      | SP object ID (`principalId`)           | Role-assignment ID                     |
| ------------- | -------------------------------------- | -------------------------------------- |
| gh-deploy-uat | `176ad327-50b8-4e32-a83b-9fc5f0d8315f` | `1b9a5451-58d1-45dc-af7e-602da11f05a4` |

Scoped to
`.../resourceGroups/rg-currencytracker-uat/providers/Microsoft.KeyVault/vaults/kv-ct-uat-0pjb`
(role definition `b86a8fe4-44ce-4948-aee5-eccb2c155cd7`, Key Vault Secrets
Officer).

**The vault name carries a `random_string` suffix**, so it differs per
environment and is regenerated if the vault is ever destroyed and recreated.
Resolve it rather than typing it, and re-record the assignment ID afterwards:

```powershell
$rg   = "rg-currencytracker-uat"      # or rg-currencytracker-prod
$kv   = az keyvault list -g $rg --query "[0].id" -o tsv
$spId = az ad sp list --display-name gh-deploy-uat --query "[0].id" -o tsv

az role assignment create `
  --assignee-object-id $spId `
  --assignee-principal-type ServicePrincipal `
  --role "Key Vault Secrets Officer" `
  --scope $kv

# Verify from the grantee's point of view:
az role assignment list --assignee $spId --scope $kv --query "[].roleDefinitionName" -o tsv
# Key Vault Secrets Officer
```

`az ad sp list` returns the **service principal's** object ID. The App
Registration's object ID (14.1 table) is a different GUID and produces a role
assignment that silently grants nothing useful.

**PROD is a two-pass first apply.** The grant needs the vault, the vault is
created by Terraform, and the same apply then tries to write secrets into it.
PROD's first `terraform apply` (through `deploy-prod.yml`'s gated `terraform`
job — never a local shell) will therefore create the vault and fail on the
secret write. Make the grant above against the newly-created PROD vault, then
re-run the job. The `terraform` job is separately re-runnable behind its own
approval, so this is a supported motion rather than a recovery.

> **Why this is bootstrap and not IaC.** An `azurerm_role_assignment` granting
> `data.azurerm_client_config.current.object_id` the officer role would work,
> and is wrong twice. A principal that can grant itself data-plane access is
> not restricted by that grant — encoding it in the plan that consumes it turns
> a permission boundary into a formality. And a self-grant used in the same
> apply races Entra replication, producing the "first apply 403s, second
> succeeds" flake that a `time_sleep` would only paper over. Same reasoning,
> and same home, as the state-account grant above.

> **Why the role includes read.** `Key Vault Secrets Officer` covers get, list,
> set and delete; there is no write-only secrets role, and Terraform needs read
> anyway — `azurerm_key_vault_secret` refreshes state by reading the value back
> to detect drift. The accepted consequence is that the deploy identity can
> read these secrets. It is acceptable **only because none of them contains a
> credential** (Postgres is Entra-only, Managed Redis has keys disabled). A
> real password must never be a Terraform-managed secret: set it out of band
> with `az keyvault secret set` and reference it with a data source. See
> `docs/configuration.md`.

Re-register this grant in a clean tenant (14.59).

## GitHub Environments (14.6)
| Environment | Reviewer | Wait  | Deployment branches | Variables                                                                    |
| ----------- | -------- | ----- | ------------------- | ---------------------------------------------------------------------------- |
| uat         | required | none  | `main`              | AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_SUBSCRIPTION_ID, AZURE_RESOURCE_GROUP |
| uat-plan    | none     | none  | any                 | AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_SUBSCRIPTION_ID                       |
| prod        | required | 5 min | `main`, `v*` tags   | AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_SUBSCRIPTION_ID, AZURE_RESOURCE_GROUP |

`uat`'s reviewer row said `none` until 2026-07-27; the API says a required
reviewer has been configured. Corrected here rather than removed from the
environment — read the API, not this table, when it matters:
`gh api repos/mpcs2013/currency-tracker/environments/uat`.

`uat-plan` (2026-07-27) exists only so `terraform-pr.yml` can plan from a PR
branch — see the federated-credentials note above. **No protection rules on
purpose**: a reviewer gate in front of a read-only plan would put a manual
approval on every infra PR, and a branch policy is precisely what it works
around. It carries no `AZURE_RESOURCE_GROUP` because `_reusable-terraform.yml`
reads only the three `ARM_*` values; add it if a plan-path workflow ever needs
it. Nothing that applies, deploys or promotes may reference this environment.

`AZURE_RESOURCE_GROUP` (added 14.31) holds the environment's RG name from the
14.3 table (`rg-currencytracker-uat` / `rg-currencytracker-prod`); workflows
resolve the ACR (and other suffixed resources) at runtime via
`az acr list -g` rather than hardcoding any resource name.

Secrets (e.g. `SLACK_WEBHOOK_URL`) are set as environment **secrets** when the
consuming workflow lands (14.39) — not recorded here.

## What is NOT here (IaC takes over at 14.B)
VNet, ACR, Postgres, Redis, Key Vault, Log Analytics / App Insights, the
Container Apps environment and apps, and all data-plane role assignments are
Terraform modules (14.15–14.25), applied into the two environment RGs above.