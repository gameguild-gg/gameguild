# Preparacao 0. Remocao da dependencia legada do Code Studio

Status: implementado localmente em 2026-11-10; PR exclusivo para `develop`
pendente.

## Objetivo

Remover `@codingame/monaco-vscode-files-service-override` do app web sem
refazer o Code Studio e sem misturar essa correcao com AIReview.

Este e um desbloqueio pequeno e independente. Ele existe porque a dependencia
legada impede o uso confiavel do app com `dev:fast`, necessario para validar o
fluxo humano de AIReview. A refundacao ou remocao completa do Code Studio
continua pertencendo ao plano 06.

## Evidencia atual

- a dependencia direta esta em `apps/web/package.json`;
- o unico import direto encontrado esta em
  `extras/code-studio/monaco-file-system.ts`;
- `CodeStudioEditor` ainda chama initialize, sync, update e dispose desse
  filesystem;
- existem consumidores confirmados do Code Studio e de seu editor Monaco no
  block editor, preview, blog e lesson;
- a dependencia veio da baseline de `develop`; ela nao pertence ao diff de
  AIReview.

Por isso, remover todo o Code Studio agora nao e uma correcao segura. Somente a
dependencia e sua integracao de filesystem entram neste corte.

## Escopo

- caracterizar o comportamento realmente usado do filesystem virtual;
- substituir o overlay da Codingame por uma composicao interna sobre APIs
  publicas do Monaco, limitada a models, URIs e extra libs necessarios;
- preservar os contratos atuais consumidos por `CodeStudioEditor`;
- remover a dependencia do package e lockfile depois de zerar os imports;
- validar `dev:fast`, build web e consumidores atuais;
- entregar a correcao em PR proprio para `develop`.

## Fora do escopo

- implementar `@game-guild/code-studio`;
- migrar o Code Studio para Emception;
- alterar node, JSON, collections, assets, layouts ou runners;
- remover consumidores atuais;
- alterar grading, AIReview ou `GameGuild.AI`;
- alterar `@game-guild/emception-ui` ou fontes do Emception.

## Etapas

### `STUDIO-UNBLOCK-01`. Caracterizar o uso atual

- cobrir initialize, sync, update, delete e dispose;
- verificar quais operacoes dependem realmente do overlay VS Code;
- registrar comportamento de multiplas instancias e TypeScript extra libs;
- executar os testes atuais antes da troca.

Gate: nenhuma capacidade e removida apenas porque o codigo parece legado.

### `STUDIO-UNBLOCK-02`. Substituir a integracao de filesystem

- criar uma implementacao interna pequena usando somente APIs publicas do
  Monaco;
- manter isolamento por `instanceId`;
- manter models e extra libs sincronizados;
- garantir dispose idempotente e sem recursos residuais;
- preservar a API interna consumida pelo editor quando ela ainda for util.

Gate: os consumidores atuais continuam funcionando sem o package da Codingame.

### `STUDIO-UNBLOCK-03`. Remover a dependencia

- remover imports estaticos e dinamicos da Codingame;
- remover a dependencia de `apps/web/package.json`;
- atualizar `pnpm-lock.yaml` pelo gerenciador de packages;
- confirmar por busca que nenhum consumidor restante exige o package.

Gate: package, lockfile e fontes nao possuem referencia residual.

### `STUDIO-UNBLOCK-04`. Validar e integrar em `develop`

- executar testes unitarios do filesystem/editor e do block editor;
- iniciar `dev:fast` e abrir sign-in, grading validation e um consumidor atual
  do editor;
- executar o build web dentro dos limites de memoria documentados;
- abrir um PR exclusivo desta remocao para `develop`;
- integrar o PR antes da validacao real e do PR de AIReview.

Gate:

- o PR nao contem AIReview, coding assessment ou refundacao do Code Studio;
- Turbopack nao tenta resolver o package removido;
- consumidores atuais nao apresentam regressao conhecida;
- CI e validacao local passam sobre a branch reconciliada com `develop`.

## Criterios de conclusao

- `@codingame/monaco-vscode-files-service-override` foi removido;
- o comportamento ainda usado possui substituto testado;
- `dev:fast` permite validar AIReview;
- o Code Studio antigo nao foi apagado antes do discovery;
- a correcao foi integrada por PR proprio em `develop`.

## Resultado da implementacao local

- o overlay da Codingame foi substituido por um registry interno baseado nas
  APIs publicas de models e URIs do Monaco;
- workspaces, models, extra libs e completions agora sao isolados por
  `instanceId`;
- update, delete, diretorios explicitos, refcount e dispose idempotente possuem
  cobertura dedicada;
- `@codingame/monaco-vscode-files-service-override` e o uso direto de
  `vscode-uri` foram removidos de `apps/web/package.json` e do lockfile;
- nenhum arquivo de Emception, grading, JSON, runner ou UI do Code Studio foi
  alterado fora da integracao Monaco necessaria.

Validacoes executadas:

```text
Vitest Code Studio: 12 arquivos, 87 testes aprovados
ESLint direcionado: aprovado
TypeScript direcionado ao registry Monaco: aprovado
Turbopack isolado: pronto em 387 ms; /sign-in respondeu HTTP 200
Busca residual do package da Codingame: nenhuma referencia
git diff --check: aprovado
```

O typecheck completo do app web atingiu o heap padrao de 4 GB antes de emitir
diagnosticos; o typecheck estreito dos arquivos centrais passou. O
`dev:fast` completo chegou a compilar o backend por cinco minutos sem warning ou
erro, mas foi encerrado antes de a API abrir a porta. `dev:stop` confirmou as
portas `3000` e `8080` livres e removeu os containers temporarios.

O ultimo criterio de conclusao, integracao por PR proprio em `develop`, continua
pendente e nao deve ser marcado como concluido por esta branch.
