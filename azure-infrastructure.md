# Azure Infrastructure

## Resources

| Resource | Name | Tier | Region |
|----------|------|------|--------|
| Resource Group | `kanban-rg` | — | West Europe |
| App Service Plan | `kanban-plan` | F1 (Free) | West Europe |
| App Service | `kanban-api-nurwell` | F1 | West Europe |
| SQL Server | `kanban-sql-nurwell` | — | West Europe |
| SQL Database | `kanban-db` | Serverless Gen5 1vCore | West Europe |

## App Service

- URL: `https://kanban-api-nurwell.azurewebsites.net`
- Runtime: .NET 8
- Plan: F1 Free

## Azure SQL Database

- Server: `kanban-sql-nurwell.database.windows.net`
- Database: `kanban-db`
- Tier: Serverless (auto-pauses after 60 min idle, min 0.5 vCore)
- Admin login: `kanbanadmin`

### Connection string (template)

```
Server=tcp:kanban-sql-nurwell.database.windows.net,1433;Database=kanban-db;User ID=kanbanadmin;Password=<password>;Encrypt=True;TrustServerCertificate=False;
```

## Firewall

- Rule `AllowAzureServices`: 0.0.0.0–0.0.0.0 (allows all Azure services including App Service)

## Provisioning commands

```bash
az group create --name kanban-rg --location westeurope

az appservice plan create \
  --name kanban-plan \
  --resource-group kanban-rg \
  --sku F1

az webapp create \
  --name kanban-api-nurwell \
  --plan kanban-plan \
  --resource-group kanban-rg \
  --runtime "dotnet:8"

az sql server create \
  --name kanban-sql-nurwell \
  --resource-group kanban-rg \
  --location westeurope \
  --admin-user kanbanadmin \
  --admin-password "<password>"

az sql db create \
  --name kanban-db \
  --server kanban-sql-nurwell \
  --resource-group kanban-rg \
  --edition GeneralPurpose \
  --family Gen5 \
  --capacity 1 \
  --compute-model Serverless \
  --auto-pause-delay 60 \
  --min-capacity 0.5

az sql server firewall-rule create \
  --server kanban-sql-nurwell \
  --resource-group kanban-rg \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```
