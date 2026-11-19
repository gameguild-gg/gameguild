# 4.5. Laboratorio web

Status: planejado; depende de `CODE-E2E-04`.

ID: `CODE-E2E-05`.

## Objetivo

Permitir que uma pessoa prepare e inspecione os mesmos cenarios usados pela
automacao sem montar manualmente curso, personas, exercises e reviews.

## Integracao

- registrar o adapter de code em
  `apps/web/src/lib/grading-scenarios/registry.server.ts`;
- usar o contrato generico `GradingScenarioAdapter`;
- manter a UI em `/workspace/learning/grading-validation`;
- oferecer cenarios individuais por exercise type e um cenario combinado;
- oferecer prepare, verify, refresh e reset;
- mostrar checkpoint, recursos, URLs por persona, capabilities e diagnosticos;
- oferecer credenciais locais curtas sem persisti-las em source control;
- limitar operacoes destrutivas ao ownership manifest.

Quiz e code continuam adapters independentes. O registry generico pode
conhecer ambos; o adapter de quiz nao pode ganhar condicionais de code e o
adapter de code nao pode depender de fixtures de quiz.

## Gate

- nenhum fluxo humano exige CLI especifica de coding;
- a interface usa o mesmo lifecycle e os mesmos case IDs do E2E;
- uma pessoa consegue continuar o fluxo nas contas de instructor e learner;
- private tests, answer keys, tokens e secrets nao sao exibidos;
- reset nao apaga dados alheios ao cenario;
- falha de API, auth, artifact ou worker aparece como diagnostico, nao como
  estado vazio.

## Fora do escopo

- criar uma segunda pagina de testing lab;
- transformar o laboratorio em feature de producao para learners;
- duplicar regras de assessment ou grading na UI.

