# Microsoft Entra ID Setup Guide

This guide wires Azure Entra ID (formerly Azure AD) to the Fraud Risk Assessment API with JWT bearer authentication and scoped authorization.

## Architecture

```
Tenant App (Vodacom)  ──client credentials──▶  Entra ID  ──JWT──▶  API (App Service)
Tenant App (MTN)      ──client credentials──▶  Entra ID  ──JWT──▶  API
```

Each contributing organisation gets its own **client app registration**. The API validates JWTs and reads `tenant_id` (or `azp`) to set SQL `SESSION_CONTEXT` for Row-Level Security.

---

## Step 1 — Register the API application

1. Open [Microsoft Entra admin center](https://entra.microsoft.com) → **App registrations** → **New registration**.
2. Name: `Comric Fraud API`.
3. Supported account types: **Single tenant** (or multi-tenant if required).
4. Register and note:
   - **Application (client) ID** → `AzureAd:ClientId`
   - **Directory (tenant) ID** → `AzureAd:TenantId`

### Expose API scopes

1. **Expose an API** → Set Application ID URI: `api://{ClientId}`.
2. Add scopes (delegated):

| Scope | Admin consent display name |
|-------|---------------------------|
| `Events.Read` | Read fraud events |
| `Events.Write` | Submit fraud events |
| `Signals.Read` | Read anonymous fraud signals |
| `Audit.Read` | Read activity log |
| `Dashboard.Read` | Read dashboard metrics |

3. Add **Application** permissions (same names) for client-credentials flow:
   - **App roles** → Create app roles matching the scope names above.
   - Assign each tenant client app to the appropriate roles.

### Optional claims

Under **Token configuration** → **Add optional claim** → Access token:
- Add custom claim `tenant_id` if you map tenant GUIDs manually.
- Otherwise the API falls back to the `azp` (authorized party) claim.

---

## Step 2 — Register tenant client applications

Repeat for each MNO/HR contributor (e.g. Vodacom, MTN):

1. **New registration** → Name: `Comric Fraud - Vodacom`.
2. Note the **Client ID** — this should match `Tenants.TenantId` in the database (or map via optional claim).
3. **Certificates & secrets** → New client secret (store in Key Vault for production).
4. **API permissions** → Add permission → **Comric Fraud API** → Application permissions:
   - `Events.Read`, `Events.Write`, `Signals.Read`, `Audit.Read`, `Dashboard.Read`
5. **Grant admin consent** for the tenant.

---

## Step 3 — Configure the API

Copy `appsettings.Entra.example.json` values into `appsettings.Development.json` or Azure App Service settings:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<your-tenant-id>",
    "ClientId": "<api-app-client-id>",
    "Audience": "api://<api-app-client-id>"
  }
}
```

For Azure App Service, set these as **Configuration → Application settings** (not committed to source control).

---

## Step 4 — Obtain a JWT (client credentials)

### Via API proxy endpoint

```http
POST /api/v1/auth/token
Content-Type: application/json

{
  "clientId": "<vodacom-client-id>",
  "clientSecret": "<vodacom-client-secret>",
  "scope": "Events.Read Events.Write Signals.Read"
}
```

### Directly from Entra

```bash
curl -X POST "https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials" \
  -d "client_id={clientId}" \
  -d "client_secret={clientSecret}" \
  -d "scope=api://{apiClientId}/.default"
```

Use the returned `access_token` as `Authorization: Bearer {token}`.

---

## Step 5 — Map tenants in the database

Ensure each client app's identity maps to a row in `Tenants`:

| TenantId (GUID) | TenantName | TenantCode |
|-----------------|------------|------------|
| `{vodacom-client-id}` | Vodacom | VOD |
| `{mtn-client-id}` | MTN | MTN |

Seed data uses fixed GUIDs for local dev; update these to match your Entra client app IDs in production.

---

## Step 6 — Verify

1. Call `GET /api/v1/hr-events` with a valid token → `200 OK` (tenant-scoped).
2. Call without token → `401 Unauthorized`.
3. Call with token missing `Events.Read` → `403 Forbidden`.
4. Submit an HR event → signal appears in `GET /api/v1/fraud-signals` without tenant attribution.

---

## Azure App Service notes

- Enable **Authentication** only if using Easy Auth; this API validates JWT in-process via `Microsoft.Identity.Web`.
- Store `AzureAd__ClientId`, `AzureAd__TenantId`, `AzureAd__Audience` as app settings.
- Use **Managed Identity** for SQL access in production (see `infra/` Bicep templates).
- Enable **HTTPS only** and configure CORS for your frontend origin.

## Security checklist

- [ ] Client secrets in Key Vault, not appsettings
- [ ] TDE enabled on Azure SQL (default on new databases)
- [ ] RLS policies applied (`SQL/RlsPolicies.sql`)
- [ ] Least-privilege app roles per tenant
- [ ] Rotate client secrets on schedule
