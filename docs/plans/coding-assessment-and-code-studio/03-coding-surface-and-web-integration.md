# Parte 3. Coding surface e integracao web

Status: planejado; dividido em quatro subplanos sequenciais.

## Objetivo

Criar `@game-guild/coding-surface` como uma nova superficie React reutilizavel
e especifica para coding assessment e grading. A superficie deve compor as APIs
publicas do Emception, os contratos de coding e o adapter de grading sem assumir
responsabilidade por rota, sessao, autorizacao ou persistencia academica.

A superficie nao sera uma IDE generica exibida do mesmo modo para todo
exercicio. Ela fornecera uma base comum de editor, arquivos, terminal, resultados
de testes e preview visual, composta por um perfil tipado para cada formato de
exercicio suportado, como funcao no modelo LeetCode ou entrada e saida.

`@game-guild/emception-ui` permanece preservado e independente. Quando for
util, composicao GameGuild existente pode ser copiada para `coding-surface`,
desde que a origem, a divergencia e o commit-base sejam registrados. A parte 3
nao modifica, move, deprecia nem remove o package preservado.

Pre-requisitos:

- [`01`](./01-domain-contracts-and-typescript-adapter.md) concluido;
- [`02`](./02-backend-code-adapter-boundary.md) concluido;
- API publica do Emception necessaria ja disponivel.

## Por que esta parte foi dividida

O plano anterior reunia caracterizacao, criacao de package, evolucao de
contratos, duas experiencias de exercicio, quatro personas, migracao web,
regressao paralela e CI. Esses trabalhos possuem riscos e evidencias diferentes.
Executa-los como uma unica unidade tornaria dificil revisar decisoes, localizar
regressoes e retomar a implementacao depois de mudancas de contexto ou merges.

Os subplanos abaixo sao gates de implementacao, nao entregas independentes para
`develop`. A estrategia do roadmap continua sendo concluir as partes 01, 02, 03
e 04 antes do PR integrado.

## Sequencia obrigatoria

| Ordem | Subplano | IDs preservados | Resultado exigido |
| --- | --- | --- | --- |
| 3.1 | [`Caracterizacao e baseline de decisoes`](./03-coding-surface/01-characterization-and-decision-baseline.md) | `CODE-UI-01` | consumidores, contratos, copias e gaps conhecidos |
| 3.2 | [`Fundacao do coding-surface`](./03-coding-surface/02-coding-surface-foundation.md) | `CODE-UI-02` | package isolado, shell e API publica testados |
| 3.3 | [`Perfis de exercicio e fronteiras por persona`](./03-coding-surface/03-exercise-profiles-and-persona-boundaries.md) | `CODE-UI-03`, `CODE-UI-04` | `function` e `standard-io` completos nos quatro modos |
| 3.4 | [`Integracao web, estabilizacao e CI`](./03-coding-surface/04-web-integration-stabilization-and-ci.md) | `CODE-UI-05`, `CODE-UI-06`, `CODE-UI-07` | consumidores de grading migrados e regressao fechada |

Um subplano so pode comecar depois que o gate anterior estiver marcado como
concluido em
[`IMPLEMENTATION-STATE.md`](./03-coding-surface/IMPLEMENTATION-STATE.md), com
evidencias reproduziveis. Marcar uma tarefa em um texto sem executar seu gate
nao autoriza o avanco.

## Protocolo de contexto persistente

A implementacao nao pode depender da memoria da conversa ou de uma unica IA.
Qualquer IA ou pessoa que execute um subplano deve persistir as decisoes e o
checkpoint nos arquivos abaixo, em vez de apenas relata-los na conversa. Antes
de iniciar qualquer subplano, o executor deve ler:

1. este indice e o subplano ativo;
2. [`IMPLEMENTATION-STATE.md`](./03-coding-surface/IMPLEMENTATION-STATE.md);
3. [`DECISIONS.md`](./03-coding-surface/DECISIONS.md);
4. [`CONSUMER-MAP.md`](./03-coding-surface/CONSUMER-MAP.md);
5. [`EMCEPTION-CAPABILITY-GAPS.md`](./03-coding-surface/EMCEPTION-CAPABILITY-GAPS.md);
6. [`DUPLICATION-REGISTER.md`](./DUPLICATION-REGISTER.md).

### Arquivos canonicos da execucao

