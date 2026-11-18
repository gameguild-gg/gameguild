# Parte 4. Validacao end-to-end e cutover de coding grading

Status: planejado.

## Objetivo

Provar o fluxo oficial de coding assessment por contratos, API, browser e
personas reais antes de considerar a extracao concluida. A validacao deve dizer
o que funciona, o que falta e a qual modulo pertence cada falha.

Pre-requisitos:

- [`01`](./01-domain-contracts-and-typescript-adapter.md) concluido;
- [`02`](./02-backend-code-adapter-boundary.md) concluido;
- [`03`](./03-coding-surface-and-web-integration.md) concluido.

## Principios

- testes publicos ajudam o learner, mas o servidor continua autoritativo;
- testes privados nao chegam ao browser learner;
- `AuthorTest` e `OfficialSubmission` sao execucoes diferentes;
- `AutomatedReview` avalia o que seu adapter suporta;
- `InstructorReview`, quando configurado depois de automated, revisa o resultado
  anterior e pode altera-lo com auditoria;
- grade so fica learner-visible depois da politica de release;
- uma falha de infraestrutura nunca e convertida em resposta errada;
- cada persona usa sessao e contexto independentes.

## Baseline de regressao existente

Este plano evolui, e nao substitui de imediato, o ciclo de coding presente na
`develop`:

- `coding-cycle-browser-e2e.mjs` cobre o ciclo integrado atual;
- `coding-cycle-web-runtime.mjs` prepara uma build web de producao isolada para
  a jornada browser;
- `coding-cycle-grading-observer.mjs` observa o acknowledgement causal de
  grading sem depender do transporte RSC concreto;
- os testes de observer, transport e web runtime protegem o harness;
- a consulta direta de `RubricScoresPayload` e um diagnostico transitorio do
  runner atual, nao um oracle aceitavel para o catalogo final.

O runner monolitico permanece como regressao enquanto o novo catalogo nao
demonstrar paridade. Sua remocao ou simplificacao exige evidencia de cobertura
equivalente, nao apenas a existencia de novos testes.

## Artefatos de validacao

Criar uma area equivalente ao programa de quiz, sem compartilhar catalogo de
capabilities especificas:

```text
docs/testing/coding-grading/
  AGENT_PROTOCOL.md
  capability-catalog.*
  capability-status.md
  product-heuristics.md
  reports/
```

O catalogo executavel possui IDs estaveis. Reports gerados sao candidatos e nao
alteram o status canonico sem revisao humana.

## Matriz minima

| ID inicial | Fluxo | Persona principal | Oracle |
| --- | --- | --- | --- |
| `CG-AUTHOR-001` | criar e reabrir definicao | instructor | roundtrip canonico |
| `CG-AUTHOR-002` | private/public tests | instructor | projecoes distintas |
| `CG-TESTRUN-001` | executar revisao candidata | instructor | nenhum efeito academico |
| `CG-LEARNER-001` | abrir atividade publicada | learner | somente delivery publica |
| `CG-LEARNER-002` | preview de public tests | learner | resultado local, sem grade |
| `CG-SUBMIT-001` | enviar arquivos oficiais | learner | submission e snapshot congelados |
| `CG-REVIEW-AUTO-001` | revisar deterministicamente | system | report e score coerentes |
| `CG-REVIEW-INSTRUCTOR-001` | revisar manualmente | instructor | override auditado |
| `CG-RELEASE-001` | publicar resultado | instructor/system | learner ve grade e feedback permitidos |
| `CG-SECURITY-001` | tentar obter private tests | learner/outsider | acesso negado sem vazamento |
| `CG-SECURITY-002` | revisar sem grant | learner/outsider | comando negado e auditavel |
| `CG-VERSION-001` | regrade congelado | instructor | mesma revisao, handler e submission |

A matriz inicial usa apenas toolchains ja suportadas e estaveis. C#,
JavaScript, TypeScript, SQL e Ruby nao bloqueiam este gate; cada uma entra
somente depois de atingir o estado exigido em seu subplano da parte 5.

## Etapas

### `CODE-E2E-01`. Fechar oracles e catalogo

Implementacao:

- definir expected state para UI, API, grade result, release e auditoria;
- mapear cada caso para owner: coding, adapter, assessments, surface, app ou
  Emception;
