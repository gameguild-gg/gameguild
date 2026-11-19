# Parte 3.3. Perfis de exercicio e fronteiras por persona

Status: concluido em 2026-11-18.

IDs preservados: `CODE-UI-03` e `CODE-UI-04`.

## Objetivo

Implementar as experiencias iniciais `function` e `standard-io` sobre o shell
do `coding-surface`, com contratos discriminados e fronteiras explicitas para
`author`, `author-test`, `learner` e `grader`. Esta etapa torna a superficie
funcional antes de acopla-la as rotas reais.

Pre-requisito: gate da
[`parte 3.2`](./02-coding-surface-foundation.md) concluido e registrado.

## Protocolo de inicio

- ler os arquivos de contexto e validar novamente os contratos executaveis;
- confirmar que nenhuma mudanca posterior da `develop` invalidou a baseline;
- conferir se o package continua isolado e se seus exports publicos sao os
  registrados;
- registrar qualquer decisao versionada antes de alterar o contrato canonico;
- acionar `EMCEPTION-GATE` antes de contornar uma capability ausente.

## `CODE-UI-03`. Catalogo extensivel de exercicios

### Contrato canonico

- evoluir de forma versionada `@game-guild/coding` para carregar um
  discriminante explicito de tipo quando o contrato atual ainda nao o fizer;
- preservar roundtrip definition-delivery-submission sem inferir tipo por
  `tests[].kind`, extensao, linguagem ou toolchain;
- manter tipos de teste como mecanismo de expectativa e tipo de exercicio como
  experiencia de produto;
- nunca representar todos os tipos por um objeto com muitos campos opcionais;
- atualizar o adapter TypeScript apenas na fronteira necessaria, preservando a
  independencia de grading e as versoes congeladas anteriores.

### Registro de perfis

- criar registro tipado e exaustivo que associa cada tipo suportado aos
  renderers `author`, `author-test`, `learner` e `grader`;
- declarar paineis e capabilities publicas requeridas por perfil;
- falhar com diagnostico explicito quando o contrato conhece um tipo sem
  renderer registrado;
- exigir novo membro tipado, renderers, fixture e teste para adicionar um tipo;
- evitar switches duplicados em varios componentes.

### Perfil `function`

- orientar autoria a enunciado, assinatura, codigo inicial, casos publicos e
  expectativas privadas;
- apresentar ao learner somente assinatura, starter code e casos autorizados;
- mostrar resultado por caso e diagnostics sem revelar private tests;
- produzir submission e evidencia tipadas para o adapter, sem calcular nota na
  superficie.

### Perfil `standard-io`

- orientar autoria a stdin, stdout esperado, stderr e exit code;
- oferecer ao learner execucao local com entradas publicas autorizadas;
- apresentar comparacao por teste e diagnostics;
- produzir submission e evidencia tipadas, sem tratar output local como nota
  oficial.

### Tipos futuros

- caracterizar `project`, `debugging`, `visual` e `sql-query` somente contra
  contratos e capabilities existentes;
- nao implementar nem exibir um tipo incompleto;
- compor preview web ou canvas apenas quando perfil e API publica declararem a
  capability;
- registrar necessidade real de novo contrato ou capability antes de expandir
  o escopo.

## `CODE-UI-04`. Fronteiras por persona

### `author`

- recebe definicao completa e produz draft validado;
- pode editar expectativas privadas sem envia-las a outra persona;
- nao publica ou persiste por conta propria.

### `author-test`

- recebe revisao candidata completa;
- executa preview do instrutor sem enrollment, attempt, gradebook ou release;
- nunca compartilha a mesma execucao de uma tentativa oficial.

### `learner`

- recebe somente delivery redigida, public tests e comandos autorizados;
- preview e rotulado como local e nao produz nota oficial;
- private tests, expected outputs privados e rubrica nao aparecem em props,
  DOM, logs, fixtures de browser ou storage;
- submissao ocorre apenas pelo callback fornecido pelo host.

### `grader`

- recebe snapshot, submission e evidencias autorizadas;
- pode inspecionar e solicitar reexecucao quando a API permitir;
- review e release continuam comandos do host;
- diferencia teste falho, executor indisponivel e resultado nao publicado.

### Estado e reidratacao

- storage segue o namespace definido no subplano 3.2;
- refresh reidrata a fonte do servidor por props do host;
- nenhum score ou feedback local substitui o resultado oficial;
- trocar modo nao concede permissao nem amplia os dados disponiveis.

## Fixtures e testes

- manter fixtures separadas por tipo e modo;
- cobrir roundtrip definition-delivery-submission dos dois tipos;
- testar todos os quatro modos para `function` e `standard-io`;
- testar tipo desconhecido ou sem renderer;
- testar que trocar tipo nao troca toolchain, persona ou review;
- testar ausencia de private data no learner em props serializaveis, DOM e
  storage;
- testar que author test nao emite intent academica;
- testar que nenhum renderer le ou emite `AIReview`, `InstructorReview`,
  `PeerReview` ou outra politica de grading.

## Fora do escopo

- migrar rotas reais de `apps/web`;
- adicionar linguagens;
- implementar tipos futuros incompletos;
- alterar politica de review;
- modificar Emception ou `@game-guild/emception-ui`;
- alterar tabelas ou migrations.

## Validacao

```bash
pnpm --filter @game-guild/coding test
pnpm --filter @game-guild/coding typecheck
pnpm --filter @game-guild/grading-adapter-code test
pnpm --filter @game-guild/grading-adapter-code typecheck
pnpm --filter @game-guild/coding-surface test
pnpm --filter @game-guild/coding-surface typecheck
```

Executar ainda os testes de privacidade e browser do package que forem criados.
Registrar resultados e eventuais indisponibilidades reais no estado.

## Gate de conclusao

- discriminante sobrevive ao roundtrip sem heuristica;
- `function` e `standard-io` possuem os quatro renderers e fixtures;
- private tests nao chegam ao learner por nenhum canal coberto;
- author test nao cria efeitos academicos;
- preview local nao e apresentado como grade oficial;
- tipos nao registrados falham explicitamente;
- politica de review nao integra o estado ou o catalogo da superficie;
- nenhuma capability privada ou modificacao do Emception foi introduzida;
- `DECISIONS.md`, gaps e estado refletem qualquer evolucao contratual;
- `IMPLEMENTATION-STATE.md` aponta o subplano 3.4 como proxima acao.

Somente apos este gate os consumidores reais podem ser migrados.
