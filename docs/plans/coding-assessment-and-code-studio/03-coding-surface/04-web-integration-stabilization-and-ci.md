# Parte 3.4. Integracao web, estabilizacao e CI

Status: planejado.

IDs preservados: `CODE-UI-05`, `CODE-UI-06` e `CODE-UI-07`.

## Objetivo

Migrar somente os consumidores de grading aprovados para
`@game-guild/coding-surface`, validar as duas superficies em paralelo e
classificar corretamente os novos paths no CI. Esta etapa nao usa a migracao
como pretexto para alterar consumidores alheios ou consolidar Emception UI.

Pre-requisito: gate da
[`parte 3.3`](./03-exercise-profiles-and-persona-boundaries.md) concluido e
registrado.

## Protocolo de inicio

- ler todos os arquivos de contexto persistente;
- atualizar `CONSUMER-MAP.md` se merges alteraram paths ou ownership;
- confirmar testes verdes do package antes da primeira migracao;
- migrar um fluxo por vez e registrar seu teste de regressao;
- manter consumidores `preservar` fora do diff;
- nao remover alias ou workaround apenas porque um consumidor foi migrado.

## `CODE-UI-05`. Migrar `apps/web`

### Autoria e author test

- migrar o editor de definicao pertencente a coding assessment;
- substituir a experiencia de IDE generica pelo perfil do tipo do exercicio;
- migrar o preview autoral para o modo `author-test` sem efeitos academicos;
- manter rota e application layer responsaveis por auth, tenant, fetch,
  mutation, conflito, retry e navegacao.

### Learner

- migrar a atividade learner de coding assessment;
- entregar somente delivery redigida e comandos autorizados;
- manter submission oficial no client/handler da aplicacao;
- reidratar estado academico do servidor depois de refresh;
- validar que a UI local nao publica score ou feedback antes da politica.

### Grader e SpeedGrader

- migrar assessment grader e SpeedGrader de code;
- adaptar snapshot, submission e evidencias aos contratos publicos;
- manter review, regrade e release como comandos da aplicacao;
- preservar comportamento de itens nao-code nos graders compartilhados.

### Regras de migracao

- atualizar testes e mocks para os novos entrypoints;
- atualizar cada linha do `CONSUMER-MAP.md` no mesmo commit da migracao;
- nao migrar consumidor fora de grading para reduzir duplicacao;
- nao remover `@game-guild/emception-ui` de `apps/web` enquanto existir qualquer
  consumidor preservado;
- preservar aliases, configuracao e testes do package existente;
- impedir import browser-only acidental em server components.

## `CODE-UI-06`. Estabilizar superficies paralelas

- confirmar por busca que cada fluxo selecionado importa `coding-surface`;
- confirmar que consumidores preservados continuam em `emception-ui`;
- manter testes originais do package preservado onde estao;
- copiar e adaptar testes necessarios para a nova superficie, sem move-los;
- registrar contratos, componentes, styles, fixtures e testes duplicados;
- documentar divergencias intencionais de UX e grading;
- executar ambas as suites para detectar quebra acidental;
- impedir abstracao compartilhada oportunista sem revisao com o responsavel do
  Emception.

## `CODE-UI-07`. Classificar no CI

- incluir paths de `coding`, `grading-adapter-code`, `coding-surface`,
  `Assessments.CodeAdapter` e consumidores web no ciclo de coding;
- manter separado o escopo produtor que recompila, empacota e publica artifacts
  do Emception;
- permitir que mudanca apenas de consumidor use artifact Emception validado sem
  simular alteracao upstream;
- adicionar testes da classificacao de paths;
- atualizar workflow e helper de aquisicao de artifact somente quando a
  classificacao existente exigir;
- nao criar changeset ou release do Emception para mudanca isolada de
  `coding-surface`.

## Ordem interna recomendada

1. autoria e author test;
2. learner;
3. assessment grader;
4. SpeedGrader;
5. regressao das superficies paralelas;
6. classificacao de CI;
7. browser E2E cruzando personas.

Cada item deve ficar verde antes do proximo. Uma falha externa conhecida e
registrada nao autoriza declarar verde o fluxo que ela impede verificar.

## Validacao

```bash
pnpm --filter @game-guild/coding-surface test
pnpm --filter @game-guild/coding-surface typecheck
pnpm --filter @game-guild/emception-ui test
pnpm --filter @game-guild/web test -- src/components/learning
pnpm --filter @game-guild/web test:browser:coding-cycle
pnpm --filter @game-guild/web build
```

Adicionar testes focados de classificacao do CI e os testes dos componentes
exatos migrados. O browser E2E deve usar personas separadas para author,
learner e grader e validar ao menos:

- author cria e testa sem tentativa academica;
- learner recebe somente dados publicos e submete oficialmente;
- grading e release seguem a politica do assessment;
- learner ve resultado somente quando liberado;
- acesso cruzado ou direto sem permissao falha fechado.

## Gate de conclusao

- todos os consumidores marcados `migrar` estao concluidos no mapa;
- todos os consumidores marcados `preservar` continuam funcionais;
- os quatro modos usam a API publica de `coding-surface` nas rotas selecionadas;
- `coding-surface` nao importa nem reexporta `emception-ui`;
- registro de duplicacao cobre todas as copias conhecidas;
- server components nao importam modulo browser-only;
- `dev:fast` e build web resolvem os entrypoints sem alias privado novo;
- suites das duas superficies e browser E2E passam;
- mudanca isolada de `coding-surface` dispara regressao de coding, mas nao
  release do Emception;
- todo path novo ou migrado possui classificacao explicita no CI;
- nenhuma tabela, migration, fonte ou default do Emception foi alterado;
- `IMPLEMENTATION-STATE.md` marca os quatro subplanos concluidos e aponta a
  [parte 4](../04-end-to-end-validation-and-cutover.md) como proxima fase.

Este gate conclui a parte 3, mas nao substitui o cutover E2E da parte 4.
