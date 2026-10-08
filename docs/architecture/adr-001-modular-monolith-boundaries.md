# ADR-001: Manter o monólito modular com limites explícitos

## Status

Aceito.

## Contexto

A API é uma aplicação ASP.NET Core única, com projetos separados para API, aplicação, domínio e infraestrutura. O bootstrap concentrava o registro de casos de uso, autenticação, autorização, CORS, rate limiting e gateways externos em `Program.cs`.

## Decisão

Manter o monólito modular e extrair a configuração de serviços da API para extensões coesas em `Triso.Api/Configuration`. `Program.cs` passa a ser somente o ponto de composição e de ordenação do pipeline HTTP.

## Racional

- Preserva contratos HTTP, regras de negócio e dependências existentes.
- Torna as responsabilidades de plataforma localizáveis e testáveis isoladamente.
- Evita uma reescrita ampla dos controllers e do acesso a dados durante uma alteração estrutural.

## Trade-offs

- A configuração continua no projeto da API; não foi criada uma abstração adicional para cada integração.
- Controllers que usam `TrisoDbContext` diretamente permanecem assim nesta etapa para não alterar comportamento.

## Consequências

- Positivas: bootstrap curto e configuração agrupada por responsabilidade.
- Negativas: a separação controller/aplicação ainda é híbrida.
- Mitigação: mover cada feature para casos de uso e portas próprias gradualmente, com testes de integração antes de cada migração.