| Arquivo | Conteudo | Regra de atualizacao |
| --- | --- | --- |
| `IMPLEMENTATION-STATE.md` | baseline, etapa ativa, evidencias, bloqueios e proxima acao | atualizar ao iniciar, interromper e concluir cada subplano |
| `DECISIONS.md` | decisoes arquiteturais aceitas, substituidas ou rejeitadas | registrar antes de implementar uma escolha com efeito entre subplanos |
| `CONSUMER-MAP.md` | consumidor atual, ownership, destino, estado e teste de regressao | atualizar em toda descoberta ou migracao de consumidor |
| `EMCEPTION-CAPABILITY-GAPS.md` | capabilities publicas ausentes e resolucao aprovada | atualizar somente com evidencia de um gap real |
| `DUPLICATION-REGISTER.md` | codigo copiado, origem, commit-base e divergencia | atualizar no mesmo commit que cria ou altera uma copia |

O codigo e os testes continuam sendo a fonte de verdade dos contratos
executaveis. Os arquivos acima preservam contexto, razoes e evidencias; eles nao
podem contradizer silenciosamente o repositorio. Quando houver divergencia, o
executor deve verificar o codigo, registrar a correcao documental e nao inferir
o estado desejado pela memoria da conversa.

### Checkpoint obrigatorio

Antes de encerrar uma sessao ou declarar um gate concluido:

- registrar o commit-base e o estado do worktree relevante;
- registrar arquivos e contratos alterados;
- registrar comandos executados e seus resultados, sem afirmar sucesso para
  comandos nao executados;
- registrar bloqueios, decisoes ainda abertas e o proximo passo exato;
- atualizar o mapa de consumidores, gaps e duplicacoes quando aplicavel;
- manter como `em andamento` qualquer subplano cujo gate esteja incompleto.

Decisoes triviais de implementacao local nao entram em `DECISIONS.md`. Entram
ali somente escolhas que alterem fronteiras, contratos publicos, ownership,
privacidade, compatibilidade, sequencia ou escopo de outro subplano.

## Fronteiras invariantes

- `coding-surface` pode depender de `@game-guild/coding`,
  `@game-guild/grading-adapter-code` e exports publicos do Emception;
- `coding-surface` nao depende de `@game-guild/emception-ui`, `apps/web`,
  Next.js, client gerado ou server actions;
- `apps/web` possui auth, tenant, fetch, mutations, conflitos, retry e
  navegacao;
- preview local nunca cria submission, score, gradebook ou release;
- a nota oficial e produzida somente pelo pipeline servidor de grading;
- politica de review pertence a `@game-guild/grading` e nao ao catalogo da UI;
- nenhum import por `src/**` ou alias privado do Emception e permitido;
- nenhuma tabela, migration ou fonte do Emception e alterada nesta parte.

Os entrypoints publicos inicialmente permitidos sao `emception`,
`@gameguild/emception-browser` e `@gameguild/emception-ide`. Outro package
publico do Emception exige uma capability concreta registrada; sua existencia
no workspace nao e justificativa suficiente para adiciona-lo.

```mermaid
flowchart LR
    CODING["@game-guild/coding"]
    GRADING["@game-guild/grading"]
    ADAPTER["@game-guild/grading-adapter-code"]
    EMCEPTION["Exports publicos do Emception"]
    SURFACE["@game-guild/coding-surface"]
    EXISTING["@game-guild/emception-ui\npreservado e independente"]
    WEB["apps/web"]

    SURFACE --> CODING
    ADAPTER --> GRADING
    ADAPTER --> CODING
    SURFACE --> ADAPTER
    SURFACE --> EMCEPTION
    EXISTING --> EMCEPTION
    WEB --> SURFACE
```

## Fora do escopo transversal

- alterar componentes internos, defaults ou fontes do Emception;
- editar, mover, renomear, consolidar, depreciar ou remover
  `@game-guild/emception-ui`;
- remover seus aliases, testes ou consumidores fora do grading selecionado;
- criar outro editor, terminal, filesystem, canvas, toolchain ou runner;
- adicionar linguagens;
- expor tipos futuros ou incompletos como opcoes desabilitadas;
- definir ou duplicar politicas de review;
- definir a UX futura do Code Studio ou preservar collections/assets antigos;
- colocar fetch, cookies, tenant ou router dentro do package;
- alterar tabelas ou migrations.

## Modelo da superficie

### Eixos independentes

O tipo de exercicio determina a experiencia de autoria e resolucao. Ele nao
deve ser confundido com:

- **toolchain ou linguagem:** C++, JavaScript, TypeScript, Ruby, SQL ou C#;
- **capability de execucao:** terminal, test runner, preview web, canvas ou
  result set;
- **persona ou modo:** author, author-test, learner ou grader;
- **politica de review:** propriedade integral de `@game-guild/grading`.

