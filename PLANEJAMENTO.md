# Planejamento — Teste Técnico Backend (Banco KRT / Onboarding)

> **Contexto:** API de gerenciamento de contas de clientes em .NET 8, com notificação
> de eventos para múltiplas áreas e otimização de custo de consultas na AWS.
> **Autor do plano:** time de desenvolvimento (visão sênior, C# + AWS).
> **Data:** 2026-06-21.

---

## 1. Entendimento do problema

O enunciado tem **três blocos** e a nota da banca é dada muito mais pelos blocos 2 e 3 do
que pelo CRUD em si. O CRUD é a parte fácil; o que separa um JR de um SR é **como** ele
resolve eventos e custo.

### 1.1. Bloco funcional — CRUD de Conta
Entidade **Conta** com:

| Campo          | Tipo                         | Observação                                        |
|----------------|------------------------------|---------------------------------------------------|
| `Id`           | `Guid`                       | Chave; gerado pelo domínio (não pelo banco).      |
| `NomeTitular`  | `string`                     | Obrigatório, normalizado (trim, sem nº excessivo).|
| `Cpf`          | `Value Object`               | Validado (dígitos verificadores), armazenado limpo.|
| `Status`       | `enum { Ativa, Inativa }`    | Transições controladas por regra de negócio.      |

Operações: **Create, Read (por id e listagem), Update, Delete**.
Decisão de design: **soft-delete não é exigido**, mas vou tratar "Delete" emitindo o
evento `ContaRemovida` — quem some é o registro, quem sobrevive é o fato (evento).

### 1.2. Bloco arquitetural #1 — "várias áreas precisam saber"
Prevenção a fraude, cartões, etc. precisam reagir a **ContaCriada / ContaAtualizada /
ContaRemovida**. Isso é o caso de uso clássico de **arquitetura orientada a eventos
(pub/sub, fan-out)**. A API **não pode** chamar cada área via HTTP síncrono — isso
acopla, derruba a disponibilidade e não escala. A resposta certa é **publicar eventos**
e deixar cada área consumir no seu ritmo.

### 1.3. Bloco arquitetural #2 — "a AWS cobra por consulta"
Várias áreas lendo a **mesma conta** várias vezes no mesmo dia. A frase "a AWS cobra por
consulta ao banco" é a pista: o custo é por leitura (perfil de **DynamoDB on-demand** /
**Aurora Serverless** / qualquer banco gerenciado com cobrança por I/O). A resposta certa
é **cache** (padrão *cache-aside*) com **TTL diário** e **invalidação no write**.

---

## 2. Decisões de arquitetura (e o porquê)

> Estas são as escolhas que eu defenderia numa sabatina técnica. Cada uma vem com a
> justificativa e a alternativa considerada — é isso que a banca quer ver.

### 2.1. Clean Architecture + DDD (4 camadas)
```
KRT.Onboarding.Domain          -> entidades, value objects, domain events, interfaces (repos)
KRT.Onboarding.Application     -> casos de uso, DTOs, validação, orquestração, interfaces de infra
KRT.Onboarding.Infrastructure  -> EF Core, repositórios, cache, publisher de eventos (AWS)
KRT.Onboarding.Api             -> Controllers (MVC), DI, middlewares, Swagger
```
Regra de dependência: tudo aponta **para dentro**. `Domain` não conhece ninguém;
`Api`/`Infrastructure` dependem de abstrações da `Application`/`Domain`.
**Por quê:** atende SOLID (DIP), DDD e MVC de uma vez, e deixa o domínio testável sem
banco/AWS. **Alternativa descartada:** projeto único em camadas lógicas — mais rápido,
mas não demonstra os conceitos que o teste avalia.

### 2.2. Banco de dados — **PostgreSQL** (com EF Core) como principal
- CRUD relacional simples, dev local trivial via Docker, EF Core maduro, migrations.
- **Alternativa de peso (recomendo discutir no README): DynamoDB.** A frase "cobra por
  consulta" é literalmente o modelo do DynamoDB on-demand. Se eu fosse "AWS-native até o
  osso", iria de DynamoDB + **DAX** (cache nativo, item 2.4). Mantenho a abstração de
  repositório para que trocar Postgres↔DynamoDB seja uma troca de implementação na
  `Infrastructure`, não uma reescrita.

### 2.3. Eventos — **Transactional Outbox** + **Amazon EventBridge** (ou SNS→SQS)
Fluxo:
1. Caso de uso salva a `Conta` **e** grava o evento na tabela `OutboxMessages` **na mesma
   transação** (resolve o problema de *dual-write*: nunca publica sem ter persistido, nem
   persiste sem publicar).
2. Um *background worker* (`IHostedService`) lê a outbox e publica no **EventBridge**.
3. Regras do EventBridge roteiam por `detail-type` para as filas/áreas (fraude, cartões…).

