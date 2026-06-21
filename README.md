# KRT Onboarding — API de Contas

API de gerenciamento de contas de clientes (time de Onboarding do banco KRT). Teste técnico
de backend em **.NET 8**, com foco em boas práticas: Clean Architecture, DDD, SOLID, MVC e
testes. Os dois desafios arquiteturais do enunciado — **notificar várias áreas** sobre
mudanças e **reduzir o custo de consultas repetidas** — são resolvidos com
arquitetura orientada a eventos e cache.

> Status: em construção. Veja o `PLANEJAMENTO.md` para o roadmap completo.

## Arquitetura

Clean Architecture em 4 camadas (dependências apontam para dentro):

```
src/
  KRT.Onboarding.Domain          # entidades, value objects, domain events, interfaces
  KRT.Onboarding.Application     # casos de uso, DTOs, validação, interfaces de infra
  KRT.Onboarding.Infrastructure  # EF Core, repositórios, cache, publisher de eventos
  KRT.Onboarding.Api             # Controllers (MVC), DI, middlewares, Swagger
tests/
  KRT.Onboarding.Domain.Tests
  KRT.Onboarding.Application.Tests
  KRT.Onboarding.IntegrationTests
```

## Como rodar (será detalhado)

```bash
# Sobe dependências (Postgres, Redis, LocalStack)
docker compose up -d

# Roda a API
dotnet run --project src/KRT.Onboarding.Api
```

A versão do SDK está fixada em **8.0.x** via `global.json`.

## As duas respostas-chave do desafio

- **Notificar as áreas (fraude, cartões, …):** arquitetura orientada a eventos — domain
  events → outbox transacional → publicação no Amazon EventBridge (ou SNS→SQS). Cada área
  consome de forma desacoplada.
- **Custo de consultas repetidas:** cache-aside com Redis/ElastiCache e TTL diário, com
  invalidação no update/delete. Em DynamoDB, a alternativa nativa seria o DAX.

Detalhamento completo no `PLANEJAMENTO.md`.