Uma experiencia de funcao pode aceitar mais de uma toolchain. Um preview visual
pode ser usado por exercicios diferentes sem se tornar um workflow de grading.

### Catalogo alvo

| Tipo | Experiencia principal | Evidencia apresentada pela superficie |
| --- | --- | --- |
| `function` | funcao com assinatura e casos de teste, no modelo LeetCode | casos publicos, resultado por caso e diagnostics |
| `standard-io` | programa que recebe entrada e produz saida | stdin, stdout, stderr, exit code e testes |
| `project` | workspace com multiplos arquivos e suite de testes | terminal, diagnostics e arvore de resultados |
| `debugging` | codigo inicial com falha que deve ser corrigido | falha inicial, execucoes e testes de regressao |
| `visual` | programa com saida web, DOM ou canvas | preview isolado, console e validacoes disponiveis |
| `sql-query` | consulta sobre schema e dados controlados | result set, diagnostics e comparacao esperada |

Somente `function` e `standard-io` pertencem ao corte inicial obrigatorio. Cada
tipo adicional so se torna selecionavel quando possuir contrato canonico
versionado, renderers dos quatro modos, evidencia executavel e testes E2E. Tipos
futuros nao aparecem como opcoes desabilitadas e nao recuam para outro tipo.

### Modos

| Modo | Dados recebidos | Acao permitida | Efeito academico |
| --- | --- | --- | --- |
| `author` | draft completo | editar e validar definicao | nenhum |
| `author-test` | revisao candidata completa | executar preview | nenhum |
| `learner` | delivery redigida | editar, executar preview publico e submeter | somente pela API oficial |
| `grader` | snapshot, submission e evidencias autorizadas | inspecionar e revisar | somente por comando da API |

O modo visual nao concede permissao. A aplicacao entrega apenas os dados que a
API autorizou para a persona. Modos e tipos de exercicio sao ortogonais e nenhum
deles escolhe uma politica de review.

### Commands e estado local

A superficie emite intencoes tipadas como `onDefinitionChange`, `onPreviewRun`,
`onSubmit` e `onReviewDecision`; ela nao chama endpoints. Workspace local pode
preservar arquivos de trabalho, mas nao substitui draft, submission ou grade no
servidor. A chave deve ser namespaced por tenant, assessment revision, actor e
attempt. Dados privados nunca entram no storage do learner.

## `EMCEPTION-GATE`

Se a superficie precisar de funcao ausente na API publica:

1. registrar a capability, caso de uso, consumidor e evidencia em
   `EMCEPTION-CAPABILITY-GAPS.md`;
2. provar que composicao pelos exports publicos nao resolve;
3. definir fallback ou reducao de escopo;
4. levar a proposta ao responsavel pelo Emception;
5. nao importar internals nem editar `tools/emception/**` nesta parte;
6. nao editar `@game-guild/emception-ui` para expor a capability;
7. aceitar evolucao upstream somente por decisao separada, aditiva e sujeita ao
   `EMCEPTION-NON-REGRESSION-GATE` do roadmap.

Encontrar um gap nao autoriza contorna-lo. O subplano permanece bloqueado no
ponto afetado enquanto outras tarefas independentes podem continuar.

## Validacao consolidada

Os subplanos definem seus testes incrementais. Antes de concluir toda a parte 3,
executar ao menos:

```bash
pnpm --filter @game-guild/coding-surface test
pnpm --filter @game-guild/coding-surface typecheck
pnpm --filter @game-guild/emception-ui test
pnpm --filter @game-guild/web test -- src/components/learning
pnpm --filter @game-guild/web test:browser:coding-cycle
pnpm --filter @game-guild/web build
```

O filtro Vitest deve corresponder aos arquivos realmente tocados. O teste
browser usa personas separadas quando cruza author, learner e grader.

## Criterios de conclusao

- os quatro subplanos estao concluidos com evidencias no estado persistente;
- autoria, author test, learner e grader de code usam `coding-surface`;
- `function` e `standard-io` oferecem experiencias proprias sobre o mesmo shell;
- o catalogo aceita novos tipos sem acoplar linguagem, persona ou review;
- nenhuma informacao privada aparece no contexto learner;
- Emception e consumido somente por APIs publicas;
- app mantem ownership de API, auth e navegacao;
- `@game-guild/emception-ui`, seus aliases, consumidores fora de grading e
  testes permanecem funcionais;
- duplicacoes, divergencias, gaps e decisoes estao registrados;
- CI valida a nova superficie como consumidor sem redefinir release do
  Emception;
- nenhuma tabela, migration ou fonte do Emception foi alterada.
