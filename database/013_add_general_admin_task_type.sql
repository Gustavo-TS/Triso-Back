-- Libera o tipo 3 = General em tarefas administrativas existentes.
-- A constraint inicial pode ter nomes distintos conforme a versão do script 010;
-- por isso ela é localizada pela definição, não por um nome fixo.

DO $$
DECLARE
    constraint_name text;
BEGIN
    -- Nome usado pela primeira versão do schema já publicada.
    ALTER TABLE public.admin_tasks DROP CONSTRAINT IF EXISTS ck_admin_tasks_type;

    FOR constraint_name IN
        SELECT c.conname
        FROM pg_constraint c
        WHERE c.conrelid = 'public.admin_tasks'::regclass
          AND c.contype = 'c'
          AND pg_get_constraintdef(c.oid) ILIKE '%task_type%'
    LOOP
        EXECUTE format('ALTER TABLE public.admin_tasks DROP CONSTRAINT %I', constraint_name);
    END LOOP;

    ALTER TABLE public.admin_tasks
        ADD CONSTRAINT chk_admin_tasks_task_type
        CHECK (task_type BETWEEN 0 AND 3);
END $$;
