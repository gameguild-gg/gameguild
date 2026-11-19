# Parte 3.5. Consolidacao do contrato canonico de coding

Status: planejado.

ID: `CODE-CONTRACT-CANONICALIZATION`.

## Objetivo

Remover a compatibilidade V1/V2 e as versoes historicas artificiais criadas ao
longo das partes 01, 02 e 03. Coding ainda nao foi lancado e nao possui dados,
revisoes oficiais ou submissions que precisem ser migrados. Portanto, deve
existir somente um contrato canonico, estrito e atual para autoria, delivery,
submission, execucao e grading de code.

Este subplano e corretivo e deve ser concluido antes do gate browser final da
parte 3.4. Ele nao cria uma nova geracao do contrato: consolida o formato atual
como a primeira versao efetiva do sistema.

## Motivacao

A parte 3 adicionou `exerciseType` por meio de contratos V2 enquanto preservou
os contratos V1 criados nas partes anteriores. O backend tambem acumulou
adapters e toolchains anteriores para snapshots que nao existem. Isso produz:

- parsers, tipos, fixtures e projections duplicados;
- branches por `schemaVersion` no runtime;
- fallbacks para payloads sem `exerciseType`;
- registros de adapters antigos sem dado real que os exija;
- risco de divergencia entre as versoes TypeScript, backend e web;
- custo de teste para compatibilidade que nao pertence ao produto.

Como nao ha legado real, a correcao e substituir esses caminhos por um unico
contrato, sem alias depreciado e sem migracao.

## Decisao canonica

### Contratos de coding

- autoria possui um unico `CodingAuthoringDefinition`;
- delivery possui um unico `CodingLearnerDelivery`;
- submission possui um unico `CodingSubmission`;
- resultado possui um unico `CodingExecutionReport`;
- tipos auxiliares de propriedade de coding nao usam sufixos `V1` ou `V2`;
- `exerciseType: function | standard-io` e obrigatorio na autoria e delivery;
- o parser aceita somente o formato canonico e falha fechado para qualquer
  variante incompleta ou desconhecida;
- nao existem aliases, overloads ou exports depreciados para os nomes antigos.

O campo `schemaVersion: 1` permanece como discriminante estrito do formato
canonico. Ele nao autoriza um parser alternativo nem representa compatibilidade
com um formato anterior. Uma futura versao so podera existir quando houver uma
mudanca real e dados publicados que justifiquem a evolucao.

### Adapter de grading de code

- existe somente um adapter de code registrado;
- sua primeira identidade efetiva e `version: "1"`;
- o handler automatizado usa a mesma versao corrente;
- o payload schema canonico e unico e nao contem sufixo de compatibilidade;
- somente a identidade de toolchain corrente e aceita;
- factories, registros e branches para adapters 1/2/3 historicos sao removidos;
- fixtures locais antigas podem ser descartadas; nao recebem migracao.

A identidade versionada exigida pelo protocolo generico de grading permanece.
Ela identifica snapshots oficiais futuros e nao significa manter varias
implementacoes de code agora.

### Limites de ownership

Os seguintes nomes continuam versionados porque pertencem a
`@game-guild/grading`, nao ao dominio de code:

- `AssessmentItemProjectionV1`;
- `AssessmentResponseEnvelopeV1`;
- `AssessmentExecutionDeliveryV1`;
- `AssessmentSubmissionRuntimeViewV1`;
- `GradeResultV1` e demais contratos genericos equivalentes.

Este subplano nao renomeia, duplica ou altera esses contratos.

## Fora do escopo

- migrar banco ou dados locais;
- criar tabela, coluna ou migration;
- manter abertura de snapshots locais gerados durante desenvolvimento;
- alterar contratos genericos de grading;
- modificar fontes, defaults ou packages do Emception;
- modificar ou remover `@game-guild/emception-ui`;
- adicionar tipo de exercicio ou linguagem;
- alterar politica de review.

## Sequencia obrigatoria

### `CANON-01`. Inventariar e congelar o escopo

- localizar tipos, parsers, fixtures, branches e docs V1/V2 pertencentes a
  coding;
- separar referencias genericas de grading que devem permanecer;
- localizar todas as versoes registradas do adapter backend e TypeScript;
- localizar payload schemas divergentes entre backend, web e package;
- registrar no estado qualquer referencia inesperada antes de editar.

Gate:

- inventario cobre `coding`, `grading-adapter-code`, `coding-surface`, backend
  CodeAdapter, web e testes;
- nenhuma referencia generica de grading foi classificada como legado de code.

### `CANON-02`. Consolidar `@game-guild/coding`

- substituir os contratos V1/V2 pelo contrato canonico completo;
- manter `schemaVersion: 1` e tornar `exerciseType` obrigatorio;
- manter um parser de autoria e um parser de delivery;
- manter uma projection learner sem branch por versao;
- remover unions de versoes e fallbacks;
- renomear tipos auxiliares para nomes sem sufixo;
- consolidar e renomear fixture JSON e exports de testing;
- testar rejeicao de payload sem `exerciseType`, com versao desconhecida ou
  campos desconhecidos.

Gate:

```bash
pnpm --filter @game-guild/coding test
pnpm --filter @game-guild/coding typecheck
```

### `CANON-03`. Consolidar `@game-guild/grading-adapter-code`

