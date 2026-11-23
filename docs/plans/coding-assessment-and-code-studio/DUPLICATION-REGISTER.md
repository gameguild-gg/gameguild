# Registro de duplicacoes entre coding-surface e Emception UI

Status: baseline funcional das partes 1 a 3; refundacao 3.6 planejada; documento vivo.

Data: 2026-11-06.

Commit-base do inventario: `255108e0003acc55b67dbf605f57e86f9a36418d`.

Baseline reconfirmada para a parte 1: `4c625d55dadedb1a699ac9028c6ad0418259b6e6`.

Baseline usada pela parte 3: `41932100b2031c0ad78d7c02545a73addc6364b3`.

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

O registro cobre sobreposicao tecnica, nao preservacao da experiencia de
produto atual. A composicao exibida hoje como `Coding assignment` em Learning e
um consumidor que sera substituido nos fluxos selecionados. Seu layout de IDE
generica, sua hierarquia visual e seu workflow nao sao baseline da nova
`coding-surface`.

Essa regra tambem se aplica a primeira implementacao do proprio coding-surface.
O registro de uma copia nao obriga preservar seu target, API ou layout. A 3.6
pode substitui-los; registrar a divergencia/remocao com evidencia depois da
migracao dos consumidores, sem alterar a origem Emception preservada.

## Regra de preservacao

Durante as partes 1 a 4:

- nao editar, mover, renomear ou remover arquivos sob
  `packages/infrastructure/ui-emception`;
- nao transformar `@game-guild/emception-ui` em dependencia de
  `coding-surface` ou vice-versa;
- nao extrair helper compartilhado apenas para reduzir linhas duplicadas;
- copiar somente o que for necessario ao grading;
- copiar mecanicas de integracao, nunca presumir que o layout ou workflow atual
  de `Coding assignment` deve ser reproduzido;
- registrar cada copia no mesmo commit que a introduzir;
- corrigir e testar divergencias no novo package sem sincronizacao silenciosa;
- levar falhas de seguranca que tambem afetem a origem ao responsavel antes de
  editar o package preservado.

## Limite da duplicacao de UI

Podem ser copiados ou reimplementados, quando realmente necessarios:

- bridges sobre APIs publicas do Emception;
- montagem de workspace, overlays e planos de execucao;
- controllers de preview, run e submission delta;
- integracao com terminal, diagnostics, resultados e preview visual;
- regras de read-only, storage namespaced, mocks e fixtures;
- estilos estritamente necessarios aos componentes copiados.

Nao devem ser copiados como definicao da nova experiencia:

- o layout atual de IDE completa usado para todo `Coding assignment`;
- a composicao de autoria que mistura definicao, testes e IDE sem considerar o
  tipo do exercicio;
- a mesma arvore visual para `function`, `standard-io`, projeto, depuracao ou
  saida visual;
- navegacao, paineis, dimensoes ou controles apenas porque existem no editor
  atual;
- politicas ou estados de review pertencentes a `@game-guild/grading`.

A nova superficie possui shell e perfis tipados proprios. Uma copia de codigo
de `CodingAssessmentEditor.tsx`, quando inevitavel, deve ser reduzida a mecanica
de composicao com o Emception e registrada como divergente desde sua criacao.

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

Os targets sao indicativos ate
[`CODE-UI-01`](./03-coding-surface/01-characterization-and-decision-baseline.md).
A implementacao deve substituir o
target pelo path real, registrar o commit exato de origem e descrever o delta.

