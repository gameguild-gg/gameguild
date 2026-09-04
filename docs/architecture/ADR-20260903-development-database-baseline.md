# ADR: Baseline global do banco durante o pré-lançamento

- Status: Aceito
- Data: 2026-09-03
- Escopo: `ApplicationDbContext`

## Contexto

A plataforma ainda não foi lançada e não possui dados de produção a migrar.
Acrescentar migrations incrementais para modelos descartados criaria dívida
técnica sem preservar informação relevante. Ao mesmo tempo, migrations atuais
podem instalar SQL que não aparece no `IModel`.

## Decisão

Mudanças estruturais aprovadas por `SCHEMA-GATE` entram diretamente em um novo
baseline global de criação. A cadeia histórica de desenvolvimento é substituída
por um único baseline final; bancos locais, de desenvolvimento e de teste são
descartados e recriados. Não existem migration incremental, migration de dados,
backfill, dual-read ou compatibilidade legacy.

Antes do reset, o gate inventaria migrations, modelo EF e todo SQL ativo fora do
`IModel`: extensões, schemas, roles, grants, policies, funções, procedures,
triggers, views, índices especiais e dados estruturais. Cada artefato recebe
owner, dependências, ordem de instalação, decisão explícita e teste funcional.

O delta de grading é apresentado separadamente do impacto operacional global.
Somente aprovação explícita das tabelas, colunas, constraints, índices,
transações e diff do modelo libera a edição de entidades EF e do baseline.

Rollback significa reverter o baseline por Git e recriar bancos descartáveis;
não significa converter dados para um modelo anterior.

## Consequências

- `Database.MigrateAsync()` continua aplicável a um banco vazio por meio de um
  único baseline;
- nenhuma estrutura de outro módulo pode mudar sem aparecer no diff aprovado;
- SQL manual ativo não pode desaparecer por omissão do snapshot EF;
- a autorização para implementar um plano não substitui o `SCHEMA-GATE` com o
  inventário concreto.

## Alternativas rejeitadas

- acrescentar migration ao fim da cadeia atual;
- preservar bancos de desenvolvimento;
- usar `EnsureCreated` sem inventário dos artefatos SQL;
- remover SQL manual apenas por não fazer parte do `IModel`.
