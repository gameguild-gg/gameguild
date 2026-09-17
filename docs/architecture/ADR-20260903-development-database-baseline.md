# ADR: Baseline global do banco durante o pré-lançamento

- Status: Aceito
- Data: 2026-09-03
- Escopo: `ApplicationDbContext`

## Contexto

A plataforma ainda não foi lançada, mas os ambientes compartilhados e bancos
locais já podem conter dados de desenvolvimento que representam trabalho
autoral, submissões e histórico acadêmico. Esses dados não podem ser tratados
como descartáveis por uma migration ordinária. Ao mesmo tempo, migrations
atuais podem instalar SQL que não aparece no `IModel`.

## Decisão

Mudanças estruturais aprovadas por `SCHEMA-GATE` entram como migrations
incrementais forward-only. Toda alteração de tipo, nulabilidade, semântica ou
nome precisa declarar conversão, backfill, compatibilidade e teste tanto em
banco vazio quanto em banco populado pela migration anterior. Uma migration
ordinária nunca descarta dados existentes apenas porque o produto ainda não foi
lançado.

Um squash para novo baseline global é uma operação separada e coordenada. Ele
somente pode ocorrer após inventário completo, backup verificável e confirmação
de que nenhum ambiente ativo depende da cadeia anterior. O baseline não pode
ser usado para esconder uma migração destrutiva dentro de um merge comum.

Antes do reset, o gate inventaria migrations, modelo EF e todo SQL ativo fora do
`IModel`: extensões, schemas, roles, grants, policies, funções, procedures,
triggers, views, índices especiais e dados estruturais. Cada artefato recebe
owner, dependências, ordem de instalação, decisão explícita e teste funcional.

O delta de grading é apresentado separadamente do impacto operacional global.
Somente aprovação explícita das tabelas, colunas, constraints, índices,
transações e diff do modelo libera a edição de entidades EF e do baseline.

O `Down` deve restaurar tipos e estruturas quando isso for seguro. Quando a
reversão implicar perda de dados, deve falhar explicitamente e exigir um plano
operacional de restore; nunca deve truncar ou sobrescrever silenciosamente.

## Consequências

- `Database.MigrateAsync()` continua aplicável a bancos vazios e existentes;
- alterações com dados existentes exigem testes reais de upgrade;
- nenhuma estrutura de outro módulo pode mudar sem aparecer no diff aprovado;
- SQL manual ativo não pode desaparecer por omissão do snapshot EF;
- a autorização para implementar um plano não substitui o `SCHEMA-GATE` com o
  inventário concreto.

## Alternativas rejeitadas

- descartar bancos compartilhados como parte de uma alteração ordinária;
- alterar tipo diretamente sem `USING`, escala ou backfill explícitos;
- renomear colunas quando o conceito novo possui semântica diferente;
- usar `EnsureCreated` sem inventário dos artefatos SQL;
- remover SQL manual apenas por não fazer parte do `IModel`.