| ID | Estado | Origem preservada | Target previsto | Parte copiada | Divergencia esperada |
| --- | --- | --- | --- | --- | --- |
| `DUP-PL-001` | `diverged` | `ui-emception/src/assessment/types.ts`; `apps/web/src/lib/coding-assignment/types.ts` | `packages/features/coding/src/contracts.ts` e `schemas.ts` | conceitos de definition, files, tests e guards | contrato novo e estrito, IDs estaveis, pesos inteiros, reports tecnicos e formas authoring/delivery/response separadas |
| `DUP-PL-002` | `diverged` | `ui-emception/src/assessment/paths.ts` | `packages/features/coding/src/paths.ts` | normalizacao `/user` para `/home/user` | o dominio aceita paths relativos e o alias preservado `/user`, sempre retorna path canonico e rejeita colisoes, absolutos externos, segmentos ambiguos e namespace reservado |
| `DUP-PL-003` | `diverged` | `ui-emception/src/assessment/plan.ts` | `packages/features/coding-surface/src/execution-plan.ts` | montagem estritamente necessaria de `TestPlan`, overlays e escopo public/full | contrato canonico e perfis explicitos; preview marcado sem autoridade academica |
| `DUP-PL-004` | `diverged` | `ui-emception/src/assessment/session.ts` | `packages/features/coding-surface/src/session.ts` | controller subset, run e submission delta | submission canonica com IDs; nenhum score oficial ou DTO da app |
| `DUP-PL-005` | `diverged` | `ui-emception/src/assessment/presets.ts` | `packages/features/coding-surface/src/workspace.ts` | workspace config por linguagem | usa somente `PRESETS` publico e limita o catalogo ao corte aprovado |
| `DUP-PL-006` | `diverged` | `ui-emception/src/assessment/samples.ts` | `packages/features/coding-surface/src/workspace.ts` e testes locais | conceito de sample por linguagem | nenhum sample foi copiado; o destino projeta somente `PRESETS` do entrypoint publico |
| `DUP-PL-007` | `diverged` | `ui-emception/src/assessment/workspace-storage.ts` | `packages/features/coding-surface/src/storage.ts` | chave de workspace | exige tenant, revisao, actor e attempt; nao armazena estado academico |
| `DUP-PL-008` | `diverged` | `ui-emception/src/components/CodingAssessmentEditor.tsx` | `packages/features/coding-surface/src/coding-surface.tsx` e `coding-workbench.tsx` | controller, read-only, workspace sync e run controls | workbench proprio sobre `createEmception()` headless; o componente monolitico `Ide` e seu layout nao sao renderizados |
| `DUP-PL-009` | `diverged` | `ui-emception/src/components/TestResultsPanel.tsx` | `packages/features/coding-surface/src/test-results-panel.tsx` | estado resumido do preview | resultado explicitamente local; review/release permanecem no host |
| `DUP-PL-010` | `diverged` | `ui-emception/src/styles/theme.css` e `css.d.ts` | estilos locais em `coding-surface.tsx` e `test-results-panel.tsx` | nenhuma folha de estilo copiada | composicao minima e propria; o tema preservado nao virou dependencia nem baseline visual |
| `DUP-PL-011` | `diverged` | `ui-emception/src/index.ts` e `assessment/editor.ts` | `packages/features/coding-surface/src/index.ts` | organizacao da API publica | API tipada por modo e sem aliases legados |
| `DUP-PL-012` | `diverged` | testes sob `ui-emception/src/assessment` e `src/components` | `packages/features/coding-surface/src/*.test.ts*` | privacy, modos, storage e boundary | testes originais permanecem; destino valida o contrato canonico |
| `DUP-PL-013` | `diverged` | mocks sob `ui-emception/src/__mocks__` | mock local em `coding-surface.test.tsx` | fronteira publica do IDE | mock minimo do entrypoint publico, sem copiar internals |
| `DUP-PL-014` | `diverged` | preserved assessment wire types/preset mapping; baseline `6d90892224` | web `execution-configuration.ts`, Courses coding execution DTO/validator and authorized grading `browser.ts` | explicit frozen environment, limits, entry and toolchain mapping | web uses the coding parser; independent C# and worker guards mirror the wire rules. Worker path normalization deliberately mirrors `coding/src/paths.ts`, reusing its own budget/frozen-binding constants (CS-ADR-032). CS-ADR-036 narrowly permits only the pure function composer, not other feature implementation imports. Negative/import-boundary tests protect this duplication; no IDE/UI/public-runtime extraction |
| `DUP-PL-015` | `diverged` | current-identity metadata checks in `tools/emception/grading/toolchain-binding.mjs`; baseline `4a212cd7ff` | `coding-surface/src/runtime-session.ts` and `runtime-capabilities.ts` | manifest release/ABI/lock/receipt preflight before lazy boot; configured frozen identity check for local operation eligibility | reuses the public manifest parser and coding constants; eligibility does not claim manifest/receipt verification; no import of the worker artifact, preserved UI or private runtime; mismatch/legacy negatives; preparation, not official execution (CS-ADR-033/035) |
| `DUP-PL-016` | `diverged` | public `emception/testing` scalar harness/type mapping and report grammar at baseline `c733a556c3` | `coding/src/native-function-support.ts`, surface `execution-plan.ts`, official grading `browser.ts` | native scalar ABI declarations, C call/report template and small canonical-to-public-harness type maps | CS-ADR-036 explicitly approves owned composition: C++ reuses the unchanged public generator; C emits its existing console grammar through the unchanged engine/parser. Pure shared source composition prevents preview/official template drift; the separate typed transport maps preserve dependency direction. Runtime parity, private-scope/cleanup negatives and native literal validation are required. No compound encoding, copied compiler, SDK/IDE/UI refactor or upstream synchronization |
| `DUP-PL-017` | `diverged` | grading preparation-budget policy at baseline `b07338ce46`; existing surface visible-output bound | `coding-surface/src/graphics-session.ts` | 120-second infrastructure preparation and 100,000-character diagnostic retention | local graphics owns its timers/channels and frozen student limit, not official grades or the worker's watchdog. Reuses the existing lazy runtime and public canvas compiler/lifecycle; no copied compiler, harness, event system or UI. The calibration source is synthetic and not copied from Emception UI. CS-ADR-038; listener cleanup remains EMC-GAP-003 |

