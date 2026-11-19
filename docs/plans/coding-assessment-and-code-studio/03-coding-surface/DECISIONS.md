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
