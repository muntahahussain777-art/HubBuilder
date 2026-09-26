-- HubBuilders — har form ka alag Supabase table (mixed hb_sync NAHI)
-- Project: HubBuilders (fkvinhehqajjnxnducgc)

create or replace function public.hb_touch_updated_at()
returns trigger language plpgsql as $$
begin
  new.updated_at = now();
  return new;
end $$;

-- 1 societies
create table if not exists public.societies (
  sync_id text primary key,
  name text not null,
  city text,
  created_at timestamptz,
  dup_key text,
  staff_name text,
  updated_at timestamptz not null default now(),
  is_deleted boolean not null default false
);
create index if not exists idx_societies_updated on public.societies(updated_at);

-- 2 blocks
create table if not exists public.blocks (
  sync_id text primary key,
  society_sync_id text,
  name text not null,
  dup_key text,
  staff_name text,
  updated_at timestamptz not null default now(),
  is_deleted boolean not null default false
);
create index if not exists idx_blocks_updated on public.blocks(updated_at);

-- 3 plot_types
create table if not exists public.plot_types (
  sync_id text primary key,
  society_sync_id text,
  code text not null,
  dup_key text,
  staff_name text,
  updated_at timestamptz not null default now(),
  is_deleted boolean not null default false
);
create index if not exists idx_plot_types_updated on public.plot_types(updated_at);

-- 4 plots
create table if not exists public.plots (
  sync_id text primary key,
  society_sync_id text,
  block_sync_id text,
  plot_type_sync_id text,
  plot_no text not null,
  status text not null default 'Available',
  created_at timestamptz,
  dup_key text,
  staff_name text,
  updated_at timestamptz not null default now(),
  is_deleted boolean not null default false
);
create index if not exists idx_plots_updated on public.plots(updated_at);

-- 5 plans
create table if not exists public.plans (
  sync_id text primary key,
  name text,
  price numeric,
  down_payment numeric,
  installments integer,
  notes text,
  dup_key text,
  staff_name text,
  updated_at timestamptz not null default now(),
  is_deleted boolean not null default false
);
create index if not exists idx_plans_updated on public.plans(updated_at);

-- 6 customers
create table if not exists public.customers (
  sync_id text primary key,
  name text not null,
  father_name text,
  mobile text,
  cnic text,
  notes text,
  created_at timestamptz,
  dup_key text,
  staff_name text,
  updated_at timestamptz not null default now(),
  is_deleted boolean not null default false
);
create index if not exists idx_customers_updated on public.customers(updated_at);
create index if not exists idx_customers_dup on public.customers(dup_key) where is_deleted = false and dup_key is not null;

-- 7 brokers
create table if not exists public.brokers (
  sync_id text primary key,
  name text not null,
  phone text,
  is_active integer not null default 1,
  commission_amount_default numeric not null default 0,
  dup_key text,
  staff_name text,
  updated_at timestamptz not null default now(),
  is_deleted boolean not null default false
);
create index if not exists idx_brokers_updated on public.brokers(updated_at);

-- 8 users
create table if not exists public.users (
  sync_id text primary key,
  username text not null,
  upass text not null,
  uname text,
  uphone text,
  role text,
  dup_key text,
  staff_name text,
  updated_at timestamptz not null default now(),
  is_deleted boolean not null default false
);
create index if not exists idx_users_updated on public.users(updated_at);

-- 9 customer_plots
create table if not exists public.customer_plots (
  sync_id text primary key,
  customer_sync_id text,
  plot_sync_id text,
  plan_sync_id text,
  broker_sync_id text,
  booking_date text,
  price numeric not null default 0,
  down_payment numeric not null default 0,
  notes text,
  status text not null default 'Active',
  broker_commission numeric,
  cancelled_date text,
  cancellation_id integer,
  deal_type text not null default 'Installment',
  dup_key text,
  staff_name text,
  updated_at timestamptz not null default now(),
  is_deleted boolean not null default false
);
create index if not exists idx_customer_plots_updated on public.customer_plots(updated_at);

-- 10 receipts
create table if not exists public.receipts (
  sync_id text primary key,
  customer_plot_sync_id text,
  receipt_date text not null,
  amount numeric not null,
  mode text not null default 'Cash',
  ref_no text,
  notes text,
  ledger_sync_id text,
  created_at timestamptz,
  dup_key text,
  staff_name text,
  updated_at timestamptz not null default now(),
  is_deleted boolean not null default false
);
create index if not exists idx_receipts_updated on public.receipts(updated_at);

