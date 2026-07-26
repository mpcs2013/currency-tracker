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

## Federated credentials (14.2) — issuer `https://token.actions.githubusercontent.com`, audience `api://AzureADTokenExchange`
| App             | Subject                                                     |
| --------------- | ---------------------------------------------------------- |
| gh-deploy-uat   | `repo:mpcs2013/currency-tracker:environment:uat`           |
| gh-deploy-prod  | `repo:mpcs2013/currency-tracker:environment:prod`          |

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
managed identity (AcrPull, KV Secrets User, etc.) are separate — 14.24. The
**deploy identities' data-plane access to the *state* account** is separate too,
and is recorded in the next section.

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

## GitHub Environments (14.6)
| Environment | Reviewer | Wait  | Deployment branches | Variables                                                                    |
| ----------- | -------- | ----- | ------------------- | ---------------------------------------------------------------------------- |
| uat         | none     | none  | `main`              | AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_SUBSCRIPTION_ID, AZURE_RESOURCE_GROUP |
| prod        | required | 5 min | `main`, `v*` tags   | AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_SUBSCRIPTION_ID, AZURE_RESOURCE_GROUP |

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