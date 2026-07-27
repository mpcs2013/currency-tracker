# Configuration

Every environment-varying value in this system arrives through one of five
layers. Higher wins. There is no sixth, and nothing in `src/` reads a file path,
a registry key, or a cloud SDK to find configuration.

| # | Layer | Local (Aspire) | Azure (Container Apps) |
| - | ----- | -------------- | ---------------------- |
| 1 | Environment variables | Injected by the AppHost (`WithReference`, `WithEnvironment`) | Container App `env` entries — either a literal `value` or a `secretRef` the platform resolved from Key Vault |
| 2 | User secrets | `dotnet user-secrets` (Development only; the Worker carries a `UserSecretsId`) | n/a |
| 3 | `appsettings.{Environment}.json` | `appsettings.Development.json` | `appsettings.Production.json` |
| 4 | `appsettings.json` | shipped defaults | shipped defaults |
| 5 | Code defaults | `?? "0 0 6 * * ?"`-style fallbacks and options defaults | same |

## Key Vault is not a layer

The Phase 14 build plan phrases the ladder as "env vars > Key Vault > defaults".
That is one layer too many. A Container App secret with a `keyVaultUrl` is
resolved **by the platform, using the app's own managed identity, at revision
start**, and delivered to the process as an ordinary environment variable. By
the time `IConfiguration` runs there is no Key Vault involved and no way to tell
a vault-backed variable from a literal one.

Consequences worth knowing:

- You cannot "override Key Vault with an environment variable" — they are the
  same slot. Declaring both a literal `env` and a `secretRef` with the same name
  is a platform-level conflict, not an application-level precedence question.
- A failure to read the vault is a **provisioning** failure
  (`Field 'configuration.secrets' is invalid … Unable to get value using Managed
  identity`), not a runtime one. Your code never sees a partial configuration —
  there is no process yet to see it.
- The application carries no Key Vault client, no
  `Azure.Extensions.AspNetCore.Configuration.Secrets`, and no startup vault round
  trip. It is vault-unaware by design (ADR 0017).

## What goes where

| Class of value | Home | Examples |
| -------------- | ---- | -------- |
| Connection strings | **Key Vault**, referenced by the Container App | `ConnectionStrings__currencytracker`, `ConnectionStrings__cache` |
| Identifiers | Plain environment variables from `envs/*/terraform.tfvars` | `Authentication__Authority`, `Authentication__Audience` |
| Switches | Plain environment variables, with an explicit default in `appsettings.json` | `Azure__UseManagedIdentity` |
| Tunables | `appsettings.json`, overridable by environment | `RateLimiting:*`, `SecurityHeaders:*`, `Worker:IngestSchedule` |
| Credentials | **Key Vault, set out of band** — never through Terraform, which would place the value in state | *(none exist today — see below)* |

This is a rule about a *class* of value, which is why it never needs
re-adjudicating per value: "everything in the vault" and "nothing in the vault"
both get refused with the same sentence.

Connection strings live in the vault even though **none of them currently
contains a credential**: Postgres is Entra-only and Managed Redis has access keys
disabled (Phase 14.C). A connection string still discloses server name, database
name and the exact principal to attack — treating it as plain text because it
happens to lack a password confuses *what a value contains* with *what a value
discloses*. Consistency also means the first real secret is one map entry rather
than a new mechanism.

## The secret inventory is empty, and that is a finding

Audited at 14.42 and again at 14.46. The commands, so the next auditor runs the
same ones:

```powershell
# Values that look like credentials anywhere in shipped configuration:
Get-ChildItem -Path src -Recurse -Filter 'appsettings*.json' |
  Select-String -Pattern '(password|pwd|secret|apikey|api_key|accesskey|connectionstring)\s*[:=]' `
                -CaseSensitive:$false

# Anything password-shaped in the infrastructure:
Get-ChildItem -Path infra/terraform -Recurse -Filter '*.tf' |
  Select-String -Pattern 'administrator_password|access_key|primary_key'

