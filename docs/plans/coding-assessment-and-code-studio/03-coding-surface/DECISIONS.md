# Coding surface decision log

Status: ativo durante a parte 3.

Este arquivo preserva decisoes que afetam mais de um subplano. Ele nao e um
diario de implementacao e nao substitui contratos ou testes. Uma decisao nova
deve usar o proximo ID `CS-ADR-NNN`, registrar evidencia e ser escrita antes da
implementacao que depende dela.

Status permitidos: `proposta`, `aceita`, `substituida` e `rejeitada`.

## `CS-ADR-001`. Superficie paralela para grading

- Data: 2026-11-18.
- Status: aceita.
- Contexto: `@game-guild/emception-ui` e um produto existente, com consumidores,
  aliases e testes que nao pertencem integralmente a grading.
- Decisao: criar `@game-guild/coding-surface` como superficie paralela.
  Consumidores de grading selecionados podem migrar; o package existente nao e
  editado, movido, deprecado ou removido.
- Alternativa rejeitada: decompor `emception-ui` para transforma-lo na base
  compartilhada da nova superficie.
- Consequencias: duplicacao de composicao GameGuild e aceita temporariamente e
  deve constar no `DUPLICATION-REGISTER.md`.
- Evidencia: indice da parte 3 e roadmap principal.
- Afeta: 3.1, 3.2, 3.4.

## `CS-ADR-002`. Efeitos academicos pertencem ao host

- Data: 2026-11-18.
- Status: aceita.
- Contexto: a mesma superficie atende preview local e fluxo oficial, mas somente
  a aplicacao possui sessao, tenant, autorizacao e client da API.
- Decisao: `coding-surface` emite intents tipadas e nunca chama endpoints nem
  persiste submission, score, review ou release.
- Alternativa rejeitada: encapsular fetch e mutations academicas no package de
  UI.
- Consequencias: `apps/web` adapta dados e comandos; refresh usa a fonte do
  servidor.
- Evidencia: contratos das partes 01 e 02 e indice da parte 3.
- Afeta: 3.2, 3.3, 3.4.

## `CS-ADR-003`. Tipo, toolchain, persona e review sao eixos independentes

- Data: 2026-11-18.
- Status: aceita.
- Contexto: inferir experiencia pelo tipo de teste, linguagem ou review cria
  acoplamento e impede extensao segura.
- Decisao: o tipo do exercicio e discriminado no contrato canonico; toolchain,
  capabilities, modo e politica de review permanecem ortogonais.
- Alternativa rejeitada: deduzir tipo por extensao, linguagem ou
  `tests[].kind`.
- Consequencias: o registro exige renderers explicitos e falha para tipo nao
  registrado. O corte inicial implementa somente `function` e `standard-io`.
- Evidencia: dominio e adapter da parte 01.
- Afeta: 3.2, 3.3, 3.4.

## `CS-ADR-004`. Contexto de execucao persistido em arquivos

- Data: 2026-11-18.
- Status: aceita.
- Contexto: a parte 3 atravessa varias sessoes, merges e contextos de IA.
- Decisao: estado, decisoes, consumidores, gaps e duplicacoes possuem arquivos
  canonicos atualizados em cada checkpoint.
- Alternativa rejeitada: depender da conversa ou de comentarios temporarios.
- Consequencias: um gate nao esta concluido enquanto as evidencias e a proxima
  acao nao estiverem registradas em `IMPLEMENTATION-STATE.md`.
- Evidencia: protocolo de contexto persistente do indice.
- Afeta: todos os subplanos.

## `CS-ADR-005`. Proposta inicial de formato paralelo para o tipo de exercicio

- Data: 2026-11-18.
- Status: substituida por `CS-ADR-008` antes de release.
- Contexto: o formato inicial nao possuia um discriminante de experiencia,
  enquanto o payload de Learning continha apenas testes e toolchain. Inferir a
  experiencia por linguagem ou por `tests[].kind` violaria `CS-ADR-003`.
- Decisao historica: preservar o formato incompleto e adicionar outro formato
  com `exerciseType: function | standard-io` e fallback na bridge do host.
