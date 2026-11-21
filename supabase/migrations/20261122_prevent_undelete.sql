-- HubBuilder: atomic anti-resurrect (production pe deploy ALAG approve ke baad).
-- Isolated/test project pe pehle verify karo. Live PC/mobile data yahan se touch nahi.
-- Agar row is_deleted=true hai, naya write is_deleted=false se overwrite nahi kar sakta.

CREATE OR REPLACE FUNCTION public.hb_prevent_undelete()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
  IF OLD.is_deleted IS TRUE
     AND (NEW.is_deleted IS DISTINCT FROM TRUE) THEN
    NEW.is_deleted := TRUE;
    -- deleted row ka updated_at cloud pe delete signal ke liye badhe (optional keep OLD)
    IF NEW.updated_at IS NULL OR NEW.updated_at < OLD.updated_at THEN
      NEW.updated_at := OLD.updated_at;
    END IF;
  END IF;
  RETURN NEW;
END;
$$;

DO $$
DECLARE
  t text;
BEGIN
  FOREACH t IN ARRAY ARRAY[
    'customers', 'customer_plots', 'receipts', 'ledger', 'plots',
    'partner_money', 'customer_plot_partners', 'brokers'
  ]
  LOOP
    EXECUTE format('DROP TRIGGER IF EXISTS trg_hb_prevent_undelete ON public.%I', t);
    EXECUTE format(
      'CREATE TRIGGER trg_hb_prevent_undelete
       BEFORE UPDATE ON public.%I
       FOR EACH ROW EXECUTE FUNCTION public.hb_prevent_undelete()', t);
  END LOOP;
END $$;
