# 4.1. Catalogo de capabilities e oracles

Status: planejado; depende de `CODE-E2E-00`.

ID: `CODE-E2E-01`.

## Objetivo

Definir um programa executavel e auditavel para coding grading, com IDs
estaveis, oracles autorizados e ownership de cada falha.

## Implementacao

Criar:

```text
docs/testing/coding-grading/
  AGENT_PROTOCOL.md
  capability-catalog.*
  capability-status.md
  product-heuristics.md
  reports/
```

O catalogo deve:

- possuir casos separados para `function` e `standard-io`;
- cobrir authoring, projecao, test run, learner, submission, review, release,
  regrade, seguranca e falhas de infraestrutura;
- registrar expected state em UI, API, grade result, release e auditoria;
- mapear owner para coding, adapter TypeScript, CodeAdapter backend,
  assessments, surface, app, harness ou Emception;
- usar os status `pass`, `bug`, `partial`, `missing`, `decision-needed`,
  `harness-gap`, `external-blocker` e `not-applicable`;
- registrar commit, ambiente, adapter/handler version e artifact em cada run;
- definir evidencia sanitizada e politica de retencao;
- mapear cada assertiva do runner anterior para case ID ou classifica-la como
  diagnostico transitorio.

## Oracles permitidos

- resposta de API autorizada;
- estado de UI carregado em sessao real da persona;
- evento, audit record ou grade result exposto por contrato oficial;
- resultado do worker vinculado causalmente a submission e review round.

Leitura direta de banco, texto incidental de log e estado apenas local nao sao
oracles de aceite. O probe de `RubricScoresPayload` continua somente como
diagnostico transitorio ate existir oracle oficial equivalente.

## Gate

- IDs sao unicos e estaveis;
- todo caso declara pre-condicoes, passos, oracle, owner e dados proibidos;
- todo skip possui ID, motivo e tracking;
- reports gerados nao alteram automaticamente o status canonico;
- private tests, answer keys, tokens, cookies e secrets nao entram em evidence;
- nenhum caso depende de inspecao direta do banco para passar.

## Fora do escopo

- corrigir bugs descobertos;
- criar fixtures no banco;
- copiar o catalogo de quiz ou misturar seus IDs com coding.