**Por que EventBridge:** roteamento por regra, schema registry, múltiplos consumidores
desacoplados — perfeito para "várias áreas". **Alternativa clássica e mais barata:
SNS (tópico) → fan-out → várias filas SQS**, uma por área. Cito as duas; recomendo
EventBridge pela elegância de roteamento e SNS+SQS pela simplicidade/custo.
**Domain Events internos** (MediatR ou dispatcher próprio) disparam a escrita na outbox.

### 2.4. Cache — **cache-aside** com **Redis (ElastiCache)** e **TTL até o fim do dia**
Resposta ao bloco de custo:
- Leitura de conta: consulta o cache primeiro (`IDistributedCache`/Redis).
  - **Hit** → devolve sem tocar no banco (zero custo de consulta).
  - **Miss** → lê o banco, popula o cache com **TTL que expira à meia-noite** ("consultada
    naquele mesmo dia"), devolve.
- **Invalidação:** `Update` e `Delete` removem/atualizam a chave (`conta:{id}`) para nunca
  servir dado velho.
- **Em AWS:** **ElastiCache for Redis**. Se o banco fosse DynamoDB, a resposta natural
  seria **DAX** (cache write-through nativo, transparente).
**Por quê TTL diário:** o enunciado fala explicitamente em "já consultada naquele mesmo
dia" — alinhar o TTL ao requisito é o detalhe que mostra leitura atenta.

### 2.5. Rodar tudo localmente sem custo — **Docker + LocalStack**
`docker-compose` sobe: PostgreSQL + Redis + **LocalStack** (emula EventBridge/SNS/SQS/
DynamoDB). Permite desenvolver e testar a stack AWS inteira sem conta/custo, e o avaliador
roda com um `docker compose up`.

---

## 3. Stack e bibliotecas

| Necessidade           | Escolha                                    | Observação                          |
|-----------------------|--------------------------------------------|-------------------------------------|
| Runtime               | **.NET 8** (`net8.0`)                       | Instalar SDK 8 (aqui há o 9; compila, mas alinhar). |
| Web                   | ASP.NET Core MVC (Controllers) + Swagger    | `Swashbuckle`.                      |
| ORM                   | EF Core 8 + Npgsql                          | Migrations versionadas.             |
| Casos de uso          | MediatR (CQRS leve) + Domain Events         | Opcional, mas limpa os controllers. |
| Validação             | FluentValidation                            | CPF, nome, payloads.                |
| Mapeamento            | Mapeamento manual ou AutoMapper             | Manual evita "mágica".              |
| Cache                 | `IDistributedCache` + Redis (`StackExchange.Redis`) | Abstrai Redis local/ElastiCache. |
| Mensageria AWS        | `AWSSDK.EventBridge` (+ `AWSSDK.SQS`/`SNS`) | LocalStack em dev.                  |
| Logging               | Serilog (estruturado)                       | Correlation id por request.         |
| Erros                 | Middleware + `ProblemDetails` (RFC 7807)    | Respostas de erro consistentes.     |
| Testes                | xUnit + FluentAssertions + NSubstitute      | + Testcontainers p/ integração.     |
| CI                    | GitHub Actions (build + test)               | Roda em todo push/PR.               |

---

## 4. Modelagem de domínio (resumo)

- **`Conta`** (Aggregate Root): encapsula invariantes. Sem setters públicos abertos;
  mudança de estado por métodos (`Ativar()`, `Inativar()`, `AtualizarTitular()`).
  Cada mudança relevante registra um **Domain Event**.
- **`Cpf`** (Value Object): valida dígitos verificadores no construtor; rejeita inválido;
  `Equals` por valor; armazena só dígitos.
- **`StatusConta`** (enum): `Ativa`, `Inativa`. Transições válidas controladas no agregado.
- **Domain Events:** `ContaCriada`, `ContaAtualizada`, `ContaRemovida` (+ payload mínimo:
  id, cpf, status, timestamp).
- **`IContaRepository`** (interface no Domain; implementação no Infrastructure).

---

## 5. Contrato da API (esboço)

| Método | Rota                  | Descrição                  | Códigos                       |
|--------|-----------------------|----------------------------|-------------------------------|
| POST   | `/api/contas`         | Cria conta                 | 201, 400 (validação), 409 (CPF duplicado) |
| GET    | `/api/contas/{id}`    | Busca por id (usa cache)   | 200, 404                      |
| GET    | `/api/contas`         | Lista (paginado)           | 200                           |
| PUT    | `/api/contas/{id}`    | Atualiza titular/status    | 200, 400, 404                 |
| DELETE | `/api/contas/{id}`    | Remove conta               | 204, 404                      |

Cada `POST/PUT/DELETE` → persiste + grava na outbox → evento publicado. `GET /{id}`
passa pelo cache. Swagger documentando tudo.

---

## 6. Estratégia de testes

- **Domínio (prioridade máxima):** validação de CPF (válidos/ inválidos/ formatados),
  criação de conta, transições de status, registro de domain events. Puro, rápido, sem mocks.
- **Application:** handlers dos casos de uso (Create/Update/Delete/Get) com repositório,
  cache e publisher mockados — incluindo o comportamento **hit/miss do cache** e
  **invalidação no write**.
- **Integração (diferencial):** Testcontainers sobe Postgres + Redis reais; testa
  repositório, migrations e cache-aside de ponta a ponta.
- Meta de cobertura no que importa (domínio/aplicação), não 100% cego.

---

## 7. Roadmap de execução (incrementos commitáveis)

> Cada etapa fecha num commit/PR coeso (conventional commits). Estimativa para calibrar
> esforço, não prazo rígido.

| # | Etapa | Entrega | Esforço |
|---|-------|---------|---------|
| 0 | **Setup** | Repo git, `.gitignore`, solution + 4 projetos + 1 de testes, README inicial, SDK 8. | 0,5h |
| 1 | **Domínio** | `Conta`, `Cpf` (VO), `StatusConta`, domain events, `IContaRepository` + testes de domínio. | 2h |
| 2 | **Application** | Casos de uso CRUD (MediatR), DTOs, FluentValidation, interfaces de cache/publisher + testes. | 2h |
| 3 | **Infra/persistência** | EF Core + Npgsql, mapeamentos, migrations, repositório, `docker-compose` (Postgres). | 2h |
| 4 | **API/MVC** | Controllers, DI, Swagger, middleware de erros (`ProblemDetails`), Serilog, health checks. | 2h |
| 5 | **Cache (bloco custo)** | Redis no compose, `IDistributedCache`, cache-aside no `GET`, TTL fim-do-dia, invalidação. | 2h |
| 6 | **Eventos (bloco áreas)** | Outbox + worker, publisher EventBridge (LocalStack), regras/filas demo (fraude, cartões). | 3h |
| 7 | **Testes de integração** | Testcontainers (Postgres + Redis), cenários fim-a-fim. | 2h |
| 8 | **CI + docs** | GitHub Actions (build+test), README final (arquitetura, diagramas, como rodar, decisões). | 1,5h |

---

## 8. Entregáveis (o que mostrar para a banca)

1. **Repositório git público** com histórico de commits limpo e semântico.
2. **README** com: visão de arquitetura (diagrama), como rodar (`docker compose up` +
   `dotnet run`), endpoints, e — crucial — **uma seção explicando as respostas dos dois
   blocos arquiteturais** (eventos e custo de cache). É aí que o teste é ganho.
3. **Coleção/Swagger** para testar os endpoints.
4. **Testes** passando no CI (badge no README).

---

## 9. Respostas diretas às duas perguntas do enunciado (rascunho do README)

**"Como notificar as áreas que uma conta foi criada/atualizada/deletada?"**
> Arquitetura orientada a eventos. O domínio emite eventos (`ContaCriada` etc.); a
> aplicação grava o evento numa **outbox transacional** junto com a escrita no banco; um
> worker publica no **Amazon EventBridge** (ou SNS→fan-out→SQS). Cada área é um consumidor
> independente, desacoplado e resiliente — a API não conhece nem espera nenhuma delas.

**"Como evitar custo de consultas repetidas à mesma conta no mesmo dia?"**
> Cache padrão **cache-aside** com **Redis (ElastiCache)**: leituras vão ao cache primeiro;
> só há consulta ao banco no *miss*. O item é cacheado com **TTL que expira no fim do dia**,
> cobrindo exatamente o "já consultada naquele mesmo dia". `Update`/`Delete` invalidam a
> chave para não servir dado velho. Em base DynamoDB, a alternativa nativa seria **DAX**.

---

## 10. Riscos / pontos de atenção

- **SDK 8 vs 9:** o teste pede .NET 8 — fixar `net8.0` e idealmente instalar o SDK 8
  (`global.json` travando a versão evita surpresa no avaliador).
- **Outbox + worker** é o item mais "caro"; se o tempo apertar, entrego o publisher
  direto (com nota no README de que o outbox seria a evolução para garantir consistência).
- **LocalStack:** documentar bem o passo a passo, senão o avaliador trava ao rodar a
  parte AWS. Sempre ter um modo "sem AWS" (eventos logados) como fallback.
- **CPF:** não inventar; usar algoritmo de dígito verificador testado. Não logar CPF em
  claro (LGPD) — mascarar em logs.

---

## 11. Próximos passos imediatos

1. Validar/instalar **.NET SDK 8** e criar `global.json`.
2. Rodar a etapa 0 (estrutura da solution + git + README).
3. Seguir o roadmap da seção 7, commitando a cada etapa.

> Quando quiser, eu já gero a estrutura da solution (etapa 0) e começo o domínio (etapa 1).
