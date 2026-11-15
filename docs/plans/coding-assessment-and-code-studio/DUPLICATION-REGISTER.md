# Registro de duplicacoes entre coding-surface e Emception UI

Status: inventario inicial; documento vivo.

Data: 2026-11-06.

Commit-base do inventario: `255108e0003acc55b67dbf605f57e86f9a36418d`.

## Objetivo

Registrar toda sobreposicao conhecida ou criada entre:

- `@game-guild/emception-ui`, que permanece preservado;
- o futuro `@game-guild/coding`;
- o futuro `@game-guild/grading-adapter-code`;
- o futuro `@game-guild/coding-surface`;
- composicoes existentes em `apps/web`;
- contratos equivalentes no backend.

Duplicacao e aceita neste programa para evitar alterar a superficie mantida pelo
time do Emception. Este documento nao e uma lista automatica de refactors. Ele
fornece evidencia para uma revisao posterior com o responsavel pelo Emception,
iniciada no subplano 5.0 e retomada quando uma linguagem criar nova copia. O
responsavel decidira o que permanece separado e o que pode convergir.

## Regra de preservacao

Durante as partes 1 a 4:

- nao editar, mover, renomear ou remover arquivos sob
  `packages/infrastructure/ui-emception`;
- nao transformar `@game-guild/emception-ui` em dependencia de
  `coding-surface` ou vice-versa;
- nao extrair helper compartilhado apenas para reduzir linhas duplicadas;
- copiar somente o que for necessario ao grading;
- registrar cada copia no mesmo commit que a introduzir;
- corrigir e testar divergencias no novo package sem sincronizacao silenciosa;
- levar falhas de seguranca que tambem afetem a origem ao responsavel antes de
  editar o package preservado.

## Estados

| Estado | Significado |
| --- | --- |
| `existing` | duplicacao ja presente no baseline |
| `planned` | candidato conhecido; target ainda nao existe |
| `copied` | copia criada e ligada a testes |
| `diverged` | implementacoes possuem comportamento intencionalmente diferente |
| `reviewed-keep` | owners decidiram manter separado |
| `reviewed-converge` | owners aprovaram um plano futuro de convergencia |
| `removed` | o target duplicado deixou de existir por decisao aprovada; nunca significa remover a origem preservada |

O estado `removed` exige referencia para a decisao e para a validacao. Ele nao
pode ser usado apenas porque uma busca deixou de encontrar o arquivo. Uma
decisao de convergencia tambem nao autoriza mover, reduzir ou remover o
Emception, `@game-guild/emception-ui` ou suas APIs existentes.

## Duplicacao existente

| ID | Estado | Capacidade | Locais | Diferenca conhecida | Tratamento neste programa |
| --- | --- | --- | --- | --- | --- |
| `DUP-EX-001` | `existing` | wire contract de coding assignment | `packages/infrastructure/ui-emception/src/assessment/types.ts`; `apps/web/src/lib/coding-assignment/types.ts` | a app possui guards e formas obrigatorias mais estritas; a UI possui tipos permissivos para composicao | preservar ambos; usar como evidencia para o contrato novo |
| `DUP-EX-002` | `existing` | contrato TypeScript/C# de coding | os dois arquivos anteriores; DTOs sob `GameGuild.Learning.Courses/Models/CodingAssignmentContent`; `CodeAssessmentTypeAdapter.cs` | espelhamento entre runtimes, nao copia literal | validar por fixtures; nao tentar compartilhar codigo entre linguagens |
| `DUP-EX-003` | `existing` | execucao de testes de coding | `ui-emception/src/assessment/plan.ts`; `ui-emception/src/assessment/session.ts`; worker backend de code | preview browser e grading servidor possuem autoridades distintas | manter separados e provar paridade de contrato, nao de implementacao |

## Copias previstas para a nova implementacao

Os targets sao indicativos ate `CODE-UI-01`. A implementacao deve substituir o
target pelo path real, registrar o commit exato de origem e descrever o delta.

