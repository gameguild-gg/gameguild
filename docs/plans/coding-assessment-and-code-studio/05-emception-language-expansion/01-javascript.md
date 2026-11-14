# Subplano 5.1. Suporte a JavaScript no Emception

Status: planejado.

Orquestrador: [`Parte 5`](../05-emception-capability-and-language-review.md).

Pre-requisito: [`5.0`](./00-shared-capability-baseline.md) concluido.

## Objetivo

Adicionar um workspace JavaScript executavel e isolado ao Emception por API
publica, sem reutilizar o contexto global da aplicacao e sem alterar os
toolchains existentes.

## Hipotese inicial

O browser ja executa JavaScript para hospedar o Emception, mas isso nao
significa que codigo do usuario possua runtime isolado. O spike deve decidir
entre Worker dedicado, engine WASM ou outra alternativa aprovada. `eval` no
main thread e acesso implicito a `window` nao sao solucoes aceitaveis.

## Primeiro corte

- programa terminal com entry point explicito;
- ECMAScript e formato de modulo definidos e versionados;
- stdout, stderr, exit status e diagnostics normalizados;
- stdin somente se o runtime escolhido o suportar com lifecycle seguro;
- imports locais pelo VFS;
- rede e imports remotos desabilitados por default;
- cancelamento, timeout e teardown obrigatorios.

DOM, canvas, npm install e compatibilidade Node completa ficam fora ate haver
caso de uso e modelo de seguranca aprovados.

## Etapas

### `EMC-JS-01`. Fechar semantica e runtime

Implementacao:

- definir versao ECMAScript e formato ESM/CommonJS suportado;
- definir diferenca entre APIs browser, Node e runtime neutro;
- avaliar alternativas de isolamento e custo de startup;
- definir entry point, argumentos, env e imports locais;
- definir ID, versao e ABI da capability;
- revisar licenca, provenance e distribuicao do runtime escolhido.

Gate:

- semantica nao depende do bundler de `apps/web`;
- codigo do usuario nao compartilha globals com a aplicacao;
- alternativa possui cancelamento e teardown comprovaveis;
- owner aprovou runtime e primeiro corte.

### `EMC-JS-02`. Executar spike descartavel

Implementacao:

- executar hello world e multiplos arquivos;
- testar ESM, erro de sintaxe e exception assincrona;
- testar loop infinito, output excessivo e consumo de memoria;
- testar import local permitido e import remoto negado;
- testar duas sessoes simultaneas sem estado compartilhado;
- medir download, warmup, execucao e teardown.

Gate:

- spike roda fora dos packages de producao;
- nenhuma falha derruba ou bloqueia o main thread;
- limites cabem no budget aprovado;
- risco residual e resultado negativo permanecem registrados.

### `EMC-JS-03`. Integrar como capability aditiva

Implementacao:

- adicionar package/runtime, descriptor ou built-in conforme o subplano 5.0;
- adicionar preset JavaScript sem alterar o preset default;
- expor workspace, run e diagnostics por entrypoints publicos;
- manter runtime browser em boundary browser-only;
- documentar self-hosting e cache dos artifacts;
- adicionar fixture e demo especificas de JavaScript.

Gate:

- C/C++, CMake e Python permanecem inalterados;
- nenhum consumidor atual precisa migrar;
- import do package core continua DOM-free;
- API publica nao contem vocabulario de grading.

### `EMC-JS-04`. Endurecer lifecycle e seguranca

Implementacao:

- testar timeout, abort, terminate e liberacao de recursos;
- limitar output, memoria, arquivos e quantidade de modulos;
- bloquear rede, host imports e APIs nao autorizadas por default;
- sanitizar stack traces sem perder paths do workspace;
- testar refresh, reset, persistencia e isolamento entre workspaces;
- registrar threat model e operacao de update.

Gate:

- nenhum teste depende de sleep arbitrario para provar teardown;
- sessao encerrada nao continua executando;
- workspace de um usuario nao aparece em outro;
- suites de nao regressao do orquestrador passam.

### `EMC-JS-05`. Classificar integracao GameGuild

Implementacao:

- provar consumo somente por API publica em `coding-surface`;
- criar fixtures compartilhadas para browser e executor servidor candidato;
- fixar identidade de runtime e semantica observavel;
- testar erro, timeout, stdout e exit status em ambos os lados;
- registrar separadamente os estados de preview e grading oficial.

Gate:

- preview local nao produz nota oficial;
- `gameguild-preview-ready` pode ser aprovado sem executor servidor;
- `official-grading-ready` exige paridade e executor autoritativo testado;
- nenhuma alteracao de grading entra no Emception.

### `EMC-JS-06`. Integrar JavaScript em `develop`

Implementacao:

- reconciliar a branch com a `develop` que contem a baseline aprovada;
- executar novamente a validacao comum e a especifica de JavaScript;
- abrir um PR exclusivo de JavaScript para `develop`, com evidencias, estados
  de readiness, limitacoes e rollback;
- cumprir o `EMC-INTEGRATION-GATE` do orquestrador.

Gate:

- o diff nao inclui TypeScript, Ruby, SQL ou C#;
- CI e testes locais passam sobre a branch reconciliada;
- o owner aprovou a capability e o PR foi integrado em `develop`;
- TypeScript somente parte da `develop` que ja contem este PR.

## Validacao especifica

- script simples e multiplo arquivo;
- ESM/import local;
- syntax error e runtime error;
- loop infinito e abort;
- output excessivo;
- rede/import remoto negados;
- duas sessoes isoladas;
- teardown sem worker residual.

Executar tambem toda a validacao comum do orquestrador.

## Criterios de conclusao

- JavaScript possui runtime, preset e API publica versionados;
- isolamento e lifecycle passam nos testes negativos;
- toolchains atuais nao sofreram regressao;
- estado `emception-supported` esta fundamentado;
- readiness de preview e grading oficial esta registrada sem inferencia;
- o suporte foi integrado por PR proprio em `develop`.
