# Free-Tier Deployment Runbook

End-to-end steps to take this repo from zero to a running deployment on Supabase +
Render + Vercel, all on free tiers. For *why* the stack looks like this, see
[deployment-guide.md](deployment-guide.md).

## 1. Create the Supabase project

1. Go to [supabase.com](https://supabase.com) → New project. Pick any region close to
   you; note the database password you set (needed for the connection string).
2. **Settings → API**: copy the **Project URL**, **anon public** key, and
   **service_role** key. No JWT secret is needed — Supabase signs access tokens with
   a project-specific asymmetric (ES256) key, and the API validates them against the
   public JWKS endpoint (`{SUPABASE_URL}/auth/v1/.well-known/jwks.json`) automatically.
3. **Settings → Database → Connection string**: copy the **Session pooler** URI (the
   Transaction pooler also works, but Session pooler is the safest default for a
   long-lived EF Core connection pool). It looks like:
   ```
   postgresql://postgres.<project-ref>:<password>@aws-0-<region>.pooler.supabase.com:5432/postgres
   ```
   Convert it to the Npgsql key/value form used by `ConnectionStrings:Postgres`:
   ```
   Host=aws-0-<region>.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<project-ref>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true
   ```

## 2. Apply the schema

Option A — Supabase CLI (recommended, keeps migrations reproducible):
```bash
npm install -g supabase
supabase login
supabase link --project-ref <project-ref>
supabase db push        # applies supabase/migrations/0001_init.sql
```

Option B — paste-and-run: open **SQL Editor** in the Supabase dashboard, paste the
contents of `supabase/migrations/0001_init.sql`, and run it.

Verify: **Table Editor** should show `branches`, `users`, `transactions`,
`audit_logs`, `attachments`, `petty_cash_requests`, `cars`, `invoices`.

## 3. Create the Storage bucket

**Storage → New bucket** → name it `attachments`, leave it **private** (do not enable
public access — the API is the only thing that ever talks to it, using the
service_role key, and relays downloads through `GET /api/v1/files/{id}`).

## 4. Create the Render service

Render has no native .NET buildpack, so the API builds from the Dockerfile at
`src/FinanceLedger.API/Dockerfile` (multi-stage: `dotnet publish` in an SDK image,
then a slim ASP.NET runtime image). The repo root `render.yaml` is a Render
**Blueprint** that declares this service so you don't have to configure build/start
commands by hand.

> **Why only one service:** Render's free tier has no Background Worker instance
> type — that requires a paid plan ($7/mo+) regardless of how many other free
> services you have. Instead of paying for an always-on process just to run the
> Google Sheets sync, that logic lives behind a Manager-only admin endpoint on this
> same free API (`POST /api/v1/admin/sync/google-sheets`), triggered on a schedule by
> a free GitHub Actions workflow — see step 6.

1. Sign in to [render.com](https://render.com) with GitHub and grant access to this repo.
2. **New → Blueprint** → select this repo. Render reads `render.yaml` and proposes
   one service: `financeledgerpro-api` (Web Service, free plan).
3. Render will prompt for every environment variable marked `sync: false` in
   `render.yaml` — fill these in from [.env.example](../.env.example) / step 1-3
   above: `ConnectionStrings__Postgres`, `Supabase__Url`, `Supabase__AnonKey`,
   `Supabase__ServiceRoleKey`, `Resend__ApiKey`, `Resend__SenderAddress`,
   `Cors__Origins__0`, `GoogleSheets__SpreadsheetId`, `GoogleSheets__CredentialsJson`
   (paste the service account JSON directly — Render's value field accepts
   multi-line text, no need to collapse it to one line). Leave `Resend__*` and the
   `GoogleSheets__*` values blank if you don't need email or the Sheets sync yet —
   nothing else depends on either.
4. Apply the blueprint. The service builds and deploys.
5. Confirm `https://<api-service>.onrender.com/health` returns 200, and
   `https://<api-service>.onrender.com/health/ready` returns 200 once Postgres/Storage
   are reachable.

The service auto-redeploys on every push to the repo's default branch. If you'd
rather configure it by hand instead of via the Blueprint, use runtime **Docker** with
the same Dockerfile and the Docker build context set to the repo root.

## 5. Create the Vercel project

1. [vercel.com](https://vercel.com) → New Project → import this repo.
2. Root Directory: `src/FinanceLedger.Web`. Framework: Vite (auto-detected).
3. Environment variable: `VITE_API_BASE_URL` = the Render API URL from step 4
   (`https://<service>.onrender.com`, no trailing slash).
4. Deploy.

## 6. Wire CORS back to the deployed frontend

Once the Vercel URL is known, set `Cors__Origins__0` on the Render API service to the
Vercel URL, and redeploy the API so the browser can call it.

## 7. Schedule the Google Sheets sync (optional, skip if not using Sheets)

Since there's no separate worker process, a scheduled GitHub Actions workflow
(`.github/workflows/sync-google-sheets.yml`, already in this repo, runs every 15
minutes for free) logs in as a Manager and calls the sync endpoint.

1. In the app, create a dedicated **Manager** user for this purpose (Users →
   Create) rather than reusing the seeded default admin — e.g.
   `sync-bot@yourdomain.com` with a strong generated password. This keeps the
   automation credential separate from your real login and easy to revoke later.
2. In the GitHub repo → **Settings → Secrets and variables → Actions**, add:
   - `SYNC_API_BASE_URL` = `https://<api-service>.onrender.com`
   - `SYNC_USER_EMAIL` = that dedicated user's email
   - `SYNC_USER_PASSWORD` = that dedicated user's password
3. The workflow now runs automatically every 15 minutes. Trigger it once manually
   (Actions tab → "Sync approved transactions to Google Sheets" → Run workflow) to
   confirm it succeeds.

## 8. Verify end-to-end

1. Log in at the Vercel URL with the seeded manager account
   (`admin@financeledger.local` / `Admin@123` — **change this password immediately**
   via Settings, since the seed step provisions it in Supabase Auth on first API
   startup).
2. Create a transaction, upload an attachment, confirm it downloads.
3. Approve the transaction; run the sync workflow (or wait for its next scheduled
   run) and confirm the row appears in the configured Google Sheet.
4. Trigger a password reset and an approval email; confirm it arrives via Resend
   (if configured).

## 9. Mobile (optional)

Set `EXPO_PUBLIC_API_BASE_URL` to the Render API URL before building
(`src/FinanceLedger.Mobile/.env` or `app.json` → `expo.extra.apiBaseUrl`).

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| `/health/ready` returns 503 | Wrong `POSTGRES_CONNECTION_STRING`, or the Supabase project is paused (free-tier projects pause after a week of inactivity — open the dashboard to wake it) |
| Login fails with a generic error | `SUPABASE_URL`/`SUPABASE_ANON_KEY` mismatch between Render env vars and the Supabase dashboard values, or the API can't reach the JWKS endpoint |
| File upload fails | `attachments` bucket doesn't exist yet, or `SUPABASE_SERVICE_ROLE_KEY` is wrong |
| Sync workflow fails at login | `SYNC_USER_EMAIL`/`SYNC_USER_PASSWORD` GitHub secrets wrong, or that user isn't `Manager` role |
| Sync workflow fails at the sync step | `GoogleSheets__CredentialsJson` malformed on Render, or the service account isn't shared as an Editor on the target Sheet |
| CORS errors in the browser console | `Cors__Origins__0` on the Render API doesn't match the Vercel URL exactly (scheme + host, no trailing slash) |
