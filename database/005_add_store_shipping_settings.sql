-- Configuração única da origem de expedição da Triso.
-- Execute junto às migrações de shipping antes de habilitar cotações em produção.
CREATE TABLE IF NOT EXISTS store_shipping_settings (
    id smallint PRIMARY KEY DEFAULT 1 CHECK (id = 1),
    origin_postal_code varchar(8) NOT NULL,
    origin_street varchar(160) NULL,
    origin_number varchar(30) NULL,
    origin_complement varchar(100) NULL,
    origin_neighborhood varchar(100) NULL,
    origin_city varchar(100) NULL,
    origin_state varchar(2) NULL,
    updated_at timestamptz NOT NULL DEFAULT now(),
    updated_by_user_id uuid NULL REFERENCES users(id)
);

-- Configure o CEP real da origem antes da primeira cotação.
-- Exemplo:
-- INSERT INTO store_shipping_settings (id, origin_postal_code, origin_street, origin_number, origin_neighborhood, origin_city, origin_state)
-- VALUES (1, '01001000', 'Praça da Sé', '1', 'Sé', 'São Paulo', 'SP')
-- ON CONFLICT (id) DO UPDATE SET origin_postal_code = EXCLUDED.origin_postal_code,
--   origin_street = EXCLUDED.origin_street, origin_number = EXCLUDED.origin_number,
--   origin_neighborhood = EXCLUDED.origin_neighborhood, origin_city = EXCLUDED.origin_city,
--   origin_state = EXCLUDED.origin_state, updated_at = now();
