-- Incremental, non-destructive alignment for databases where shipping_quotes
-- was created by an earlier version of the shipping feature.
--
-- The application persists the displayed carrier and service in these columns.
-- This script does not delete, rename, or overwrite any existing data.

BEGIN;

ALTER TABLE IF EXISTS public.shipping_quotes
    ADD COLUMN IF NOT EXISTS user_id uuid,
    ADD COLUMN IF NOT EXISTS origin_cep varchar(8),
    ADD COLUMN IF NOT EXISTS destination_postal_code varchar(8),
    ADD COLUMN IF NOT EXISTS items_hash varchar(64),
    ADD COLUMN IF NOT EXISTS provider varchar(30),
    ADD COLUMN IF NOT EXISTS carrier varchar(120),
    ADD COLUMN IF NOT EXISTS service varchar(120),
    ADD COLUMN IF NOT EXISTS price_cents bigint,
    ADD COLUMN IF NOT EXISTS delivery_days integer,
    ADD COLUMN IF NOT EXISTS expires_at timestamptz,
    ADD COLUMN IF NOT EXISTS created_at timestamptz;

-- Some homologation databases contain columns from the previous quote schema
-- (carrier_name, service_name and service_code).  The current application
-- stores the canonical values in carrier and service above.  Keep legacy
-- columns and data intact, but remove their obsolete NOT NULL requirement so
-- an insert made by the current application is not rejected.
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'shipping_quotes'
          AND column_name = 'service_code'
    ) THEN
        ALTER TABLE public.shipping_quotes ALTER COLUMN service_code DROP NOT NULL;
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'shipping_quotes'
          AND column_name = 'carrier_name'
    ) THEN
        ALTER TABLE public.shipping_quotes ALTER COLUMN carrier_name DROP NOT NULL;
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'shipping_quotes'
          AND column_name = 'service_name'
    ) THEN
        ALTER TABLE public.shipping_quotes ALTER COLUMN service_name DROP NOT NULL;
    END IF;
END $$;

COMMIT;

-- Optional verification after applying:
-- SELECT column_name, data_type, is_nullable
-- FROM information_schema.columns
-- WHERE table_schema = 'public' AND table_name = 'shipping_quotes'
-- ORDER BY ordinal_position;
