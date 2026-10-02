-- Responsável atual da tarefa administrativa.
-- A coluna é opcional para preservar dados existentes; novas tarefas recebem o criador como padrão pela API.

ALTER TABLE public.admin_tasks
    ADD COLUMN IF NOT EXISTS assigned_to_user_id uuid NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_admin_tasks_assigned_to_user'
          AND conrelid = 'public.admin_tasks'::regclass
    ) THEN
        ALTER TABLE public.admin_tasks
            ADD CONSTRAINT fk_admin_tasks_assigned_to_user
            FOREIGN KEY (assigned_to_user_id) REFERENCES public.users(id)
            ON DELETE SET NULL;
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ix_admin_tasks_assigned_to_user_id
    ON public.admin_tasks (assigned_to_user_id);
