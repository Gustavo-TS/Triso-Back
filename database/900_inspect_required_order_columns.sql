-- Read-only diagnostic: identifies columns that an INSERT must provide.
-- Run this in the homologation database before changing any further schema.
SELECT
    table_name,
    column_name,
    data_type,
    column_default
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name IN ('orders', 'order_items', 'order_addresses', 'payments', 'order_status_history')
  AND is_nullable = 'NO'
  AND column_default IS NULL
ORDER BY table_name, ordinal_position;