- Motivo da substituicao: nao havia dado oficial que justificasse dois formatos;
  a proposta criava compatibilidade artificial dentro do mesmo desenvolvimento.
- Consequencias atuais: `CS-ADR-008` tornou `exerciseType` obrigatorio no unico
  contrato canonico. Nao ha fallback, tabela ou migration.
- Evidencia: inventario e consolidacao da parte 3.5.
- Afeta: 3.3 e 3.4.

## `CS-ADR-008`. Um unico contrato canonico para code antes do lancamento

- Data: 2026-11-18.
- Status: aceita.
- Contexto: contratos paralelos e adapters historicos foram criados durante o
  mesmo programa de implementacao. Nao ha dados, revisoes ou submissions
  publicados que exijam compatibilidade.
- Decisao: consolidar o formato completo atual como o primeiro contrato
  canonico de code. `exerciseType` e obrigatorio; tipos de propriedade de code
  nao usam sufixos numericos preventivos; existe somente um adapter e uma
  toolchain corrente.
  `schemaVersion: 1` e a identidade `adapterVersion: "1"` permanecem como
  discriminantes estritos, sem parser ou registro alternativo.
- Alternativa rejeitada: preservar caminhos criados apenas durante o
  desenvolvimento sob a justificativa de compatibilidade futura.
- Consequencias: fixtures locais podem ser descartadas, nao ha migration e
  payloads incompletos falham fechado. Contratos V1 importados do grading
  generico permanecem fora deste corte.
- Evidencia: ausencia de release e inventario inicial da parte 3.5.
- Afeta: partes 01, 02, 3.3, 3.4 e 3.5.

## `CS-ADR-006`. Bridge de wire pertence ao host

- Data: 2026-11-18.
- Status: aceita.
- Contexto: o DTO atual usa PascalCase e a estrutura historica de Learning,
  enquanto `coding-surface` deve consumir contratos canonicos e nao conhecer
  endpoints ou DTOs da aplicacao.
- Decisao: manter a conversao wire-canonico em `apps/web`; a superficie recebe
  somente contratos de `@game-guild/coding` e emite submissions canonicas.
- Alternativa rejeitada: fazer `coding-surface` depender dos tipos ou actions de
  `apps/web`.
- Consequencias: auth, tenant, mutations, retry e navegacao permanecem no host.
- Evidencia: consumidores mapeados em `CONSUMER-MAP.md`.
- Afeta: 3.4.

## `CS-ADR-007`. Browser cycle exige artifact exato e falha fechado

- Data: 2026-11-18.
- Status: aceita.
- Contexto: o CDN versionado presente no workspace ainda usa manifest schema 1,
  mas o coding cycle V3 exige schema 2, receipt, fingerprint e identidade
  congelada correspondente ao commit testado.
- Decisao: o lane `coding` adquire o artifact Toolchain exato pelo helper
  oficial, valida sua identidade e somente depois monta o runtime e executa o
  browser E2E. A verificacao local e do CI permanece fail-closed.
- Alternativas rejeitadas: relaxar o verificador, aceitar o artifact legado ou
  fabricar manualmente manifest e provenance.
- Consequencias: os testes de package, componentes, API e runtime podem passar
  localmente; a conclusao do gate browser depende da evidencia do lane com o
  artifact correto.
- Evidencia: `apps/web/scripts/coding-cycle-toolchain-support.mjs`,
  `scripts/ci/acquire-emception-release.py` e lane `coding` em
  `.github/workflows/pr-verify.yml`.
- Afeta: 3.4.

## `CS-ADR-009`. Correcao test-only do binding do adapter extraido

- Data: 2026-11-18.
- Status: aceita.
- Contexto: a parte 2 moveu `CodeAssessmentTypeAdapter.cs` para o modulo
  `GameGuild.Learning.Assessments.CodeAdapter`, mas o teste de binding do
  Toolchain ainda lia o path removido dentro de Assessments.
- Decisao: atualizar somente a referencia de arquivo em
  `tools/emception/grading/toolchain-binding.test.mjs` para o path atual.
