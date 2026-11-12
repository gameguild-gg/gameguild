# Subplano 5.2. Suporte a TypeScript no Emception

Status: planejado; bloqueado por JavaScript.

Orquestrador: [`Parte 5`](../05-emception-capability-and-language-review.md).

Pre-requisitos:

- [`5.0`](./00-shared-capability-baseline.md) concluido;
- [`5.1`](./01-javascript.md) em `emception-supported`.

## Objetivo

Adicionar TypeScript como toolchain versionada sobre o runtime JavaScript
aprovado, com diagnostics e source maps reproduziveis, sem acoplar o Emception
a versao de TypeScript usada para construir a aplicacao.

## Hipotese inicial

TypeScript pode usar transpile, typecheck ou ambos. O plano nao assume que o
package `typescript` de desenvolvimento seja o artifact correto para o browser.
A versao do compiler, lib files, config permitida e estrategia de carga devem
ser decididas e fixadas como parte da capability.

## Primeiro corte

- `.ts` e, somente se aprovado, `.tsx` sem dependencia de DOM;
- `tsconfig` controlado ou subconjunto validado;
- diagnostics sintaticos e semanticos;
- transpile para o formato suportado pelo runtime JavaScript;
- source maps que apontem para os arquivos do workspace;
- sem package install ou resolucao remota por default.

## Etapas

### `EMC-TS-01`. Fechar o modo de compilacao

Implementacao:

- comparar transpile-only, incremental typecheck e compilacao completa;
- escolher versao fixada do compiler e das standard libraries;
- definir target ECMAScript e module resolution;
- definir opcoes aceitas, bloqueadas e defaults;
- definir import local e tratamento de declarations;
- definir ID de toolchain, versao e relacao com JavaScript.

Gate:

- comportamento nao depende do TypeScript transitivo do monorepo;
- config arbitraria nao consegue escapar do workspace;
- output e diagnostics sao deterministas;
- owner aprovou escopo e artifact.

### `EMC-TS-02`. Executar spike descartavel

Implementacao:

- transpilar e executar programa simples e multiplo arquivo;
- testar erro sintatico, erro de tipo e runtime error;
- testar import local, declaration e source map;
- medir download, inicializacao, recompilacao e memoria;
- testar cancelamento durante typecheck pesado;
- comparar diagnostics em browser e executor servidor candidato.

Gate:

- spike nao altera os packages de producao;
- compiler pode ser interrompido e descartado;
- source map nao revela paths do host;
- custo cabe no budget aprovado.

### `EMC-TS-03`. Integrar a toolchain aditivamente

Implementacao:

- registrar TypeScript pelo mecanismo aprovado no subplano 5.0;
- reutilizar o runtime JavaScript sem copiar seu lifecycle;
- adicionar preset e workspace sample especificos;
- expor compile/typecheck/run por APIs publicas;
- publicar artifacts com versao, integridade e licenca;
- manter JavaScript utilizavel sem carregar TypeScript.

Gate:

- dependencias de TypeScript sao lazy e opcionais;
- preset JavaScript continua com bundle e comportamento caracterizados;
- C/C++, CMake e Python nao mudam;
- nenhum consumidor atual precisa migrar.

### `EMC-TS-04`. Endurecer diagnostics e lifecycle

Implementacao:

- normalizar severity, arquivo, linha, coluna e codigo;
- mapear stack trace do JavaScript gerado para TypeScript;
- testar abort, timeout, memoria e output excessivo;
- testar cache incremental sem vazamento entre workspaces;
- limitar quantidade/tamanho de arquivos e libs;
- testar reset completo do compiler service.

Gate:

- diagnostics continuam uteis sem expor internals;
- cache de uma sessao nao contamina outra;
- worker e memoria sao liberados no teardown;
- suites comuns e de JavaScript permanecem verdes.

### `EMC-TS-05`. Classificar integracao GameGuild

Implementacao:

- consumir preset e diagnostics publicos em `coding-surface`;
- criar fixtures de compile, typecheck e run;
- fixar compiler version, target e config no executor servidor;
- comparar diagnostics, output e exit status;
- registrar readiness de preview e grading oficial separadamente.

Gate:

- definicao academica referencia identidade de toolchain, nao package privado;
- preview nao decide nota;
- grading oficial exige compiler/config equivalentes no servidor;
- detalhes de grading permanecem fora do Emception.

### `EMC-TS-06`. Integrar TypeScript em `develop`

Implementacao:

- partir da `develop` que ja contem o suporte JavaScript aprovado;
- reconciliar a branch e executar novamente as suites comuns, JavaScript e
  TypeScript;
- abrir um PR exclusivo de TypeScript para `develop`, com evidencias, estados
  de readiness, limitacoes e rollback;
- cumprir o `EMC-INTEGRATION-GATE` do orquestrador.

Gate:

- o diff nao reimplementa nem altera ocultamente o lifecycle JavaScript;
- CI e testes locais passam sobre a branch reconciliada;
- o owner aprovou a capability e o PR foi integrado em `develop`;
- nenhuma outra linguagem e incluida por conveniencia.

## Validacao especifica

- transpile e execucao simples;
- multiplo arquivo e import local;
- syntax e type errors;
- source maps;
- cancelamento de typecheck;
- cache isolado por workspace;
- JavaScript sem custo de TypeScript quando nao selecionado.

Executar tambem as suites comuns e o subplano JavaScript.

## Criterios de conclusao

- TypeScript possui compiler e preset publicos versionados;
- diagnostics e source maps sao deterministas;
- runtime JavaScript continua independente;
- estado `emception-supported` esta fundamentado;
- preview e grading oficial possuem decisoes e evidencias separadas;
- o suporte foi integrado por PR proprio em `develop`.