-- 11 ledger
create table if not exists public.ledger (
  sync_id text primary key,
  customer_plot_sync_id text,
  txn_date text not null,
  voucher_no text,
  narration text,
  debit numeric not null default 0,
  credit numeric not null default 0,
  txn_type text not null,
  created_at timestamptz,
  dup_key text,
  staff_name text,
  updated_at timestamptz not null default now(),
  is_deleted boolean not null default false
);
create index if not exists idx_ledger_updated on public.ledger(updated_at);

-- 12 expensetbl
create table if not exists public.expensetbl (
  sync_id text primary key,
  name text,
  amount numeric,
  category text,
  note text,
  edate text,
  dup_key text,
  staff_name text,
  updated_at timestamptz not null default now(),
  is_deleted boolean not null default false
);
create index if not exists idx_expensetbl_updated on public.expensetbl(updated_at);

-- 13 person
create table if not exists public.person (
  sync_id text primary key,
  name text not null,
  phone text,
  note text,
  is_partner integer not null default 0,
  is_owner integer not null default 0,
  created_on timestamptz,
  dup_key text,
  staff_name text,
  updated_at timestamptz not null default now(),
  is_deleted boolean not null default false
);
create index if not exists idx_person_updated on public.person(updated_at);

-- 14 deal
create table if not exists public.deal (
  sync_id text primary key,
  society_sync_id text,
  owner_sync_id text,
  amount numeric not null,
  deal_date text,
  note text,
  created_on timestamptz,
  dup_key text,
  staff_name text,
  updated_at timestamptz not null default now(),
  is_deleted boolean not null default false
);
create index if not exists idx_deal_updated on public.deal(updated_at);

-- 15 credit
create table if not exists public.credit (
  sync_id text primary key,
  deal_sync_id text,
  partner_sync_id text,
  amount numeric not null,
  pay_date text,
  note text,
  created_on timestamptz,
  dup_key text,
  staff_name text,
  updated_at timestamptz not null default now(),
  is_deleted boolean not null default false
);
create index if not exists idx_credit_updated on public.credit(updated_at);

-- 16 customer_plot_partners
create table if not exists public.customer_plot_partners (
  sync_id text primary key,
  customer_plot_sync_id text,
  partner_sync_id text,
  created_at timestamptz,
  dup_key text,
  staff_name text,
  updated_at timestamptz not null default now(),
  is_deleted boolean not null default false
);
create index if not exists idx_cpp_updated on public.customer_plot_partners(updated_at);

-- 17 partner_money
create table if not exists public.partner_money (
  sync_id text primary key,
  partner_sync_id text,
  txn_date text not null,
  direction text not null,
  amount numeric not null,
  mode text,
  category text,
  customer_plot_sync_id text,
  receipt_sync_id text,
  society_sync_id text,
  note text,
  created_at timestamptz,
  dup_key text,
  staff_name text,
  updated_at timestamptz not null default now(),
  is_deleted boolean not null default false
);
create index if not exists idx_partner_money_updated on public.partner_money(updated_at);

-- 18 staff_entry_log
create table if not exists public.staff_entry_log (
  sync_id text primary key,
  action_type text not null,
  module_name text not null,
  detail text,
  staff_username text,
  staff_name text,
  staff_role text,
  created_at timestamptz,
  dup_key text,
  updated_at timestamptz not null default now(),
  is_deleted boolean not null default false
);
create index if not exists idx_staff_entry_log_updated on public.staff_entry_log(updated_at);

-- 19 block_monthly_installments (VIP block default monthly)
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

-- RLS open (anon desktop/mobile clients)
do $$ declare t text; begin
  foreach t in array array[
    'societies','blocks','plot_types','plots','plans','customers','brokers','users',
    'customer_plots','receipts','ledger','expensetbl','person','deal','credit',
    'customer_plot_partners','partner_money','staff_entry_log','block_monthly_installments'
  ] loop
    execute format('alter table public.%I enable row level security', t);
    execute format('drop policy if exists hb_all on public.%I', t);
    execute format('create policy hb_all on public.%I for all using (true) with check (true)', t);
    execute format('grant all on table public.%I to anon, authenticated, service_role', t);
  end loop;
end $$;

notify pgrst, 'reload schema';