# The same question asked of the deployed surface:
$rg  = "rg-currencytracker-uat"
$app = az containerapp list -g $rg --query "[?ends_with(name,'-api')].name | [0]" -o tsv
az containerapp show -n $app -g $rg --query "properties.configuration.secrets[?value]" -o json
# [] — no container-app secret carries a literal value; every one is a keyVaultUrl.
```

The `appsettings` audit returns nothing. The Terraform audit returns three lines,
and reading them is the point: `access_keys_authentication_enabled = false`
(`modules/redis`), `shared_access_key_enabled = false` (`modules/storage-logs`)
and a comment about the same on the state account. Every hit is a credential path
being *switched off*, which is the opposite of a secret to move.

The rest of the inventory is empty for reasons already on the record:
`modules/postgres` sets `password_auth_enabled = false` (there is no
`administrator_login` or `administrator_password` to relocate), `IAlertNotifier`'s
only adapter writes a structured log line — there is no SMTP anywhere in the
solution — and the Frankfurter API is public and unauthenticated.

When a real credential does arrive, it does **not** go through Terraform — that
would place the value in state. Set it out of band with `az keyvault secret set`
and reference it with a data source or `ignore_changes`. `modules/keyvault`'s
`secrets` variable says so in its description, because the next person will reach
for the resource that already exists.

## Placeholders are not used

`appsettings*.json` contains no empty-string placeholders, deliberately. In .NET
an empty string is a configuration *value*, not an absence: with

```json
{ "ConnectionStrings": { "currencytracker": "" } }
```

`IConfiguration.GetConnectionString("currencytracker")` returns `""`, not `null`,
and every `?? throw` guard in the codebase is a null check. The placeholder
disarms all of them — the host stops fail-fasting at boot with an actionable
message and instead fails somewhere downstream, where an `NpgsqlDataSource`
pointed at nothing surfaces on the first query.

It is worse in local development, where the placeholder wins wherever the
environment is silent: every test host, every `dotnet run` outside the AppHost,
every future `dotnet ef` invocation. Those are exactly the cases a fail-fast
exists for.

So the guards now test `string.IsNullOrWhiteSpace`, not null:

| Guard | Where |
| ----- | ----- |
| `currencytracker` connection string | `Infrastructure/Persistence/ApplicationDataSource.cs` (14.44) |
| `cache` connection string | `Infrastructure/DependencyInjection.cs` (14.45) |
| `Authentication:Authority` | `Api/Program.cs` (14.46) |
| `Authentication:Audience` | `Api/Program.cs` (14.46) |

A placeholder added in future therefore still fails fast — but the convention
stands: **a key the environment supplies does not appear in the file at all.**

`AGENTS.md` carries the same rule as a standing gotcha, with one wrinkle worth
correcting: it says a connection string in `appsettings.json` would *shadow* the
Aspire-injected one. Environment variables are added after the JSON providers in
the default host builder, so an env var actually wins. The conclusion holds for
the sharper reason above.

## Which profile does each environment load?

Both Azure environments set `ASPNETCORE_ENVIRONMENT` / `DOTNET_ENVIRONMENT` to
`Production` (14.43), so both load `appsettings.Production.json`. UAT exists to
rehearse PROD, and a UAT that loads a different configuration file is rehearsing
a different application. The environments are distinguished by resource group,
`name_prefix`, state key, ACR, GitHub Environment and OIDC subject — not by a
profile name. `ASPNETCORE_ENVIRONMENT` distinguishes *behavioural profiles*, and
this project has exactly two: Development (Scalar UI, migration runner, seeder,
lax backchannel TLS, Wolverine `Solo` durability) and everything else.

Note that environment-variable names use `__` (double underscore) as the section
separator: `Azure__UseManagedIdentity`, not `Azure:UseManagedIdentity`. The colon
form works on Linux only by accident of shell quoting and not at all in a
Container App `env` entry.

## Related

- `docs/auth.md` — the `Authentication:*` keys and the Entra ID swap.
- `docs/caching.md` — the cache endpoint and its Entra authentication.
- `docs/ci-cd/pipelines.md` — where each variable is set, per environment.
- `docs/decisions/0017-passwordless-azure-data-plane-auth.md`.
- `docs/decisions/0010-oidc-jwt-auth.md` — why the Api names no identity provider.
