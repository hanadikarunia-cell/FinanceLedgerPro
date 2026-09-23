-- Row-Level Security: a second, database-level enforcement of site isolation on top of the
-- EF Core global query filter. Applying this migration has NO effect on the running app:
-- the connection role Render uses today (postgres.<project ref>) has BYPASSRLS, so it keeps
-- working exactly as before. Isolation only becomes DB-enforced once the API's connection
-- string is switched to the app_api role created below (a separate, deliberate step -
-- see docs/architecture.md §7a "Row-level security"). Reverting that one env var instantly
-- rolls this back with no further migration needed.

do $$
begin
  if not exists (select from pg_roles where rolname = 'app_api') then
    create role app_api with login password 'Cc5BLAcgOiYfXmx45RVZRk7tvySjyXSu';
  end if;
end$$;

grant usage on schema public to app_api;
grant select, insert, update, delete on
  tenants, users, branches, transactions, audit_logs, attachments,
  petty_cash_requests, cars, invoices, feedback, release_notes
  to app_api;

alter table users               enable row level security;
alter table branches            enable row level security;
alter table transactions        enable row level security;
alter table audit_logs          enable row level security;
alter table attachments         enable row level security;
alter table petty_cash_requests enable row level security;
alter table cars                enable row level security;
alter table invoices            enable row level security;

-- One identical policy per tenant-scoped table: a row is visible/writable when its
-- tenant_id matches the session's app.tenant_id, or the session has explicitly opted
-- into bypass. Both session variables are set by the API's TenantSessionInterceptor on
-- every connection open, from the request's effective identity - never from request
-- input - and bypass is only ever turned on for the app's own few cross-site code paths
-- (login lookup, "View as" resolution, site management, startup seeding).
do $$
declare
  t text;
begin
  foreach t in array array['users','branches','transactions','audit_logs','attachments','petty_cash_requests','cars','invoices']
  loop
    execute format(
      'drop policy if exists tenant_isolation on %I; create policy tenant_isolation on %I
         using (tenant_id = coalesce(current_setting(''app.tenant_id'', true), '''') or coalesce(current_setting(''app.bypass_rls'', true), ''off'') = ''on'')
         with check (tenant_id = coalesce(current_setting(''app.tenant_id'', true), '''') or coalesce(current_setting(''app.bypass_rls'', true), ''off'') = ''on'')',
      t, t);
  end loop;
end$$;
