# Code Explanation — Comric Fraud Calculator Backend

This document explains how the backend codebase is structured and how requests flow through authentication, tenancy, and domain services.

## 1. Big picture

The solution is an **ASP.NET Core API** that also hosts the **React SPA** from `wwwroot/`. In Azure it runs on App Service (**ComricWA**), reads secrets from Key Vault (**cmckv**), and uses Azure SQL database **`COMRIC_DB`**.

```
Browser (React SPA)
    │  MSAL → Entra ID (JWT)
    ▼
App Service (API + static files)
    ├── Key Vault  → SqlConnectionString, PlatformSalt
    └── Azure SQL  → COMRIC_DB (tenant tables + anonymous Signals)
```

| Concern | Where |
|---------|--------|
| HTTP API | `Controllers/` |
| Business logic | `Services/` |
| EF Core + SQL | `Data/`, `Entities/`, `Migrations/` |
| Auth / scopes | `Authentication/`, `Authorization/`, `Middleware/` |
| Deploy / ops | `scripts/`, `.github/workflows/`, `infra/` |

---

## 2. Solution layout

```
ComricFraudCalculatorBackend/
├── Program.cs                 # DI, auth, middleware, migrate/seed, SPA
├── Controllers/               # REST endpoints under /api/v1/...
├── Services/                  # Domain services
├── Data/                      # DbContext, seeder, SESSION_CONTEXT interceptor
├── Entities/                  # Tenant, HrEvent, MnoEvent, Signal, ActivityLog
├── Models/Requests|Responses  # DTOs
├── Middleware/                # Org email, RLS, activity logging
├── Authorization/             # Scopes, policies, email-domain check
├── Authentication/            # DevAuthenticationHandler
├── Configuration/             # PlatformOptions, Key Vault helpers
├── Enums/                     # Event / signal / status enums
├── Migrations/                # EF Core migrations
├── SQL/                       # RLS policies (manual apply)
├── wwwroot/                   # Built SPA (from UI repo)
├── scripts/                   # Deploy, KV, migrations, UI build
├── infra/                     # Bicep (SQL, KV, App Service)
└── docs/                      # Setup + this explanation
```

### API routes

| Controller | Route | Auth policy |
|------------|-------|-------------|
| `HrEventsController` | `/api/v1/hr-events` | `Events.Read` / `Events.Write` |
| `MnoEventsController` | `/api/v1/mno-events` | `Events.Read` / `Events.Write` |
| `FraudSignalsController` | `/api/v1/fraud-signals` | `Signals.Read` |
| `LookupController` | `/api/v1/lookup` | `Signals.Read` |
| `DashboardController` | `/api/v1/dashboard` | `Dashboard.Read` |
| `ActivityLogController` | `/api/v1/activity-log` | `Audit.Read` |
| `AuthController` | `/api/v1/auth` | token helper (client credentials) |

---

## 3. Startup and request pipeline (`Program.cs`)

### On startup

1. Load config + Key Vault (`AddAppKeyVault`).
2. Require `Platform:Salt` in Production.
3. Register SQL `ApplicationDbContext` + `TenantSessionContextInterceptor` (or InMemory for Testing).
4. Choose auth: **DevAuth** or **Entra JWT**.
5. Register scope-based authorization policies.
6. Non-Testing: `Database.MigrateAsync()` + `DatabaseSeeder.SeedAsync()`.

### Per HTTP request (order)

1. Forwarded headers (App Service TLS termination)
2. HTTPS redirect (non-Production)
3. Static files / default files (SPA)
4. **Authentication**
5. **`OrganizationEmailMiddleware`** (Entra only — `@solugrowth.com`)
6. **Authorization** (scope policies)
7. **`TenantRlsMiddleware`** (if `LocalDevelopment:EnableRls`)
8. **`ActivityLoggingMiddleware`**
9. Controllers
10. SPA fallback → `index.html`

---

## 4. Authentication and authorization

### Production — Microsoft Entra JWT

- Config section: `AzureAd` (`TenantId`, `ClientId`, `Audience`, …).
- Validated by `Microsoft.Identity.Web` (signature, issuer, expiry).
- Audiences accepted: configured audience, raw client ID, and `api://{clientId}`.
- API access is **scope-based** (`scp` claim), not app roles.

| Scope | Policy constant | Typical use |
|-------|-----------------|-------------|
| `Events.Read` | `AuthPolicies.EventsRead` | List HR/MNO events |
| `Events.Write` | `AuthPolicies.EventsWrite` | Submit events |
| `Signals.Read` | `AuthPolicies.SignalsRead` | Fraud signals / ID check |
| `Audit.Read` | `AuthPolicies.AuditRead` | Activity log |
| `Dashboard.Read` | `AuthPolicies.DashboardRead` | Dashboard |

Scope matching (`ScopeAuthorization.HasScope`) accepts short names (`Events.Read`) or full URIs (`api://…/Events.Read`).

### Local / PoC — DevAuth

When `LocalDevelopment:UseDevAuth=true`:

- Handler: `Authentication/DevAuthenticationHandler.cs`
- Send `Authorization: Bearer dev-token`
- Optional headers: `X-Dev-TenantId`, `X-Dev-Scopes`

### Organization email filter

`OrganizationEmailMiddleware` rejects authenticated `/api/*` calls unless the user email/UPN ends with `Platform:AllowedEmailDomain` (default `solugrowth.com`). Skipped under DevAuth.

