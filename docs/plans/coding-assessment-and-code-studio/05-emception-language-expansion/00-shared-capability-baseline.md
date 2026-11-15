# Subplano 5.0. Baseline comum de capabilities do Emception

Status: planejado; bloqueia todos os subplanos de linguagem.

Orquestrador: [`Parte 5`](../05-emception-capability-and-language-review.md).

## Objetivo

Registrar o comportamento publico atual do Emception e decidir como novas
linguagens podem ser adicionadas sem substituir presets, contracts ou defaults
existentes.

Este subplano nao adiciona uma linguagem por si mesmo. Ele fecha somente a
fronteira comum que evita cinco implementacoes incompativeis.

## Evidencia inicial

O codigo atual indica:

- `ToolchainPreset` como enum de built-ins;
- `WorkspaceBuildConfig` e `WorkspaceOptions` como unions discriminadas;
- `RunType` validado por lista fechada;
- `BUILD_PRESETS`, `PRESETS` e `TOOLCHAIN_PRESETS` como registros de built-ins;
- toolchain browser versionada por manifest e ABI;
- C/C++, CMake e Python como caminhos publicos existentes.

Esses pontos devem ser confirmados no commit-base. Eles nao sao defeitos a
corrigir automaticamente.

## Fora do escopo

- adicionar JavaScript, TypeScript, Ruby, SQL ou C#;
- introduzir tipos, DTOs ou workflow de grading;
- mudar o preset default;
- renomear ou remover enum, union, run type, export ou package existente;
- migrar consumidores atuais para uma API nova;
- alterar `@game-guild/emception-ui`.

## Decisoes obrigatorias

| Tema | Decisao esperada |
| --- | --- |
| extensao | built-in aditivo ou extension point publico, sem quebrar os built-ins |
| identidade | ID estavel de linguagem, runtime, toolchain, versao e ABI |
| workspace | arquivos, entry point, config, seed e persistencia neutros |
| execucao | compile, transpile, interpret, query ou run sem assumir C/C++ |
| lifecycle | worker, warmup, cancelamento, timeout, teardown e cache |
| artifacts | origem, integridade, manifest, CDN/self-host e reproducibilidade |
| integracao | branch, ordem de dependencia, PR para `develop` e evidencia exigida |
| erros | diagnostics, falha de runtime, cancelamento e indisponibilidade |
| teste | fixture minima comum e suite especifica por linguagem |
| paridade | como um executor servidor declara compatibilidade observavel |

Nao se cria uma abstracao apenas para tornar o diagrama simetrico. Se o owner
aprovar built-ins aditivos independentes, a baseline deve documentar essa
decisao e os limites de duplicacao.

## Etapas

### `EMC-BASE-01`. Congelar a baseline publica

Implementacao:

- registrar commit, packages e versoes;
- inventariar entrypoints e exports publicos;
- mapear presets, configs, run types, registries e runtime adapters;
- localizar consumidores dentro e fora da GameGuild;
- executar e registrar as suites existentes;
- registrar o workflow e a classificacao de source scope usados pelo CI;
- capturar fixtures minimas de C, C++, CMake e Python.

Gate:

- cada capability existente possui owner e teste de caracterizacao;
- defaults e compatibilidade atual estao documentados;
- nenhum comportamento privado e promovido a contrato por conveniencia.

### `EMC-BASE-02`. Decidir o modelo aditivo de extensao

Implementacao:

- comparar built-in aditivo, descriptor registrado e package de runtime;
- provar como a opcao preserva `ToolchainPreset` e unions existentes;
- definir API publica minima para workspace e lifecycle;
- definir como packages opcionais registram capabilities sem side effect
  oculto;
- definir deteccao de ID duplicado, versao desconhecida e runtime ausente;
- revisar a proposta com o responsavel pelo Emception.

Gate:

- consumidores atuais nao precisam migrar;
- nenhuma opcao muda o preset default;
- tree shaking e browser-only boundaries continuam verificaveis;
- a API nao cita grading, assessment, learner, score ou review.

### `EMC-BASE-03`. Fechar seguranca e supply chain

Implementacao:

- definir politica default de rede, imports e filesystem;
- definir limites e cancelamento obrigatorios por runtime;
- definir manifest, checksum, licenca e provenance dos artifacts;
- definir cache, invalidacao e self-hosting;
- definir o formato de identidade usado para paridade servidor/browser;
- definir sanitizacao de diagnostics e output.

Gate:

- runtime sem teardown ou timeout comprovado nao pode ser publicado;
- artifact remoto sem versao e integridade nao pode ser carregado;
- suporte local nao e confundido com executor oficial de grading.

### `EMC-BASE-04`. Implementar a fundacao aprovada

Executar somente se `EMC-BASE-02` concluir que uma fundacao compartilhada e
necessaria.

Implementacao:

- adicionar a API publica minima ao lado dos contratos existentes;
- manter adaptadores para todos os built-ins atuais;
- adicionar testes de registro, colisao, runtime ausente e disposal;
- documentar um exemplo neutro sem linguagem nova;
- cumprir o requisito de changeset existente quando aplicavel, sem alterar o
  pipeline de versionamento e publicacao.

Gate:

- diff nao remove nem renomeia API existente;
- fixtures atuais produzem o mesmo comportamento observavel;
- packages atuais continuam compilando sem alteracao de consumidor;
- owner aprovou a superficie publica final.

### `EMC-BASE-05`. Registrar a decisao e liberar os subplanos

Implementacao:

- registrar a arquitetura escolhida e alternativas rejeitadas;
- registrar comandos, resultados e riscos residuais;
- atualizar o estado `EMC-BASELINE` no orquestrador;
- fornecer template de evidencia para cada linguagem;
- incluir no template o PR para `develop`, changeset quando aplicavel e rollback
  operacional;
- listar blockers que cada subplano deve resolver sozinho.

Gate:

- cada plano de linguagem sabe onde registrar runtime e preset;
- nenhuma linguagem ficou implicitamente aprovada;
- baseline possui rollback operacional e owner.

### `EMC-BASE-06`. Integrar a fundacao compartilhada, quando houver

Executar somente se o subplano produzir codigo compartilhado necessario para
mais de uma linguagem.

Implementacao:

- reconciliar a branch da baseline com a `develop` atual;
- executar novamente todas as suites comuns;
- abrir um PR preparatorio e exclusivo da fundacao para `develop`;
- integrar esse PR antes de criar branches que dependam da nova API.

Gate:

- o PR nao inclui implementacao de linguagem;
- a fundacao permanece aditiva, neutra e aprovada pelo owner;
- cada branch de linguagem parte da `develop` que ja contem a fundacao.

## Validacao

```bash
pnpm build:emception
pnpm test:emception
pnpm test:emception:types
pnpm test:emception:e2e
pnpm --filter @game-guild/emception-ui test
```

Se a etapa 04 nao alterar producao, os comandos ainda devem ser registrados
como baseline para comparacao futura.

## Criterios de conclusao

- baseline publica e consumidores atuais estao inventariados;
- modelo de extensao foi aprovado pelo owner;
- C/C++, CMake e Python mantem comportamento e defaults;
- runtime e supply chain possuem requisitos comuns;
- nao existe dependencia de grading dentro do Emception;
- eventual fundacao compartilhada esta integrada em `develop` antes das
  linguagens consumidoras;
- os cinco subplanos de linguagem podem prosseguir sem compartilhar commits.
