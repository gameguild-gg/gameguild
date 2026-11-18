# Parte 3.1. Caracterizacao e baseline de decisoes

Status: planejado.

ID preservado: `CODE-UI-01`.

## Objetivo

Caracterizar o fluxo atual de coding assessment antes de criar ou migrar UI.
Esta etapa transforma fatos espalhados no repositorio em um mapa verificavel de
consumidores, contratos, testes, copias e capabilities. Ela nao altera a
experiencia de produto.

Pre-requisitos:

- partes [`01`](../01-domain-contracts-and-typescript-adapter.md) e
  [`02`](../02-backend-code-adapter-boundary.md) concluidas;
- leitura do [indice da parte 3](../03-coding-surface-and-web-integration.md);
- worktree compreendido e sem merge ou conflito oculto.

## Protocolo de inicio

Antes de editar qualquer produto:

1. registrar o `HEAD`, a branch e mudancas preexistentes em
   [`IMPLEMENTATION-STATE.md`](./IMPLEMENTATION-STATE.md);
2. conferir os contratos e exports realmente presentes em `@game-guild/coding`,
   `@game-guild/grading-adapter-code` e Emception;
3. ler as decisoes aceitas e nao reabrir uma decisao sem nova evidencia;
4. levantar os consumidores por busca no repositorio, nao por memoria;
5. separar fatos atuais, destino planejado e decisao ainda aberta.

## Implementacao

### Consumidores e ownership

- listar todos os imports de `@game-guild/emception-ui` e de seus subpaths;
- listar imports diretos dos packages publicos do Emception usados pelos fluxos
  de coding assessment;
- mapear autoria de definicao, author test, learner activity, assessment grader
  e SpeedGrader;
- identificar consumidores fora de grading que devem permanecer no package
  preservado;
- registrar para cada consumidor sua rota, modo, dados recebidos, callbacks,
  storage, testes e destino em [`CONSUMER-MAP.md`](./CONSUMER-MAP.md);
- classificar cada item como `migrar`, `preservar` ou `investigar`, sempre com
  evidencia de path.

### Contratos e comportamento atual

- registrar props e callbacks realmente usados nos quatro modos;
- caracterizar arquivos editaveis e read-only, public/private tests, score de
  preview, workspace storage e reidratacao;
- localizar o limite entre preview local e submission oficial;
- identificar onde auth, tenant, fetch, mutation, retry e navegacao vivem;
- capturar testes que ja protegem privacidade, execucao e callbacks;
- registrar bugs conhecidos separadamente, para que nao sejam promovidos a
  contrato por um characterization test.

### Copias e Emception

- classificar cada arquivo relevante de `@game-guild/emception-ui` como
  `candidato a copia`, `referencia somente` ou `fora do escopo`;
- registrar aliases e workarounds de bundler que pertencem ao package
  preservado e nao podem ser removidos;
- preencher o mapa inicial no
  [`DUPLICATION-REGISTER.md`](../DUPLICATION-REGISTER.md);
- testar se cada necessidade pode ser atendida pelos exports publicos do
  Emception;
- registrar gaps confirmados em
  [`EMCEPTION-CAPABILITY-GAPS.md`](./EMCEPTION-CAPABILITY-GAPS.md), sem criar
  import privado ou alterar upstream.

### Baseline de decisoes

- registrar em [`DECISIONS.md`](./DECISIONS.md) somente escolhas que precisem
  sobreviver aos demais subplanos;
- incluir contexto, alternativas, decisao, consequencias, evidencia e planos
  afetados;
- marcar decisao incerta como `proposta`, nunca como aceita por conveniencia;
- nao usar o Code Studio antigo como fonte normativa de requisitos.

## Artefatos obrigatorios

Ao final, os seguintes arquivos devem refletir o repositorio observado:

- `CONSUMER-MAP.md` sem consumidor de grading conhecido ausente;
- `DUPLICATION-REGISTER.md` com candidatos e origens conhecidos;
- `EMCEPTION-CAPABILITY-GAPS.md` com gaps provados ou declaracao explicita de
  que nenhum gap foi confirmado;
- `DECISIONS.md` com decisoes transversais descobertas;
- `IMPLEMENTATION-STATE.md` com baseline, evidencias e proxima acao.

## Fora do escopo

- criar `@game-guild/coding-surface`;
- migrar imports ou rotas;
- alterar contratos persistidos;
- modificar `@game-guild/emception-ui` ou Emception;
- corrigir bugs sem antes separa-los da caracterizacao;
- desenhar tipos futuros alem do necessario para confirmar as fronteiras.

## Validacao

- executar buscas reproduziveis dos imports e guardar os comandos no estado;
- executar os testes atuais diretamente relacionados aos consumidores
  caracterizados quando o ambiente permitir;
- conferir que nenhuma alteracao de produto entrou no diff desta etapa;
- executar `git diff --check`.

## Gate de conclusao

- todo consumidor de grading selecionado possui path e destino no mapa;
- consumidores fora desse escopo permanecem associados ao package preservado;
- ownership de efeitos academicos esta explicito;
- public/private data e storage estao caracterizados por persona;
- candidatos a copia e aliases preservados estao registrados;
- nenhum requisito foi inferido do Code Studio antigo;
- nenhuma fonte de Emception ou `@game-guild/emception-ui` foi alterada;
- o checkpoint foi gravado em `IMPLEMENTATION-STATE.md` com o subplano 3.2
  como proxima acao.

Somente apos este gate `CODE-UI-02` pode comecar.
