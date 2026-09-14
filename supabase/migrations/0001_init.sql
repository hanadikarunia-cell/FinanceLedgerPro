-- FinanceLedgerPro initial schema — Postgres replacement for the 5 (really 8) Cosmos
-- DB containers. IDs stay as `text` (GUID strings) to match the app's existing
-- string-typed entity IDs with zero EF Core type-mapping friction. There are no
-- foreign keys: the previous Cosmos design had none either (each container was an
-- independent document store), and adding them now would require enforcing
-- insert/seed ordering the app doesn't currently guarantee. Optimistic concurrency
-- uses Postgres's built-in `xmin` system column (via EF Core's
-- UseXminAsConcurrencyToken), so no explicit version column is needed anywhere.

create table if not exists branches (
    id text primary key,
    name text not null,
    code text not null,
    address text not null default '',
    is_active boolean not null default true
);

create unique index if not exists ix_branches_code on branches (code);

-- users.id is the Supabase Auth user id (auth.users.id) — the app provisions the
-- Auth identity first (via the Admin API) and then inserts this row with the same id,
-- so the two stay joined without a separate mapping table. Role/branches are also
-- mirrored into the Auth user's app_metadata (see SupabaseAuthClient) so they're
-- embedded directly in every access token; this table is the editable source of truth
-- an admin updates, which then gets pushed to app_metadata.
create table if not exists users (
    id text primary key,
    email text not null,
    display_name text not null default '',
    role text not null check (role in ('Manager', 'User')),
    assigned_branches text[] not null default '{}',
    is_active boolean not null default true,
    created_date timestamptz not null default now(),
    -- Unused since the migration to Supabase Auth (credentials live there now);
    -- kept nullable because FinanceLedger.Domain.Entities.User still has the property.
    password_hash text
);

create unique index if not exists ix_users_email on users (lower(email));
create index if not exists ix_users_role on users (role);

create table if not exists transactions (
    id text primary key,
    type text not null check (type in ('Income', 'Expense')),
    category text not null default '',
    description text not null default '',
    amount numeric(18, 2) not null,
    transaction_date timestamptz not null,
    branch text not null,
    created_by text not null default '',
    created_by_name text not null default '',
    created_date timestamptz not null default now(),
    status text not null default 'Draft' check (status in ('Draft', 'Submitted', 'Approved', 'Rejected')),
    approved_by text,
    approved_date timestamptz,
    attachment_ids text[] not null default '{}',
    related_user_id text,
    car_id text
);

create index if not exists ix_transactions_branch_date on transactions (branch, transaction_date desc);
create index if not exists ix_transactions_branch_status_date on transactions (branch, status, transaction_date desc);
create index if not exists ix_transactions_branch_type_date on transactions (branch, type, transaction_date desc);
create index if not exists ix_transactions_created_by on transactions (created_by);
create index if not exists ix_transactions_approved_by on transactions (approved_by);

create table if not exists audit_logs (
    id text primary key,
    user_id text not null default '',
    user_name text not null default '',
    action text not null check (action in ('Create', 'Update', 'Delete', 'Approve', 'Reject', 'Void')),
    entity text not null default '',
    entity_id text not null default '',
    old_value text,
    new_value text,
    timestamp timestamptz not null default now()
);

create index if not exists ix_audit_logs_user_timestamp on audit_logs (user_id, timestamp desc);
create index if not exists ix_audit_logs_entity_timestamp on audit_logs (entity_id, timestamp desc);

-- storage_path holds a Supabase Storage object key (e.g. "3fa8...pdf") scoped to the
-- `attachments` bucket, replacing the previous full Azure Blob URI.
create table if not exists attachments (
    id text primary key,
    transaction_id text not null default '',
    file_name text not null default '',
    content_type text not null default '',
    size_bytes bigint not null default 0,
    storage_path text not null default '',
    uploaded_by text not null default '',
    uploaded_date timestamptz not null default now()
);

create index if not exists ix_attachments_transaction_id on attachments (transaction_id);

create table if not exists petty_cash_requests (
    id text primary key,
    amount numeric(18, 2) not null,
    reason text not null default '',
    branch text not null,
    requested_by text not null default '',
    requested_by_name text not null default '',
    requested_date timestamptz not null default now(),
    status text not null check (status in ('Draft', 'Submitted', 'Approved', 'Rejected')),
    approved_by text,
    approved_date timestamptz,
    linked_transaction_id text
);

create index if not exists ix_petty_cash_branch_status on petty_cash_requests (branch, status);
create index if not exists ix_petty_cash_requested_by on petty_cash_requests (requested_by);

create table if not exists cars (
    id text primary key,
    branch text not null,
    client text not null default '',
    type text not null default '',
    model text not null default '',
    plate_number text not null default '',
    monthly_bill numeric(18, 2) not null default 0,
    initial_debt numeric(18, 2) not null default 0,
    contract_start_date timestamptz not null,
    contract_duration_months int not null default 0,
    notes text,
    is_active boolean not null default true,
    created_by text not null default '',
    created_date timestamptz not null default now()
);

create index if not exists ix_cars_branch on cars (branch);

create table if not exists invoices (
    id text primary key,
    type text not null check (type in ('Rental', 'ServiceBill')),
    branch text not null,
    client_name text not null default '',
    invoice_date timestamptz not null default now(),
    car_id text,
    monthly_bill numeric(18, 2),
    driver_name text,
    wage_deposit numeric(18, 2),
    fee numeric(18, 2),
    tax_scheme text check (tax_scheme in ('Combined', 'WageOnly', 'FeeOnly')),
    ppn_amount numeric(18, 2) not null default 0,
    pph23_amount numeric(18, 2) not null default 0,
    total_amount numeric(18, 2) not null default 0,
    status text not null default 'Unpaid' check (status in ('Unpaid', 'Paid')),
    paid_date timestamptz,
    linked_income_transaction_id text,
    linked_expense_transaction_id text,
    created_by text not null default '',
    created_by_name text not null default '',
    created_date timestamptz not null default now()
);

create index if not exists ix_invoices_branch_date on invoices (branch, invoice_date desc);
create index if not exists ix_invoices_status on invoices (status);
