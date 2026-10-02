-- Complemento incremental para as tarefas administrativas criadas pelo script 010.
-- Preserva todas as tarefas e todos os vínculos já existentes.
-- Execute somente depois de 010_add_admin_tasks.sql.

BEGIN;

ALTER TABLE public.admin_tasks
    ADD COLUMN IF NOT EXISTS is_completed boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS completion_source varchar(16),
    ADD COLUMN IF NOT EXISTS updated_by_user_id uuid;

-- Mantém o estado histórico coerente sem alterar datas ou responsáveis existentes.
UPDATE public.admin_tasks
SET is_completed = (completed_at IS NOT NULL),
    completion_source = CASE
        WHEN completed_at IS NOT NULL AND completion_source IS NULL THEN 'manual'
        ELSE completion_source
    END
WHERE is_completed IS DISTINCT FROM (completed_at IS NOT NULL)
   OR (completed_at IS NOT NULL AND completion_source IS NULL);

UPDATE public.admin_tasks
SET completion_source = 'manual'
WHERE completion_source IS NULL;

ALTER TABLE public.admin_tasks
    ALTER COLUMN completion_source SET DEFAULT 'manual',
    ALTER COLUMN completion_source SET NOT NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_admin_tasks_updated_by_user'
          AND conrelid = 'public.admin_tasks'::regclass
    ) THEN
        ALTER TABLE public.admin_tasks
            ADD CONSTRAINT fk_admin_tasks_updated_by_user
            FOREIGN KEY (updated_by_user_id) REFERENCES public.users(id);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'chk_admin_tasks_completion_source'
          AND conrelid = 'public.admin_tasks'::regclass
    ) THEN
        ALTER TABLE public.admin_tasks
            ADD CONSTRAINT chk_admin_tasks_completion_source
            CHECK (completion_source IN ('manual', 'automatic'));
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ix_admin_tasks_due_date_is_completed
    ON public.admin_tasks (due_date, is_completed);

COMMIT;