- Alternativas rejeitadas: recriar um arquivo legado no path antigo ou remover
  a verificacao de paridade.
- Consequencias: nenhuma fonte, runtime, default ou UI do Emception muda; o
  teste volta a verificar as mesmas constantes no owner atual do adapter.
- Evidencia: build do runtime grading com 49 testes verdes e bundle Vite verde.
- Afeta: 3.4.

## `CS-ADR-010`. Workbench proprio sobre o runtime headless

- Data: 2026-11-19.
- Status: aceita.
- Contexto: a primeira implementacao de `coding-surface` renderizava o
  componente monolitico `Ide` de `@gameguild/emception-ide`. Embora o package
  e os contratos fossem novos, Explorer, docking, canvas, terminal e hierarquia
  visual continuavam sendo a IDE generica. Isso contradizia o modelo de
  superficie orientada ao tipo de exercicio e a experiencia observada no
  Learning permanecia praticamente igual ao `Coding assignment` anterior.
- Decisao: `coding-surface` possui o workbench visual. Ele usa o entrypoint
  publico headless `createEmception()` para runtime, VFS e test runner, e usa
  Monaco para editar os arquivos autorizados. O package nao renderiza `Ide`,
  nao importa internals e nao modifica Emception ou `emception-ui`.
- Alternativas rejeitadas: renomear apenas a opcao no Learning; aplicar CSS
  sobre a IDE completa; ou alterar/decompor o package mantido pelo time do
  Emception.
- Consequencias vigentes: tabs, contexto, console e resultados pertencem ao
  novo package. A composicao headless permanece; controllers e intents nossos
  podem evoluir na refundacao. A restricao original de canvas/terminal a um
  perfil futuro foi substituida por `CS-ADR-012`: sao capabilities ortogonais,
  sujeitas a prova da combinacao e nao a criacao artificial de outro tipo.
- Evidencia: `packages/features/coding-surface/src/coding-workbench.tsx` e teste
  que prova a ausencia do componente monolitico.
- Afeta: 3.2, 3.3, 3.4 e validacao 4.0.

## `CS-ADR-011`. Autoria e execucao usam superficies distintas

- Data: 2026-11-19.
- Status: substituida por `CS-ADR-012`; mantida como historico do plano inicial.
- Contexto: a primeira autoria de coding reutilizou a superficie de execucao e
  injetou formularios de linguagem, arquivos e testes nos slots do workbench.
  Isso misturou configuracao do exercicio, codigo, terminal e edicao de testes
  numa coluna longa, mesmo quando o Learning oferecia largura suficiente. O
  seletor de tipo tambem nao selecionava um editor de configuracao proprio,
  como ocorre no registry de tipos do Quiz.
- Decisao: criar `CodingAuthoringSurface` como shell exclusivo do professor e
  manter `CodingSurface` orientada a execucao. A autoria tera workbench amplo e
  inspector lateral com Setup, Tests e Files. Um registry tipado selecionara o
  editor, validacao, defaults e capabilities de cada `exerciseType`. O estado
  de formulario sera um draft discriminado que pode estar incompleto; o
  contrato canonico `CodingAuthoringDefinition` so sera produzido depois da
  validacao. Formularios de autoria nao serao injetados em slots genericos do
  console ou do explorer.
- Alternativas rejeitadas: apenas ampliar o container atual; manter todos os
  formularios abaixo do terminal; ou adicionar condicionais de tipo no host sem
  um boundary de perfil.
- Consequencias: `standard-io` expoe somente testes stdio e `function` somente
  casos funcionais; troca de tipo trata dados incompativeis explicitamente. A
  composicao responsiva usa a largura real do container. O runtime headless e
  os exports publicos do Emception continuam preservados e nenhuma mudanca em
  `@game-guild/emception-ui` e necessaria.
- Evidencia: inspecao humana da autoria no Learning e plano
  `06-authoring-information-architecture-and-teacher-ux.md`.
- Afeta: 3.2, 3.3, 3.4, 3.6 e entrada da parte 4.

