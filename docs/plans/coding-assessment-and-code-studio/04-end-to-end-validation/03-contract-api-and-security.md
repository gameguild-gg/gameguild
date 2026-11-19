# 4.3. Contratos, API e seguranca

Status: planejado; depende de `CODE-E2E-02`.

ID: `CODE-E2E-03`.

## Objetivo

Provar que o pipeline servidor executa, avalia e libera coding assessments sem
conhecimento de code no core generico e sem vazamento de material privado.

## Cobertura obrigatoria

- paridade de fixtures TypeScript e C# para `function` e `standard-io`;
- roundtrip e redacao de definition, delivery, submission e evidence;
- identidade canonica `code-assessment-type@1`, `code-wasm-review@1` e
  `code-files`;
- falha fechada para type, version, handler ou schema desconhecido;
- submission invalida, limite, timeout, cancelamento e worker indisponivel;
- automated result, instructor override, release e regrade congelado;
- score inteiro, `maxScore`, report por teste e feedback permitido;
- autorizacao negativa, outsider e tenant isolation;
- ausencia de private tests em responses, logs e evidence learner.

## Suites minimas

- `@game-guild/coding`;
- `@game-guild/grading-adapter-code`;
- `GameGuild.Learning.Assessments.CodeAdapter.UnitTests`;
- `GameGuild.Learning.Assessments.UnitTests` focados;
- `GameGuild.Learning.Courses.UnitTests` focados;
- integracao PostgreSQL/HTTP aplicavel ao pipeline;
- suites de arquitetura e autorizacao exigidas pelo `AGENTS.md` se o diff
  tocar seus limites.

## Gate

- erro tecnico permanece erro tecnico e nunca vira resposta errada;
- resultado aponta causalmente para submission, revision, handler e round;
- regrade usa as identidades congeladas, nao defaults atuais;
- core de assessments continua sem condicionais de code;
- todos os endpoints tocados mantem decisao explicita de autorizacao;
- testes e build backend afetados ficam warning-clean.

## Tratamento de achados

Correcao de harness pode permanecer neste subplano. Mudanca de regra de
produto, schema, migration, plataforma ou Emception exige issue/reproducer e
trabalho separado antes de alterar o codigo.