- definir status `pass`, `bug`, `partial`, `missing`, `decision-needed`,
  `harness-gap`, `external-blocker` e `not-applicable`;
- definir evidencia sanitizada e politica de retencao de reports;
- registrar commit, ambiente e versoes dos adapters em cada run;
- mapear cada assertiva do runner existente para um case ID ou classifica-la
  explicitamente como diagnostico transitorio.

Gate:

- nenhum caso depende de inspecao direta do banco para passar;
- `RubricScoresPayload` possui um oracle autorizado por API ou UI antes que o
  probe direto existente deixe de ser apenas diagnostico;
- cada skip possui ID, motivo e tracking;
- answer keys, private tests, cookies e tokens nao entram em artifacts.

### `CODE-E2E-02`. Criar fixtures e lifecycle isolado

Implementacao:

- criar personas instructor, learner, reviewer autorizado e outsider;
- preparar curso, enrollment, assessment, revisao e grading group pela API/UI
  oficial;
- executar explicitamente author -> submit -> approve -> publish nos cenarios
  que exigem atividade learner; nao depender do worker de publicacao agendada;
- usar nomes deterministas e ownership manifest para cleanup;
- permitir checkpoint humano depois de authoring, learner-ready, review-ready e
  released;
- resetar somente recursos pertencentes ao manifest do run;
- tornar o setup idempotente e diagnosticar residuos sem apagar dados alheios.

Gate:

- setup nao escreve diretamente no banco;
- reset nao remove recurso sem ownership comprovado;
- uma falha no meio pode ser retomada ou limpa com diagnostico;
- o estado publicado e confirmado antes de abrir a jornada learner;
- persona admin nao substitui os papeis testados.

### `CODE-E2E-03`. Automatizar contratos e API

Implementacao:

- testar paridade das fixtures TypeScript/C#;
- testar schemas, limites e redacao;
- testar resolucao de versoes congeladas;
- testar submission invalida, timeout, cancellation e worker indisponivel;
- testar automated result, instructor override, release e regrade;
- testar autorizacao negativa e tenant isolation.

Gate:

- erro tecnico permanece erro tecnico;
- score usa somente inteiros e conserva `maxScore`;
- private tests nao aparecem em response, logs ou evidence learner;
- core de assessments nao precisa conhecer code para os casos passarem.

### `CODE-E2E-04`. Automatizar browser por persona

Implementacao:

- usar contextos de browser separados;
- provar autoria e reabertura;
- provar que author test nao afeta gradebook;
- provar tentativa learner, submission e estado aguardando review;
- provar revisao instructor e release;
- provar que refresh learner carrega a correcao publicada;
- executar a jornada pesada sobre o runtime web de producao isolado ja
  caracterizado, ou substituto com paridade comprovada;
- manter a observacao de acknowledgement desacoplada do transporte concreto;
- incluir screenshots/trace apenas em falha e sanitizar artifacts.

Gate:

- nenhum teste troca persona alterando apenas props ou localStorage;
- UI e API concordam sobre estado e score;
- refresh e nova sessao observam o mesmo resultado publicado;
- negativos de acesso nao dependem de esconder botoes;
- testes unitarios do observer, transport e web runtime passam separadamente
  antes da jornada integrada;
- a jornada de build/browser possui limite de concorrencia e nao disputa
  memoria com outra suite pesada.

### `CODE-E2E-05`. Disponibilizar laboratorio humano

Implementacao:

- integrar coding ao laboratorio web de validacao por um provider registrado,
  sem introduzir condicionais de code no fluxo de quiz;
- permitir preparar, atualizar, verificar e resetar cenarios pela interface;
- oferecer URLs e credenciais locais curtas para cada persona;
- mostrar checkpoint, recursos criados, capability e diagnosticos;
- manter comandos destrutivos limitados ao manifest do cenario.

Gate:

- o fluxo humano nao exige CLI especifica de coding;
- quiz e code podem contribuir cenarios sem depender um do outro;
- a interface nao exibe segredo ou private test;
- cleanup e verificacao usam as mesmas regras do E2E.

### `CODE-E2E-06`. Cutover e auditoria final

Implementacao:

- executar o catalogo completo;
- revisar reports e promover status manualmente;
- atualizar o registro de duplicacoes com os arquivos efetivamente clonados;
- remover somente implementacoes transitorias criadas dentro do novo fluxo;
- preservar `@game-guild/emception-ui`, seus testes e seus consumidores;
- preservar `tools/emception/**`, suas capabilities e todos os consumidores
  que nao pertencem ao novo fluxo de grading;
