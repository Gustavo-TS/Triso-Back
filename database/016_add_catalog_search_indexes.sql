-- Índices para busca parcial do catálogo. Requer PostgreSQL com extensão pg_trgm.
CREATE EXTENSION IF NOT EXISTS pg_trgm;

CREATE INDEX IF NOT EXISTS ix_products_name_trgm
    ON public.products USING gin (name gin_trgm_ops)
    WHERE deleted_at IS NULL;

CREATE INDEX IF NOT EXISTS ix_products_description_trgm
    ON public.products USING gin (description gin_trgm_ops)
    WHERE deleted_at IS NULL AND description IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_products_catalog_published_created_at
    ON public.products (created_at DESC)
    WHERE deleted_at IS NULL AND status = 'published';
