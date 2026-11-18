# Subplano 5.3. Suporte a Ruby no Emception

Status: planejado.

Orquestrador: [`Parte 5`](../05-emception-capability-and-language-review.md).

Pre-requisito: [`5.0`](./00-shared-capability-baseline.md) concluido.

## Objetivo

Adicionar um runtime Ruby isolado e versionado ao Emception, preservando os
toolchains atuais e sem assumir que uma implementacao browser esteja pronta
para grading servidor.

## Hipotese inicial

`ruby.wasm` e um candidato relevante, mas nao e uma decisao. O spike deve
comparar distribuicao oficial, suporte WASI/browser, tamanho, startup,
filesystem, stdin, gems, licenca e estrategia de update. Outra alternativa so
pode ser escolhida com evidencia equivalente.

## Primeiro corte

- programa terminal com entry point `.rb`;
- multiplos arquivos locais por `require_relative` ou mecanismo aprovado;
- stdout, stderr, exit status e exceptions normalizados;
- standard library explicitamente inventariada;
- rede desabilitada por default;
- gems e native extensions fora do primeiro corte, salvo decisao especifica.

## Etapas

### `EMC-RB-01`. Selecionar runtime e contrato

Implementacao:

- comparar runtime oficial e alternativas mantidas;
- fixar versao Ruby, ABI, build e standard library;
- definir VFS, cwd, argv, env, stdin e load path;
- definir politica de gems e extensoes nativas;
- revisar licenca, provenance, artifact e self-hosting;
- definir ID e versao da capability.

Gate:

- runtime possui origem e build reproduzivel;
- nao ha download dinamico nao versionado;
- primeiro corte nao promete compatibilidade MRI completa sem prova;
- owner aprovou runtime e limites.

### `EMC-RB-02`. Executar spike descartavel

Implementacao:

- executar hello world e multiplos arquivos;
- testar standard library minima e `require_relative`;
- testar syntax error, exception e exit;
- testar loop infinito, output excessivo e memoria;
- testar arquivos permitidos e paths proibidos;
- medir download, startup, warm run e teardown.

Gate:

- spike permanece fora dos packages de producao;
- timeout encerra efetivamente a execucao;
- VFS nao expoe arquivos de outra sessao;
- tamanho e latencia cabem no budget aprovado.

### `EMC-RB-03`. Integrar como capability aditiva

Implementacao:

- registrar Ruby pelo modelo aprovado no subplano 5.0;
- adicionar preset, sample e diagnostics publicos;
- integrar runtime em worker e boundary browser-only;
- publicar artifacts com manifest, integridade e licenca;
- documentar standard library e features nao suportadas;
- manter carga lazy quando Ruby nao estiver selecionado.

Gate:

- nenhum preset existente muda;
- core continua sem dependencia browser-only;
- runtime Ruby nao entra no bundle de outros presets;
- API publica nao contem conceito academico.

### `EMC-RB-04`. Endurecer isolamento e compatibilidade

Implementacao:

- testar abort, timeout, teardown e memoria residual;
- limitar output, arquivos, load path e quantidade de requires;
- bloquear rede e host filesystem por default;
- sanitizar backtrace mantendo paths do workspace;
- testar duas sessoes e resets sucessivos;
- documentar divergencias conhecidas em relacao ao Ruby servidor.

Gate:

- runtime encerrado nao mantem tarefa ativa;
- nenhuma sessao compartilha globals ou arquivos;
- divergencias de semantica possuem fixture e classificacao;
- suites de nao regressao do Emception passam.

### `EMC-RB-05`. Classificar integracao GameGuild

Implementacao:

- consumir somente o preset publico em `coding-surface`;
- criar fixtures browser/servidor para sintaxe, output e exceptions;
- decidir se o executor oficial usa o mesmo runtime ou Ruby compativel;
- fixar versao e standard library nos dois ambientes;
- registrar readiness de preview e grading oficial.

Gate:

- `gameguild-preview-ready` nao exige nota automatica;
- `official-grading-ready` exige paridade observavel comprovada;
- diferenca de engine nao e escondida;
- grading continua fora do Emception.

### `EMC-RB-06`. Integrar Ruby em `develop`

Implementacao:

- reconciliar a branch com a `develop` que contem a baseline aprovada;
- executar novamente a validacao comum e a especifica de Ruby;
- abrir um PR exclusivo de Ruby para `develop`, com evidencias, estados de
  readiness, limitacoes e rollback;
- cumprir o `EMC-INTEGRATION-GATE` do orquestrador.

Gate:

- a branch nao depende de commits nao integrados de outra linguagem;
- CI e testes locais passam sobre a branch reconciliada;
- o owner aprovou a capability e o PR foi integrado em `develop`;
- o diff nao inclui outra linguagem por conveniencia.

## Validacao especifica

- script simples e multiplos arquivos;
- standard library aprovada;
- syntax error e exception;
- loop infinito, timeout e abort;
- filesystem e rede negados;
- sessoes isoladas;
- artifact lazy e teardown completo.

Executar tambem toda a validacao comum do orquestrador.

## Criterios de conclusao

- runtime Ruby possui versao, provenance e API publica;
- escopo de compatibilidade esta documentado;
- isolamento e lifecycle passam nos negativos;
- estado `emception-supported` esta fundamentado;
- preview e grading oficial possuem decisoes independentes;
- o suporte foi integrado por PR proprio em `develop`.
