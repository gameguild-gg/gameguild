# Decisoes da parte 4

Atualizado: 2026-11-19.

## Decisoes vigentes

### `D-04-01`. A parte 4 aceita o cutover, nao o repete

Os consumidores de grading foram migrados na parte 3. A parte 4 valida a
integracao e registra gaps; uma nova migracao ou remocao exige trabalho
separado.

### `D-04-02`. O laboratorio e web-only e usa o adapter generico

Code implementa `GradingScenarioAdapter` e entra no registry existente. Nao ha
CLI especifica de coding nem rota paralela de laboratorio.

### `D-04-03`. Os dois tipos iniciais sao obrigatorios

`function` e `standard-io` possuem casos separados em contratos, API, browser
e validacao humana. Um nao serve de proxy para o outro.

### `D-04-04`. A identidade canonica nao possui fallback

Este corte usa `code-assessment-type@1`, `code-wasm-review@1` e `code-files`.
Identidades desconhecidas falham fechadas; nao criar aliases para contratos
intermediarios do roadmap.

### `D-04-05`. Somente o artifact exato fecha o browser gate

O artifact Emception schema 2 deve vir do produtor oficial. Manifest fabricado,
schema 1 ou verificacao relaxada nao fecha `CODE-UI-07`.

### `D-04-06`. Achado e correcao sao trabalhos distintos

O harness pode corrigir suas fixtures e instrumentacao. Mudanca de regra de
produto, schema, migration, auth de plataforma ou Emception exige owner,
reproducer e trabalho separado.

### `D-04-07`. O harness nao muda persistencia nem plataforma

Nenhuma tabela, migration, modulo de plataforma ou fonte do Emception sera
alterada para preparar ou limpar cenarios. Setup usa APIs oficiais e ownership
manifest.

### `D-04-08`. Usar somente a toolchain de testes existente

O programa compoe Vitest, testes .NET, Playwright, scripts Node e CI ja usados
no repositorio. Nova dependencia de teste exige necessidade comprovada e
decisao separada.

### `D-04-09`. Evidencia intermediaria nao substitui o PR final

`workflow_dispatch` ou draft PR podem produzir o artifact e executar a lane.
O PR final continua sendo a integracao conjunta das partes 01 a 04 depois de
todos os gates.

## Como atualizar

Registrar aqui somente decisoes que alterem fronteiras, oracles, privacidade,
ownership, compatibilidade, sequencia ou escopo entre subplanos. Uma decisao
substituida deve permanecer no historico com referencia para sua sucessora.
