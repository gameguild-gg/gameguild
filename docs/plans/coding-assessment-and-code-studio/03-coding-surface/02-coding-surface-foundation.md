# Parte 3.2. Fundacao do `coding-surface`

Status: concluido em 2026-11-18.

ID preservado: `CODE-UI-02`.

## Objetivo

Criar `@game-guild/coding-surface` como package React isolado, com API publica,
shell visual, controllers e estados operacionais suficientes para receber os
perfis de exercicio. Esta etapa fecha a fronteira do package sem migrar as rotas
de `apps/web` e sem implementar antecipadamente o catalogo futuro.

Pre-requisito: gate da
[`parte 3.1`](./01-characterization-and-decision-baseline.md) concluido e
registrado no estado persistente.

## Protocolo de inicio

- ler todos os arquivos de contexto listados no indice da parte 3;
- confirmar que o mapa de consumidores ainda corresponde ao repositorio depois
  de merges;
- registrar o novo `HEAD` e qualquer divergencia da baseline;
- resolver divergencia documental antes de usar o mapa para implementar;
- confirmar os entrypoints publicos do Emception instalados nesta revisao.

## Implementacao

### Package e dependencias

- criar `packages/features/coding-surface` seguindo os padroes dos packages de
  feature existentes;
- depender somente de `@game-guild/coding`,
  `@game-guild/grading-adapter-code`, React e entrypoints publicos do Emception
  necessarios ao corte confirmado;
- nao importar `@game-guild/emception-ui`, `apps/web`, Next.js, client gerado,
  server actions ou internals `src/**` de outro package;
- separar controllers puros, componentes React, estilos, fixtures e exports de
  testing;
- expor uma API publica minima e testar seus exports.

### Shell comum

- criar areas estaveis para enunciado, workspace/editor, arquivos, terminal,
  resultados e preview;
- ativar paineis por capabilities tipadas do perfil, sem condicionais de
  linguagem ou politica de review espalhadas na arvore;
- manter dimensoes responsivas estaveis e impedir que loading, labels ou output
  redimensionem a ferramenta de forma incoerente;
- cobrir acessibilidade de teclado, foco e nomes acessiveis;
- implementar estados de loading, vazio, erro, indisponivel e read-only;
- distinguir erro de configuracao, erro de executor e teste falho.

### Controllers e intents

- definir os modos `author`, `author-test`, `learner` e `grader` como contratos
  visuais tipados, sem inferir autorizacao pelo modo;
- emitir intents como `onDefinitionChange`, `onPreviewRun`, `onSubmit` e
  `onReviewDecision` sem chamar endpoints;
- manter efeitos academicos fora do package;
- definir uma fronteira de registro de perfis que falhe explicitamente para um
  tipo sem renderer;
- evitar uma estrutura unica com muitos campos opcionais.

### Estado local

- definir a porta de storage sem acoplar uma implementacao de browser ao core
  dos controllers;
- exigir namespace por tenant, assessment revision, actor e attempt;
- impedir que score, grade, release ou private tests sejam tratados como estado
  local autoritativo;
- registrar qualquer composicao copiada no `DUPLICATION-REGISTER.md` no mesmo
  commit, incluindo arquivo de origem e commit-base.

## Corte desta entrega

O package pode usar fixtures sinteticas para provar o shell e sua API. A
experiencia completa de `function` e `standard-io`, a evolucao versionada do
discriminante e os testes cruzados por persona pertencem ao subplano 3.3. Esta
etapa nao exibe tipos futuros nem cria fallbacks temporarios que depois virem
contrato.

## Fora do escopo

- migrar consumidor de `apps/web`;
- editar ou remover aliases de `@game-guild/emception-ui`;
- recriar IDE, VFS, terminal, canvas, toolchain ou runner;
- implementar `project`, `debugging`, `visual` ou `sql-query`;
- decidir politica de review;
- alterar tabelas, migrations ou Emception.

## Validacao

```bash
pnpm --filter @game-guild/coding-surface test
pnpm --filter @game-guild/coding-surface typecheck
```

Adicionar tambem os checks padrao de lint e build de package realmente
disponiveis no workspace. Registrar comandos e resultados no estado; nao
inventar um comando ausente para satisfazer o plano.

## Gate de conclusao

- package testa, typechecka e resolve seus entrypoints isoladamente;
- API publica possui teste de exports;
- shell e estados operacionais possuem testes de componente;
- package nao importa app, Next.js, client, server actions, internals ou
  `@game-guild/emception-ui`;
- nenhum runner, IDE, VFS ou terminal foi duplicado;
- toda copia de composicao GameGuild esta registrada;
- diff em `packages/infrastructure/ui-emception/**` e fontes do Emception esta
  vazio;
- `IMPLEMENTATION-STATE.md` registra evidencias e aponta o subplano 3.3 como
  proxima acao.

Somente apos este gate os perfis completos podem ser implementados.
