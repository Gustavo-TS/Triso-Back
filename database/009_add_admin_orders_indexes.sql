-- Índices para consultas administrativas de pedidos e fila de expedição.
-- Incremental, sem remoção de dados.

CREATE INDEX IF NOT EXISTS ix_orders_status_created_at
    ON public.orders(status, created_at DESC);

CREATE INDEX IF NOT EXISTS ix_payments_paid_at
    ON public.payments(paid_at DESC)
    WHERE paid_at IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_order_items_product_id
    ON public.order_items(product_id);
