# Histórico de alterações do banco

Os arquivos desta pasta são scripts PostgreSQL incrementais. Eles não são executados automaticamente pela API.

Execute primeiro em homologação, faça backup antes de produção e registre quais arquivos foram aplicados. Todos os scripts devem ser executados com a mesma `DATABASE_URL` usada pela API.

## Ordem para um banco existente

1. `001_create_checkout_schema.sql` — somente quando as tabelas de checkout ainda não existem.
2. `002_align_orders_and_payments_schema.sql` — alinha tabelas de pedidos/pagamentos já existentes ao contrato atual.
3. `003_add_shipping_and_product_logistics.sql`
4. `004_align_shipping_quotes_schema.sql`
5. `005_add_store_shipping_settings.sql`
6. `006_add_user_addresses.sql`
7. `007_add_user_address_label.sql` — somente se `user_addresses` foi criada por uma versão anterior sem `label`.
8. `008_add_payment_webhook_events.sql`
9. `009_add_admin_orders_indexes.sql`
10. `010_add_admin_tasks.sql`
11. `011_align_admin_tasks_completion.sql` — complementa as tarefas com estado, origem da conclusão e último usuário responsável, sem apagar dados.
12. `012_align_admin_tasks_task_type.sql` — compatibiliza a coluna `task_type` com versões iniciais que a chamavam de `type`.
13. `013_add_general_admin_task_type.sql` — libera o tipo `3` (`General`) para tarefas administrativas.
14. `014_align_admin_tasks_completion_source.sql` — padroniza `completion_source` para `manual` ou `automatic`.
15. `015_add_admin_task_assignee.sql` — adiciona o responsável atribuído à tarefa.
16. `016_add_catalog_search_indexes.sql` — adiciona índices trigram e de catálogo publicado para busca rápida.

`900_inspect_required_order_columns.sql` é somente consulta diagnóstica e não altera dados.

## Execução no PowerShell

```powershell
psql $env:DATABASE_URL -f .\database\002_align_orders_and_payments_schema.sql
```

Não execute scripts de criação de tabela em bancos onde já há tabelas divergentes sem antes revisar os `ALTER TABLE` de alinhamento.

## Alterações registradas

- Dados logísticos de produtos: peso, largura, altura, comprimento e `requires_shipping`.
- Cotações de frete, origem configurável da loja e total de frete no pedido.
- Dados de checkout e pagamento InfinitePay.
- Endereços salvos por usuário, apelido, destinatário e endereço padrão.
- Campos e tabela de auditoria para confirmação de pagamento por webhook.
