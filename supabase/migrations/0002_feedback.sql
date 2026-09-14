-- Feedback: any user can submit feedback (optionally with a pasted image, uploaded
-- through the existing attachment pipeline), a Manager triages each as Major/Minor.

create table if not exists feedback (
    id text primary key,
    message text not null default '',
    image_url text,
    severity text check (severity in ('Minor', 'Major')),
    submitted_by text not null default '',
    submitted_by_name text not null default '',
    submitted_date timestamptz not null default now(),
    app_version text not null default ''
);

create index if not exists ix_feedback_submitted_by on feedback (submitted_by);
create index if not exists ix_feedback_submitted_date on feedback (submitted_date desc);
