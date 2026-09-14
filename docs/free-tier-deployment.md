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

## 4. Create the Render services

Render has no native .NET buildpack, so both services build from the Dockerfiles at
`src/FinanceLedger.API/Dockerfile` and `src/FinanceLedger.Worker/Dockerfile`
(multi-stage: `dotnet publish` in an SDK image, then a slim ASP.NET/runtime image).
The repo root `render.yaml` is a Render **Blueprint** that declares both services so
you don't have to configure build/start commands by hand.

1. Sign in to [render.com](https://render.com) with GitHub and grant access to this repo.
2. **New → Blueprint** → select this repo. Render reads `render.yaml` and proposes
   two services: `financeledgerpro-api` (Web Service) and `financeledgerpro-worker`
   (Background Worker), both on the free plan.
3. Render will prompt for every environment variable marked `sync: false` in
   `render.yaml` — fill these in from [.env.example](../.env.example) /
   step 1-3 above (`ConnectionStrings__Postgres`, `Supabase__Url`,
   `Supabase__AnonKey`, `Supabase__ServiceRoleKey`, `Resend__ApiKey`,
   `Resend__SenderAddress`, `Cors__Origins__0` for the API;
   `GoogleSheets__SpreadsheetId`/`GoogleSheets__CredentialsJson` for the worker —
   paste the service account JSON as a single-line value).
4. Apply the blueprint. Both services build and deploy.
5. Confirm `https://<api-service>.onrender.com/health` returns 200, and
   `https://<api-service>.onrender.com/health/ready` returns 200 once Postgres/Storage
   are reachable. Check the worker's logs for "Google Sheets sync worker started."

Both services auto-redeploy on every push to the repo's default branch. If you'd
rather configure services by hand instead of via the Blueprint, use runtime **Docker**
with the same two Dockerfiles as the Docker build context set to the repo root.

## 5. Create the Vercel project

1. [vercel.com](https://vercel.com) → New Project → import this repo.
2. Root Directory: `src/FinanceLedger.Web`. Framework: Vite (auto-detected).
3. Environment variable: `VITE_API_BASE_URL` = the Render API URL from step 4
   (`https://<service>.onrender.com`, no trailing slash).
4. Deploy.

## 6. Wire CORS back to the deployed frontend

Once the Vercel URL is known, set `Cors__Origins__0` (or `FRONTEND_ORIGIN`, per
however you templated `appsettings.Production.json`) on the Render API service to the
Vercel URL, and redeploy the API so the browser can call it.

## 7. Verify end-to-end

1. Log in at the Vercel URL with the seeded manager account
   (`admin@financeledger.local` / `Admin@123` — **change this password immediately**
   via Settings, since the seed step provisions it in Supabase Auth on first API
   startup).
2. Create a transaction, upload an attachment, confirm it downloads.
3. Approve the transaction; confirm the Worker's next sync cycle (or trigger it
   manually if you've wired the admin endpoint) writes it to the configured Google
   Sheet.
4. Trigger a password reset and an approval email; confirm it arrives via Resend.

## 8. Mobile (optional)

Set `EXPO_PUBLIC_API_BASE_URL` to the Render API URL before building
(`src/FinanceLedger.Mobile/.env` or `app.json` → `expo.extra.apiBaseUrl`).

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| `/health/ready` returns 503 | Wrong `POSTGRES_CONNECTION_STRING`, or the Supabase project is paused (free-tier projects pause after a week of inactivity — open the dashboard to wake it) |
| Login fails with a generic error | `SUPABASE_URL`/`SUPABASE_ANON_KEY` mismatch between Render env vars and the Supabase dashboard values, or the API can't reach the JWKS endpoint |
| File upload fails | `attachments` bucket doesn't exist yet, or `SUPABASE_SERVICE_ROLE_KEY` is wrong |
| Worker never syncs | `GOOGLE_SHEETS_CREDENTIALS_JSON` malformed, or the service account isn't shared as an editor on the target Sheet |
| CORS errors in the browser console | `Cors__Origins__0` on the Render API doesn't match the Vercel URL exactly (scheme + host, no trailing slash) |
