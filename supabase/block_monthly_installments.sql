-- Block monthly installment defaults (VIP/Admin config)
-- Syncs like other HubBuilders tables (sync_id PK, updated_at, is_deleted).

create table if not exists public.block_monthly_installments (
  sync_id text primary key,
  block_sync_id text,
  monthly_amount numeric not null default 0,
  dup_key text,
  staff_name text,
  updated_at timestamptz not null default now(),
  is_deleted boolean not null default false
);
create index if not exists idx_block_monthly_installments_updated
  on public.block_monthly_installments(updated_at);
create index if not exists idx_block_monthly_installments_block
  on public.block_monthly_installments(block_sync_id)
  where is_deleted = false;

drop trigger if exists trg_block_monthly_installments_touch on public.block_monthly_installments;
create trigger trg_block_monthly_installments_touch
  before update on public.block_monthly_installments
  for each row execute function public.hb_touch_updated_at();

alter table public.block_monthly_installments enable row level security;
drop policy if exists hb_all on public.block_monthly_installments;
create policy hb_all on public.block_monthly_installments
  for all using (true) with check (true);
grant all on table public.block_monthly_installments to anon, authenticated, service_role;

notify pgrst, 'reload schema';
