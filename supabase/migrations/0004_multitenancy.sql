-- Multi-tenancy ("sites"): one shared database, every business row belongs to a tenant.
--
-- This step is deliberately backward compatible: tenant_id defaults to Client 1, so the
-- previous API build keeps working while this is applied. Migration 0005 removes the
-- defaults and adds row-level security once the new build is live.

create table if not exists tenants (
    id text primary key,
    name text not null,
    code text not null,
    is_active boolean not null default true,
    created_date timestamptz not null default now()
);

create unique index if not exists ix_tenants_code on tenants (lower(code));

-- Fixed ids: the seeder and the Worker refer to Client 1 by this id.
insert into tenants (id, name, code) values
    ('00000000-0000-0000-0000-000000000001', 'Client 1', 'CLIENT1'),
    ('00000000-0000-0000-0000-000000000002', 'Client 2', 'CLIENT2')
on conflict (id) do nothing;

-- Existing data becomes Client 1's.
alter table users               add column if not exists tenant_id text not null default '00000000-0000-0000-0000-000000000001';
alter table branches            add column if not exists tenant_id text not null default '00000000-0000-0000-0000-000000000001';
alter table transactions        add column if not exists tenant_id text not null default '00000000-0000-0000-0000-000000000001';
alter table audit_logs          add column if not exists tenant_id text not null default '00000000-0000-0000-0000-000000000001';
alter table attachments         add column if not exists tenant_id text not null default '00000000-0000-0000-0000-000000000001';
alter table petty_cash_requests add column if not exists tenant_id text not null default '00000000-0000-0000-0000-000000000001';
alter table cars                add column if not exists tenant_id text not null default '00000000-0000-0000-0000-000000000001';
alter table invoices            add column if not exists tenant_id text not null default '00000000-0000-0000-0000-000000000001';

-- users and audit_logs keep no foreign key: the Application Admin's own user row and
-- app-level audit entries legitimately belong to no site (tenant_id = '').
alter table branches            add constraint fk_branches_tenant            foreign key (tenant_id) references tenants (id);
alter table transactions        add constraint fk_transactions_tenant        foreign key (tenant_id) references tenants (id);
alter table attachments         add constraint fk_attachments_tenant         foreign key (tenant_id) references tenants (id);
alter table petty_cash_requests add constraint fk_petty_cash_tenant          foreign key (tenant_id) references tenants (id);
alter table cars                add constraint fk_cars_tenant                foreign key (tenant_id) references tenants (id);
alter table invoices            add constraint fk_invoices_tenant            foreign key (tenant_id) references tenants (id);

-- New role: the Application Admin.
alter table users drop constraint if exists users_role_check;
alter table users add constraint users_role_check check (role in ('Manager', 'User', 'AppAdmin'));

-- Branch codes are unique per site, not globally (two clients can both have "JKT").
drop index if exists ix_branches_code;
create unique index if not exists ix_branches_tenant_code on branches (tenant_id, lower(code));

create index if not exists ix_users_tenant                on users (tenant_id);
create index if not exists ix_transactions_tenant_date    on transactions (tenant_id, transaction_date desc);
create index if not exists ix_audit_logs_tenant_timestamp on audit_logs (tenant_id, timestamp desc);
create index if not exists ix_attachments_tenant          on attachments (tenant_id);
create index if not exists ix_petty_cash_tenant           on petty_cash_requests (tenant_id);
create index if not exists ix_cars_tenant                 on cars (tenant_id);
create index if not exists ix_invoices_tenant_date        on invoices (tenant_id, invoice_date desc);
