-- Use somente se a tabela user_addresses já foi criada pela migration anterior.
-- Para tabelas com dados, use um valor temporário e depois solicite que cada cliente ajuste o apelido.

ALTER TABLE public.user_addresses
    ADD COLUMN IF NOT EXISTS label varchar(60);

UPDATE public.user_addresses
SET label = 'Endereço salvo'
WHERE label IS NULL;

ALTER TABLE public.user_addresses
    ALTER COLUMN label SET NOT NULL;
