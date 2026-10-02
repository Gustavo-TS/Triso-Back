-- Alinha bancos PostgreSQL que já possuíam pedidos/pagamentos antes do checkout.
-- É incremental e não remove tabelas, colunas ou dados.
-- Revise em homologação e faça backup antes de executar em produção.

BEGIN;

ALTER TABLE IF EXISTS public.orders
    ADD COLUMN IF NOT EXISTS customer_name varchar(120),
    ADD COLUMN IF NOT EXISTS customer_email varchar(254),
    ADD COLUMN IF NOT EXISTS subtotal_cents bigint NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS shipping_price_cents bigint NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS shipping_quote_id uuid,
    ADD COLUMN IF NOT EXISTS shipping_carrier varchar(120),
    ADD COLUMN IF NOT EXISTS shipping_service varchar(120),
    ADD COLUMN IF NOT EXISTS shipping_delivery_days integer,
    ADD COLUMN IF NOT EXISTS tracking_code varchar(120),
    ADD COLUMN IF NOT EXISTS updated_at timestamptz NOT NULL DEFAULT now();

ALTER TABLE IF EXISTS public.order_addresses
    ADD COLUMN IF NOT EXISTS complement varchar(100);

ALTER TABLE IF EXISTS public.payments
    ADD COLUMN IF NOT EXISTS checkout_url text,
    ADD COLUMN IF NOT EXISTS transaction_nsu varchar(100),
    ADD COLUMN IF NOT EXISTS invoice_slug varchar(200),
    ADD COLUMN IF NOT EXISTS receipt_url text,
    ADD COLUMN IF NOT EXISTS paid_amount_cents bigint,
    ADD COLUMN IF NOT EXISTS installments integer,
    ADD COLUMN IF NOT EXISTS capture_method varchar(20),
    ADD COLUMN IF NOT EXISTS paid_at timestamptz,
    ADD COLUMN IF NOT EXISTS updated_at timestamptz NOT NULL DEFAULT now();

CREATE INDEX IF NOT EXISTS ix_orders_user_id ON public.orders(user_id);
CREATE INDEX IF NOT EXISTS ix_orders_status ON public.orders(status);
CREATE INDEX IF NOT EXISTS ix_payments_order_id ON public.payments(order_id);
CREATE INDEX IF NOT EXISTS ix_payments_order_nsu ON public.payments(order_nsu);
CREATE UNIQUE INDEX IF NOT EXISTS ux_payments_transaction_nsu
    ON public.payments(transaction_nsu) WHERE transaction_nsu IS NOT NULL;

COMMIT;

-- Verifique a integridade de valores após aplicar:
-- SELECT id, subtotal_cents, shipping_price_cents, total_cents
-- FROM public.orders
-- WHERE subtotal_cents < 0 OR shipping_price_cents < 0
--    OR total_cents <> subtotal_cents + shipping_price_cents;