| ID | Estado | Origem preservada | Target previsto | Parte copiada | Divergencia esperada |
| --- | --- | --- | --- | --- | --- |
| `DUP-PL-001` | `planned` | `ui-emception/src/assessment/types.ts`; `apps/web/src/lib/coding-assignment/types.ts` | `packages/features/coding/src/**` | conceitos de definition, files, tests e guards | novo contrato versionado, inteiro e orientado ao grading |
| `DUP-PL-002` | `planned` | `ui-emception/src/assessment/paths.ts` | `packages/features/coding-surface/src/**` ou `coding/src/**` | normalizacao `/user` para `/home/user` | ownership sera decidido pela dependencia mais baixa que realmente precisar dela |
| `DUP-PL-003` | `planned` | `ui-emception/src/assessment/plan.ts` | `packages/features/coding-surface/src/**` | montagem de `TestPlan`, overlays e escopo public/full | usar contratos novos e manter preview sem autoridade academica |
| `DUP-PL-004` | `planned` | `ui-emception/src/assessment/session.ts` | `packages/features/coding-surface/src/**` | controller subset, run e submission delta | modos e callbacks especificos do workflow de grading |
| `DUP-PL-005` | `planned` | `ui-emception/src/assessment/presets.ts` | `packages/features/coding-surface/src/**` | workspace config por linguagem | catalogo limitado as capabilities aprovadas para grading |
| `DUP-PL-006` | `planned` | `ui-emception/src/assessment/samples.ts` | `packages/features/coding-surface/src/testing/**` ou fixtures de app | samples de arquivos e toolchains | fixtures orientadas a testes, sem se tornarem contrato de dominio |
| `DUP-PL-007` | `planned` | `ui-emception/src/assessment/workspace-storage.ts` | `packages/features/coding-surface/src/**` | chave e restauracao de workspace | namespace deve incluir tenant, actor, revisao e attempt quando aplicavel |
| `DUP-PL-008` | `planned` | `ui-emception/src/components/CodingAssessmentEditor.tsx` | `packages/features/coding-surface/src/**` | composicao de `Ide`, extensoes, read-only e run controls | UI voltada aos modos author-test, learner e grader de grading |
| `DUP-PL-009` | `planned` | `ui-emception/src/components/TestResultsPanel.tsx` | `packages/features/coding-surface/src/**` | apresentacao de report e score | estados academicos, release e erro tecnico permanecem distintos |
| `DUP-PL-010` | `planned` | `ui-emception/src/styles/theme.css` e `css.d.ts` | `packages/features/coding-surface/src/styles/**` | estilos necessarios a composicao | identidade visual pode divergir sem alterar a origem |
| `DUP-PL-011` | `planned` | `ui-emception/src/index.ts` e `assessment/editor.ts` | exports de `coding-surface` | organizacao da API publica | nova API nao preserva aliases por compatibilidade |
| `DUP-PL-012` | `planned` | testes sob `ui-emception/src/assessment` e `src/components` | testes dos novos packages | cenarios de privacy, overlays, session, editor e results | testes originais permanecem; copias passam a validar a nova semantica |
| `DUP-PL-013` | `planned` | mocks sob `ui-emception/src/__mocks__` | mocks de `coding-surface` | fronteiras publicas do Emception | manter apenas mocks necessarios, sem copiar internals |

## Composicoes web afetadas

Estes arquivos sao consumidores, nao fontes a serem movidas automaticamente.
Quando um deles migrar para `coding-surface`, registrar no PR quais cenarios de
teste foram copiados ou adaptados:

| Area | Consumidores conhecidos |
| --- | --- |
| learner | `apps/web/src/components/learning/coding-activity-client.tsx`; rota `learn/.../coding-activity` |
| authoring | `coding-definition-editor.tsx`; `coding-assignment-preview.tsx` |
| instructor review | `assessment-grading/assessment-grader.tsx` |
| SpeedGrader | `speedgrader/.../code-grader-panel.tsx` |
| workspace restore | `components/learning/resolve-seed.ts` |
| bundling | aliases de `@game-guild/emception-ui` em `apps/web/next.config.ts` |

Os aliases existentes nao serao removidos por este programa. O novo package
recebe configuracao propria somente se o bundler realmente exigir.

## Template para novas entradas

Cada copia implementada deve adicionar uma linha com:

| Campo | Conteudo obrigatorio |
| --- | --- |
| ID | `DUP-NEW-NNN` estavel |
| origem | path, simbolos e commit copiado |
| destino | path e exports novos |
| motivo | capability concreta de grading |
| delta inicial | mudancas feitas durante a copia |
| divergencia futura | se deve ou nao acompanhar a origem |
| testes | suites que cobrem origem e destino |
| owner | responsavel pela nova copia |
| decisao | link para revisao posterior, quando existir |

## Gate de revisao futura

A revisao com o responsavel pelo Emception deve decidir item por item:

1. manter as implementacoes independentes;
2. mover uma capability neutra para uma API publica do Emception;
3. manter a capability apenas em `emception-ui`;
4. manter a capability apenas em `coding-surface`;
5. aprovar um terceiro package neutro, somente se houver ownership claro.

Nenhuma dessas opcoes e presumida agora. Ate a revisao, duplicacao registrada e
preferivel a acoplamento prematuro ou alteracao de uma superficie mantida por
outro desenvolvedor.

## Criterios de completude

- toda copia criada possui uma entrada com origem e target reais;
- cada entrada informa divergencia e testes;
- `packages/infrastructure/ui-emception/**` permanece sem diff;
- suites da origem e do destino podem executar independentemente;
- nenhum helper compartilhado foi criado sem decisao dos owners;
- o documento esta pronto para revisao item a item com o responsavel pelo
  Emception.
