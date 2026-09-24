-- Structured audit trail and server-side impersonation sessions. Purely additive and safe to
-- apply BEFORE the API build that uses it (the previous build simply ignores the new columns
-- and table). Idempotent; reverse in supabase/rollback/0006_audit_and_impersonation.down.sql.
--
-- The hardening that needs the newer API build (removing tenant defaults, row-level security
-- on `tenants`) is deliberately a separate migration, 0007, applied AFTER that build is live.

-- ---------------------------------------------------------------------------------------
-- 1. Impersonation sessions. The client only ever holds a session id; the target person is
--    read from here, never from the request. Every session is read-only, so there is no
--    mode column. Reachable only through the API's bypass scope.
-- ---------------------------------------------------------------------------------------
create table if not exists impersonation_sessions (
    id text primary key,
    admin_user_id text not null references users (id),
    target_user_id text not null references users (id),
    target_tenant_id text not null references tenants (id),
    started_at timestamptz not null default now(),
    expires_at timestamptz not null,
    ended_at timestamptz,
    ip_address text,
    user_agent text,
    constraint ck_impersonation_expiry check (expires_at > started_at)
);

create index if not exists ix_impersonation_admin_started on impersonation_sessions (admin_user_id, started_at desc);
create index if not exists ix_impersonation_target on impersonation_sessions (target_user_id);

alter table impersonation_sessions enable row level security;

drop policy if exists impersonation_bypass_only on impersonation_sessions;
create policy impersonation_bypass_only on impersonation_sessions
  using (coalesce(current_setting('app.bypass_rls', true), 'off') = 'on')
  with check (coalesce(current_setting('app.bypass_rls', true), 'off') = 'on');

grant select, insert, update, delete on impersonation_sessions to app_api;

-- ---------------------------------------------------------------------------------------
-- 2. Audit trail. `user_id` stays the effective user (who the action was done as);
--    `actor_user_id` is the real signed-in person; `acting_as_user_id` is set only while
--    impersonating. Existing rows get actor = user (nobody was impersonating before).
-- ---------------------------------------------------------------------------------------
alter table audit_logs add column if not exists actor_user_id text;
alter table audit_logs add column if not exists acting_as_user_id text;
alter table audit_logs add column if not exists impersonation_session_id text;
alter table audit_logs add column if not exists metadata text;

update audit_logs set actor_user_id = user_id where actor_user_id is null;

create index if not exists ix_audit_logs_actor on audit_logs (actor_user_id, timestamp desc);
create index if not exists ix_audit_logs_session on audit_logs (impersonation_session_id)
  where impersonation_session_id is not null;

alter table audit_logs drop constraint if exists audit_logs_action_check;
alter table audit_logs add constraint audit_logs_action_check check (action in (
    'Create', 'Update', 'Delete', 'Approve', 'Reject', 'Void',
    'Login', 'Logout', 'PasswordReset', 'RoleChange', 'Deactivate',
    'TenantCreate', 'TenantUpdate', 'ImpersonationStart', 'ImpersonationEnd', 'Export'));