## `CS-ADR-012`. Refundacao orientada ao produto, nao ao wrapper atual

- Data: 2026-11-19.
- Status: aceita.
- Contexto: a revisao do fluxo e das referencias oficiais de LeetCode,
  HackerRank, Codio, CodeGrade e CodinGame mostrou que mover formularios para
  uma lateral nao resolve autoria, resolucao, recuperacao e validacao da
  atividade. O usuario autorizou reescrever o plano para refundar a superficie,
  sem preservar por preciosismo a implementacao inicial.
- Decisao: executar a 3.6 em seis etapas de produto, dados, runtime, autoria,
  learner/reviewer e integracao. Autoria e resolucao possuem composicoes
  proprias sobre uma fundacao compartilhada; paineis sao adaptados a tarefa,
  sem largura lateral obrigatoria. Componentes, slots e contratos nossos
  podem ser substituidos quando o modelo exigir, com consumidores e dados
  inventariados. Nao criar compatibilidade V2 especulativa.
- Decisao de modelo: familia, preparacao pedagogica, linguagem/ambiente,
  capabilities, apresentacao e politica academica sao eixos separados.
  Canvas e terminal podem atender familias existentes quando a combinacao
  estiver comprovada; nao sao reservados a um futuro tipo visual. Debugging
  normalmente e variante de starter code. Function e standard I/O sao o corte
  inicial; arrays/matrizes exigem decisao de viabilidade e escopo explicita.
- Fronteiras preservadas: grading possui score/review/release; apps/web e o
  dominio possuem persistencia/autorizacao. Emception e emception-ui nao sao
  editados. Mudanca necessaria de banco segue `SCHEMA-GATE` e migration real;
  nenhum dado existente e descartado apenas por ser de desenvolvimento.
- Alternativas rejeitadas: manter a arvore atual a qualquer custo; colocar toda
  a autoria num inspector estreito; tratar canvas como tipo; adicionar campos
  somente na UI sem persistencia; ou expandir para todas as features dos concorrentes.
- Consequencias: qualidade e medida pelos casos `CS-REF-*`, incluindo jornadas
  humanas e recursos. A matriz e requisito planejado, nao evidencia de sucesso.
  Limitacoes nao podem ser descartadas pelo executor para declarar conclusao.
- Evidencia: indice 3.6, `06-refoundation/COMPETITIVE-BASELINE.md` e
  `06-refoundation/ACCEPTANCE-MATRIX.md`.
- Afeta: 3.2 a 3.6, entrada da parte 4 e criterios de aceite do roadmap.
- Substitui: `CS-ADR-011` e a restricao de capabilities futuras de `CS-ADR-010`.

## `CS-ADR-013`. Preparacao do harness precede validacao da refundacao

- Data: 2026-11-19.
- Status: aceita.
- Contexto: 3.6 exigia runtime/browser reais, mas a fixture canonica e a
  aquisicao do artifact estavam somente em 4.0, bloqueado pela propria 3.6.
- Decisao: `CODE-UX-01` prepara fixture, artifact exato e preflight com os
  mecanismos existentes. Os testes reais dos subplanos usam essa preparacao;
  4.0 revalida na branch reconciliada e fecha a evidencia oficial de `CODE-UI-07`.
- Alternativas rejeitadas: esperar 4.0 para testar 3.6; substituir execucao real
  por mocks; relaxar provenance; ou declarar aceite E2E so com preflight verde.
- Consequencias: gates de produto e gate CI conservam evidencias distintas;
  indisponibilidade do artifact bloqueia o runtime afetado sem autorizar
  manifest fabricado ou alteracao no Emception.
- Evidencia: sequencia 3.6.1 -> 3.6.6 -> 4.0.
- Afeta: 3.4, 3.6 e 4.0.

## Modelo para novas decisoes

```text
## `CS-ADR-NNN`. Titulo

- Data: YYYY-MM-DD.
- Status: proposta | aceita | substituida | rejeitada.
- Contexto:
- Decisao:
- Alternativas:
- Consequencias:
- Evidencia:
- Afeta:
- Substitui ou foi substituida por: N/A.
```
