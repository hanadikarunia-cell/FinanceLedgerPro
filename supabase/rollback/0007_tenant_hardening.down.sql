-- Reverse of supabase/migrations/0007_tenant_hardening.sql. NOT run automatically (the Supabase
-- CLI only applies supabase/migrations/); run it by hand if 0007 must be undone.

-- 2. tenants: back to no row-level security.
drop policy if exists tenants_delete on tenants;
drop policy if exists tenants_update on tenants;
drop policy if exists tenants_insert on tenants;
drop policy if exists tenants_select on tenants;
alter table tenants disable row level security;

-- 1. Restore the Client 1 defaults from 0004.
alter table users               alter column tenant_id set default '00000000-0000-0000-0000-000000000001';
alter table branches            alter column tenant_id set default '00000000-0000-0000-0000-000000000001';
alter table transactions        alter column tenant_id set default '00000000-0000-0000-0000-000000000001';
alter table audit_logs          alter column tenant_id set default '00000000-0000-0000-0000-000000000001';
alter table attachments         alter column tenant_id set default '00000000-0000-0000-0000-000000000001';
alter table petty_cash_requests alter column tenant_id set default '00000000-0000-0000-0000-000000000001';
alter table cars                alter column tenant_id set default '00000000-0000-0000-0000-000000000001';
alter table invoices            alter column tenant_id set default '00000000-0000-0000-0000-000000000001';
