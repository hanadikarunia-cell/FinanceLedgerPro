-- Reverse of supabase/migrations/0006_audit_and_impersonation.sql. NOT run automatically; run it
-- by hand if 0006 must be undone. Deploy the previous API build first: it does not know the new
-- audit columns or table, and the current one writes them.

-- 2. Audit trail: drop the new columns and restore the original action list. NOT VALID so rows
--    already written with a newer action value do not block the rollback.
drop index if exists ix_audit_logs_session;
drop index if exists ix_audit_logs_actor;
alter table audit_logs drop column if exists metadata;
alter table audit_logs drop column if exists impersonation_session_id;
alter table audit_logs drop column if exists acting_as_user_id;
alter table audit_logs drop column if exists actor_user_id;

alter table audit_logs drop constraint if exists audit_logs_action_check;
alter table audit_logs add constraint audit_logs_action_check
  check (action in ('Create', 'Update', 'Delete', 'Approve', 'Reject', 'Void')) not valid;

-- 1. Impersonation sessions.
drop table if exists impersonation_sessions;
