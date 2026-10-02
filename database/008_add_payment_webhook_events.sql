-- Auditoria e idempotência de notificações de pagamento. Execute após backup.
ALTER TABLE public.payments ADD COLUMN IF NOT EXISTS invoice_slug varchar(200);
ALTER TABLE public.payments ADD COLUMN IF NOT EXISTS receipt_url text;
ALTER TABLE public.payments ADD COLUMN IF NOT EXISTS paid_amount_cents bigint;
ALTER TABLE public.payments ADD COLUMN IF NOT EXISTS paid_at timestamp with time zone;

CREATE TABLE IF NOT EXISTS public.payment_webhook_events (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), order_id uuid NULL REFERENCES public.orders(id) ON DELETE SET NULL,
    provider varchar(50) NOT NULL, order_nsu varchar(100), external_transaction_id varchar(100), external_invoice_slug varchar(200),
    status varchar(30) NOT NULL, failure_reason varchar(500), sanitized_payload jsonb NOT NULL,
    received_at timestamp with time zone NOT NULL DEFAULT now(), processed_at timestamp with time zone NULL
);
CREATE INDEX IF NOT EXISTS ix_payment_webhook_events_order_id ON public.payment_webhook_events(order_id);
CREATE INDEX IF NOT EXISTS ix_payment_webhook_events_status ON public.payment_webhook_events(status);
CREATE UNIQUE INDEX IF NOT EXISTS ux_payment_webhook_events_provider_transaction ON public.payment_webhook_events(provider, external_transaction_id) WHERE external_transaction_id IS NOT NULL;
