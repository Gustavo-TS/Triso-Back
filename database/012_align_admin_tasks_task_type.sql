-- Compatibilidade entre versões iniciais que criaram a coluna "type"
-- e o contrato atual, que utiliza "task_type".
-- Não remove dados: apenas renomeia a coluna quando necessário.

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'admin_tasks' AND column_name = 'type'
    ) AND NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'admin_tasks' AND column_name = 'task_type'
    ) THEN
        ALTER TABLE public.admin_tasks RENAME COLUMN type TO task_type;
    END IF;
END $$;
