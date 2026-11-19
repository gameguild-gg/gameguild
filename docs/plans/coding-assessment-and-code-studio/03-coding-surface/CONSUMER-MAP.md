# Coding surface consumer map

Status: consumidores de grading migrados e validados sobre o commit-base
`41932100b2031c0ad78d7c02545a73addc6364b3`.

Este mapa separa consumidores pertencentes ao fluxo de grading daqueles que
devem continuar em `@game-guild/emception-ui`. Um path encontrado por busca nao
e automaticamente autorizado a migrar. Cada linha precisa de ownership,
destino e teste de regressao confirmados.

Status permitidos: `investigar`, `preservar`, `pronto para migrar`, `migrando`,
`migrado` e `validado`.

## Consumidores de grading candidatos

| Fluxo | Path atual | Modo | Dependencia atual | Destino esperado | Estado | Regressao obrigatoria |
| --- | --- | --- | --- | --- | --- | --- |
| definicao de coding assessment | `apps/web/src/components/learning/console/courses/[course]/assessments/[assessmentId]/coding-definition/coding-definition-editor.tsx` | `author` | `@game-guild/coding-surface` e bridge do host | `coding-surface` | validado | roundtrip de draft, tipo explicito e expectativas privadas |
| preview de coding no authoring | `apps/web/src/components/learning/authoring/coding-assignment-preview.tsx` | `author-test` | `@game-guild/coding-surface` | `coding-surface` | validado | preview sem attempt, gradebook ou release |
| atividade learner | `apps/web/src/components/learning/coding-activity-client.tsx` | `learner` | `@game-guild/coding-surface` e delivery redigida | `coding-surface` | validado | public data, storage namespaced, submit oficial e refresh |
| assessment grader de code | `apps/web/src/components/learning/assessment-grading/assessment-grader.tsx` | `grader` | `@game-guild/coding-surface` e snapshot do host | `coding-surface` | validado | snapshot, evidencias e review autorizada |
| painel code do SpeedGrader | `apps/web/src/app/[locale]/(speedgrader)/speedgrader/assessments/[assessmentId]/code-grader-panel.tsx` | `grader` | reusa o assessment grader migrado | `coding-surface` por composicao migrada | validado | code e itens nao-code permanecem funcionais |

Os sete arquivos de teste focados desses fluxos e da bridge somam 77 testes
verdes. O browser cycle integral permanece no lane `coding`: sua fixture deve
emitir `ExerciseType` canonico e sua execucao exige o artifact Toolchain schema
2 exato.

## Consumidores preservados

| Fluxo | Path atual | Motivo para preservar | Teste de regressao | Estado |
| --- | --- | --- | --- | --- |
| package preservado e seus testes | `packages/infrastructure/ui-emception/**` | produto Emception existente, fora do ownership de grading | `pnpm --filter @game-guild/emception-ui test` | preservar |
| aliases e suporte do ciclo antigo | `apps/web/next.config.ts` e scripts `coding-cycle-*` | ainda protegem o package preservado e nao pertencem ao novo contrato | `pnpm --filter @game-guild/web test:browser:coding-cycle` | preservar |
| restore de seed legado | `apps/web/src/components/learning/resolve-seed.ts` | helper do host ainda converte payloads existentes; nao pertence ao package novo | testes de coding activity | preservar |

## Configuracao compartilhada a preservar

| Item | Path | Regra |
| --- | --- | --- |
| dependencia workspace | `apps/web/package.json` | nao remover enquanto houver consumidor preservado |
| aliases e transpilation | `apps/web/next.config.ts` | nao remover ou remodelar durante a migracao sem prova de ownership exclusivo |
| package existente | `packages/infrastructure/ui-emception/**` | nao editar, mover, deprecar ou remover nesta parte |

## Checklist por consumidor

Antes de marcar uma linha como `migrado`:

- [x] ownership de grading confirmado;
- [x] props e callbacks atuais registrados;
- [x] dados publicos e privados classificados;
- [x] source of truth e reidratacao identificadas;
- [x] destino usa apenas API publica de `coding-surface`;
- [x] host continua possuindo auth, tenant, fetch e mutations;
- [x] teste unitario ou de componente atualizado;
- [x] teste browser ou integracao identificado;
- [x] imports antigos removidos somente deste consumidor;
- [x] consumidores preservados e aliases nao foram afetados.
