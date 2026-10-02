-- Cria endereços salvos pelos clientes. Não altera os endereços históricos dos pedidos.
-- Execute após realizar backup do banco de homologação/produção.

CREATE TABLE IF NOT EXISTS public.user_addresses (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    label varchar(60) NOT NULL,
    recipient_name varchar(120) NOT NULL,
    postal_code varchar(8) NOT NULL,
    street varchar(160) NOT NULL,
    number varchar(30) NOT NULL,
    complement varchar(100) NULL,
    neighborhood varchar(100) NOT NULL,
    city varchar(100) NOT NULL,
    state varchar(2) NOT NULL,
    is_default boolean NOT NULL DEFAULT false,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    updated_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT chk_user_addresses_postal_code CHECK (postal_code ~ '^[0-9]{8}$'),
    CONSTRAINT chk_user_addresses_state CHECK (state ~ '^[A-Z]{2}$')
);

CREATE INDEX IF NOT EXISTS ix_user_addresses_user_id ON public.user_addresses (user_id);
CREATE UNIQUE INDEX IF NOT EXISTS ux_user_addresses_one_default_per_user
    ON public.user_addresses (user_id)
    WHERE is_default;