## Composicoes web afetadas

Estes arquivos sao consumidores a serem migrados ou substituidos, nao fontes de
UX nem arquivos a serem movidos automaticamente. Quando um deles migrar para
`coding-surface`, registrar no PR quais cenarios de teste foram copiados ou
adaptados e qual perfil de exercicio passou a atender aquele fluxo:

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

## Inventario executado na parte 1

### Consumidores dos contratos preservados

O baseline encontrou os seguintes consumidores diretos de
`apps/web/src/lib/coding-assignment/types.ts`:

- rota learner `learn/courses/[slug]/activities/[activityId]/page.tsx`;
- `coding-assignment-preview.tsx` e `lesson-authoring-workspace.tsx`;
- `coding-activity-client.tsx` e seu teste;
- editor e testes de coding definition, inclusive functional test;
- `resolve-seed.ts`.

Os tipos de `ui-emception/src/assessment/types.ts` continuam consumidos por
`CodingAssessmentEditor.tsx`, pelos mapeadores `plan.ts` e `session.ts`, por
seus testes e pelos exports publicos do package. Nenhum desses imports foi
alterado na parte 1.

### Classificacao dos campos existentes

| Campo ou capacidade | Classificacao | Tratamento no contrato novo |
| --- | --- | --- |
| environment language/tools/lib bundle | dominio + execucao | substituido por identidade estavel de toolchain; presets ficam fora do dominio |
| workspace files | dominio | IDs e paths canonicos, encoding, mutabilidade e visibilidade explicitos |
| public/private tests | dominio + seguranca | visibilidade por teste; delivery possui apenas `publicTests` |
| standard/functional expectations | dominio | `stdio` e `function`, com IDs estaveis e tipos validados |
| `Weight: number` | dominio legado | `weightUnits` inteiro positivo; nenhum peso academico fracionario novo |
| `Grading.MaxScore` | workflow academico | removido do dominio coding; fornecido ao adapter de grading como `ScoreValue` |
| editor mode, controller, storage e callbacks | UI/execucao | fora de `@game-guild/coding`; permanece no package preservado nesta fase |
| score de preview browser | UI/execucao | nao e report oficial e nao entra no adapter puro |
| aliases de casing e guards permissivos | compatibilidade legada | preservados no consumidor atual; parser novo e estrito e fail-closed |

Nenhum campo foi classificado como obsoleto comprovado nesta fase. Por isso,
nenhum contrato, import ou teste existente foi removido.

### Identidade e fixtures canonicas

- adapter backend e TypeScript: `code-assessment-type`, versao `1`;
- handler automatizado: `code-wasm-review`, versao `1`;
- payload de submission: `code-files`;
- toolchain corrente: artifact `4.5.2`, ABI `emception-browser-v1`.

As formas canonicas do adapter estao registradas em
`packages/features/grading-adapter-code/fixtures/code-adapter-cases.json`.
Authoring, delivery redigida, submission, report e casos invalidos estao em
`packages/features/coding/fixtures/coding-contract-cases.json`. A parte 2
consome essas fixtures para provar paridade C# sem compartilhar codigo entre
runtimes. Identidades diferentes das correntes falham fechado.

### Copias concretas da parte 1

