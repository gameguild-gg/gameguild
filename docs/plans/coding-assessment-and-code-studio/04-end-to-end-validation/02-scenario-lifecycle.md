# 4.2. Lifecycle de cenarios

Status: planejado; depende de `CODE-E2E-01`.

ID: `CODE-E2E-02`.

## Objetivo

Criar um lifecycle reutilizavel por Playwright e pelo laboratorio web para
preparar, verificar, retomar e limpar cenarios oficiais de coding grading.

## Implementacao

- implementar um `GradingScenarioAdapter` de code em
  `apps/web/src/lib/grading-scenarios/adapters/code/`;
- preparar instructor, learner, reviewer autorizado e outsider com sessoes
  independentes;
- criar curso, enrollment, conteudo, assessment, revisao e grading group pelas
  APIs e comandos oficiais;
- executar author -> approve -> publish explicitamente quando o checkpoint
  exigir jornada learner;
- suportar checkpoints `authoring-ready`, `learner-ready`, `review-ready` e
  `released`;
- registrar recursos em ownership manifest por run;
- tornar setup e verify idempotentes;
- limpar somente recursos comprovadamente pertencentes ao manifest;
- produzir diagnostico acionavel para residuo, API indisponivel, auth, artifact
  e worker;
- compartilhar o mesmo lifecycle entre automacao e uso humano.

O adapter deve usar os contratos de autenticacao e MFA vigentes depois da
reconciliacao com `develop`. Nao alterar auth para facilitar os cenarios e nao
usar SystemAdmin como substituto das personas sob teste.

## Dados minimos

Cada run deve incluir pelo menos um exercicio `function` e um `standard-io`,
ambos com public tests e private tests distintos. Credenciais locais podem ser
curtas, mas nao entram no repositorio nem em artifacts de CI.

## Gate

- setup nao escreve diretamente no banco;
- reset nao remove recurso sem ownership comprovado;
- uma falha intermediaria pode ser retomada ou limpa com diagnostico;
- o estado publicado e confirmado antes de abrir a jornada learner;
- manifests distinguem run, tenant, persona e recursos;
- automation e web lab usam a mesma implementacao de lifecycle;
- nao existe CLI especifica de coding.

## Fora do escopo

- definir politica de review dentro do adapter de cenario;
- expor private tests ao learner;
- mudar schema, migration, tenant ou permission model.

