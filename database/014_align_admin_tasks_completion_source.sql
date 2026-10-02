-- Padroniza o domínio de completion_source usado pela API.
-- Preserva tarefas existentes e transforma valores legados em "manual".

BEGIN;

UPDATE public.admin_tasks
SET completion_source = 'manual'
WHERE completion_source IS NULL
   OR completion_source NOT IN ('manual', 'automatic');

DO $$
DECLARE
    constraint_name text;
BEGIN
    FOR constraint_name IN
        SELECT c.conname
        FROM pg_constraint c
        WHERE c.conrelid = 'public.admin_tasks'::regclass
          AND c.contype = 'c'
          AND pg_get_constraintdef(c.oid) ILIKE '%completion_source%'
    LOOP
        EXECUTE format('ALTER TABLE public.admin_tasks DROP CONSTRAINT %I', constraint_name);
    END LOOP;

    ALTER TABLE public.admin_tasks
        ADD CONSTRAINT chk_admin_tasks_completion_source
        CHECK (completion_source IN ('manual', 'automatic'));
END $$;

COMMIT;
