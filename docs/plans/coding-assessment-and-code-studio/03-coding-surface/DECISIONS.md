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