| ID | Origem e baseline | Destino | Motivo | Delta | Testes | Owner |
| --- | --- | --- | --- | --- | --- | --- |
| `DUP-NEW-001` | `apps/web/src/lib/coding-assignment/types.ts` e `ui-emception/src/assessment/types.ts` em `4c625d55da` | `packages/features/coding/src/contracts.ts` e `schemas.ts` | fonte TypeScript autoritativa para novos fluxos de grading | schema camelCase novo, IDs, inteiros, redaction estrutural e report tecnico | `@game-guild/coding` | coding/grading |
| `DUP-NEW-002` | `ui-emception/src/assessment/paths.ts` em `4c625d55da` | `packages/features/coding/src/paths.ts` | impedir colisoes de aliases e traversal antes de snapshot/submission | aceita relativo e `/user`, sempre canonicaliza em `/home/user`; demais regras sao mais estritas e independentes | `@game-guild/coding` | coding/grading |

Essas copias nao devem acompanhar silenciosamente mudancas futuras na origem.
Qualquer convergencia depende da revisao com o responsavel pelo Emception.

### Copias e reimplementacoes concretas da parte 3

| ID | Origem ou referencia em `41932100b2` | Destino | Motivo | Delta | Testes | Owner |
| --- | --- | --- | --- | --- | --- | --- |
| `DUP-NEW-003` | `ui-emception/src/assessment/plan.ts` | `coding-surface/src/execution-plan.ts` | montar preview publico/full pelos contratos canonicos | perfis explicitos, IDs estaveis e nenhum efeito academico | `coding-surface` | coding/grading |
| `DUP-NEW-004` | `ui-emception/src/assessment/session.ts` | `coding-surface/src/session.ts` | executar preview e extrair submission canonica | API minima, learner redigido e score somente local | `coding-surface` | coding/grading |
| `DUP-NEW-005` | presets e samples publicos do Emception | `coding-surface/src/workspace.ts` | compor workspace sem importar `emception-ui` | usa `PRESETS` publico; samples antigos nao foram copiados | `coding-surface` | coding/grading |
| `DUP-NEW-006` | `ui-emception/src/assessment/workspace-storage.ts` | `coding-surface/src/storage.ts` | isolar drafts locais | namespace exige tenant, revisao, actor e attempt | `coding-surface` e web | coding/grading |
| `DUP-NEW-007` | controller e sincronizacao publica de `CodingAssessmentEditor.tsx`; API `createEmception()` | `coding-surface/src/coding-surface.tsx` e `coding-workbench.tsx` | prover runtime, VFS e editor aos quatro modos sem herdar a IDE generica | workbench proprio com contexto, tabs, Monaco, console e resultados; nenhum `Ide`, docking ou canvas implicito | `coding-surface` e web | coding/grading |
| `DUP-NEW-008` | conceito de `TestResultsPanel.tsx` | `coding-surface/src/test-results-panel.tsx` | exibir evidencia de preview | distingue preview local de grade oficial | `coding-surface` | coding/grading |
| `DUP-NEW-009` | testes e mocks do package preservado | testes de `coding-surface` | proteger boundary, privacy, modos e storage | mocks apenas do entrypoint publico; suites independentes | `coding-surface`; `emception-ui` | coding/grading |

Nenhuma entrada acima exige sincronizacao automatica com a origem. O package
preservado manteve diff vazio e sua suite permaneceu verde.

## Refoundation discovery - 2026-11-19

`CODE-UX-01` added an independent layout specimen and moved our existing
browser-cycle fixture into a directly testable helper. Neither copies Emception
source, changes its package or creates a shared runtime abstraction. No new
upstream code duplication was introduced in this gate. Current consumer
dispositions are recorded in part 3's `CONSUMER-MAP.md`; production replacements
remain pending. Historical duplication entries above are retained.

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
- nenhuma entrada trata o layout ou workflow atual de `Coding assignment` como
  requisito de compatibilidade da nova superficie;
- copias de composicao visual identificam a mecanica reaproveitada e provam que
  shell e perfis de exercicio pertencem a `coding-surface`;
- `packages/infrastructure/ui-emception/**` permanece sem diff;
- suites da origem e do destino podem executar independentemente;
- nenhum helper compartilhado foi criado sem decisao dos owners;
- o documento esta pronto para revisao item a item com o responsavel pelo
  Emception.
