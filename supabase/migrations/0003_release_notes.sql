-- "What's New" changelog: admin-authored version history, distinct from the
-- user-submitted `feedback` table.

create table if not exists release_notes (
    id text primary key,
    version text not null default '',
    title text not null default '',
    type text not null check (type in ('Major', 'Minor', 'Patch')),
    notes text[] not null default '{}',
    published_by text not null default '',
    published_by_name text not null default '',
    published_date timestamptz not null default now()
);

create index if not exists ix_release_notes_published_date on release_notes (published_date desc);