---

## 5. Multi-tenancy and RLS

| Layer | Mechanism |
|-------|-----------|
| Resolve tenant | `TenantProvider` — claims `tenant_id` / `extension_TenantId` / `tid`, else app id `azp`/`appid` |
| App filter | Services filter `HrEvents` / `MnoEvents` / `ActivityLogs` by `TenantId` |
| SQL session | `TenantSessionContextInterceptor` sets `SESSION_CONTEXT(N'TenantId')` |
| DB RLS | `SQL/RlsPolicies.sql` (FILTER/BLOCK on tenant tables) |

**Important:** `Signal` (fraud signals) has **no** `TenantId`. It stores only an **ID-number hash** so alerts can be shared across tenants without exposing raw PII or tenant identity.

Seeded demo tenants (see `DatabaseSeeder`): fixed GUIDs for Vodacom / MTN used with DevAuth.

---

## 6. Core domain flows

### Submit HR event — `POST /api/v1/hr-events`

```
HrEventsController
  → HrEventService.SubmitAsync
      → RiskScoreService.CalculateHrRiskScore
      → save HrEvent (tenant-scoped; ID stored as submitted)
      → HashingService.HashIdNumber
      → FraudSignalService.UpsertFromHrEventAsync  (HR_Alert signal)
```

### Submit MNO event — `POST /api/v1/mno-events`

Same pattern via `MnoEventService` → `CalculateMnoRiskScore` → upsert `MNO_Alert` (e.g. category `SIMVelocity`).

### Fraud signals — `GET /api/v1/fraud-signals`

Anonymous list/detail: hash, type, category, occurrence count, risk score — **no** employer, MSISDN, or tenant fields.

### ID check — `POST /api/v1/lookup/id-check`

Hash the submitted ID → return matching active signals (`FraudSignalService.CheckIdAsync`).

### Dashboard / activity log

- Dashboard aggregates tenant-scoped HR/MNO stats + top signals.
- Activity log is written by middleware after each authenticated API call (best-effort).

---

## 7. Key services (why they exist)

### `HashingService`

- Input: SA ID number (+ `Platform:Salt` from Key Vault).
- Output: lowercase SHA-256 hex used as `Signal.IdNumberHash`.
- Enables cross-tenant matching without storing the ID on the signal row.

### `RiskScoreService`

- Scores individual HR/MNO events (0–100).
- Aggregates signal scores when the same hash/type/category is seen again.

### `FraudSignalService`

- Upserts signals from HR/MNO submissions.
- Serves list / by-hash / ID-check APIs.

---

## 8. Configuration and secrets

| Setting | Purpose |
|---------|---------|
| `ConnectionStrings:DefaultConnection` | SQL (from Key Vault secret `SqlConnectionString` in Azure) |
| `Platform:Salt` | Hashing salt (Key Vault `PlatformSalt`) |
| `Platform:AllowedEmailDomain` | Email domain gate (`solugrowth.com`) |
| `AzureAd:*` | Entra app registration |
| `LocalDevelopment:UseDevAuth` | Switch DevAuth vs Entra |
| `LocalDevelopment:EnableRls` | Enable `TenantRlsMiddleware` |
| `KeyVault:VaultUri` | e.g. `https://cmckv.vault.azure.net/` |

App Service wires:

```text
ConnectionStrings__DefaultConnection = @Microsoft.KeyVault(SecretUri=.../SqlConnectionString)
Platform__Salt                       = @Microsoft.KeyVault(SecretUri=.../PlatformSalt)
```

Current PoC DB target: **`COMRIC_DB`** on `sql-comric-poc.database.windows.net` (see `scripts/set-keyvault-secrets.ps1`).

---

## 9. Deploy and CI

| Path | What it does |
|------|----------------|
| `scripts/build-ui.ps1` | Build UI → copy into `wwwroot` |
| `scripts/deploy-appservice-zip.ps1` | Manual zip deploy to App Service |
| `.github/workflows/deploy-appservice.yml` | On push to `main`: build UI + API → deploy **ComricWA** |
| `scripts/apply-migrations-azure.ps1` | `dotnet ef database update` via Key Vault |
| `infra/main.bicep` | Provision SQL / KV / App Service |

Migrations also run automatically on app startup (`MigrateAsync` in `Program.cs`).

---

## 10. How to read the code (suggested order)

1. `Program.cs` — wiring and pipeline  
2. `Controllers/HrEventsController.cs` + `Services/HrEventService.cs` — happy path  
3. `Services/FraudSignalService.cs` + `Services/HashingService.cs` — cross-tenant model  
4. `Middleware/` + `Authorization/` — security gates  
5. `Data/TenantSessionContextInterceptor.cs` + `SQL/RlsPolicies.sql` — tenancy at SQL  
6. `scripts/deploy-appservice-zip.ps1` — how it reaches Azure  

UI companion repo: `ComricFraudCalculatorUI` (MSAL login, attaches Bearer token on API calls).

---

## 11. PoC limitations (intentional / known gaps)

- Raw ID numbers / MSISDNs are stored on tenant event tables (hashed only on signals).
- No response-side PII masking helpers yet.
- SPA is same-origin on App Service (not Azure Static Web Apps).
- Production deploy path is **zip** to App Service, not Docker CI (Docker scripts exist optionally).
- Authorization uses **scopes (`scp`)**, not Entra app **roles**.
