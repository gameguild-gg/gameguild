# Subplano 5.5. Suporte a C# no Emception

Status: planejado.

Orquestrador: [`Parte 5`](../05-emception-capability-and-language-review.md).

Pre-requisito: [`5.0`](./00-shared-capability-baseline.md) concluido.

## Objetivo

Adicionar uma toolchain C# isolada e versionada ao Emception para programas de
console, preservando os runtimes atuais e sem prometer toda a plataforma .NET
no primeiro corte.

## Hipotese inicial

Runtime .NET/Mono para WebAssembly e compilador compativel sao candidatos, nao
decisoes. O spike deve identificar uma distribuicao mantida, sua licenca,
modelo de worker, tamanho, startup, compilacao, assemblies, BCL, cancelamento e
paridade com o executor servidor.

## Primeiro corte

- programa console com entry point definido;
- projeto simples ou conjunto pequeno de arquivos `.cs`;
- versao de linguagem, compiler, runtime e BCL fixadas;
- stdout, stderr, exit status e diagnostics normalizados;
- references restritas a assemblies aprovados;
- sem restore remoto de NuGet por default;
- ASP.NET, desktop UI e workloads adicionais fora do escopo.

## Etapas

### `EMC-CS-01`. Selecionar compiler e runtime

Implementacao:

- comparar alternativas oficiais ou mantidas para browser/WASM;
- decidir compile no browser, artifacts precompilados ou modelo hibrido;
- fixar versao C#, compiler, runtime, BCL e ABI;
- definir projeto, entry point, references e output;
- avaliar licenca, provenance, tamanho e self-hosting;
- definir ID e versao da capability.

Gate:

- alternativa possui lifecycle e suporte verificaveis;
- runtime nao depende de API privada da aplicacao;
- restore remoto nao faz parte implicita da execucao;
- owner aprovou runtime e primeiro corte.

### `EMC-CS-02`. Executar spike descartavel

Implementacao:

- compilar e executar hello world e multiplos arquivos;
- testar diagnostics de compilacao e runtime exception;
- testar tipos basicos, collections e BCL aprovada;
- testar loop infinito, output excessivo e memoria;
- testar reference permitida e assembly nao autorizado;
- medir download, startup, compile, warm run e teardown.

Gate:

- spike permanece fora dos packages de producao;
- compilacao e execucao podem ser interrompidas;
- runtime cabe no budget aprovado ou recebe estado `deferred`;
- limitations ficam registradas sem fallback oculto.

### `EMC-CS-03`. Integrar como capability aditiva

Implementacao:

- registrar C# pelo modelo aprovado no subplano 5.0;
- adicionar preset, sample e project config minimos;
- expor compile, run e diagnostics por entrypoints publicos;
- executar compiler/runtime em worker ou boundary equivalente aprovada;
- publicar artifacts com manifest, integridade, licenca e ABI;
- carregar C# somente quando selecionado.

Gate:

- C/C++, CMake e Python permanecem inalterados;
- core continua runtime-agnostic e DOM-free;
- bundle de outros presets nao incorpora .NET;
- API publica nao contem conceito de grading.

### `EMC-CS-04`. Endurecer seguranca e lifecycle

Implementacao:

- limitar assemblies, arquivos, output, memoria e tempo;
- bloquear rede, host filesystem, interop e reflection perigosa quando
  aplicavel;
- testar abort durante compile e run;
- testar unload/terminate e memoria residual;
- sanitizar diagnostics e stack traces;
- testar sessoes simultaneas e resets sucessivos.

Gate:

- execucao encerrada nao continua ativa;
- uma sessao nao acessa artifacts de outra;
- APIs fora do primeiro corte falham fechado;
- suites de nao regressao do Emception passam.

### `EMC-CS-05`. Classificar integracao GameGuild

Implementacao:

- consumir preset e diagnostics publicos em `coding-surface`;
- criar fixtures browser/servidor para compile e run;
- fixar compiler, language version, runtime e references no servidor;
- comparar diagnostics, output, exceptions e exit status;
- registrar diferencas inevitaveis de runtime;
- registrar readiness de preview e grading oficial.

Gate:

- `gameguild-preview-ready` pode ser aprovado sem grading oficial;
- `official-grading-ready` exige toolchain servidor autoritativa e compativel;
- incompatibilidade relevante impede grading automatico;
- detalhes academicos permanecem no adapter.

### `EMC-CS-06`. Integrar C# em `develop`

Implementacao:

- reconciliar a branch com a `develop` que contem a baseline aprovada;
- executar novamente a validacao comum e a especifica de C#;
- abrir um PR exclusivo de C# para `develop`, com evidencias, estados de
  readiness, limitacoes e rollback;
- cumprir o `EMC-INTEGRATION-GATE` do orquestrador.

Gate:

- a branch nao depende de commits nao integrados de outra linguagem;
- CI e testes locais passam sobre a branch reconciliada;
- o owner aprovou a capability e o PR foi integrado em `develop`;
- o diff nao inclui outra linguagem por conveniencia.

## Validacao especifica

- compile/run simples e multiplo arquivo;
- diagnostics de compilacao;
- exception e stack trace;
- BCL e references permitidas;
- assembly, rede e paths proibidos;
- timeout durante compile e run;
- sessoes isoladas e teardown;
- artifact lazy fora do preset C#.

Executar tambem toda a validacao comum do orquestrador.

## Criterios de conclusao

- compiler, runtime, BCL e ABI possuem versoes fixadas;
- preset e diagnostics sao publicos e testados;
- runtime e artifacts ficam fora dos demais bundles;
- estado `emception-supported` esta fundamentado;
- preview e grading oficial possuem decisoes independentes;
- o suporte foi integrado por PR proprio em `develop`.
