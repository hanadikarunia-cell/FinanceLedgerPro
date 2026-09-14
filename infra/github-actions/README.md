# GitHub Actions - Finance Ledger Pro

> These `.yml` files are **documentation copies**. The live workflows GitHub runs
> are the identical copies under [`.github/workflows/`](../../.github/workflows/).
> Keep both in sync (or delete these and keep only `.github/workflows/`).
>
> **`appservice.yml` and `infra.yml` are superseded** — the app now deploys to
> Render + Vercel via their native git integration (see
> [docs/deployment-guide.md](../../docs/deployment-guide.md)), not via these Azure
> OIDC deploy workflows. Kept for historical reference only. `mobile.yml` and
> `codeql.yml` are unaffected (CI only, not Azure-specific) and still run.

## Workflows

| File | Purpose | Trigger |
| --- | --- | --- |
| `appservice.yml` | Build/test .NET 8 API + React web, deploy both to App Service via OIDC | push to `main`, manual |
| `infra.yml` | What-if + deploy Bicep via `azure/arm-deploy` | push to `infra/bicep/**`, manual (env choice) |
| `mobile.yml` | Build Android APK (EAS or gradlew), upload artifact | push to `src/mobile/**`, manual |
| `codeql.yml` | CodeQL scan for C# and JS/TS | push, PR, weekly cron |

## Required configuration

Set these in **Settings -> Secrets and variables -> Actions**.

### Secrets

| Secret | Used by |
| --- | --- |
| `AZURE_CLIENT_ID` | App registration client ID for OIDC federated credential |
| `AZURE_TENANT_ID` | Azure AD tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Target subscription |
| `EXPO_TOKEN` | `mobile.yml` EAS path only |

### Variables

| Variable | Used by | Example |
| --- | --- | --- |
| `AZURE_API_APP_NAME` | appservice.yml | `financeledger-prod-api` |
| `AZURE_WEB_APP_NAME` | appservice.yml | `financeledger-prod-web` |
| `API_BASE_URL` | appservice.yml (web build) | `https://financeledger-prod-api.azurewebsites.net` |
| `AZURE_LOCATION` | infra.yml | `eastus` |
| `MOBILE_BUILD_TYPE` | mobile.yml | `gradle` or `eas` |

## OIDC setup (no stored passwords)

```bash
# Create app registration + service principal
az ad app create --display-name "financeledger-github"
# Assign Contributor + User Access Administrator on the target scope, then add a
# federated credential bound to this repo/branch:
az ad app federated-credential create \
  --id <appId> \
  --parameters '{
    "name": "github-main",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:<org>/<repo>:ref:refs/heads/main",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

For `environment: production` in `appservice.yml`, also add a federated credential
with subject `repo:<org>/<repo>:environment:production`.

## Path assumptions

- API project: `src/FinanceLedger.Api/FinanceLedger.Api.csproj` (**not yet in repo** — create it or edit the path)
- API tests: `tests/FinanceLedger.Api.Tests/…` (**not yet in repo**)
- Web app: `src/FinanceLedger.Web` (Vite, build output `dist/`)
- Mobile app: `src/FinanceLedger.Mobile` (Expo)

Adjust the `env:` blocks at the top of each workflow to match the actual repo layout.
