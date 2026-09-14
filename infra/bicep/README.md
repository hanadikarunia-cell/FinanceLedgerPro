# Finance Ledger Pro - Infrastructure (Bicep)

> **Superseded.** The app now runs on Render + Supabase + Vercel free tiers (see
> [docs/deployment-guide.md](../../docs/deployment-guide.md) and
> [docs/free-tier-deployment.md](../../docs/free-tier-deployment.md)). This Bicep IaC
> is kept for historical reference only — it is not part of the current deploy path
> and can be deleted at any time without affecting the app.

Infrastructure-as-Code for **Finance Ledger Pro**: an ASP.NET Core 8 API + React web app on Azure App Service, backed by Cosmos DB (SQL API), Blob Storage, Application Insights, Key Vault, and Azure Communication Services (email).

## Resources provisioned

| Module | Resource |
| --- | --- |
| `modules/appInsights.bicep` | Log Analytics workspace + Application Insights (workspace-based) |
| `modules/keyVault.bicep` | Key Vault (RBAC auth, purge protection) |
| `modules/keyVaultSecrets.bicep` | Secrets: `cosmos-connection-string`, `blob-connection-string`, `jwt-secret`, `acs-connection-string`, `google-sheet-id` |
| `modules/keyVaultRoleAssignment.bicep` | Grants app identities *Key Vault Secrets User* |
| `modules/storage.bicep` | Storage account + private `attachments` container |
| `modules/storageRoleAssignment.bicep` | Grants API identity *Storage Blob Data Contributor* |
| `modules/cosmos.bicep` | Cosmos DB account + `FinanceLedger` DB + 5 containers |
| `modules/cosmosRoleAssignment.bicep` | Grants API identity Cosmos *Data Contributor* |
| `modules/communicationServices.bicep` | ACS + Email service + Azure-managed domain |
| `modules/appServicePlan.bicep` | Linux App Service Plan |
| `modules/appService-api.bicep` | .NET 8 API App Service (managed identity, KV references) |
| `modules/appService-web.bicep` | React web App Service (Node) |

### Cosmos containers

| Container | Partition key |
| --- | --- |
| `Users` | `/id` |
| `Transactions` | `/branch` |
| `AuditLogs` | `/userId` |
| `Branches` | `/id` |
| `Attachments` | `/transactionId` |

Dev uses **serverless** Cosmos (pay-per-request, cheapest for low/spiky load). Prod uses **autoscale** (default 10,000 RU/s max per container).

## Prerequisites

- Azure CLI 2.55+ with the Bicep CLI (`az bicep upgrade`)
- An existing resource group
- Contributor + User Access Administrator (role assignments are created)

## Deploy

Set `adminObjectId` in the parameter file (or pass `-p adminObjectId=<your-oid>`) to grant yourself Key Vault access.

```bash
# 1. Create resource group
az group create --name rg-financeledger-dev --location eastus

# 2. Validate / what-if
az deployment group what-if \
  --resource-group rg-financeledger-dev \
  --template-file main.bicep \
  --parameters main.parameters.dev.json

# 3. Deploy (dev)
az deployment group create \
  --resource-group rg-financeledger-dev \
  --template-file main.bicep \
  --parameters main.parameters.dev.json \
  --name financeledger-dev

# Deploy (prod)
az group create --name rg-financeledger-prod --location eastus
az deployment group create \
  --resource-group rg-financeledger-prod \
  --template-file main.bicep \
  --parameters main.parameters.prod.json \
  --name financeledger-prod
```

### Override parameters inline

```bash
az deployment group create \
  --resource-group rg-financeledger-dev \
  --template-file main.bicep \
  --parameters main.parameters.dev.json \
  --parameters adminObjectId=$(az ad signed-in-user show --query id -o tsv)
```

## Outputs

After deployment, read outputs with:

```bash
az deployment group show \
  --resource-group rg-financeledger-dev \
  --name financeledger-dev \
  --query properties.outputs
```

Outputs: `apiUrl`, `webUrl`, `keyVaultName`, `cosmosEndpoint`, `storageAccountName`, `appInsightsConnectionString`.

## Post-deploy notes

- **Secrets are placeholders.** `jwt-secret` and `google-sheet-id` deploy with dummy values. Replace them:

  ```bash
  az keyvault secret set --vault-name <kv-name> --name jwt-secret --value "<strong-secret>"
  az keyvault secret set --vault-name <kv-name> --name google-sheet-id --value "<sheet-id>"
  ```

- **Email sender:** the Azure-managed domain yields a `DoNotReply@<guid>.azurecomm.net` address. For a branded sender, add a `CustomDomains` sub-resource and verify DNS (SPF/DKIM/TXT). Then create an email sender username and use it in the API.
- **Worker (Google Sheets sync):** deploy as a WebJob under the API App Service (drop a `App_Data/jobs/triggered/<name>` package) or add a second App Service reusing `appService-api.bicep` as a template. It reuses `google-sheet-id` from Key Vault.
- **Key Vault references** require the app's managed identity to have *Key Vault Secrets User* (granted by the role assignment module). Allow a few minutes after first deploy for references to resolve.
