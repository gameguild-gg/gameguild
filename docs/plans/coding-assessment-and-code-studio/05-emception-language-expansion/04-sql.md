# Subplano 5.4. Suporte a SQL no Emception

Status: planejado.

Orquestrador: [`Parte 5`](../05-emception-capability-and-language-review.md).

Pre-requisito: [`5.0`](./00-shared-capability-baseline.md) concluido.

## Objetivo

Adicionar um workspace SQL deterministico ao Emception, com engine isolada,
fixtures versionadas e resultados normalizados, sem transformar o core em um
sistema de grading ou assumir que todo banco segue a semantica do SQLite.

## Hipotese inicial

SQLite WASM e um candidato para o primeiro runtime browser por possuir
distribuicao e modelo embarcado conhecidos. A escolha exige validar origem,
build, licenca, extensoes compiladas, worker, persistencia e paridade com o
executor servidor. O nome publico da capability deve declarar o dialect real.

## Primeiro corte

- dialect SQLite explicitamente versionado;
- arquivos de schema, seed e queries;
- execucao de script ou statement selecionado;
- grid de resultados e diagnostics normalizados;
- banco efemero/resetavel para execucao deterministica;
- persistencia interativa somente se isolada do modo resetavel;
- rede, `load_extension` e acesso ao host desabilitados.

## Etapas

### `EMC-SQL-01`. Fechar dialect e modelo de workspace

Implementacao:

- confirmar engine, versao, compile options e artifact;
- definir schema, seed, query entry point e ordem de aplicacao;
- separar sessao interativa de execucao resetavel;
- definir tipos, `NULL`, blobs, errors e serializacao de rows;
- definir regra para ordem de resultados sem `ORDER BY`;
- definir ID, versao, dialect e ABI da capability.

Gate:

- capability nao se apresenta como SQL generico;
- resultado sem ordenacao nao recebe oracle implicito;
- reset reproduz a mesma base inicial;
- owner aprovou engine e modelo de workspace.

### `EMC-SQL-02`. Executar spike descartavel

Implementacao:

- criar schema, seed e consultar multiplas tabelas;
- testar DDL, DML, transaction, rollback e constraints;
- testar syntax error, constraint error e query longa;
- testar output excessivo e dataset acima do limite;
- testar reset entre execucoes e duas sessoes simultaneas;
- medir artifact, startup, seed, query e teardown.

Gate:

- spike fica fora dos packages de producao;
- reset nao reutiliza estado residual;
- query interrompida nao continua consumindo recursos;
- custo cabe no budget aprovado.

### `EMC-SQL-03`. Integrar como capability aditiva

Implementacao:

- registrar SQLite pelo modelo aprovado no subplano 5.0;
- adicionar preset, sample e arquivos iniciais;
- expor run, reset, diagnostics e result sets por API publica;
- executar engine em worker e boundary browser-only;
- publicar artifact com manifest, integridade e compile options;
- manter carga lazy para workspaces nao SQL.

Gate:

- presets atuais nao mudam;
- core nao importa engine WASM diretamente;
- result set possui contrato neutro e versionado;
- API nao contem rubric, answer key ou score.

### `EMC-SQL-04`. Endurecer isolamento e determinismo

Implementacao:

- desabilitar extensoes, rede e filesystem nao autorizado;
- revisar `ATTACH`, pragmas e funcoes que ampliem acesso;
- limitar statements, rows, columns, bytes, memoria e tempo;
- normalizar errors sem expor internals;
- testar transaction aberta, cancelamento e teardown;
- provar isolamento entre banco interativo e execucao resetavel.

Gate:

- uma execucao nao altera a fixture inicial da seguinte;
- database de uma sessao nao aparece em outra;
- limites falham fechado e com diagnostics uteis;
- suites de nao regressao do Emception passam.

### `EMC-SQL-05`. Classificar integracao GameGuild

Implementacao:

- consumir preset e result sets publicos em `coding-surface`;
- criar fixtures browser/servidor com schema e seed identicos;
- fixar versao, compile options e dialect no executor servidor;
- comparar errors, types, rows e efeitos transacionais;
- manter fixtures privadas somente no executor oficial;
- registrar readiness de preview e grading oficial.

Gate:

- browser nao recebe fixture privada;
- preview nao produz resultado academico;
- `official-grading-ready` exige engine/config compativeis;
- avaliacao de resposta SQL pertence ao adapter, nao ao Emception.

### `EMC-SQL-06`. Integrar SQL em `develop`

Implementacao:

- reconciliar a branch com a `develop` que contem a baseline aprovada;
- executar novamente a validacao comum e a especifica de SQL;
- abrir um PR exclusivo de SQL para `develop`, com evidencias, estados de
  readiness, limitacoes e rollback;
- cumprir o `EMC-INTEGRATION-GATE` do orquestrador.

Gate:

- a branch nao depende de commits nao integrados de outra linguagem;
- CI e testes locais passam sobre a branch reconciliada;
- o owner aprovou a capability e o PR foi integrado em `develop`;
- o diff nao inclui outra linguagem por conveniencia.

## Validacao especifica

- schema, seed e query simples;
- joins, constraints e transaction;
- syntax e constraint errors;
- reset deterministico;
- timeout e output excessivo;
- `load_extension`, rede e paths proibidos;
- duas sessoes isoladas;
- artifact lazy e teardown completo.

Executar tambem toda a validacao comum do orquestrador.

## Criterios de conclusao

- capability identifica claramente SQLite e sua versao;
- workspace oferece execucao resetavel e isolada;
- result sets e diagnostics possuem contrato publico;
- estado `emception-supported` esta fundamentado;
- preview e grading oficial possuem decisoes independentes;
- o suporte foi integrado por PR proprio em `develop`.
