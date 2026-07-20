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
managed identity (AcrPull, KV Secrets User, etc.) are separate — 14.24.

## GitHub Environments (14.6)
| Environment | Reviewer | Wait  | Deployment branches | Variables                                            	 |
| ----------- | -------- | ----- | ------------------- | ------------------------------------------------------- |
| uat         | none     | none  | `main`              | AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_SUBSCRIPTION_ID |
| prod        | required | 5 min | `main`, `v*` tags   | AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_SUBSCRIPTION_ID |

Secrets (e.g. `SLACK_WEBHOOK_URL`) are set as environment **secrets** when the
consuming workflow lands (14.39) — not recorded here.

## What is NOT here (IaC takes over at 14.B)
VNet, ACR, Postgres, Redis, Key Vault, Log Analytics / App Insights, the
Container Apps environment and apps, and all data-plane role assignments are
Terraform modules (14.15–14.25), applied into the two environment RGs above.