- atualizar documentos de tipos e arquitetura;
- registrar blockers no subplano da linguagem correspondente na parte 5;
- registrar questoes de Code Studio no plano 06, sem mistura-las ao aceite;
- manter o runner anterior ate que todos os seus checks tenham case ID e oracle
  equivalente, registrando qualquer probe de banco restante como divida do
  harness e nunca como prova de aceite.

Gate:

- todos os casos obrigatorios estao `pass` ou possuem blocker externo aceito;
- os fluxos de grading selecionados usam `coding-surface`;
- consumidores de `emception-ui` fora do corte continuam funcionais;
- toda duplicacao conhecida esta registrada e classificada;
- nao existem condicionais de code no core generico;
- validacao humana e automatica exercitam o mesmo produto;
- CI executa o ciclo para mudancas nos novos packages e no `CodeAdapter`, sem
  classifica-las automaticamente como mudanca de artifact Emception.

### `CODE-E2E-07`. Integrar as partes 01 a 04 em `develop`

Executar somente depois que todos os gates das partes 01, 02, 03 e 04 estiverem
fechados.

Implementacao:

- atualizar a branch de trabalho com a `develop` mais recente e resolver
  conflitos preservando os contratos e ownership definidos neste roadmap;
- executar novamente contratos, testes backend, testes dos packages, ciclo
  browser e verificacoes de arquitetura sobre o diff final;
- revisar o diff completo para excluir trabalho das partes 05 e 06 e mudancas
  alheias ao programa;
- abrir um unico PR para `develop` cobrindo conjuntamente as partes 01 a 04;
- anexar ao PR os comandos executados, resultados, case IDs, riscos residuais e
  blockers externos aceitos.

Gate:

- todas as partes 01 a 04 estao concluidas, nao apenas implementadas;
- o diff do PR corresponde ao dominio, adapter, backend, superficie e E2E
  aprovados por estes planos;
- a suite final passa sobre a branch ja reconciliada com `develop`;
- o PR nao inclui expansao de linguagem nem refundacao do Code Studio;
- nenhuma falha da parte alterada e mascarada como preexistente.

## Validacao sugerida

Os comandos finais devem ser consolidados em scripts estaveis, por exemplo:

```bash
pnpm --filter @game-guild/coding test
pnpm --filter @game-guild/grading-adapter-code test
pnpm --filter @game-guild/coding-surface test
pnpm --filter @game-guild/emception-ui test
dotnet test apps/api/tests/GameGuild.Learning.Assessments.UnitTests/GameGuild.Learning.Assessments.UnitTests.csproj --filter FullyQualifiedName~Code
node --test apps/web/scripts/coding-cycle-grading-observer.test.mjs apps/web/scripts/coding-cycle-grading-transport.test.mjs apps/web/scripts/coding-cycle-web-runtime.test.mjs
pnpm --filter @game-guild/web test:browser:coding-cycle
dotnet build apps/api/GameGuild.sln
pnpm --filter @game-guild/web build
```

Quando a nova suite existir, o repositorio deve oferecer comandos separados de
contracts, smoke e full. O smoke nao pode substituir o full no cutover.
O ciclo browser, por construir e iniciar a aplicacao isoladamente, deve rodar
como job pesado unico ou com concorrencia explicitamente limitada.

## Criterios de conclusao

- fluxo author -> publish -> learner -> review -> release -> learner esta
  comprovado;
- author test nao gera efeitos academicos;
- automated e instructor review possuem evidencia e auditoria;
- privacidade e autorizacao tem casos negativos;
- versoes congeladas continuam executaveis;
- o ciclo anterior permanece verde ate existir paridade comprovada no catalogo;
- nenhum aceite final depende de leitura direta do banco;
- laboratorio humano e Playwright usam lifecycle oficial;
- catalogo registra gaps sem mascarar falhas;
- cutover ativou a nova superficie de grading sem alterar ou remover
  `@game-guild/emception-ui`;
- nenhuma fonte, capability ou comportamento default do Emception foi alterado
  pelo cutover;
- registro de duplicacoes esta completo e pronto para revisao com seu
  responsavel;
- as partes 01 a 04 estao prontas em um PR unico e validado para `develop`.
