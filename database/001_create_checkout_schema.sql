-- Incremental PostgreSQL schema for customer checkout. Review in Neon before applying.
-- No existing table, column, or data is removed by this script.
INSERT INTO permission (permission) VALUES ('cliente') ON CONFLICT (permission) DO NOTHING;
CREATE UNIQUE INDEX IF NOT EXISTS ux_users_email ON users(email);

CREATE TABLE IF NOT EXISTS orders (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), order_number varchar(32) NOT NULL, customer_name varchar(120) NOT NULL, customer_email varchar(254) NOT NULL,
    user_id uuid NULL REFERENCES users(id) ON DELETE SET NULL, status varchar(30) NOT NULL,
    total_cents bigint NOT NULL CHECK (total_cents >= 0), created_at timestamptz NOT NULL DEFAULT NOW(), updated_at timestamptz NOT NULL DEFAULT NOW());
CREATE UNIQUE INDEX IF NOT EXISTS ux_orders_order_number ON orders(order_number);
CREATE INDEX IF NOT EXISTS ix_orders_user_id ON orders(user_id);
CREATE INDEX IF NOT EXISTS ix_orders_status ON orders(status);
CREATE TABLE IF NOT EXISTS order_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), order_id uuid NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    product_id uuid NOT NULL, product_name varchar(120) NOT NULL, unit_price_cents bigint NOT NULL CHECK (unit_price_cents >= 0), quantity integer NOT NULL CHECK (quantity > 0), total_cents bigint NOT NULL CHECK (total_cents >= 0));
CREATE TABLE IF NOT EXISTS order_addresses (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), order_id uuid NOT NULL UNIQUE REFERENCES orders(id) ON DELETE CASCADE,
    street varchar(160) NOT NULL, number varchar(30) NOT NULL, complement varchar(100), neighborhood varchar(100) NOT NULL, city varchar(100) NOT NULL, state varchar(40) NOT NULL, cep varchar(20) NOT NULL);
CREATE TABLE IF NOT EXISTS payments (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), order_id uuid NOT NULL UNIQUE REFERENCES orders(id) ON DELETE CASCADE,
    provider varchar(50) NOT NULL, order_nsu varchar(100) NOT NULL UNIQUE, transaction_nsu varchar(100) NULL,
    status varchar(20) NOT NULL, capture_method varchar(20), amount_cents bigint NOT NULL CHECK (amount_cents >= 0), checkout_url varchar(2048), created_at timestamptz NOT NULL DEFAULT NOW(), updated_at timestamptz NOT NULL DEFAULT NOW());
CREATE INDEX IF NOT EXISTS ix_payments_order_id ON payments(order_id);
CREATE INDEX IF NOT EXISTS ix_payments_order_nsu ON payments(order_nsu);
CREATE UNIQUE INDEX IF NOT EXISTS ux_payments_transaction_nsu ON payments(transaction_nsu) WHERE transaction_nsu IS NOT NULL;
CREATE TABLE IF NOT EXISTS order_status_history (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), order_id uuid NOT NULL REFERENCES orders(id) ON DELETE CASCADE, new_status varchar(30) NOT NULL, note varchar(500), created_at timestamptz NOT NULL DEFAULT NOW());
CREATE INDEX IF NOT EXISTS ix_order_status_history_order_id ON order_status_history(order_id);
CREATE TABLE IF NOT EXISTS notifications (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), user_id uuid NULL, type varchar(100) NOT NULL, content text NOT NULL, created_at timestamptz NOT NULL DEFAULT NOW());
CREATE TABLE IF NOT EXISTS outbox_messages (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), type varchar(100) NOT NULL, destination varchar(254) NOT NULL, payload jsonb NOT NULL, status varchar(20) NOT NULL, attempts integer NOT NULL DEFAULT 0, next_attempt_at timestamptz NOT NULL DEFAULT NOW(), created_at timestamptz NOT NULL DEFAULT NOW(), processed_at timestamptz NULL, last_error text NULL);
CREATE INDEX IF NOT EXISTS ix_outbox_messages_status_next_attempt_at ON outbox_messages(status, next_attempt_at);