- consumir somente o contrato canonico de coding;
- remover branches por `definition.schemaVersion`;
- remover sufixos de versao dos tipos que pertencem ao adapter de code;
- preservar os tipos versionados importados do grading generico;
- alinhar key, versao e payload schema com o backend;
- substituir fixtures versionadas por uma fixture canonica;
- manter score allocation, privacy e mapeamento de evidencia inalterados.

Gate:

```bash
pnpm --filter @game-guild/grading-adapter-code test
pnpm --filter @game-guild/grading-adapter-code typecheck
```

### `CANON-04`. Consolidar o backend CodeAdapter

- manter somente o adapter corrente e registra-lo uma vez;
- remover `LegacyVersion`, `Version2`, factories antigas e loops de registro;
- reiniciar a identidade efetiva do adapter e handler em `"1"`;
- manter somente a identidade atual do toolchain;
- remover resolucao de toolchains de releases anteriores;
- fazer runtime e feedback aceitarem somente a identidade corrente;
- atualizar testes para provar rejeicao de outra versao, em vez de provar
  compatibilidade historica;
- nao criar migration.

Gate:

```bash
dotnet test apps/api/tests/GameGuild.Learning.Assessments.CodeAdapter.UnitTests/GameGuild.Learning.Assessments.CodeAdapter.UnitTests.csproj --no-restore
dotnet test apps/api/tests/GameGuild.Learning.Assessments.UnitTests/GameGuild.Learning.Assessments.UnitTests.csproj --no-restore
dotnet build apps/api/GameGuild.sln --no-restore --nologo -m:2
```

### `CANON-05`. Atualizar coding-surface e web

- consumir somente tipos e parsers canonicos;
- remover fallback de `ExerciseType` no bridge;
- fazer ausencia ou valor invalido falhar antes de montar a superficie;
- persistir explicitamente o tipo escolhido em toda nova autoria;
- aceitar no runtime somente o adapter corrente;
- alinhar o payload schema de submission com o backend;
- manter auth, tenant, persistence, review e release no host;
- manter privacy de testes privados e storage namespaced.

Gate:

```bash
pnpm --filter @game-guild/coding-surface test
pnpm --filter @game-guild/coding-surface typecheck
pnpm --filter @game-guild/web exec tsc --noEmit -p tsconfig.json
```

Executar tambem os testes focados dos cinco consumidores registrados em
`CONSUMER-MAP.md`.

### `CANON-06`. Sincronizar API, client e documentacao

- confirmar que `ExerciseType` e requerido no boundary de autoria;
- capturar OpenAPI pela aplicacao publicada e regenerar o client oficial;
- nao editar manualmente arquivos gerados;
- substituir o ADR V2 pela decisao de contrato canonico;
- remover referencias V1/V2 de coding nos planos, estado e registro de
  duplicacao;
- registrar qualquer sufixo versionado remanescente e provar que pertence ao
  grading generico ou a outro protocolo fora de code.

Gate:

- OpenAPI e client concordam com o backend;
- documentacao nao recomenda compatibilidade inexistente;
- diff de migrations permanece vazio.

### `CANON-07`. Regressao consolidada

Executar, com baixo paralelismo:

```bash
pnpm --filter @game-guild/coding test
pnpm --filter @game-guild/grading-adapter-code test
pnpm --filter @game-guild/coding-surface test
pnpm --filter @game-guild/web test -- <arquivos-focados>
pnpm --filter @game-guild/emception-ui test
pnpm --filter @game-guild/web build
dotnet build apps/api/GameGuild.sln --no-restore --nologo -m:2
```

Depois, executar o browser coding cycle no lane que fornece o artifact exato.

## Buscas de auditoria

As buscas devem ser revisadas, nao aplicadas cegamente:

```bash
rg -n "Coding[A-Za-z0-9_]*V[0-9]|Code[A-Za-z0-9_]*V[0-9]" \
  packages/features/coding \
  packages/features/grading-adapter-code \
  packages/features/coding-surface \
  apps/web/src/lib/coding-assignment

rg -n "LegacyVersion|Version2|ForVersion|FROZEN_CODE_ADAPTER_VERSIONS|schemaVersion ===" \
  apps/api/Source/Modules/GameGuild.Learning.Assessments.CodeAdapter \
  packages/features/grading-adapter-code \
  packages/features/coding \
  apps/web/src/lib/coding-assignment
```

Resultados importados de `@game-guild/grading` sao permitidos. Definicoes ou
aliases pertencentes a code nao sao permitidos.

## Gate de conclusao

- existe um unico contrato de autoria e delivery de code;
- `exerciseType` e obrigatorio e nunca inferido ou preenchido por fallback;
- existe um unico parser para cada payload de code;
- nao existem unions, aliases ou fixtures de compatibilidade V1/V2 de code;
- backend e TypeScript registram exatamente um adapter corrente e concordam em
  key, version, handler e payload schema;
- runtime rejeita qualquer identidade diferente da corrente;
- somente a identidade atual do toolchain e executavel;
- testes privados continuam ausentes da delivery learner;
- suites de coding, adapter, surface, web e backend passam;
- browser cycle passa com o artifact exato;
- `@game-guild/emception-ui` e fontes do Emception permanecem sem diff;
- nenhuma tabela ou migration foi criada ou alterada;
- `IMPLEMENTATION-STATE.md` registra a evidencia e aponta a parte 4 como
  proxima acao.

