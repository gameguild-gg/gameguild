# Decisoes da parte 4

Atualizado: 2026-11-20.

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

Schema-2 assets must match the frozen executable identity. Existing published
packages are valid consumer inputs; exact-source producer provenance remains
required when the existing CI classifier selects it. A fabricated manifest,
schema 1 or relaxed check cannot close `CODE-UI-07`. This does not require
building or publishing the independent IDE for a coding-surface UI commit.

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

The existing `PR Verify` entry or a draft PR can execute the consumer lane.
This does not authorize dispatching Emception's independent IDE deployment.
Source artifact production remains conditional on the existing classifier.
The final PR still integrates parts 01 through 04 after their gates pass.

### `D-04-10`. Preparacao de fixture/artifact ocorre antes da refundacao

Updated by part 3 `CS-ADR-024`: `CODE-UX-01` prepares the canonical fixture,
frozen-binding contract and lightweight tests. `CODE-UX-02` does not depend on
compiler execution. `CODE-UX-03` synchronizes compatible published assets and
passes actual local preflight before real runtime checks. Part 4.0 revalidates
these on the reconciled branch and closes `CODE-UI-07` with official evidence.
The prior unconditional source-build/artifact requirement is withdrawn,
without weakening conditional CI provenance or creating a dependency cycle.

Os casos `CS-REF-*` da refundacao sao requisitos a mapear ao catalogo desta
parte, nao uma segunda fonte de status executavel. Evidencia antiga so e
reaproveitada quando o commit e o escopo ainda correspondem ao produto atual.

## Como atualizar

Registrar aqui somente decisoes que alterem fronteiras, oracles, privacidade,
ownership, compatibilidade, sequencia ou escopo entre subplanos. Uma decisao
substituida deve permanecer no historico com referencia para sua sucessora.
