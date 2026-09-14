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

Sign in to [render.com](https://render.com) with GitHub and grant access to this repo.

### API (Web Service)
1. New → Web Service → select this repo.
2. Root/build settings:
   - Runtime: Docker, or Native (.NET) if Render's native .NET runtime is selected.
   - Build command: `dotnet publish src/FinanceLedger.API -c Release -o out`
   - Start command: `dotnet out/FinanceLedger.API.dll`
3. Health check path: `/health`.
4. Environment variables (Render dashboard → Environment): every `POSTGRES_*`,
   `SUPABASE_*`, `RESEND_*`, and `FRONTEND_ORIGIN`/`Cors__Origins__0` value from
   [.env.example](../.env.example). Set `ASPNETCORE_ENVIRONMENT=Production`.
5. Deploy. Confirm `https://<service>.onrender.com/health` returns 200, and
   `https://<service>.onrender.com/health/ready` returns 200 once Postgres/Storage are
   reachable.

### Worker (Background Worker)
1. New → Background Worker → same repo.
2. Build command: `dotnet publish src/FinanceLedger.Worker -c Release -o out`
3. Start command: `dotnet out/FinanceLedger.Worker.dll`
4. Environment variables: same `POSTGRES_*`/`SUPABASE_*` as the API, plus
   `GOOGLE_SHEET_ID` and `GOOGLE_SHEETS_CREDENTIALS_JSON` (paste the service account
   JSON as a single-line value, or use a Render Secret File and point
   `GoogleSheets:CredentialsJson` at its path).
5. Deploy. Check the Render logs for "Google Sheets sync worker started."

Both services auto-redeploy on every push to the repo's default branch.

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
