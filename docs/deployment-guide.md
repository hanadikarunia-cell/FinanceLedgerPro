# Finance Ledger Pro — Deployment Guide

> **Version:** 2.0 — Render + Supabase + Vercel free-tier stack
> **Audience:** Engineering / DevOps
>
> For exact click-by-click / command-by-command setup steps, see
> [free-tier-deployment.md](free-tier-deployment.md). This document explains the
> topology and the reasoning behind it.

---

## 1. Architecture Recap (what gets deployed, and where)

| Component | Where | Notes |
|-----------|-------|-------|
| `FinanceLedger.API` | Render Web Service | ASP.NET Core 8, `*.onrender.com` domain, auto-deployed on push to the default branch |
| `FinanceLedger.Worker` | Render Background Worker | Same repo/image, different entrypoint/start command; polls approved transactions and syncs to Google Sheets |
| `FinanceLedger.Web` | Vercel | Vite static build, framework auto-detected |
| `FinanceLedger.Mobile` | Not deployed by this pipeline | Built locally / via `infra/github-actions/mobile.yml`; only needs `EXPO_PUBLIC_API_BASE_URL` pointed at the Render API |
| Postgres | Supabase project | Schema applied from `supabase/migrations/*.sql` |
| File storage | Supabase Storage | Private `attachments` bucket, accessed server-side only via the service_role key |
| Auth | Supabase Auth | Email/password; JWTs validated by the API using the project's JWT secret |
| Email | Resend | Free-tier transactional email for approval notifications |

Everything above fits comfortably within each platform's free tier for a
single-team/personal deployment. There is no separate CI/CD pipeline to deploy the
API or web app — Render and Vercel both watch the GitHub repo directly and redeploy
on every push (configurable per-branch). GitHub Actions (`.github/workflows/`) is
still used for CI checks (CodeQL, mobile build) but is not part of the deploy path.

## 2. Why this replaced the previous Azure/Entra/Cosmos stack

This app previously ran on Azure App Service + Cosmos DB + Blob Storage + Entra
External ID + Application Insights + Key Vault + Bicep/GitHub Actions OIDC deploys.
That stack has no meaningful free tier at this app's scale. The equivalent free-tier
stack keeps the same Clean Architecture boundaries — only the `Infrastructure` project
and `Program.cs` DI wiring changed:

| Azure | Free-tier replacement |
|---|---|
| Cosmos DB (EF Core Cosmos provider) | Supabase Postgres (EF Core Npgsql provider) |
| Blob Storage | Supabase Storage (REST API, private bucket, server-side only) |
| Entra External ID | Supabase Auth (GoTrue REST API + Admin API) |
| Azure Communication Services | Resend |
| Key Vault | Render/Vercel environment variables |
| Application Insights | `/health` + `/health/ready` endpoints (Sentry free tier optional, not wired up) |
| App Service + Bicep + GitHub Actions OIDC | Render/Vercel git-native auto-deploy |

`infra/bicep` and `infra/github-actions/appservice.yml`/`infra.yml` are left in the
repo, unmodified, purely for historical reference — they are not part of the current
deploy path and can be deleted at any time without affecting the app.

## 3. Supabase project (Postgres + Storage + Auth)

One Supabase project covers all three services. See
[free-tier-deployment.md](free-tier-deployment.md) for the exact steps to:

- Create the project and apply `supabase/migrations/0001_init.sql` (via `supabase db
  push` or the SQL editor).
- Create the private `attachments` Storage bucket.
- Collect the four values the API needs: project URL, anon key, service_role key, and
  JWT secret (Settings → API).
- Note the Postgres connection string (Settings → Database → Connection string —
  prefer the **Session pooler** variant for Render's free tier, since it's
  IPv4-compatible and Render's outbound network is IPv4-only).

RBAC (`Manager`/`User` role + assigned branches) is not stored in Supabase Auth's
built-in fields — it's stored in the app's own `users` table (source of truth, edited
via the Users admin screen) and mirrored into each Supabase Auth user's `app_metadata`
whenever a Manager creates or updates a user. Supabase embeds `app_metadata` into
every access token it issues, so the API can read `role`/`branches` straight off the
JWT without an extra database round-trip or a custom Postgres Auth Hook.

## 4. Render (API + Worker)

Two services, same repo:

- **API** — Web Service, `src/FinanceLedger.API`. Build: `dotnet publish -c Release -o
  out`. Start: `dotnet out/FinanceLedger.API.dll`. Render injects `PORT`; the app
  binds to it automatically via Kestrel's default `ASPNETCORE_URLS` handling — set
  `ASPNETCORE_URLS=http://+:%PORT%` if it doesn't pick it up implicitly on Render's
  runtime. Health check path: `/health`.
- **Worker** — Background Worker, `src/FinanceLedger.Worker`. Build: `dotnet publish
  -c Release -o out`. Start: `dotnet out/FinanceLedger.Worker.dll`. No public port; it
  runs the Google Sheets sync loop on the interval configured via `Sync:IntervalMinutes`.

Both read the same `POSTGRES_CONNECTION_STRING`/`SUPABASE_*`/`RESEND_*` environment
variables (see [.env.example](../.env.example)); the Worker additionally needs
`GOOGLE_SHEET_ID` and `GOOGLE_SHEETS_CREDENTIALS_JSON`.

Render's free tier spins the API down after inactivity and cold-starts on the next
request (expect a ~30-60s first request after idle) — acceptable for a personal
project; there is no free "always-on" tier.

## 5. Vercel (Web frontend)

`src/FinanceLedger.Web` is a standard Vite + React app — Vercel auto-detects the
framework. Set the project's Root Directory to `src/FinanceLedger.Web` and set
`VITE_API_BASE_URL` to the Render API's `https://<service>.onrender.com` URL. No
backend code runs on Vercel; it's a static SPA calling the Render API over HTTPS.

## 6. Health checks & observability

`GET /health` is a liveness probe (always 200 once the process is up, no dependency
checks — used as Render's health check path so a slow/unreachable Postgres doesn't
cause Render to kill and restart a perfectly healthy process). `GET /health/ready`
additionally checks Postgres and Supabase Storage reachability — use this one for
manual/external monitoring, not as the platform's own health check path.

Application Insights is not replaced with an equivalent — it's optional at this
scale. If you want error tracking, wire up Sentry's free tier (`Sentry.AspNetCore`)
separately; this migration does not include it.

## 7. Rollback

Render keeps previous deploys and supports one-click rollback to any prior successful
deploy per service. Vercel does the same for the frontend (Deployments → ⋯ → Promote
to Production on any previous deployment). There are no infrastructure-level rollback
concerns (no Bicep/Terraform state) — rolling back is purely "redeploy an older commit."

Database changes are additive-only SQL migration files
(`supabase/migrations/000N_*.sql`); there is no automatic down-migration tooling. Write
and apply a new forward migration to undo a schema change rather than editing an
already-applied one.

## 8. Mobile app

`src/FinanceLedger.Mobile` is unaffected by this migration beyond its API base URL —
it has no direct Azure/Cosmos/Blob/Entra SDK usage. Set
`EXPO_PUBLIC_API_BASE_URL` to the Render API URL (or `app.json` →
`expo.extra.apiBaseUrl`) when building.
