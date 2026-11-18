# Coding surface consumer map

Status: preliminar; deve ser verificado e completado em `CODE-UI-01`.

Este mapa separa consumidores pertencentes ao fluxo de grading daqueles que
devem continuar em `@game-guild/emception-ui`. Um path encontrado por busca nao
e automaticamente autorizado a migrar. Cada linha precisa de ownership,
destino e teste de regressao confirmados.

Status permitidos: `investigar`, `preservar`, `pronto para migrar`, `migrando`,
`migrado` e `validado`.

## Consumidores de grading candidatos

| Fluxo | Path atual | Modo | Dependencia atual | Destino esperado | Estado | Regressao obrigatoria |
| --- | --- | --- | --- | --- | --- | --- |
| definicao de coding assessment | `apps/web/src/components/learning/console/courses/[course]/assessments/[assessmentId]/coding-definition/coding-definition-editor.tsx` | `author` | `@game-guild/emception-ui` e presets | `coding-surface` | investigar | roundtrip de draft e expectativas privadas |
| preview de coding no authoring | `apps/web/src/components/learning/authoring/coding-assignment-preview.tsx` | `author-test` a confirmar | editor e presets de `emception-ui` | `coding-surface` | investigar | preview sem attempt, gradebook ou release |
| atividade learner | `apps/web/src/components/learning/coding-activity-client.tsx` | `learner` | editor, plan, presets e storage de `emception-ui` | `coding-surface` | investigar | public data, submit oficial e refresh |
| assessment grader de code | `apps/web/src/components/learning/assessment-grading/assessment-grader.tsx` | `grader` | editor e presets de `emception-ui` | `coding-surface` | investigar | snapshot, evidencias e review autorizada |
| painel code do SpeedGrader | `apps/web/src/app/[locale]/(speedgrader)/speedgrader/assessments/[assessmentId]/code-grader-panel.tsx` | `grader` | reexporta o assessment grader | `coding-surface` por composicao migrada | investigar | code e itens nao-code permanecem funcionais |

As linhas acima foram semeadas a partir dos imports conhecidos durante o
planejamento. `CODE-UI-01` deve confirmar rotas chamadoras, props, callbacks,
storage, testes e ownership antes de mudar o estado para `pronto para migrar`.

## Consumidores preservados

Nenhum consumidor especifico foi classificado ainda. `CODE-UI-01` deve listar
todo import restante de `@game-guild/emception-ui`, indicar seu produto e
registrar `preservar` quando nao pertencer ao fluxo de grading selecionado.

| Fluxo | Path atual | Motivo para preservar | Teste de regressao | Estado |
| --- | --- | --- | --- | --- |
| a preencher | a preencher | a preencher | a preencher | investigar |

## Configuracao compartilhada a preservar

| Item | Path | Regra |
| --- | --- | --- |
| dependencia workspace | `apps/web/package.json` | nao remover enquanto houver consumidor preservado |
| aliases e transpilation | `apps/web/next.config.ts` | nao remover ou remodelar durante a migracao sem prova de ownership exclusivo |
| package existente | `packages/infrastructure/ui-emception/**` | nao editar, mover, deprecar ou remover nesta parte |

## Checklist por consumidor

Antes de marcar uma linha como `migrado`:

- [ ] ownership de grading confirmado;
- [ ] props e callbacks atuais registrados;
- [ ] dados publicos e privados classificados;
- [ ] source of truth e reidratacao identificadas;
- [ ] destino usa apenas API publica de `coding-surface`;
- [ ] host continua possuindo auth, tenant, fetch e mutations;
- [ ] teste unitario ou de componente atualizado;
- [ ] teste browser ou integracao identificado;
- [ ] imports antigos removidos somente deste consumidor;
- [ ] consumidores preservados e aliases nao foram afetados.
