-- Review before applying. Incremental and non-destructive.
ALTER TABLE products ADD COLUMN IF NOT EXISTS weight_grams integer NOT NULL DEFAULT 0;
ALTER TABLE products ADD COLUMN IF NOT EXISTS width_cm numeric(8,2) NOT NULL DEFAULT 0;
ALTER TABLE products ADD COLUMN IF NOT EXISTS height_cm numeric(8,2) NOT NULL DEFAULT 0;
ALTER TABLE products ADD COLUMN IF NOT EXISTS length_cm numeric(8,2) NOT NULL DEFAULT 0;
ALTER TABLE products ADD COLUMN IF NOT EXISTS requires_shipping boolean NOT NULL DEFAULT true;
-- Produtos existentes sem logística permanecem visíveis, mas não podem ser cotados/comprados até serem preenchidos.
ALTER TABLE products ALTER COLUMN weight_grams DROP NOT NULL;
ALTER TABLE products ALTER COLUMN width_cm DROP NOT NULL;
ALTER TABLE products ALTER COLUMN height_cm DROP NOT NULL;
ALTER TABLE products ALTER COLUMN length_cm DROP NOT NULL;
ALTER TABLE orders ADD COLUMN IF NOT EXISTS subtotal_cents bigint NOT NULL DEFAULT 0;
ALTER TABLE orders ADD COLUMN IF NOT EXISTS customer_name varchar(120) NULL;
ALTER TABLE orders ADD COLUMN IF NOT EXISTS customer_email varchar(254) NULL;
ALTER TABLE orders ADD COLUMN IF NOT EXISTS shipping_price_cents bigint NOT NULL DEFAULT 0;
ALTER TABLE orders ADD COLUMN IF NOT EXISTS shipping_quote_id uuid NULL;
ALTER TABLE orders ADD COLUMN IF NOT EXISTS shipping_carrier varchar(120) NULL;
ALTER TABLE orders ADD COLUMN IF NOT EXISTS shipping_service varchar(120) NULL;
ALTER TABLE orders ADD COLUMN IF NOT EXISTS shipping_delivery_days integer NULL;
ALTER TABLE orders ADD COLUMN IF NOT EXISTS tracking_code varchar(120) NULL;
CREATE TABLE IF NOT EXISTS shipping_quotes (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), user_id uuid NOT NULL REFERENCES users(id), origin_cep varchar(8) NOT NULL, destination_cep varchar(8) NOT NULL, request_hash varchar(64) NOT NULL, items_snapshot jsonb NOT NULL, provider varchar(30) NOT NULL, carrier varchar(120) NOT NULL, service varchar(120) NOT NULL, price_cents bigint NOT NULL CHECK(price_cents >= 0), estimated_days integer NOT NULL, expires_at timestamptz NOT NULL, created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_shipping_quotes_user_id ON shipping_quotes(user_id);
CREATE INDEX IF NOT EXISTS ix_shipping_quotes_expires_at ON shipping_quotes(expires_at);
