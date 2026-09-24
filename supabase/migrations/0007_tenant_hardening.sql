-- Tenant hardening. Apply AFTER the API build that contains the bypass-scoped TenantRepository is
-- live: the previous build reads `tenants` without that scope, so with row-level security on the
-- table below it would find no site at login. Idempotent; reverse in
-- supabase/rollback/0007_tenant_hardening.down.sql.
--
--  1. Remove the fail-open tenant defaults left by 0004.
--  2. Row-level security on `tenants`.

-- ---------------------------------------------------------------------------------------
-- 1. tenant_id had DEFAULT 'Client 1' so the old API build kept working while 0004 was
--    applied. That default is now a liability: any code path that forgot to name a site
--    would silently write into Client 1. With no default the insert fails loudly instead.
--    (The app always stamps the site; users/audit_logs use '' for "no site" explicitly.)
-- ---------------------------------------------------------------------------------------
alter table users               alter column tenant_id drop default;
alter table branches            alter column tenant_id drop default;
alter table transactions        alter column tenant_id drop default;
alter table audit_logs          alter column tenant_id drop default;
alter table attachments         alter column tenant_id drop default;
alter table petty_cash_requests alter column tenant_id drop default;
alter table cars                alter column tenant_id drop default;
alter table invoices            alter column tenant_id drop default;

-- ---------------------------------------------------------------------------------------
-- 2. `tenants`: a site session may read only its own row and can never write; everything
--    else needs the API's explicit bypass (site lookups at login, Application Admin's site
--    management). Same session variables as the policies from 0005.
-- ---------------------------------------------------------------------------------------
alter table tenants enable row level security;

drop policy if exists tenants_select on tenants;
create policy tenants_select on tenants for select
  using (id = coalesce(current_setting('app.tenant_id', true), '')
         or coalesce(current_setting('app.bypass_rls', true), 'off') = 'on');

drop policy if exists tenants_insert on tenants;
create policy tenants_insert on tenants for insert
  with check (coalesce(current_setting('app.bypass_rls', true), 'off') = 'on');

drop policy if exists tenants_update on tenants;
create policy tenants_update on tenants for update
  using (coalesce(current_setting('app.bypass_rls', true), 'off') = 'on')
  with check (coalesce(current_setting('app.bypass_rls', true), 'off') = 'on');

drop policy if exists tenants_delete on tenants;
create policy tenants_delete on tenants for delete
  using (coalesce(current_setting('app.bypass_rls', true), 'off') = 'on');
